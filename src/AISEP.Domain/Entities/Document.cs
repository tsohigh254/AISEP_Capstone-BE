namespace AISEP.Domain.Entities;

public class Document
{
    public int DocumentID { get; set; }
    public int StartupID { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileURL { get; set; } = string.Empty;
    public int FileSize { get; set; }
    public string? FileFormat { get; set; }
    public string? Version { get; set; }
    public bool IsAnalyzed { get; set; }
    public string? AnalysisStatus { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? AnalyzedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public bool IsArchived { get; set; }

    // Navigation properties
    public Startup Startup { get; set; } = null!;
    public DocumentBlockchainProof? BlockchainProof { get; set; }
}
