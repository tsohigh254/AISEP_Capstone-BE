using AISEP.Application.DTOs.Advisor;
using AISEP.Application.DTOs.Common;
using AISEP.Application.QueryParams;

namespace AISEP.Application.Interfaces;

public interface IAdvisorService
{
    // Profile
    Task<ApiResponse<AdvisorMeDto>> CreateProfileAsync(int userId, CreateAdvisorRequest request);
    Task<ApiResponse<AdvisorMeDto>> GetMyProfileAsync(int userId);
    Task<ApiResponse<AdvisorMeDto>> UpdateProfileAsync(int userId, UpdateAdvisorRequest request);
    Task<ApiResponse<AdvisorMeDto>> SubmitForApprovalAsync(int userId);
    Task<ApiResponse<AdvisorKYCStatusDto>> GetKYCStatusAsync(int userId);
    Task<ApiResponse<AdvisorKYCStatusDto>> SubmitKYCAsync(int userId, SubmitAdvisorKYCRequest request);
    Task<ApiResponse<AdvisorKYCStatusDto>> SaveKYCDraftAsync(int userId, SaveAdvisorKYCDraftRequest request);

    //// Expertise
    //Task<ApiResponse<List<ExpertiseItemDto>>> UpdateExpertiseAsync(int userId, UpdateExpertiseRequest request);

    // Availability
    Task<ApiResponse<AvailabilityDto>> UpdateAvailabilityAsync(int userId, UpdateAvailabilityRequest request);

    // Time slots
    Task<ApiResponse<List<TimeSlotDto>>> GetTimeSlotsAsync(int userId);
    Task<ApiResponse<List<TimeSlotDto>>> UpsertTimeSlotsAsync(int userId, UpsertTimeSlotsRequest request);

    // Search & Public info
    Task<ApiResponse<PagedResponse<AdvisorSearchItemDto>>> SearchAdvisorsAsync(AdvisorQueryParams advisorQueryParams);
    Task<ApiResponse<AdvisorDetailDto>> GetAdvisorDetailAsync(int advisorId, string userType = "");

    // Feedback management (advisor-facing)
    Task<ApiResponse<PagedResponse<AdvisorFeedbackListItemDto>>> GetMyFeedbacksAsync(int userId, int? ratingFilter, string? sort, int page, int pageSize);
    Task<ApiResponse<AdvisorFeedbackSummaryDto>> GetMyFeedbackSummaryAsync(int userId);
    Task<ApiResponse<FeedbackResponseDto>> RespondToFeedbackAsync(int userId, int feedbackId, RespondToFeedbackRequest request);
}
