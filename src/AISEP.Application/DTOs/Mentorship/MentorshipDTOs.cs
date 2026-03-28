using AISEP.Domain.Enums;

namespace AISEP.Application.DTOs.Mentorship;

// ============================= REQUEST DTOs =============================

/// <summary>
/// Create a new mentorship request (Startup → Advisor).
/// Example: { "advisorId": 1, "challengeDescription": "Go-to-market strategy for B2B SaaS",
///            "specificQuestions": "How to price our product?", "preferredFormat": "Video",
///            "expectedDuration": "3 months", "expectedScope": "Weekly 1h sessions" }
/// </summary>
public class CreateMentorshipRequest
{
    public int AdvisorId { get; set; }
    public string ChallengeDescription { get; set; } = null!;
    public string? SpecificQuestions { get; set; }
    public string? ExpectedDuration { get; set; }
    public string? ExpectedScope { get; set; }
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
/// Create a session within an accepted mentorship (Advisor only).
/// Example: { "scheduledStartAt": "2026-03-01T10:00:00Z", "durationMinutes": 60,
///            "sessionFormat": "Video", "meetingUrl": "https://meet.google.com/abc" }
/// </summary>
public class CreateSessionRequest
{
    public DateTime ScheduledStartAt { get; set; }
    public int DurationMinutes { get; set; }
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
    public string? MeetingUrl { get; set; }
    public SessionStatus SessionStatus { get; set; }
    public string? TopicsDiscussed { get; set; }
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

/// <summary>Basic mentorship DTO returned after create/update.</summary>
public class MentorshipDto
{
    public int MentorshipID { get; set; }
    public int StartupID { get; set; }
    public int AdvisorID { get; set; }
    public string MentorshipStatus { get; set; } = string.Empty;
    public string? ChallengeDescription { get; set; }
    public string? SpecificQuestions { get; set; }
    public string? PreferredFormat { get; set; }
    public string? ExpectedDuration { get; set; }
    public string? ExpectedScope { get; set; }
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
    public int AdvisorID { get; set; }
    public string AdvisorName { get; set; } = string.Empty;
    public string MentorshipStatus { get; set; } = string.Empty;
    public string? ChallengeDescription { get; set; }
    public DateTime? RequestedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Detail DTO with sessions, reports, feedbacks.</summary>
public class MentorshipDetailDto
{
    public int MentorshipID { get; set; }
    public int StartupID { get; set; }
    public string StartupName { get; set; } = string.Empty;
    public int AdvisorID { get; set; }
    public string AdvisorName { get; set; } = string.Empty;
    public string MentorshipStatus { get; set; } = string.Empty;
    public string? ChallengeDescription { get; set; }
    public string? ExpectedDuration { get; set; }
    public DateTime? RequestedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectedReason { get; set; }
    public bool CompletionConfirmedByStartup { get; set; }
    public bool CompletionConfirmedByAdvisor { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Session DTO.</summary>
public class SessionDto
{
    public int SessionID { get; set; }
    public int MentorshipID { get; set; }
    public DateTime? ScheduledStartAt { get; set; }
    public int? DurationMinutes { get; set; }
    public string? MeetingURL { get; set; }
    public string SessionStatus { get; set; }
    public string? TopicsDiscussed { get; set; }
    public DateTime CreatedAt { get; set; }
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
