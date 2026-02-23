namespace AISEP.Domain.Interfaces;

/// <summary>
/// Abstraction for blockchain interaction (submit hash, verify, check tx status).
/// Implement with real RPC (Ethereum/Polygon/etc.) for production.
/// </summary>
public interface IBlockchainService
{
    /// <summary>
    /// Submit a file hash to the blockchain.
    /// Returns the transaction hash.
    /// </summary>
    Task<string> SubmitHashAsync(string fileHash, BlockchainSubmitMeta metadata, CancellationToken ct = default);

    /// <summary>
    /// Verify that a file hash exists on-chain and matches.
    /// </summary>
    Task<bool> VerifyHashAsync(string fileHash, CancellationToken ct = default);

    /// <summary>
    /// Check the status of a previously submitted transaction.
    /// </summary>
    Task<BlockchainTxStatusResult> GetTxStatusAsync(string txHash, CancellationToken ct = default);
}

/// <summary>Metadata passed when submitting a hash to the blockchain.</summary>
public class BlockchainSubmitMeta
{
    public int DocumentID { get; set; }
    public int StartupID { get; set; }
    public string DocumentType { get; set; } = null!;
    public string FileName { get; set; } = null!;
}

/// <summary>Result from checking a blockchain transaction status.</summary>
public class BlockchainTxStatusResult
{
    public string Status { get; set; } = null!; // Pending | Confirmed | Failed
    public string? BlockNumber { get; set; }
    public DateTime? ConfirmedAt { get; set; }
}
