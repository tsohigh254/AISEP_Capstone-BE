using System;
using System.Collections.Generic;

namespace AISEP.Application.DTOs.Advisor;

public class AdvisorKYCStatusDto
{
    public string WorkflowStatus { get; set; } = "NOT_STARTED";
    public string VerificationLabel { get; set; } = "NONE";
    public string Explanation { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public bool RequiresNewEvidence { get; set; }
    public List<string>? FlaggedFields { get; set; }
    public List<AdvisorKYCHistoryDto>? History { get; set; }
    public AdvisorKYCSubmissionSummaryDto? SubmissionSummary { get; set; }
    public AdvisorKYCCurrentSubmissionDto? CurrentSubmission { get; set; }
    public object? PreviousSubmission { get; set; }
    public object? DraftData { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class AdvisorKYCHistoryDto
{
    public string Action { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Remark { get; set; }
}

public class AdvisorKYCSubmissionSummaryDto
{
    public string FullName { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public int Version { get; set; }
    public List<AdvisorKYCEvidenceFileDto> EvidenceFiles { get; set; } = new();
}

public class AdvisorKYCEvidenceFileDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? FileType { get; set; }
    public string Url { get; set; } = string.Empty;
}

public class AdvisorKYCCurrentSubmissionDto
{
    public string FullName { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? CurrentRoleTitle { get; set; }
    public string? CurrentOrganization { get; set; }
    public string? PrimaryExpertise { get; set; }
    public string? Bio { get; set; }
    public string? ProfessionalProfileLink { get; set; }
    public string? BasicExpertiseProofFileURL { get; set; }
    public string? BasicExpertiseProofFileName { get; set; }
    public int? YearsOfExperience { get; set; }
    public string? MentorshipPhilosophy { get; set; }
}
