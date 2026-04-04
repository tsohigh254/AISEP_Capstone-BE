namespace AISEP.Application.DTOs.Mentorship;

// ============================= REQUEST DTOs =============================

/// <summary>
/// Create a new mentorship request (Startup → Advisor).
/// Example: { "advisorId": 1, "challengeDescription": "Go-to-market strategy for B2B SaaS",
///            "additionalNotes": "How to price our product?", "preferredFormat": "Video",
///            "durationMinutes": "3 months", "scopeTags": "Weekly 1h sessions" }
/// </summary>
public class CreateMentorshipRequest
{
    public int AdvisorId { get; set; }
    public string ProblemContext { get; set; } = null!;
    public string? AdditionalNotes { get; set; }
    public string? PreferredFormat { get; set; }
    public int? DurationMinutes { get; set; }
    public List<string>? ScopeTags { get; set; }
    public string? Objective { get; set; }
    public List<RequestedSlotDto> RequestedSlots { get; set; } = new();
}

public class RequestedSlotDto
{
    public int? SlotID { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string? Timezone { get; set; }
    public string? Note { get; set; }
    public string ProposedBy { get; set; } = "Startup";
    public bool IsActive { get; set; } = true;
}

public class ProposeSlotsRequest
{
    public List<RequestedSlotDto> RequestedSlots { get; set; } = new();
}

/// <summary>
/// Reject a mentorship request (Advisor only).
/// Example: { "reason": "Schedule conflict, unable to commit right now." }
/// </summary>
public class RejectMentorshipRequest
{
    public string? Reason { get; set; }
}

/// <summary>
/// Schedule an accepted mentorship (Advisor only).
/// Automatically calculates duration and creates the first session.
/// Example: { "startAt": "2026-03-31T10:00:00Z", "endAt": "2026-03-31T11:00:00Z", "meetingLink": "https://meet.google.com/abc" }
/// </summary>
public class ScheduleMentorshipRequest
{
    public int SelectedSlotId { get; set; }
    public string? MeetingLink { get; set; }
}

/// <summary>
/// Create a session within an accepted mentorship (Advisor only).
/// Example: { "scheduledStartAt": "2026-03-01T10:00:00Z", "durationMinutes": 60,
///            "sessionFormat": "Video", "meetingUrl": "https://meet.google.com/abc" }
/// </summary>
public class CreateSessionRequest
{
    public DateTime ScheduledStartAt { get; set; }
    public int DurationMinutes { get; set; }
    public string? SessionFormat { get; set; }
    public string? MeetingUrl { get; set; }
}

/// <summary>
/// Update an existing session (Advisor only).
/// Example: { "scheduledStartAt": "2026-03-01T14:00:00Z", "durationMinutes": 90,
///            "meetingUrl": "https://zoom.us/j/123", "sessionStatus": "Completed",
///            "topicsDiscussed": "...", "keyInsights": "...", "actionItems": "...", "nextSteps": "..." }
/// </summary>
public class UpdateSessionRequest
{
    public DateTime? ScheduledStartAt { get; set; }
    public int? DurationMinutes { get; set; }
    public string? SessionFormat { get; set; }
    public string? MeetingUrl { get; set; }
    public string? SessionStatus { get; set; }
    public string? TopicsDiscussed { get; set; }
    public string? KeyInsights { get; set; }
    public string? ActionItems { get; set; }
    public string? NextSteps { get; set; }
}

/// <summary>
/// Create a mentorship report (Advisor only).
/// Example: { "sessionId": 5, "reportSummary": "Great progress on GTM strategy",
///            "detailedFindings": "...", "recommendations": "Focus on enterprise segment" }
/// </summary>
public class CreateReportRequest
{
    public int? SessionId { get; set; }
    public string ReportSummary { get; set; } = null!;
    public string? DetailedFindings { get; set; }
    public string? Recommendations { get; set; }
}

/// <summary>
/// Create feedback on a mentorship or session (Startup only).
/// Example: { "sessionId": 5, "rating": 5, "comment": "Extremely helpful session!" }
/// </summary>
public class CreateFeedbackRequest
{
    public int? SessionId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}

// ============================= RESPONSE DTOs =============================

public class AdvisorSummaryDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? ProfilePhotoURL { get; set; }
}

