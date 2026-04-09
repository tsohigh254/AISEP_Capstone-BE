using AISEP.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace AISEP.Domain.Entities;

public class InvestorKycSubmission
{
    [Key]
    public int SubmissionID { get; set; }
    public int InvestorID { get; set; }
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public InvestorKycWorkflowStatus WorkflowStatus { get; set; } = InvestorKycWorkflowStatus.Draft;
    public InvestorKycResultLabel ResultLabel { get; set; } = InvestorKycResultLabel.None;

    // KYC Form Data
    public string? InvestorCategory { get; set; } // "INSTITUTIONAL" | "INDIVIDUAL_ANGEL"
    public string? FullName { get; set; }
    public string? ContactEmail { get; set; }
    public string? OrganizationName { get; set; }
    public string? CurrentRoleTitle { get; set; }
    public string? Location { get; set; }
    public string? Website { get; set; }
    public string? LinkedInURL { get; set; }
    public string? SubmitterRole { get; set; }
    public string? TaxIdOrBusinessCode { get; set; }

    // Review fields
    public string? Explanation { get; set; }
    public string? Remarks { get; set; }
    public bool RequiresNewEvidence { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int? ReviewedBy { get; set; }

    // Navigation
    public Investor Investor { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
    public ICollection<InvestorKycEvidenceFile> EvidenceFiles { get; set; } = new List<InvestorKycEvidenceFile>();
}
