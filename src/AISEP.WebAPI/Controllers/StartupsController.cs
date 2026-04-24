using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Investor;
using AISEP.Application.DTOs.Startup;
using AISEP.Application.DTOs.Bookmark;
using AISEP.Application.Interfaces;
using AISEP.Application.QueryParams;
using AISEP.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AISEP.WebAPI.Controllers;

/// <summary>
/// Startup profile &amp; team member management.
/// Owner endpoints (/me) require Startup role.
/// Public endpoints require authentication (any role).
/// </summary>
[ApiController]
[Route("api/startups")]
[Tags("Startups")]
public class StartupsController : ControllerBase
{
    private readonly IStartupService _startupService;
    private readonly IAdvisorBookmarkService _bookmarkService;

    public StartupsController(IStartupService startupService, IAdvisorBookmarkService bookmarkService)
    {
        _startupService = startupService;
        _bookmarkService = bookmarkService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    private string GetCurrentUserType()
        => User.FindFirst("userType")?.Value ?? string.Empty;

    // ================================================================
    // STARTUP OWNER ENDPOINTS (Role = Startup)
    // ================================================================

    /// <summary>
    /// Create startup profile for current user.
    /// Each Startup user can only have one profile.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/startups
    ///     {
    ///       "companyName": "AISEP Demo",
    ///       "oneLiner": "AI-powered Startup Ecosystem Platform",
    ///       "description": "We build tools for startups and investors...",
    ///       "industry": "Fintech",
    ///       "subIndustry": "Digital Wallets &amp; Payments",
    ///       "stage": "Seed",
    ///       "location": "HCMC",
    ///       "country": "Vietnam",
    ///       "website": "https://example.com",
    ///       "fundingAmountSought": 100000,
    ///       "currentFundingRaised": 0
    ///     }
    /// </remarks>
    [HttpPost]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<StartupMeDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<StartupMeDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<StartupMeDto>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateStartup([FromForm] CreateStartupRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _startupService.CreateStartupAsync(userId, request);
        if (!result.Success) return result.ToErrorResult();
        return result.ToCreatedEnvelope();
    }

    /// <summary>
    /// Get current user's startup profile (including team members)
    /// </summary>
    [HttpGet("me")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<StartupMeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<StartupMeDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyStartup()
    {
        var userId = GetCurrentUserId();
        var result = await _startupService.GetMyStartupAsync(userId);
        return result.ToActionResult();
    }

    /// <summary>
    /// Update current user's startup profile
    /// </summary>
    [HttpPut("me")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<StartupMeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<StartupMeDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMyStartup([FromForm] UpdateStartupRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _startupService.UpdateStartupAsync(userId, request);
        return result.ToActionResult();
    }

    /// <summary>
    /// Submit startup profile for approval (Draft → PendingApproval)
    /// </summary>
    [HttpPost("me/submit-for-approval")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<StartupMeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<StartupMeDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<StartupMeDto>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitForApproval()
    {
        var userId = GetCurrentUserId();
        var result = await _startupService.SubmitForApprovalAsync(userId);
        return result.ToActionResult();
    }

    [HttpPut("me/visibility")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleVisibility([FromBody] ToggleVisibilityRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _startupService.ToggleVisibilityAsync(userId, request.IsVisible);
        return result.ToActionResult();
    }

    // ================================================================
    // KYC / VERIFICATION ENDPOINTS
    // ================================================================

    /// <summary>
    /// Get current startup's KYC/Verification status
    /// </summary>
    [HttpGet("me/kyc/status")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<StartupKYCStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetKYCStatus()
    {
        var userId = GetCurrentUserId();
        var result = await _startupService.GetKYCStatusAsync(userId);
        return result.ToActionResult();
    }

    /// <summary>
    /// Submit startup KYC details for verification
    /// </summary>
    [HttpPost("me/kyc/submit")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<StartupKYCStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitKYC([FromForm] SubmitStartupKYCRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _startupService.SubmitKYCAsync(userId, request);
        return result.ToActionResult();
    }

    /// <summary>
    /// Save startup KYC details as draft
    /// </summary>
    [HttpPatch("me/kyc/draft")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<StartupKYCStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveKYCDraft([FromForm] SaveStartupKYCDraftRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _startupService.SaveKYCDraftAsync(userId, request);
        return result.ToActionResult();
    }

    // ================================================================
    // PUBLIC / MARKETPLACE ENDPOINTS (Authenticated users)
    // ================================================================

    /// <summary>
    /// Get startup by ID (public view for investors/advisors)
    /// </summary>
    /// <remarks>
    /// Sample response:
    ///
    ///     {
    ///       "success": true,
    ///       "data": {
    ///         "startupID": 10,
    ///         "companyName": "AISEP Demo",
    ///         "industry": "Fintech",
    ///         "stage": "Seed",
    ///         "teamMembers": [
    ///           { "fullName": "Tran A", "role": "CEO" }
    ///         ]
    ///       }
    ///     }
    /// </remarks>
    [HttpGet("{startupId:int}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<StartupPublicDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<StartupPublicDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStartupById(int startupId)
    {
        var result = await _startupService.GetStartupByIdAsync(startupId, GetCurrentUserId(), GetCurrentUserType());
        return result.ToActionResult();
    }

    /// <summary>
    /// Search/filter startups (for investors, advisors, staff, admin)
    /// </summary>
    /// <param name="q">Keyword search by company name or one-liner</param>
    /// <param name="industry">Filter by industry name</param>
    /// <param name="stage">Filter by stage</param>
    /// <param name="page">Page number (default 1)</param>
    /// <param name="pageSize">Items per page (default 20, max 100)</param>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<StartupListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchStartups([FromQuery] StartupQueryParams startupQuery)
    {
        var result = await _startupService.SearchStartupsAsync(startupQuery, GetCurrentUserType(), GetCurrentUserId());
        return result.ToActionResult();
    }

    // ================================================================
    // BROWSE INVESTORS (Startup role — discover investors)
    // ================================================================

    /// <summary>
    /// Search/filter investors for connection opportunities (Startup role only)
    /// </summary>
    /// <param name="keyword">Keyword search by name, firm or bio</param>
    /// <param name="stage">Filter by preferred stage (e.g. Seed, Series A)</param>
    /// <param name="industry">Filter by preferred industry name</param>
    /// <param name="sortBy">Sort: recent (default)</param>
    /// <param name="page">Page number (default 1)</param>
    /// <param name="pageSize">Items per page (default 12, max 100)</param>
    [HttpGet("investors")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<InvestorSearchItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchInvestors([FromQuery] InvestorQueryParams investorQuery)
    {
        var result = await _startupService.SearchInvestorsAsync(investorQuery, GetCurrentUserId());
        return result.ToActionResult();
    }

    /// <summary>
    /// Get investor profile by ID (Startup role only)
    /// </summary>
    [HttpGet("investors/{investorId:int}")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<InvestorDetailForStartupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InvestorDetailForStartupDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInvestorById(int investorId)
    {
        var result = await _startupService.GetInvestorByIdAsync(investorId);
        return result.ToActionResult();
    }

    // ================================================================
    // TEAM MEMBER ENDPOINTS (Owner only)
    // ================================================================

    /// <summary>
    /// Get all team members of current user's startup
    /// </summary>
    [HttpGet("me/team-members")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<List<TeamMemberDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<TeamMemberDto>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTeamMembers()
    {
        var userId = GetCurrentUserId();
        var result = await _startupService.GetTeamMembersAsync(userId);
        return result.ToActionResult();
    }

    /// <summary>
    /// Add a team member to current user's startup
    /// </summary>
    [HttpPost("me/team-members")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<TeamMemberDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<TeamMemberDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<TeamMemberDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddTeamMember([FromForm] CreateTeamMemberRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _startupService.AddTeamMemberAsync(userId, request);
        if (!result.Success) return result.ToErrorResult();
        return result.ToCreatedEnvelope();
    }

    /// <summary>
    /// Update a team member of current user's startup
    /// </summary>
    [HttpPut("me/team-members/{teamMemberId:int}")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<TeamMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TeamMemberDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTeamMember(int teamMemberId, [FromForm] UpdateTeamMemberRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _startupService.UpdateTeamMemberAsync(userId, teamMemberId, request);
        return result.ToActionResult();
    }

    /// <summary>
    /// Delete a team member from current user's startup
    /// </summary>
    [HttpDelete("me/team-members/{teamMemberId:int}")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTeamMember(int teamMemberId)
    {
        var userId = GetCurrentUserId();
        var result = await _startupService.DeleteTeamMemberAsync(userId, teamMemberId);
        return result.ToActionResult();
    }

    // ================================================================
    // ADVISOR BOOKMARK ENDPOINTS  (Role = Startup)
    // ================================================================

    /// <summary>
    /// Bookmark an advisor.
    /// Requires: email verified (MSG012), KYC approved (MSG005), advisor found (MSG077).
    /// Returns MSG004 if already bookmarked, MSG001 on success.
    /// </summary>
    [HttpPost("me/advisor-bookmarks")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<AdvisorBookmarkDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<AdvisorBookmarkDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AdvisorBookmarkDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<AdvisorBookmarkDto>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> BookmarkAdvisor([FromBody] BookmarkAdvisorRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _bookmarkService.CreateBookmarkAsync(userId, request.AdvisorId);
        if (!result.Success) return result.ToErrorResult();
        return result.ToCreatedEnvelope();
    }

    /// <summary>
    /// Get paginated list of bookmarked advisors for the current startup.
    /// </summary>
    [HttpGet("me/advisor-bookmarks")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<AdvisorBookmarkListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyAdvisorBookmarks(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        var result = await _bookmarkService.GetMyBookmarksAsync(userId, page, pageSize);
        return result.ToPagedEnvelope();
    }

    /// <summary>
    /// Return the list of advisor IDs bookmarked by the current startup.
    /// Lightweight endpoint for FE to check saved status in bulk.
    /// </summary>
    [HttpGet("me/advisor-bookmarks/ids")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<BookmarkedAdvisorIdsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBookmarkedAdvisorIds()
    {
        var userId = GetCurrentUserId();
        var result = await _bookmarkService.GetBookmarkedAdvisorIdsAsync(userId);
        return result.ToActionResult();
    }

    /// <summary>
    /// Remove (unsave) an advisor bookmark.
    /// </summary>
    [HttpDelete("me/advisor-bookmarks/{advisorId:int}")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAdvisorBookmark(int advisorId)
    {
        var userId = GetCurrentUserId();
        var result = await _bookmarkService.DeleteBookmarkAsync(userId, advisorId);
        if (!result.Success) return result.ToErrorResult();
        return result.ToDeletedEnvelope();
    }

    // ================================================================
    // INTERESTED INVESTORS (Role = Startup)
    // ================================================================

    /// <summary>
    /// Get list of investors who have added this startup to their watchlist (passive interest signals).
    /// Only returns investors with an Approved profile and an active watchlist entry.
    /// Validates: email verified (MSG012), profile complete (MSG041).
    /// Returns NO_INTERESTED_INVESTORS when no results are found.
    /// </summary>
    [HttpGet("me/interested-investors")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<InterestedInvestorDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInterestedInvestors(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? keyword = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var userId = GetCurrentUserId();
        var result = await _startupService.GetInterestedInvestorsAsync(
            userId, page, pageSize, keyword, sortBy, fromDate, toDate);
        if (!result.Success) return result.ToErrorResult();
        return result.ToPagedEnvelope();
    }
}
