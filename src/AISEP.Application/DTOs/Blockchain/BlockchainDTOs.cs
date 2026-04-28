namespace AISEP.Application.DTOs.Blockchain;

// ──────────────────────────────────────────────
// Request DTOs
// ──────────────────────────────────────────────

/// <summary>
/// Request to verify a SHA-256 hash against the blockchain and the system's records.
/// Client computes the hash locally (e.g. browser SubtleCrypto) from a downloaded file
/// and submits the hex string here — the file itself is never uploaded.
/// </summary>
public class HashLookupRequestDto
{
    /// <summary>SHA-256 as 64 lowercase hex characters. Optional leading "0x" is accepted.</summary>
    public string Hash { get; set; } = null!;
}

// ──────────────────────────────────────────────
// Response DTOs
// ──────────────────────────────────────────────

/// <summary>
/// Result of looking up a hash against the blockchain + system records.
/// </summary>
public class HashLookupResponseDto
{
    /// <summary>Normalized lowercase hex hash that was looked up.</summary>
    public string Hash { get; set; } = null!;

    /// <summary>Did the blockchain contract confirm this hash is anchored?</summary>
    public bool OnChainVerified { get; set; }

    /// <summary>Does the AISEP database have a proof record for this hash?</summary>
    public bool RecordedInSystem { get; set; }

    /// <summary>Verified | OnChainOnly | NotFound.</summary>
    public string Status { get; set; } = null!;

    /// <summary>Document ID if a matching proof exists in the AISEP database.</summary>
    public int? DocumentID { get; set; }

    /// <summary>When the hash was anchored, if recorded in DB.</summary>
    public DateTime? AnchoredAt { get; set; }

    /// <summary>Etherscan link for the anchoring transaction, if known.</summary>
    public string? EtherscanUrl { get; set; }
}

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
