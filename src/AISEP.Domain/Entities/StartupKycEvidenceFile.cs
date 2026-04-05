using AISEP.Domain.Enums;

namespace AISEP.Domain.Entities;

public class StartupKycEvidenceFile
{
    public int EvidenceFileID { get; set; }
    public int SubmissionID { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string? StorageKey { get; set; }
    public StartupKycEvidenceKind Kind { get; set; } = StartupKycEvidenceKind.Other;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }

    public StartupKycSubmission Submission { get; set; } = null!;
}
