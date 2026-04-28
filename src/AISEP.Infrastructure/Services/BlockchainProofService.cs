using System.Security.Cryptography;
using AISEP.Application.DTOs.Blockchain;
using AISEP.Application.DTOs.Common;
using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Domain.Enums;
using AISEP.Domain.Interfaces;
using AISEP.Application.Configuration;
using AISEP.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AISEP.Infrastructure.Services;

public class BlockchainProofService : IBlockchainProofService
{
    private readonly ApplicationDbContext _context;
    private readonly IBlockchainService _blockchain;
    private readonly IAuditService _audit;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ILogger<BlockchainProofService> _logger;
    private readonly BlockchainSettings _blockchainSettings;

    public BlockchainProofService(
        ApplicationDbContext context,
        IBlockchainService blockchain,
        IAuditService audit,
        ICloudinaryService cloudinaryService,
        ILogger<BlockchainProofService> logger,
        IOptions<BlockchainSettings> blockchainSettings)
    {
        _context = context;
        _blockchain = blockchain;
        _audit = audit;
        _cloudinaryService = cloudinaryService;
        _logger = logger;
        _blockchainSettings = blockchainSettings.Value;
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

        if (doc.IsArchived)
            return ApiResponse<HashResponseDto>.ErrorResponse("DOCUMENT_ARCHIVED",
                "Cannot compute hash for an archived document.");

        // Kiểm tra xem hash đã được tính khi upload chưa
        var existingProof = await _context.DocumentBlockchainProofs
            .FirstOrDefaultAsync(p => p.DocumentID == documentId, ct);

        string fileHash;
        bool hashAlreadyExists = false;

        if (existingProof != null && !string.IsNullOrWhiteSpace(existingProof.FileHash))
        {
            // Hash đã có sẵn từ lúc upload, không cần tải file lại
            fileHash = existingProof.FileHash;
            hashAlreadyExists = true;
            _logger.LogInformation("Hash already exists for document {DocumentID}, skipping recomputation", documentId);
        }
        else
        {
            // Fallback: tính hash từ file (cho documents cũ hoặc uploaded trước khi có tính năng này)
            fileHash = await ComputeFileHashAsync(doc.FileURL, ct);
        }

        // Upsert proof record
        if (existingProof == null)
        {
            existingProof = new DocumentBlockchainProof
            {
                DocumentID = documentId,
                FileHash = fileHash,
                HashAlgorithm = "SHA-256",
                ProofStatus = ProofStatus.HashComputed,
                AnchoredBy = userId
            };
            _context.DocumentBlockchainProofs.Add(existingProof);
        }
        else if (!hashAlreadyExists)
        {
            // Chỉ update nếu hash mới được tính
            existingProof.FileHash = fileHash;
            existingProof.HashAlgorithm = "SHA-256";
        }

        await _context.SaveChangesAsync(ct);

        await _audit.LogAsync("COMPUTE_HASH", "DocumentBlockchainProof", existingProof.ProofID,
            $"Hash for document {documentId}: {fileHash[..16]}... {(hashAlreadyExists ? "(existing)" : "(computed)")}");

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

        if (doc.IsArchived)
            return ApiResponse<SubmitChainResponseDto>.ErrorResponse("DOCUMENT_ARCHIVED",
                "Cannot submit an archived document to blockchain.");

        // Prevent double-submit
        var proof = doc.BlockchainProof;
        if (proof != null && (proof.ProofStatus == ProofStatus.Pending || proof.ProofStatus == ProofStatus.Anchored))
            return ApiResponse<SubmitChainResponseDto>.ErrorResponse("PROOF_ALREADY_SUBMITTED",
                $"This document has already been submitted to blockchain (status: {proof.ProofStatus}).");
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

        // Extract filename from Cloudinary URL
        var fileName = ExtractFileNameFromCloudinaryUrl(doc.FileURL) ?? "Unknown";

        // Submit to blockchain
        var metadata = new BlockchainSubmitMeta
        {
            DocumentID = doc.DocumentID,
            StartupID = doc.StartupID,
            DocumentType = doc.DocumentType,
            FileName = fileName
        };

        string txHash;
        try
        {
            txHash = await _blockchain.SubmitHashAsync(fileHash, metadata, ct);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already registered"))
        {
            _logger.LogWarning(ex, "Hash already on-chain for document {DocumentID}", documentId);
            return ApiResponse<SubmitChainResponseDto>.ErrorResponse("HASH_ALREADY_EXISTS",
                "This document's hash is already registered on the blockchain.");
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
        proof.AnchoredBy = userId;
        proof.BlockchainNetwork = _blockchainSettings.NetworkName;

        await _context.SaveChangesAsync(ct);

        await _audit.LogAsync("SUBMIT_CHAIN", "DocumentBlockchainProof", proof.ProofID,
            $"Submitted hash to blockchain for document {documentId}, tx={txHash[..20]}...");

        return ApiResponse<SubmitChainResponseDto>.SuccessResponse(new SubmitChainResponseDto
        {
            DocumentID = documentId,
            FileHash = fileHash,
            TransactionHash = txHash,
            Status = "Pending",
            SubmittedAt = proof.AnchoredAt!.Value,
            EtherscanUrl = BuildEtherscanUrl(txHash)
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

        // Update proof in DB — map blockchain status strings to ProofStatus enum
        proof.ProofStatus = txStatus.Status?.ToLowerInvariant() switch
        {
            "confirmed" => ProofStatus.Anchored,
            "pending"   => ProofStatus.Pending,
            "failed"    => ProofStatus.Failed,
            _           => proof.ProofStatus  // unknown status — no change
        };
        if (txStatus.BlockNumber != null) proof.BlockNumber = txStatus.BlockNumber;

        await _context.SaveChangesAsync(ct);

        return ApiResponse<TxStatusResponseDto>.SuccessResponse(new TxStatusResponseDto
        {
            DocumentID = documentId,
            TransactionHash = proof.TransactionHash,
            Status = txStatus.Status ?? "Unknown",
            BlockNumber = txStatus.BlockNumber,
            ConfirmedAt = txStatus.ConfirmedAt,
            EtherscanUrl = BuildEtherscanUrl(proof.TransactionHash)
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

    /// <summary>
    /// Tính hash SHA-256 từ file URL trên Cloudinary.
    /// ⚠️ FALLBACK ONLY: Method này chỉ dùng cho documents cũ uploaded trước khi có auto-hash.
    /// Documents mới đã có hash tính sẵn khi upload, không cần download lại.
    /// </summary>
    private async Task<string> ComputeFileHashAsync(string fileUrl, CancellationToken ct)
    {
        try
        {
            var fileBytes = await _cloudinaryService.DownloadFileAsync(fileUrl, ct);
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(fileBytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compute hash for URL: {FileUrl}", fileUrl);
            throw;
        }
    }

    private async Task<ApiResponse<VerifyChainResponseDto>> VerifyHashInternalAsync(Document doc, CancellationToken ct)
    {
        var proof = doc.BlockchainProof;
        if (proof == null || string.IsNullOrWhiteSpace(proof.FileHash))
        {
            return ApiResponse<VerifyChainResponseDto>.SuccessResponse(new VerifyChainResponseDto
            {
                DocumentID = doc.DocumentID,
                ComputedHash = null!,
                OnChainVerified = false,
                Status = "NotFound",
                AnchoredAt = null,
                EtherscanUrl = null
            });
        }

        // Recompute hash from the live file on Cloudinary so tampering after registration is detectable.
        string freshHash;
        try
        {
            freshHash = await ComputeFileHashAsync(doc.FileURL, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file from storage for document {DocumentID}", doc.DocumentID);
            return ApiResponse<VerifyChainResponseDto>.ErrorResponse("FILE_DOWNLOAD_FAILED",
                "Failed to download file from storage to verify hash.");
        }

        var storedHash = proof.FileHash;

        // If the live file's hash no longer matches the hash recorded at upload time,
        // the file has been modified after registration.
        if (!string.Equals(freshHash, storedHash, StringComparison.OrdinalIgnoreCase))
        {
            return ApiResponse<VerifyChainResponseDto>.SuccessResponse(new VerifyChainResponseDto
            {
                DocumentID = doc.DocumentID,
                ComputedHash = freshHash,
                OnChainVerified = false,
                Status = "Mismatch",
                AnchoredAt = proof.AnchoredAt,
                EtherscanUrl = BuildEtherscanUrl(proof.TransactionHash)
            });
        }

        // Live hash matches DB — confirm it is actually anchored on-chain.
        bool onChain;
        try
        {
            onChain = await _blockchain.VerifyHashAsync(freshHash, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blockchain verify failed for document {DocumentID}", doc.DocumentID);
            return ApiResponse<VerifyChainResponseDto>.ErrorResponse("BLOCKCHAIN_ERROR",
                "Failed to verify hash on blockchain.");
        }

        string status = onChain ? "Verified" : "NotFound";

        if (onChain && proof.ProofStatus != ProofStatus.Anchored)
        {
            proof.ProofStatus = ProofStatus.Anchored;
            await _context.SaveChangesAsync(ct);
        }

        return ApiResponse<VerifyChainResponseDto>.SuccessResponse(new VerifyChainResponseDto
        {
            DocumentID = doc.DocumentID,
            ComputedHash = freshHash,
            OnChainVerified = onChain,
            Status = status,
            AnchoredAt = proof.AnchoredAt,
            EtherscanUrl = BuildEtherscanUrl(proof.TransactionHash)
        });
    }

    // ================================================================
    // 6) Verify a client-supplied hash (startup/investor self-service)
    // ================================================================
    public async Task<ApiResponse<HashLookupResponseDto>> VerifyHashLookupAsync(
        string hash, CancellationToken ct = default)
    {
        var normalized = NormalizeHash(hash);
        if (normalized == null)
            return ApiResponse<HashLookupResponseDto>.ErrorResponse("HASH_INVALID",
                "Hash must be a 64-character hex SHA-256 string (optional 0x prefix).");

        var proof = await _context.DocumentBlockchainProofs
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.FileHash != null && p.FileHash.ToLower() == normalized, ct);

        bool onChain;
        try
        {
            onChain = await _blockchain.VerifyHashAsync(normalized, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blockchain RPC failed during hash lookup");
            return ApiResponse<HashLookupResponseDto>.ErrorResponse("BLOCKCHAIN_RPC_FAILED",
                "Failed to query blockchain. Please try again later.");
        }

        var recordedInSystem = proof != null;
        var status = (onChain, recordedInSystem) switch
        {
            (true, true) => "Verified",
            (true, false) => "OnChainOnly",
            _ => "NotFound"
        };

        return ApiResponse<HashLookupResponseDto>.SuccessResponse(new HashLookupResponseDto
        {
            Hash = normalized,
            OnChainVerified = onChain,
            RecordedInSystem = recordedInSystem,
            Status = status,
            DocumentID = proof?.DocumentID,
            AnchoredAt = proof?.AnchoredAt,
            EtherscanUrl = BuildEtherscanUrl(proof?.TransactionHash)
        });
    }

    private static string? NormalizeHash(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var s = input.Trim();
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s[2..];
        if (s.Length != 64) return null;
        for (int i = 0; i < s.Length; i++)
        {
            var c = s[i];
            var hex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
            if (!hex) return null;
        }
        return s.ToLowerInvariant();
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


    private string? BuildEtherscanUrl(string? txHash)
    {
        if (string.IsNullOrWhiteSpace(txHash)) return null;
        return $"{_blockchainSettings.EtherscanBaseUrl.TrimEnd('/')}/tx/{txHash}";
    }

    /// <summary>Extract filename from Cloudinary URL.</summary>
    /// <example>
    /// Input: "https://res.cloudinary.com/xxx/image/upload/v123/document_abc123.pdf"
    /// Output: "document_abc123.pdf"
    /// </example>
    private static string? ExtractFileNameFromCloudinaryUrl(string cloudinaryUrl)
    {
        try
        {
            var uri = new Uri(cloudinaryUrl);
            var lastSegment = uri.Segments.Last();
            return Uri.UnescapeDataString(lastSegment);
        }
        catch
        {
            return null;
        }
    }
}
