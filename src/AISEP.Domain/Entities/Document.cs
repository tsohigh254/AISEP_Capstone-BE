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

    // Navigation properties
    public Startup Startup { get; set; } = null!;
    public DocumentBlockchainProof? BlockchainProof { get; set; }
}
