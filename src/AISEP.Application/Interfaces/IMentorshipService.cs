using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Mentorship;
using AISEP.Application.DTOs.Slot;
using AISEP.Application.QueryParams;

namespace AISEP.Application.Interfaces;

public interface IMentorshipService
{
    // Mentorship lifecycle
    Task<ApiResponse<MentorshipDto>> CreateRequestAsync(int userId, CreateMentorshipRequest request);
    Task<ApiResponse<PagedResponse<MentorshipListItemDto>>> GetMyMentorshipsAsync(int userId, string userType, MentorshipQueryParams query);
    Task<ApiResponse<MentorshipDetailDto>> GetDetailAsync(int userId, string userType, int mentorshipId);
    Task<ApiResponse<MentorshipDto>> AcceptAsync(int userId, int mentorshipId);
    Task<ApiResponse<MentorshipDto>> RejectAsync(int userId, int mentorshipId, string? reason);
    Task<ApiResponse<MentorshipDto>> CancelAsync(int userId, int mentorshipId, string? reason);
    
    // Sessions
    Task<ApiResponse<PagedResponse<SessionListItemDto>>> GetMySessionsAsync(int userId, string userType, string? status, int page, int pageSize);
    Task<ApiResponse<SessionDto>> CreateSessionAsync(int userId, int mentorshipId, CreateSessionRequest request);
    Task<ApiResponse<SessionDto>> UpdateSessionAsync(int userId, int sessionId, UpdateSessionRequest request);
    Task<ApiResponse<PagedResponse<SessionDto>>> GetSessions(int userId, string userType, SessionQueryParams query);

    // Reports & Feedback
    Task<ApiResponse<ReportDto>> CreateReportAsync(int userId, int mentorshipId, CreateReportRequest request);
    Task<ApiResponse<ReportDto>> GetReportAsync(int userId, string userType, int reportId);
    Task<ApiResponse<FeedbackDto>> CreateFeedbackAsync(int userId, int mentorshipId, CreateFeedbackRequest request);

    // Additional
    Task<ApiResponse<MentorshipDto>> CompleteAsync(int userId, int mentorshipId);
    Task<ApiResponse<List<SessionDto>>> GetMentorshipSessionsAsync(int userId, string userType, int mentorshipId);
    Task<ApiResponse<List<FeedbackDto>>> GetMentorshipFeedbacksAsync(int userId, string userType, int mentorshipId);

    // Available Slots
    Task<ApiResponse<AvailableSlotDto>> CreateAvailableSlotAsync(int userId, CreateAvailableSlotRequest request);
    Task<ApiResponse<List<AvailableSlotDto>>> CreateMultipleAvailableSlotsAsync(int userId, CreateMultipleAvailableSlotsRequest request);
    Task<ApiResponse<AvailableSlotDto>> UpdateAvailableSlotAsync(int userId, int slotId, UpdateAvailableSlotRequest request);
    Task<ApiResponse<string>> DeleteAvailableSlotAsync(int userId, int slotId);
    Task<ApiResponse<PagedResponse<AvailableSlotDto>>> GetMyAvailableSlotsAsync(int userId, AvailableSlotQueryParams queryParams);
    Task<ApiResponse<PagedResponse<AvailableSlotDto>>> GetAdvisorAvailableSlotsAsync(int advisorId, AvailableSlotQueryParams queryParams);
    Task<ApiResponse<SessionDto>> BookSessionFromSlotAsync(int userId, BookSessionFromSlotRequest request);
}
