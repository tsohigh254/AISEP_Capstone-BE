using AISEP.Domain.Enums;

namespace AISEP.Domain.Entities;

public class StartupKycSubmission
{
    public int SubmissionID { get; set; }
    public int StartupID { get; set; }
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public StartupKycWorkflowStatus WorkflowStatus { get; set; } = StartupKycWorkflowStatus.Draft;
    public StartupKycResultLabel ResultLabel { get; set; } = StartupKycResultLabel.None;
    public StartupVerificationType StartupVerificationType { get; set; }
    public string? LegalFullName { get; set; }
    public string? ProjectName { get; set; }
    public string? EnterpriseCode { get; set; }
    public string RepresentativeFullName { get; set; } = string.Empty;
    public string RepresentativeRole { get; set; } = string.Empty;
    public string WorkEmail { get; set; } = string.Empty;
    public string? PublicLink { get; set; }
    public string? Explanation { get; set; }
    public string? Remarks { get; set; }
    public bool RequiresNewEvidence { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int? ReviewedBy { get; set; }

    public Startup Startup { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
    public ICollection<StartupKycEvidenceFile> EvidenceFiles { get; set; } = new List<StartupKycEvidenceFile>();
    public ICollection<StartupKycRequestedItem> RequestedAdditionalItems { get; set; } = new List<StartupKycRequestedItem>();
}
