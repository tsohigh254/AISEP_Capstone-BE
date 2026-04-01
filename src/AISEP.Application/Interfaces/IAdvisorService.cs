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

    //// Expertise
    //Task<ApiResponse<List<ExpertiseItemDto>>> UpdateExpertiseAsync(int userId, UpdateExpertiseRequest request);

    // Availability
    Task<ApiResponse<AvailabilityDto>> UpdateAvailabilityAsync(int userId, UpdateAvailabilityRequest request);

    // Search & Public info
    Task<ApiResponse<PagedResponse<AdvisorSearchItemDto>>> SearchAdvisorsAsync(AdvisorQueryParams advisorQueryParams);
    Task<ApiResponse<AdvisorDetailDto>> GetAdvisorDetailAsync(int advisorId);
}
