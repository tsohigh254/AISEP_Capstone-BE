using System.Security.Cryptography;
using AISEP.Application.DTOs.Blockchain;
using AISEP.Application.DTOs.Common;
using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Domain.Enums;
using AISEP.Domain.Interfaces;
using AISEP.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AISEP.Infrastructure.Services;

public class BlockchainProofService : IBlockchainProofService
{
    private readonly ApplicationDbContext _context;
    private readonly IStorageService _storage;
    private readonly IBlockchainService _blockchain;
    private readonly IAuditService _audit;
    private readonly ILogger<BlockchainProofService> _logger;

    public BlockchainProofService(
        ApplicationDbContext context,
        IStorageService storage,
        IBlockchainService blockchain,
        IAuditService audit,
        ILogger<BlockchainProofService> logger)
    {
        _context = context;
        _storage = storage;
        _blockchain = blockchain;
        _audit = audit;
        _logger = logger;
    }

    // ================================================================
    // 1) Compute SHA-256 hash
    // ================================================================
    public async Task<ApiResponse<HashResponseDto>> ComputeHashAsync(
        int documentId, int userId, CancellationToken ct = default)
    {
        var doc = await GetOwnedDocumentAsync(documentId, userId, ct);
        if (doc == null)
            return ApiResponse<HashResponseDto>.ErrorResponse("DOCUMENT_NOT_FOUND", "Document not found or not owned by you.");

        var fileHash = await ComputeFileHashAsync(doc.FileURL, ct);

        // Upsert proof record
        var proof = await _context.DocumentBlockchainProofs
            .FirstOrDefaultAsync(p => p.DocumentID == documentId, ct);

        if (proof == null)
        {
            proof = new DocumentBlockchainProof
            {
                DocumentID = documentId,
                FileHash = fileHash,
                HashAlgorithm = "SHA-256",
                ProofStatus = ProofStatus.HashComputed,
                AnchoredBy = userId
            };
            _context.DocumentBlockchainProofs.Add(proof);
        }
        else
        {
            proof.FileHash = fileHash;
            proof.HashAlgorithm = "SHA-256";
        }

        await _context.SaveChangesAsync(ct);

        await _audit.LogAsync("COMPUTE_HASH", "DocumentBlockchainProof", proof.ProofID,
            $"Computed SHA-256 for document {documentId}: {fileHash[..16]}...");

        return ApiResponse<HashResponseDto>.SuccessResponse(new HashResponseDto
        {
            DocumentID = documentId,
            Algorithm = "SHA-256",
            FileHash = fileHash
        });
    }

