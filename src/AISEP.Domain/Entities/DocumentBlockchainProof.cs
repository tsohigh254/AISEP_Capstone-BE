using AISEP.Domain.Enums;

namespace AISEP.Domain.Entities;

public class DocumentBlockchainProof
{
    public int ProofID { get; set; }
    public int DocumentID { get; set; }
    public string FileHash { get; set; } = string.Empty;
    public string HashAlgorithm { get; set; } = string.Empty;
    public string? BlockchainNetwork { get; set; }
    public string? TransactionHash { get; set; }
    public string? BlockNumber { get; set; }
    public DateTime? AnchoredAt { get; set; }
    public int? AnchoredBy { get; set; }
    public ProofStatus ProofStatus { get; set; } = ProofStatus.Anchored;

    // Navigation properties
    public Document Document { get; set; } = null!;
    public User? AnchoredByUser { get; set; }
}
