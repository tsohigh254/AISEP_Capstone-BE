using System;
using System.Collections.Generic;

namespace AISEP.Application.DTOs.Advisor;

public class AdvisorKYCStatusDto
{
    public string WorkflowStatus { get; set; } = "NOT_STARTED";
    public string VerificationLabel { get; set; } = "NONE";
    public string Explanation { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public List<string>? FlaggedFields { get; set; }
    public List<AdvisorKYCHistoryDto>? History { get; set; }
    public AdvisorKYCSubmissionSummaryDto? SubmissionSummary { get; set; }
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
}
