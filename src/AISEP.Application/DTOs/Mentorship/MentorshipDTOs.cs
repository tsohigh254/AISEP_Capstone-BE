using Microsoft.AspNetCore.Http;
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
    public string? PreferredFormat { get; set; }
    public string? ExpectedDuration { get; set; }
    public string? ExpectedScope { get; set; }
    public List<RequestedSlotDto>? RequestedSlots { get; set; }
}

public class RequestedSlotDto
{   
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string? Timezone { get; set; }
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
    public string? SessionFormat { get; set; }
    public string? MeetingUrl { get; set; }
    /// <summary>Lời nhắn ghi chú của advisor khi đề xuất / lên lịch session.</summary>
    public string? Note { get; set; }
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
    /// <summary>Optional file attachment (PDF/DOCX/PNG, max 10 MB)</summary>
    public IFormFile? AttachmentFile { get; set; }
    /// <summary>If true, saves as Draft (reviewStatus = "Draft") and does not enter staff review queue.</summary>
    public bool IsDraft { get; set; } = false;
}

/// <summary>
/// Update a draft report. Only allowed when reviewStatus = "Draft".
/// Set IsDraft = false to submit officially (moves to PendingReview).
/// </summary>
public class UpdateReportRequest
{
    public string? ReportSummary { get; set; }
    public string? DetailedFindings { get; set; }
    public string? Recommendations { get; set; }
    public IFormFile? AttachmentFile { get; set; }
    /// <summary>Set to false to submit the draft officially for staff review.</summary>
    public bool IsDraft { get; set; } = true;
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
    /// <summary>URL logo của startup.</summary>
    public string? StartupLogoUrl { get; set; }
    /// <summary>Tên ngành của startup (null nếu chưa set).</summary>
    public string? StartupIndustry { get; set; }
    /// <summary>Giai đoạn startup: Idea | PreSeed | Seed | SeriesA | SeriesB | SeriesC | Growth (null nếu chưa set).</summary>
    public string? StartupStage { get; set; }
    public int AdvisorID { get; set; }
    public string AdvisorName { get; set; } = string.Empty;
    public string? AdvisorTitle { get; set; }
    public string? AdvisorPhotoURL { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ChallengeDescription { get; set; }
    public string? PreferredFormat { get; set; }
    public DateTime? RequestedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool HasReport { get; set; }
    public int ReportCount { get; set; }
    public DateTime? LatestReportSubmittedAt { get; set; }
    /// <summary>true khi có ít nhất 1 session đang chờ startup chọn slot (ProposedByAdvisor).</summary>
    public bool HasAdvisorProposedSlot { get; set; }
    /// <summary>Giá gốc Startup thanh toán.</summary>
    public decimal SessionAmount { get; set; }
    /// <summary>Phí nền tảng 15%.</summary>
    public decimal PlatformFeeAmount { get; set; }
    /// <summary>Số tiền Advisor thực nhận (= SessionAmount - PlatformFeeAmount). Đây là field dùng cho payout.</summary>
    public decimal ActualAmount { get; set; }
    /// <summary>true khi đủ điều kiện payout (all sessions Completed, all reports Passed, no dispute).</summary>
    public bool IsPayoutEligible { get; set; }
    /// <summary>Thời điểm Staff release payout. Null = chưa release.</summary>
    public DateTime? PayoutReleasedAt { get; set; }
    /// <summary>Thời lượng dự kiến (ví dụ: "60 minutes"). Dùng để tính giá dự kiến nếu SessionAmount = 0.</summary>
    public string? ExpectedDuration { get; set; }
    /// <summary>Phí tư vấn theo giờ của cố vấn tại thời điểm lấy danh sách.</summary>
    public decimal? AdvisorHourlyRate { get; set; }
}

/// <summary>Detail DTO with sessions, reports, feedbacks.</summary>
public class MentorshipDetailDto
{
    public int MentorshipID { get; set; }
    public int StartupID { get; set; }
    public int AdvisorID { get; set; }
    public string StartupName { get; set; } = string.Empty;
    public string? StartupLogoUrl { get; set; }
    public string? StartupIndustry { get; set; }
    public string? StartupStage { get; set; }
    public string AdvisorName { get; set; } = string.Empty;
    public string? AdvisorTitle { get; set; }
    public string? AdvisorPhotoURL { get; set; }
    public string MentorshipStatus { get; set; } = string.Empty;
    public string? ChallengeDescription { get; set; }
    public string? SpecificQuestions { get; set; }
    public string? PreferredFormat { get; set; }
    public string? ExpectedDuration { get; set; }
    public string? ExpectedScope { get; set; }
    public string? ObligationSummary { get; set; }
    public DateTime? RequestedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectedReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    /// <summary>"Startup" | "Advisor" | "System" — null chỉ tồn tại với data cũ trước khi có field này.</summary>
    public string? CancelledBy { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool CompletionConfirmedByStartup { get; set; }
    public bool CompletionConfirmedByAdvisor { get; set; }
    public decimal SessionAmount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;  // "Pending" | "Completed" | "Failed"
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    /// <summary>true khi tất cả sessions đã Completed, Startup đã xác nhận, và tất cả reports đã Passed — advisor đủ điều kiện nhận payout.</summary>
    public bool IsPayoutEligible { get; set; }
    /// <summary>Thời điểm Staff release payout vào AdvisorWallet. Null = chưa release, !null = đã release.</summary>
    public DateTime? PayoutReleasedAt { get; set; }
    public List<SessionDto> Sessions { get; set; } = new();
    public List<ReportDto> Reports { get; set; } = new();
    public List<FeedbackDto> Feedbacks { get; set; } = new();
    public List<TimelineEventDto> TimelineEvents { get; set; } = new();
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
    /// <summary>"Startup" | "Advisor" — ai là người đề xuất slot này ban đầu.</summary>
    public string? ProposedBy { get; set; }
    public string? TopicsDiscussed { get; set; }
    public string? KeyInsights { get; set; }
    public string? ActionItems { get; set; }
    public string? NextSteps { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? StartupConfirmedConductedAt { get; set; }
    public string? DisputeReason { get; set; }
    public string? ResolutionNote { get; set; }
    public int? MarkedByStaffID { get; set; }
    public DateTime? MarkedAt { get; set; }
}

public class SessionAdvisorDto
{
    public int AdvisorID { get; set; }
    public string? FullName { get; set; }
    public string? Title { get; set; }
    public string? ProfilePhotoURL { get; set; }
}

public class SessionListItemDto : SessionDto
{
    public int AdvisorID { get; set; }
    public string? AdvisorName { get; set; }
    public string? AdvisorProfilePhotoURL { get; set; }
    public SessionAdvisorDto? Advisor { get; set; }
    public int StartupID { get; set; }
    public string? StartupName { get; set; }
    /// <summary>Mô tả thách thức của mentorship cha — dùng làm displayTopic khi session chưa có topicsDiscussed.</summary>
    public string? MentorshipChallengeDescription { get; set; }
    /// <summary>true nếu đã có ít nhất 1 report gắn với session này.</summary>
    public bool HasReport { get; set; }
    /// <summary>Trạng thái của mentorship cha. Dùng để FE override display khi mentorship bị cancel.</summary>
    public string? MentorshipStatus { get; set; }
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
    public DateTime? StartupAcknowledgedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsLatestForSession { get; set; }
    public string ReviewStatus { get; set; } = string.Empty;
    public string? StaffReviewNote { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    /// <summary>= SubmittedAt + 24h. Null if report not yet submitted.</summary>
    public DateTime? IssueReportDeadlineAt { get; set; }
    /// <summary>True if now is within the 24h issue-report window.</summary>
    public bool CanSubmitIssueReport { get; set; }
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

/// <summary>
/// A single ordered event in the mentorship timeline.
/// type taxonomy: Requested | Accepted | InProgress | Rejected | Cancelled | Completed
/// actor taxonomy: Startup | Advisor | System
/// </summary>
public class TimelineEventDto
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public DateTime HappenedAt { get; set; }
}

// ============================= STAFF OVERSIGHT =============================

/// <summary>Staff review report request.</summary>
public class ReviewReportRequest
{
    public string ReviewStatus { get; set; } = null!;
    public string? Note { get; set; }
}

/// <summary>Resolve dispute request (session-level).</summary>
public class ResolveDisputeRequest
{
    public string Resolution { get; set; } = string.Empty;
    public bool RestoreCompleted { get; set; }
    public bool RefundToStartup { get; set; }
}

/// <summary>Staff mark session completed request.</summary>
public class StaffSessionNoteRequest
{
    public string? Note { get; set; }
}

/// <summary>Staff mark session dispute request.</summary>
public class StaffMarkDisputeRequest
{
    public string Reason { get; set; } = null!;
}

/// <summary>Report item in staff oversight queue.</summary>
public class ReportOversightDto
{
    public int ReportID { get; set; }
    public int MentorshipID { get; set; }
    public int? SessionID { get; set; }
    public int? AdvisorID { get; set; }
    public string? AdvisorName { get; set; }
    public int? StartupID { get; set; }
    public string? StartupName { get; set; }
    public string? ReportSummary { get; set; }
    public string? DetailedFindings { get; set; }
    public string? Recommendations { get; set; }
    public string? AttachmentsURL { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string ReviewStatus { get; set; } = string.Empty;
    public int? ReviewedByStaffID { get; set; }
    public string? StaffReviewNote { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int? SupersededByReportID { get; set; }
    public bool IsLatestForSession { get; set; }
    public string? SessionStatus { get; set; }
    public DateTime? StartupConfirmedConductedAt { get; set; }
    public DateTime? StartupAcknowledgedAt { get; set; }
    public string? MentorshipStatus { get; set; }
    public string? ChallengeDescription { get; set; }
}

/// <summary>Result after staff reviews report.</summary>
public class ReportReviewResultDto
{
    public int ReportID { get; set; }
    public int MentorshipID { get; set; }
    public string ReviewStatus { get; set; } = string.Empty;
    public string? StaffReviewNote { get; set; }
    public int? ReviewedByStaffID { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

/// <summary>Result after staff marks session completed/dispute/resolved.</summary>
public class SessionOversightResultDto
{
    public int SessionID { get; set; }
    public string SessionStatus { get; set; } = string.Empty;
    public string? DisputeReason { get; set; }
    public string? ResolutionNote { get; set; }
    public int MentorshipID { get; set; }
    public string MentorshipStatus { get; set; } = string.Empty;
    public bool IsPayoutEligible { get; set; }
    public int? MarkedByStaffID { get; set; }
    public DateTime? MarkedAt { get; set; }
}

/// <summary>Result after staff releases payout to AdvisorWallet.</summary>
public class ReleasePayoutResultDto
{
    public int MentorshipID { get; set; }
    /// <summary>Số tiền được credit vào AdvisorWallet (= ActualAmount của mentorship).</summary>
    public decimal CreditedAmount { get; set; }
    /// <summary>Thời điểm release. Luôn != null sau khi thành công.</summary>
    public DateTime PayoutReleasedAt { get; set; }
    /// <summary>isPayoutEligible vẫn = true sau release — dùng PayoutReleasedAt để phân biệt "eligible chưa release" vs "đã release".</summary>
    public bool IsPayoutEligible { get; set; }
    public int ReleasedByStaffID { get; set; }
}
