using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Staff;

namespace AISEP.Application.Interfaces;

public interface IStaffDashboardService
{
    Task<ApiResponse<StaffDashboardStatsDto>> GetDashboardStatsAsync();
    Task<ApiResponse<KycTrendDto>> GetKycTrendAsync(string period);
    Task<ApiResponse<List<ActivityFeedItemDto>>> GetActivityFeedAsync(int limit);
}
