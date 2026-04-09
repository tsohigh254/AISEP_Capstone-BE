using AISEP.Domain.Enums;

namespace AISEP.Domain.Entities;

public class Document
{
    public int DocumentID { get; set; }
    public int StartupID { get; set; }
    public DocumentType DocumentType { get; set; }
    public string? Title { get; set; }
    public string FileURL { get; set; } = string.Empty;
    public string? Version { get; set; }
    public bool IsAnalyzed { get; set; }
    public AnalysisStatus AnalysisStatus { get; set; } = AnalysisStatus.NOTANALYZE;
    public DateTime UploadedAt { get; set; }
    public DateTime? AnalyzedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public bool IsArchived { get; set; }

    // Staff review
    public DocumentReviewStatus ReviewStatus { get; set; } = DocumentReviewStatus.Pending;
    public int? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }

    // Version history
    public int? ParentDocumentID { get; set; }

    // Navigation properties
    public User? ReviewedByUser { get; set; }
    public Startup Startup { get; set; } = null!;
    public DocumentBlockchainProof? BlockchainProof { get; set; }
    public Document? ParentDocument { get; set; }
    public ICollection<Document> ChildVersions { get; set; } = new List<Document>();
}
