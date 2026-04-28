using AISEP.Application.DTOs.Blockchain;
using AISEP.Application.DTOs.Common;
using AISEP.Application.Interfaces;
using AISEP.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AISEP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using AISEP.Domain.Enums;

namespace AISEP.WebAPI.Controllers;

/// <summary>
/// Blockchain proof &amp; IP protection for documents.
/// Compute hash, submit to blockchain, verify integrity, check tx status.
/// </summary>
[ApiController]
[Tags("Blockchain Proof")]
public class BlockchainController : ControllerBase
{
    private readonly IBlockchainProofService _proofService;

    public BlockchainController(IBlockchainProofService proofService)
    {
        _proofService = proofService;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    private async Task<bool> IsBlockchainFeatureBlockedAsync(ApplicationDbContext db, int userId, CancellationToken ct)
    {
        var startup = await db.Startups.AsNoTracking().FirstOrDefaultAsync(s => s.UserID == userId, ct);
        return startup != null && startup.SubscriptionPlan == StartupSubscriptionPlan.Free;
    }

    // ================================================================
    // 1) POST /api/documents/{documentId}/hash — Compute file hash
    // ================================================================

    /// <summary>
    /// Compute SHA-256 hash from the stored file and persist to DocumentBlockchainProofs.
    /// </summary>
    [HttpPost("api/documents/{documentId:int}/hash")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<HashResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<HashResponseDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ComputeHash(int documentId, [FromServices] ApplicationDbContext db, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (await IsBlockchainFeatureBlockedAsync(db, userId, ct))
            return ApiResponse<HashResponseDto>.ErrorResponse("FEATURE_REQUIRES_UPGRADE", "Blockchain verification requires a Pro or Fundraising plan.").ToActionResult();

        var result = await _proofService.ComputeHashAsync(documentId, userId, ct);
        return result.ToActionResult();
    }

    // ================================================================
    // 2) POST /api/documents/{documentId}/submit-chain — Submit to blockchain
    // ================================================================

    /// <summary>
    /// Submit the document's file hash to the blockchain.
    /// Automatically computes hash if not already done.
    /// Returns transaction hash and "Pending" status.
    /// </summary>
    [HttpPost("api/documents/{documentId:int}/submit-chain")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<SubmitChainResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SubmitChainResponseDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SubmitChainResponseDto>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SubmitToChain(int documentId, [FromServices] ApplicationDbContext db, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (await IsBlockchainFeatureBlockedAsync(db, userId, ct))
            return ApiResponse<SubmitChainResponseDto>.ErrorResponse("FEATURE_REQUIRES_UPGRADE", "Blockchain verification requires a Pro or Fundraising plan.").ToActionResult();

        var result = await _proofService.SubmitToChainAsync(documentId, userId, ct);
        return result.ToActionResult();
    }

    // ================================================================
    // 3) GET /api/documents/{documentId}/verify-chain — Verify on-chain
    // ================================================================

    /// <summary>
    /// Recompute hash from stored file and verify against the blockchain record.
    /// Returns whether the document is verified, mismatched, or not found on-chain.
    /// </summary>
    [HttpGet("api/documents/{documentId:int}/verify-chain")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<VerifyChainResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<VerifyChainResponseDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerifyOnChain(int documentId, [FromServices] ApplicationDbContext db, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (await IsBlockchainFeatureBlockedAsync(db, userId, ct))
            return ApiResponse<VerifyChainResponseDto>.ErrorResponse("FEATURE_REQUIRES_UPGRADE", "Blockchain verification requires a Pro or Fundraising plan.").ToActionResult();

        var result = await _proofService.VerifyOnChainAsync(documentId, userId, ct);
        return result.ToActionResult();
    }

    // ================================================================
    // 4) GET /api/documents/{documentId}/chain/tx-status — Transaction status
    // ================================================================

    /// <summary>
    /// Check the blockchain transaction status for this document's proof.
    /// Updates the proof record in DB with latest status and block number.
    /// </summary>
    [HttpGet("api/documents/{documentId:int}/chain/tx-status")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<TxStatusResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TxStatusResponseDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<TxStatusResponseDto>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetTxStatus(int documentId, [FromServices] ApplicationDbContext db, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (await IsBlockchainFeatureBlockedAsync(db, userId, ct))
            return ApiResponse<TxStatusResponseDto>.ErrorResponse("FEATURE_REQUIRES_UPGRADE", "Blockchain verification requires a Pro or Fundraising plan.").ToActionResult();

        var result = await _proofService.GetTxStatusAsync(documentId, userId, ct);
        return result.ToActionResult();
    }

    // ================================================================
    // 5) POST /api/blockchain/verify-hash — Self-service hash lookup
    // ================================================================

    /// <summary>
    /// Verify a SHA-256 hash that the caller computed locally from a downloaded file.
    /// Returns whether the hash is anchored on-chain and/or recorded in AISEP.
    /// Lets startups and investors confirm a downloaded document hasn't been tampered with.
    /// </summary>
    [HttpPost("api/blockchain/verify-hash")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<HashLookupResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<HashLookupResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<HashLookupResponseDto>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> VerifyHashLookup([FromBody] HashLookupRequestDto request, CancellationToken ct)
    {
        var result = await _proofService.VerifyHashLookupAsync(request?.Hash ?? string.Empty, ct);
        return result.ToActionResult();
    }

    // ================================================================
    // 6) POST /api/staff/documents/{documentId}/verify-hash — Staff verify
    // ================================================================

    /// <summary>
    /// Staff/Admin: cross-check file hash with blockchain record for any document.
    /// No ownership restriction.
    /// </summary>
    [HttpPost("api/staff/documents/{documentId:int}/verify-hash")]
    [Authorize(Policy = "StaffOrAdmin")]
    [ProducesResponseType(typeof(ApiResponse<VerifyChainResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<VerifyChainResponseDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StaffVerifyHash(int documentId, CancellationToken ct)
    {
        var staffUserId = GetCurrentUserId();
        var result = await _proofService.StaffVerifyHashAsync(documentId, staffUserId, ct);
        return result.ToActionResult();
    }
}
