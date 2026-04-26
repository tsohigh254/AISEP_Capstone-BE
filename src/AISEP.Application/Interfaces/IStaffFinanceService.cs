using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Staff;

namespace AISEP.Application.Interfaces;

public interface IStaffFinanceService
{
    Task<ApiResponse<StaffFinanceStatsDto>> GetFinanceOverviewAsync(string period = "30D", int page = 1, int pageSize = 10);
}
