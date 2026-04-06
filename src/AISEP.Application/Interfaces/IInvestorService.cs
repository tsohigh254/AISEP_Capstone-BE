using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Investor;

namespace AISEP.Application.Interfaces;

public interface IInvestorService
{
    // Profile
    Task<ApiResponse<InvestorDto>> CreateProfileAsync(int userId, CreateInvestorRequest request);
    Task<ApiResponse<InvestorDto>> GetMyProfileAsync(int userId);
    Task<ApiResponse<InvestorDto>> UpdateProfileAsync(int userId, UpdateInvestorRequest request);
    Task<ApiResponse<InvestorDto>> UploadPhotoAsync(int userId, Microsoft.AspNetCore.Http.IFormFile photo);
    Task<ApiResponse<InvestorDto>> SubmitForApprovalAsync(int userId);

    // KYC
    Task<ApiResponse<InvestorKYCStatusDto>> GetKYCStatusAsync(int userId);
    Task<ApiResponse<InvestorKYCStatusDto>> SubmitKYCAsync(int userId, SubmitInvestorKYCRequest request, string? idProofUrl, string? investmentProofUrl);
    Task<ApiResponse<InvestorKYCStatusDto>> SaveKYCDraftAsync(int userId, SaveInvestorKYCDraftRequest request);

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
}
