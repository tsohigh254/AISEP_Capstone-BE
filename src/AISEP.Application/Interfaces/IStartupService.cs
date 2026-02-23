using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Startup;

namespace AISEP.Application.Interfaces;

public interface IStartupService
{
    // Startup profile (owner)
    Task<ApiResponse<StartupMeDto>> CreateStartupAsync(int userId, CreateStartupRequest request);
    Task<ApiResponse<StartupMeDto>> GetMyStartupAsync(int userId);
    Task<ApiResponse<StartupMeDto>> UpdateStartupAsync(int userId, UpdateStartupRequest request);
    Task<ApiResponse<StartupMeDto>> SubmitForApprovalAsync(int userId);

    // Public (investors/advisors)
    Task<ApiResponse<StartupPublicDto>> GetStartupByIdAsync(int startupId);
    Task<ApiResponse<PagedResponse<StartupListItemDto>>> SearchStartupsAsync(
        string? keyword, string? industry, string? stage,
        int page, int pageSize);

    // Team members (owner)
    Task<ApiResponse<List<TeamMemberDto>>> GetTeamMembersAsync(int userId);
    Task<ApiResponse<TeamMemberDto>> AddTeamMemberAsync(int userId, CreateTeamMemberRequest request);
    Task<ApiResponse<TeamMemberDto>> UpdateTeamMemberAsync(int userId, int teamMemberId, UpdateTeamMemberRequest request);
    Task<ApiResponse<string>> DeleteTeamMemberAsync(int userId, int teamMemberId);
}
