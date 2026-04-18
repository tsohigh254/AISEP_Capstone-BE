using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Investor;
using AISEP.Application.DTOs.Startup;
using AISEP.Application.QueryParams;

namespace AISEP.Application.Interfaces;

public interface IStartupService
{
    // Startup profile (owner)
    Task<ApiResponse<StartupMeDto>> CreateStartupAsync(int userId, CreateStartupRequest request);
    Task<ApiResponse<StartupMeDto>> GetMyStartupAsync(int userId);
    Task<ApiResponse<StartupMeDto>> UpdateStartupAsync(int userId, UpdateStartupRequest request);
    Task<ApiResponse<StartupMeDto>> SubmitForApprovalAsync(int userId);
    Task<ApiResponse<string>> ToggleVisibilityAsync(int userId, bool isVisible);

    // KYC
    Task<ApiResponse<StartupKYCStatusDto>> GetKYCStatusAsync(int userId);
    Task<ApiResponse<StartupKYCStatusDto>> SubmitKYCAsync(int userId, SubmitStartupKYCRequest request);
    Task<ApiResponse<StartupKYCStatusDto>> SaveKYCDraftAsync(int userId, SaveStartupKYCDraftRequest request);

    // Public (investors/advisors)
    Task<ApiResponse<StartupPublicDto>> GetStartupByIdAsync(int startupId, int requestingUserId, string userType);
    // requestingUserId: the current caller's user id (used to determine connection status)
    Task<ApiResponse<PagedResponse<StartupListItemDto>>> SearchStartupsAsync(StartupQueryParams startupQuery, int requestingUserId, string userType);

    // Browse investors (Startup role)
    Task<ApiResponse<PagedResponse<InvestorSearchItemDto>>> SearchInvestorsAsync(InvestorQueryParams investorQuery, int requestingUserId);
    Task<ApiResponse<InvestorDetailForStartupDto>> GetInvestorByIdAsync(int investorId);

    // Team members (owner)
    Task<ApiResponse<List<TeamMemberDto>>> GetTeamMembersAsync(int userId);
    Task<ApiResponse<TeamMemberDto>> AddTeamMemberAsync(int userId, CreateTeamMemberRequest request);
    Task<ApiResponse<TeamMemberDto>> UpdateTeamMemberAsync(int userId, int teamMemberId, UpdateTeamMemberRequest request);
    Task<ApiResponse<string>> DeleteTeamMemberAsync(int userId, int teamMemberId);
}
