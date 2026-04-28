using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Mentorship;
using AISEP.Application.QueryParams;

namespace AISEP.Application.Interfaces;

public interface IMentorshipService
{
    // Mentorship lifecycle
    Task<ApiResponse<MentorshipDto>> CreateRequestAsync(int userId, CreateMentorshipRequest request);
    Task<ApiResponse<PagedResponse<MentorshipListItemDto>>> GetMyMentorshipsAsync(
        int userId, string userType, string? status, int page, int pageSize, bool? isPayoutEligible = null);
    Task<ApiResponse<MentorshipDetailDto>> GetDetailAsync(int userId, string userType, int mentorshipId);
    Task<ApiResponse<MentorshipDetailDto>> GetMentorshipBySessionIdAsync(int userId, string userType, int sessionId);
    Task<ApiResponse<MentorshipDetailDto>> GetMentorshipByReportIdAsync(int userId, string userType, int reportId);
    Task<ApiResponse<MentorshipDto>> AcceptAsync(int userId, int mentorshipId);
    Task<ApiResponse<MentorshipDto>> RejectAsync(int userId, int mentorshipId, string? reason);
    Task<ApiResponse<MentorshipDto>> CancelAsync(int userId, int mentorshipId, string? reason);
    Task<ApiResponse<MentorshipDto>> CompleteAsync(int userId, int mentorshipId);
    Task<ApiResponse<SessionDto>> CreateSessionAsync(int userId, int mentorshipId, CreateSessionRequest request);
    Task<ApiResponse<SessionDto>> UpdateSessionAsync(int userId, int sessionId, UpdateSessionRequest request);
    Task<ApiResponse<SessionDto>> ConfirmSessionAsync(int userId, int mentorshipId, int sessionId);
    Task<ApiResponse<SessionDto>> AcceptSessionAsync(int userId, int mentorshipId, int sessionId);

    // Reports
    Task<ApiResponse<ReportDto>> CreateReportAsync(int userId, int mentorshipId, CreateReportRequest request);
    Task<ApiResponse<ReportDto>> UpdateReportAsync(int userId, int mentorshipId, int reportId, UpdateReportRequest request);
    Task<ApiResponse<ReportDto>> GetReportAsync(int userId, string userType, int reportId);
    Task<ApiResponse<ReportDto>> AcknowledgeReportAsync(int userId, int mentorshipId, int reportId);

    // Feedback
    Task<ApiResponse<FeedbackDto>> CreateFeedbackAsync(int userId, int mentorshipId, CreateFeedbackRequest request);

    // Additional
    Task<ApiResponse<List<SessionDto>>> GetMentorshipSessionsAsync(int userId, string userType, int mentorshipId);
    Task<ApiResponse<PagedResponse<SessionListItemDto>>> GetMySessionsAsync(int userId, string userType, string? status, int page, int pageSize);
    Task<ApiResponse<List<FeedbackDto>>> GetMentorshipFeedbacksAsync(int userId, string userType, int mentorshipId);

    // Staff oversight — Report review
    Task<ApiResponse<PagedResponse<ReportOversightDto>>> GetReportsForOversightAsync(
        string? reviewStatus, int? advisorId, int? startupId,
        DateTime? from, DateTime? to, int page, int pageSize);
    Task<ApiResponse<ReportReviewResultDto>> ReviewReportAsync(
        int staffUserId, int reportId, ReviewReportRequest request);

    // Startup — Confirm conducted
    Task<ApiResponse<SessionDto>> ConfirmConductedAsync(
        int userId, int mentorshipId, int sessionId);

    // Staff oversight — Payout release
    Task<ApiResponse<ReleasePayoutResultDto>> ReleasePayoutAsync(int staffUserId, int mentorshipId);

    // Staff oversight — Session actions
    Task<ApiResponse<SessionOversightResultDto>> MarkSessionCompletedAsync(
        int staffUserId, int mentorshipId, int sessionId, string? note);
    Task<ApiResponse<SessionOversightResultDto>> MarkSessionDisputeAsync(
        int staffUserId, int mentorshipId, int sessionId, string reason);
    Task<ApiResponse<SessionOversightResultDto>> MarkSessionResolvedAsync(
        int staffUserId, int mentorshipId, int sessionId, ResolveDisputeRequest request);
}
