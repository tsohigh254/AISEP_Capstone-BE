using AISEP.Application.DTOs.Advisor;
using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.QueryParams;

namespace AISEP.Application.Interfaces;

public interface IAdvisorService
{
    // Profile
    Task<ApiResponse<AdvisorMeDto>> CreateProfileAsync(int userId, CreateAdvisorRequest request);
    Task<ApiResponse<AdvisorMeDto>> GetMyProfileAsync(int userId);
    Task<ApiResponse<AdvisorMeDto>> UpdateProfileAsync(int userId, UpdateAdvisorRequest request);

    //// Expertise
    //Task<ApiResponse<List<ExpertiseItemDto>>> UpdateExpertiseAsync(int userId, UpdateExpertiseRequest request);

    // Availability
    Task<ApiResponse<AvailabilityDto>> UpdateAvailabilityAsync(int userId, UpdateAvailabilityRequest request);

    // Search
    Task<ApiResponse<PagedResponse<AdvisorSearchItemDto>>> SearchAdvisorsAsync(AdvisorQueryParams advisorQueryParams);
}
