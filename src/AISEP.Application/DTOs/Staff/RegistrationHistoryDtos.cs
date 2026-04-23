namespace AISEP.Application.DTOs.Staff;

public class RegistrationHistoryItemDto
{
    public int ApplicantId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public string RoleType { get; set; } = string.Empty;   // STARTUP | ADVISOR | INVESTOR
    public string Result { get; set; } = string.Empty;     // APPROVED | REJECTED | PENDING_MORE_INFO
    public DateTime? ProcessedAt { get; set; }
    public string? ReviewedBy { get; set; }                // email of staff
    public string? Remarks { get; set; }
    public string? AvatarUrl { get; set; }                 // logoURL (Startup) | profilePhotoURL (Advisor/Investor)
}

/// <summary>
/// One entry in the per-case KYC review timeline.
/// Each entry represents one submission version (one review cycle).
/// Note: internalNote and field-level assessments are NOT available — those fields do not
/// exist in the current schema. Only user-facing 'remarks' is stored per cycle.
/// </summary>
public class KycCaseHistoryEntryDto
{
    /// <summary>Submission version number (1 = first submission, 2 = first resubmission, etc.).</summary>
    public int Version { get; set; }

    /// <summary>ISO timestamp when the applicant submitted this version. Null if still in Draft.</summary>
    public DateTime? SubmittedAt { get; set; }

    /// <summary>ISO timestamp when staff reviewed this version. Null if not yet reviewed.</summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>Email of the staff member who reviewed. Null if not yet reviewed.</summary>
    public string? ReviewedByEmail { get; set; }

    /// <summary>
    /// Action outcome of this version:
    /// UNDER_REVIEW | APPROVED | REJECTED | REQUESTED_MORE_INFO | SUPERSEDED | DRAFT
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Result label: NONE | BASIC_VERIFIED | VERIFIED_COMPANY | VERIFIED_ADVISOR | PENDING_MORE_INFO | VERIFICATION_FAILED</summary>
    public string ResultLabel { get; set; } = string.Empty;

    /// <summary>User-facing note from staff (what is sent to the applicant). Null if no note was written.</summary>
    public string? Remarks { get; set; }

    /// <summary>Whether staff required the applicant to upload new evidence for this cycle.</summary>
    public bool RequiresNewEvidence { get; set; }
}
