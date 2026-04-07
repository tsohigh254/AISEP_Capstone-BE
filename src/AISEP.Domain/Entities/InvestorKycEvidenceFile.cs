using AISEP.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace AISEP.Domain.Entities;

public class InvestorKycEvidenceFile
{
    [Key]
    public int EvidenceFileID { get; set; }
    public int SubmissionID { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string? StorageKey { get; set; }
    public InvestorKycEvidenceKind Kind { get; set; } = InvestorKycEvidenceKind.Other;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }

    public InvestorKycSubmission Submission { get; set; } = null!;
}
