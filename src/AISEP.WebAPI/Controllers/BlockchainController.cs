using AISEP.Application.DTOs.Blockchain;
using AISEP.Application.DTOs.Common;
using AISEP.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<IActionResult> ComputeHash(int documentId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _proofService.ComputeHashAsync(documentId, userId, ct);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
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
    public async Task<IActionResult> SubmitToChain(int documentId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _proofService.SubmitToChainAsync(documentId, userId, ct);

        if (!result.Success && result.Error?.Code == "DOCUMENT_NOT_FOUND")
            return NotFound(result);

        if (!result.Success && result.Error?.Code == "BLOCKCHAIN_ERROR")
            return StatusCode(StatusCodes.Status500InternalServerError, result);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
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
    public async Task<IActionResult> VerifyOnChain(int documentId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _proofService.VerifyOnChainAsync(documentId, userId, ct);

        if (!result.Success && result.Error?.Code == "DOCUMENT_NOT_FOUND")
            return NotFound(result);

        if (!result.Success)
            return StatusCode(StatusCodes.Status500InternalServerError, result);

        return Ok(result);
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
    public async Task<IActionResult> GetTxStatus(int documentId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _proofService.GetTxStatusAsync(documentId, userId, ct);

        if (!result.Success && result.Error?.Code == "DOCUMENT_NOT_FOUND")
            return NotFound(result);

        if (!result.Success && result.Error?.Code == "PROOF_NOT_SUBMITTED")
            return UnprocessableEntity(result);

        if (!result.Success)
            return StatusCode(StatusCodes.Status500InternalServerError, result);

        return Ok(result);
    }

    // ================================================================
    // 5) POST /api/staff/documents/{documentId}/verify-hash — Staff verify
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

        if (!result.Success && result.Error?.Code == "DOCUMENT_NOT_FOUND")
            return NotFound(result);

        if (!result.Success)
            return StatusCode(StatusCodes.Status500InternalServerError, result);

        return Ok(result);
    }
}
