using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Investor;

namespace AISEP.Application.Interfaces;

public interface IInvestorService
{
    // Profile
    Task<ApiResponse<InvestorDto>> CreateProfileAsync(int userId, CreateInvestorRequest request);
    Task<ApiResponse<InvestorDto>> GetMyProfileAsync(int userId);
    Task<ApiResponse<InvestorDto>> UpdateProfileAsync(int userId, UpdateInvestorRequest request);
    Task<ApiResponse<InvestorDto>> SubmitForApprovalAsync(int userId);

    // Preferences
    Task<ApiResponse<PreferencesDto>> GetPreferencesAsync(int userId);
    Task<ApiResponse<PreferencesDto>> UpdatePreferencesAsync(int userId, UpdatePreferencesRequest request);

    // Watchlist
    Task<ApiResponse<WatchlistItemDto>> AddToWatchlistAsync(int userId, WatchlistAddRequest request);
    Task<ApiResponse<PagedResponse<WatchlistItemDto>>> GetWatchlistAsync(int userId, int page, int pageSize);
    Task<ApiResponse<string>> RemoveFromWatchlistAsync(int userId, int startupId);

    // Search startups
    Task<ApiResponse<PagedResponse<StartupSearchItemDto>>> SearchStartupsAsync(
        string? q, int? industryId, string? stage, string? location,
        string? sortBy, int page, int pageSize);

    // Industry focus
    Task<ApiResponse<List<IndustryFocusDto>>> GetIndustryFocusAsync(int userId);
    Task<ApiResponse<IndustryFocusDto>> AddIndustryFocusAsync(int userId, AddIndustryFocusRequest request);
    Task<ApiResponse<string>> RemoveIndustryFocusAsync(int userId, int focusId);

    // Stage focus
    Task<ApiResponse<List<StageFocusDto>>> GetStageFocusAsync(int userId);
    Task<ApiResponse<StageFocusDto>> AddStageFocusAsync(int userId, AddStageFocusRequest request);
    Task<ApiResponse<string>> RemoveStageFocusAsync(int userId, int stageFocusId);

    // Compare
    Task<ApiResponse<List<StartupCompareDto>>> CompareStartupsAsync(List<int> startupIds);
}
