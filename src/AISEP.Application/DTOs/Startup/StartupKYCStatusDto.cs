using System;
using System.Collections.Generic;

namespace AISEP.Application.DTOs.Startup;

public class StartupKYCStatusDto
{
    public string WorkflowStatus { get; set; } = "NOT_SUBMITTED";
    public string ResultLabel { get; set; } = "NONE";
    public string Explanation { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public bool RequiresNewEvidence { get; set; }
    public int? SubmissionId { get; set; }
    public int? Version { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public StartupKYCSubmissionSummaryDto? SubmissionSummary { get; set; }
    public List<StartupKycRequestedItemDto> RequestedAdditionalItems { get; set; } = new();

    // Legacy fields kept to avoid breaking older code while FE migrates.
    public string VerificationLabel { get; set; } = "NONE";
    public List<string>? FlaggedFields { get; set; }
    public List<StartupKYCHistoryDto>? History { get; set; }
    public object? PreviousSubmission { get; set; }
    public object? DraftData { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class StartupKYCHistoryDto
{
    public string Action { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Remark { get; set; }
}

public class StartupKycSubmissionDto
{
    public int Id { get; set; }
    public int StartupId { get; set; }
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public string WorkflowStatus { get; set; } = string.Empty;
    public string ResultLabel { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int? ReviewedBy { get; set; }
    public StartupKYCSubmissionSummaryDto SubmissionSummary { get; set; } = new();
    public List<StartupKycRequestedItemDto> RequestedAdditionalItems { get; set; } = new();
    public string? Explanation { get; set; }
    public string? Remarks { get; set; }
    public bool RequiresNewEvidence { get; set; }
}

public class StartupKYCSubmissionSummaryDto
{
    public string CompanyName { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public int Version { get; set; }
    public string StartupVerificationType { get; set; } = string.Empty;
    public string? LegalFullName { get; set; }
    public string? EnterpriseCode { get; set; }
    public string? ProjectName { get; set; }
    public string RepresentativeFullName { get; set; } = string.Empty;
    public string RepresentativeRole { get; set; } = string.Empty;
    public string WorkEmail { get; set; } = string.Empty;
    public string? PublicLink { get; set; }
    public List<StartupKycEvidenceFileDto> EvidenceFiles { get; set; } = new();
}

public class StartupKycEvidenceFileDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? FileType { get; set; }
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? StorageKey { get; set; }
}

public class StartupKycRequestedItemDto
{
    public int Id { get; set; }
    public string FieldKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
