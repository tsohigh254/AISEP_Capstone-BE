using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.MasterData;

namespace AISEP.Application.Interfaces;

public interface IStaffMasterDataService
{
    // Industry Management
    Task<ApiResponse<List<IndustryDto>>> GetAllIndustriesAsync();
    Task<ApiResponse<IndustryDto>> CreateIndustryAsync(ManageIndustryRequest request);
    Task<ApiResponse<IndustryDto>> UpdateIndustryAsync(int id, ManageIndustryRequest request);
    Task<ApiResponse<string>> DeleteIndustryAsync(int id);

    // Stage Management
    Task<ApiResponse<List<StartupStageDto>>> GetAllStagesAsync();
    Task<ApiResponse<StartupStageDto>> CreateStageAsync(ManageStageRequest request);
    Task<ApiResponse<StartupStageDto>> UpdateStageAsync(int id, ManageStageRequest request);
    Task<ApiResponse<string>> DeleteStageAsync(int id);
    Task<ApiResponse<string>> ReorderStagesAsync(ReorderStageRequest request);
}