    // ================================================================
    // 2) Submit hash to blockchain
    // ================================================================
    public async Task<ApiResponse<SubmitChainResponseDto>> SubmitToChainAsync(
        int documentId, int userId, CancellationToken ct = default)
    {
        var doc = await GetOwnedDocumentWithProofAsync(documentId, userId, ct);
        if (doc == null)
            return ApiResponse<SubmitChainResponseDto>.ErrorResponse("DOCUMENT_NOT_FOUND", "Document not found or not owned by you.");

        // Ensure hash exists — compute if not
        var proof = doc.BlockchainProof;
        string fileHash;

        if (proof == null || string.IsNullOrWhiteSpace(proof.FileHash))
        {
            fileHash = await ComputeFileHashAsync(doc.FileURL, ct);

            if (proof == null)
            {
                proof = new DocumentBlockchainProof
                {
                    DocumentID = documentId,
                    FileHash = fileHash,
                    HashAlgorithm = "SHA-256",
                    AnchoredBy = userId
                };
                _context.DocumentBlockchainProofs.Add(proof);
            }
            else
            {
                proof.FileHash = fileHash;
                proof.HashAlgorithm = "SHA-256";
            }
        }
        else
        {
            fileHash = proof.FileHash;
        }

        // Submit to blockchain
        var metadata = new BlockchainSubmitMeta
        {
            DocumentID = doc.DocumentID,
            StartupID = doc.StartupID,
            DocumentType = doc.DocumentType,
            FileName = doc.FileName
        };

        string txHash;
        try
        {
            txHash = await _blockchain.SubmitHashAsync(fileHash, metadata, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blockchain submit failed for document {DocumentID}", documentId);
            return ApiResponse<SubmitChainResponseDto>.ErrorResponse("BLOCKCHAIN_ERROR",
                "Failed to submit hash to blockchain. Please try again later.");
        }

        // Update proof record
        proof.TransactionHash = txHash;
        proof.ProofStatus = ProofStatus.Pending;
        proof.AnchoredAt = DateTime.UtcNow;
        proof.BlockchainNetwork = "Stub"; // TODO: set from config when using real blockchain

        await _context.SaveChangesAsync(ct);

        await _audit.LogAsync("SUBMIT_CHAIN", "DocumentBlockchainProof", proof.ProofID,
            $"Submitted hash to blockchain for document {documentId}, tx={txHash[..20]}...");

        return ApiResponse<SubmitChainResponseDto>.SuccessResponse(new SubmitChainResponseDto
        {
            DocumentID = documentId,
            FileHash = fileHash,
            TransactionHash = txHash,
            Status = "Pending",
            SubmittedAt = proof.AnchoredAt!.Value
        });
    }

    // ================================================================
    // 3) Verify on-chain
    // ================================================================
    public async Task<ApiResponse<VerifyChainResponseDto>> VerifyOnChainAsync(
        int documentId, int userId, CancellationToken ct = default)
    {
        var doc = await GetOwnedDocumentWithProofAsync(documentId, userId, ct);
        if (doc == null)
            return ApiResponse<VerifyChainResponseDto>.ErrorResponse("DOCUMENT_NOT_FOUND", "Document not found or not owned by you.");

        return await VerifyHashInternalAsync(doc, ct);
    }

    // ================================================================
    // 4) Get transaction status
    // ================================================================
    public async Task<ApiResponse<TxStatusResponseDto>> GetTxStatusAsync(
        int documentId, int userId, CancellationToken ct = default)
    {
        var doc = await GetOwnedDocumentWithProofAsync(documentId, userId, ct);
        if (doc == null)
            return ApiResponse<TxStatusResponseDto>.ErrorResponse("DOCUMENT_NOT_FOUND", "Document not found or not owned by you.");

        var proof = doc.BlockchainProof;
        if (proof == null || string.IsNullOrWhiteSpace(proof.TransactionHash))
            return ApiResponse<TxStatusResponseDto>.ErrorResponse("PROOF_NOT_SUBMITTED",
                "No blockchain transaction has been submitted for this document.");

        BlockchainTxStatusResult txStatus;
        try
        {
            txStatus = await _blockchain.GetTxStatusAsync(proof.TransactionHash, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blockchain tx-status check failed for tx {TxHash}", proof.TransactionHash);
            return ApiResponse<TxStatusResponseDto>.ErrorResponse("BLOCKCHAIN_ERROR",
                "Failed to check transaction status on blockchain.");
        }

        // Update proof in DB
        if (Enum.TryParse<ProofStatus>(txStatus.Status, true, out var parsedProofStatus))
            proof.ProofStatus = parsedProofStatus;
        else if (string.Equals(txStatus.Status, "Confirmed", StringComparison.OrdinalIgnoreCase))
            proof.ProofStatus = ProofStatus.Anchored;
        if (txStatus.BlockNumber != null) proof.BlockNumber = txStatus.BlockNumber;

        await _context.SaveChangesAsync(ct);

        return ApiResponse<TxStatusResponseDto>.SuccessResponse(new TxStatusResponseDto
        {
            DocumentID = documentId,
            TransactionHash = proof.TransactionHash,
            Status = txStatus.Status,
            BlockNumber = txStatus.BlockNumber,
            ConfirmedAt = txStatus.ConfirmedAt
        });
    }

    // ================================================================
    // 5) Staff verify hash
    // ================================================================
    public async Task<ApiResponse<VerifyChainResponseDto>> StaffVerifyHashAsync(
        int documentId, int staffUserId, CancellationToken ct = default)
    {
        // Staff can verify any document (no ownership check)
        var doc = await _context.Documents
            .Include(d => d.BlockchainProof)
            .Include(d => d.Startup)
            .FirstOrDefaultAsync(d => d.DocumentID == documentId, ct);

        if (doc == null)
            return ApiResponse<VerifyChainResponseDto>.ErrorResponse("DOCUMENT_NOT_FOUND", "Document not found.");

        var result = await VerifyHashInternalAsync(doc, ct);

        if (result.Success)
        {
            await _audit.LogAsync("STAFF_VERIFY_HASH", "DocumentBlockchainProof", documentId,
                $"Staff user {staffUserId} verified document {documentId}: {result.Data!.Status}");
        }

        return result;
    }

    // ================================================================
    // Private helpers
    // ================================================================

    private async Task<string> ComputeFileHashAsync(string fileUrl, CancellationToken ct)
    {
        await using var stream = await _storage.OpenReadAsync(fileUrl, ct);
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream, ct);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private async Task<ApiResponse<VerifyChainResponseDto>> VerifyHashInternalAsync(Document doc, CancellationToken ct)
    {
        // Recompute hash from stored file
        string computedHash;
        try
        {
            computedHash = await ComputeFileHashAsync(doc.FileURL, ct);
        }
        catch (FileNotFoundException)
        {
            return ApiResponse<VerifyChainResponseDto>.ErrorResponse("FILE_MISSING",
                "The physical file could not be found. Cannot verify.");
        }

        var proof = doc.BlockchainProof;
        if (proof == null || string.IsNullOrWhiteSpace(proof.FileHash))
        {
            return ApiResponse<VerifyChainResponseDto>.SuccessResponse(new VerifyChainResponseDto
            {
                DocumentID = doc.DocumentID,
                ComputedHash = computedHash,
                OnChainVerified = false,
                Status = "NotFound"
            });
        }

        // Verify on-chain
        bool onChain;
        try
        {
            onChain = await _blockchain.VerifyHashAsync(computedHash, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blockchain verify failed for document {DocumentID}", doc.DocumentID);
            return ApiResponse<VerifyChainResponseDto>.ErrorResponse("BLOCKCHAIN_ERROR",
                "Failed to verify hash on blockchain.");
        }

        // Compare computed hash with stored hash
        var hashMatch = string.Equals(computedHash, proof.FileHash, StringComparison.OrdinalIgnoreCase);
        var verified = onChain && hashMatch;

        string status;
        if (verified)
            status = "Verified";
        else if (!hashMatch)
            status = "Mismatch"; // file was modified after anchoring
        else
            status = "NotFound"; // not on chain

        // Update proof status if verified
        if (verified && proof.ProofStatus != ProofStatus.Anchored)
        {
            proof.ProofStatus = ProofStatus.Anchored;
            await _context.SaveChangesAsync(ct);
        }

        return ApiResponse<VerifyChainResponseDto>.SuccessResponse(new VerifyChainResponseDto
        {
            DocumentID = doc.DocumentID,
            ComputedHash = computedHash,
            OnChainVerified = verified,
            Status = status
        });
    }

    /// <summary>Get document belonging to the user's startup (tracked).</summary>
    private async Task<Document?> GetOwnedDocumentAsync(int documentId, int userId, CancellationToken ct)
    {
        return await _context.Documents
            .Include(d => d.Startup)
            .Where(d => d.DocumentID == documentId && d.Startup.UserID == userId)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>Get document with blockchain proof belonging to the user's startup (tracked).</summary>
    private async Task<Document?> GetOwnedDocumentWithProofAsync(int documentId, int userId, CancellationToken ct)
    {
        return await _context.Documents
            .Include(d => d.Startup)
            .Include(d => d.BlockchainProof)
            .Where(d => d.DocumentID == documentId && d.Startup.UserID == userId)
            .FirstOrDefaultAsync(ct);
    }
}
