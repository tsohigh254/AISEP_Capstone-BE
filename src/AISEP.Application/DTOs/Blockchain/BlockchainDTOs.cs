namespace AISEP.Application.DTOs.Blockchain;

// ──────────────────────────────────────────────
// Response DTOs
// ──────────────────────────────────────────────

/// <summary>
/// Response after computing a document's file hash.
/// </summary>
public class HashResponseDto
{
    public int DocumentID { get; set; }
    public string Algorithm { get; set; } = "SHA-256";
    public string FileHash { get; set; } = null!;
}

/// <summary>
/// Response after submitting a hash to the blockchain.
/// </summary>
public class SubmitChainResponseDto
{
    public int DocumentID { get; set; }
    public string FileHash { get; set; } = null!;
    public string TransactionHash { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime SubmittedAt { get; set; }
    public string? EtherscanUrl { get; set; }
}

/// <summary>
/// Response from on-chain verification of a document's integrity.
/// </summary>
public class VerifyChainResponseDto
{
    public int DocumentID { get; set; }
    public string ComputedHash { get; set; } = null!;
    public bool OnChainVerified { get; set; }
    public string Status { get; set; } = null!; // Verified | Mismatch | NotFound
    public DateTime? AnchoredAt { get; set; }
    public string? EtherscanUrl { get; set; }
}

/// <summary>
/// Response from checking the transaction status on-chain.
/// </summary>
public class TxStatusResponseDto
{
    public int DocumentID { get; set; }
    public string? TransactionHash { get; set; }
    public string Status { get; set; } = null!; // Pending | Confirmed | Failed
    public string? BlockNumber { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string? EtherscanUrl { get; set; }
}