/// <summary>Basic mentorship DTO returned after create/update.</summary>
public class MentorshipDto
{
    public int MentorshipID { get; set; }
    public int StartupID { get; set; }
    public int AdvisorID { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ProblemContext { get; set; }
    public string? AdditionalNotes { get; set; }
    public string? PreferredFormat { get; set; }
    public int? DurationMinutes { get; set; }
    public List<string>? ScopeTags { get; set; }
    public DateTime? RequestedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectedReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>List item DTO with participant names for search/listing.</summary>
public class MentorshipListItemDto
{
    public int MentorshipID { get; set; }
    public int StartupID { get; set; }
    public string StartupName { get; set; } = string.Empty;
    public string? StartupLogoUrl { get; set; }
    public string? StartupIndustry { get; set; }
    public string? StartupStage { get; set; }
    public AdvisorSummaryDto Advisor { get; set; } = new();
    public string Status { get; set; } = string.Empty;
    public string? ProblemContext { get; set; }
    public string? Objective { get; set; }
    public string? PreferredFormat { get; set; }
    public DateTime? RequestedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<RequestedSlotDto> RequestedSlots { get; set; } = new();
}

/// <summary>Detail DTO with sessions, reports, feedbacks.</summary>
public class MentorshipDetailDto
{
    public int MentorshipID { get; set; }
    public int StartupID { get; set; }
    public string StartupName { get; set; } = string.Empty;
    public string? StartupLogoUrl { get; set; }
    public string? StartupIndustry { get; set; }
    public string? StartupStage { get; set; }
    public AdvisorSummaryDto Advisor { get; set; } = new();
    public string Status { get; set; } = string.Empty;
    public string? ProblemContext { get; set; }
    public string? AdditionalNotes { get; set; }
    public string? PreferredFormat { get; set; }
    public int? DurationMinutes { get; set; }
    public List<string>? ScopeTags { get; set; }
    public string? Objective { get; set; }
    public DateTime? RequestedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectedReason { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool CompletionConfirmedByStartup { get; set; }
    public bool CompletionConfirmedByAdvisor { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<RequestedSlotDto> RequestedSlots { get; set; } = new();
    public List<SessionDto> Sessions { get; set; } = new();
    public List<ReportDto> Reports { get; set; } = new();
    public List<FeedbackDto> Feedbacks { get; set; } = new();
}

/// <summary>Session DTO.</summary>
public class SessionDto
{
    public int SessionID { get; set; }
    public int MentorshipID { get; set; }
    public DateTime? ScheduledStartAt { get; set; }
    public int? DurationMinutes { get; set; }
    public string? SessionFormat { get; set; }
    public string? MeetingURL { get; set; }
    public string? SessionStatus { get; set; }
    public string? TopicsDiscussed { get; set; }
    public string? KeyInsights { get; set; }
    public string? ActionItems { get; set; }
    public string? NextSteps { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class SessionListItemDto : SessionDto
{
    public int AdvisorID { get; set; }
    public string? AdvisorName { get; set; }
    public string? AdvisorProfilePhotoURL { get; set; }
    public int StartupID { get; set; }
    public string? StartupName { get; set; }
}

public class FinalReportResponseDto
{
    public string ReportId { get; set; } = string.Empty;
    public string MentorshipId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    
    public FinalReportAdvisorDto Advisor { get; set; } = new();
}

public class FinalReportAdvisorDto
{
    public string FullName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
}

/// <summary>Report DTO.</summary>
public class ReportDto
{
    public int ReportID { get; set; }
    public int MentorshipID { get; set; }
    public int? SessionID { get; set; }
    public int? CreatedByAdvisorID { get; set; }
    public string? ReportSummary { get; set; }
    public string? DetailedFindings { get; set; }
    public string? Recommendations { get; set; }
    public string? AttachmentsURL { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Feedback DTO.</summary>
public class FeedbackDto
{
    public int FeedbackID { get; set; }
    public int MentorshipID { get; set; }
    public int? SessionID { get; set; }
    public string FromRole { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime? SubmittedAt { get; set; }
}
