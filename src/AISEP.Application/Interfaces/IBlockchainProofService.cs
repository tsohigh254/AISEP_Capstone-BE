using AISEP.Application.DTOs.Blockchain;
using AISEP.Application.DTOs.Common;

namespace AISEP.Application.Interfaces;

/// <summary>
/// Orchestration service for blockchain proof operations.
/// Combines storage (hash computation), blockchain RPC, and DB persistence.
/// </summary>
public interface IBlockchainProofService
{
    /// <summary>Compute SHA-256 hash of the document file and persist to DB.</summary>
    Task<ApiResponse<HashResponseDto>> ComputeHashAsync(int documentId, int userId, CancellationToken ct = default);

    /// <summary>Submit the file hash to the blockchain and record the transaction.</summary>
    Task<ApiResponse<SubmitChainResponseDto>> SubmitToChainAsync(int documentId, int userId, CancellationToken ct = default);

    /// <summary>Recompute hash and verify against on-chain record.</summary>
    Task<ApiResponse<VerifyChainResponseDto>> VerifyOnChainAsync(int documentId, int userId, CancellationToken ct = default);

    /// <summary>Check the blockchain transaction status for a document's proof.</summary>
    Task<ApiResponse<TxStatusResponseDto>> GetTxStatusAsync(int documentId, int userId, CancellationToken ct = default);

    /// <summary>Staff/Admin: cross-check file hash with on-chain record.</summary>
    Task<ApiResponse<VerifyChainResponseDto>> StaffVerifyHashAsync(int documentId, int staffUserId, CancellationToken ct = default);
}
