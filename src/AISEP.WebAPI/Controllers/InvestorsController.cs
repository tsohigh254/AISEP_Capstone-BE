using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Investor;
using AISEP.Application.Interfaces;
using AISEP.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AISEP.WebAPI.Controllers;

/// <summary>
/// Investor profile, preferences, watchlist &amp; startup search.
/// All endpoints require Investor role unless otherwise noted.
/// </summary>
[ApiController]
[Route("api/investors")]
[Tags("Investors")]
[Authorize(Policy = "InvestorOnly")]
public class InvestorsController : ControllerBase
{
    private readonly IInvestorService _investorService;

    public InvestorsController(IInvestorService investorService)
    {
        _investorService = investorService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    // ================================================================
    // PROFILE
    // ================================================================

    /// <summary>
    /// Create investor profile for current user.
    /// Each user can only have one investor profile.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/investors
    ///     {
    ///       "fullName": "Nguyen Van A",
    ///       "firmName": "ABC Capital",
    ///       "title": "Managing Partner",
    ///       "bio": "10+ years in VC investing...",
    ///       "investmentThesis": "Focus on B2B SaaS in SEA",
    ///       "location": "HCMC",
    ///       "country": "Vietnam",
    ///       "website": "https://abccapital.vn"
    ///     }
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<InvestorDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<InvestorDto>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateProfile([FromBody] CreateInvestorRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _investorService.CreateProfileAsync(userId, request);
        if (!result.Success) return result.ToErrorResult();
        return result.ToCreatedEnvelope();
    }

    /// <summary>
    /// Get current investor's profile
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<InvestorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InvestorDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = GetCurrentUserId();
        var result = await _investorService.GetMyProfileAsync(userId);
        return result.ToActionResult();
    }

    /// <summary>
    /// Update current investor's profile
    /// </summary>
    /// <remarks>
    /// Only fields provided (non-null) will be updated. Send null to keep existing value.
    ///
    ///     PUT /api/investors/me
    ///     {
    ///       "firmName": "ABC Capital (new name)",
    ///       "bio": "Updated bio text"
    ///     }
    /// </remarks>
    [HttpPut("me")]
    [ProducesResponseType(typeof(ApiResponse<InvestorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InvestorDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateInvestorRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _investorService.UpdateProfileAsync(userId, request);
        return result.ToActionResult();
    }

    /// <summary>
    /// Upload or update the investor's profile photo (Avatar)
    /// </summary>
    [HttpPost("me/photo")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<InvestorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InvestorDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<InvestorDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadPhoto(IFormFile photo)
    {
        var userId = GetCurrentUserId();
        var result = await _investorService.UploadPhotoAsync(userId, photo);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get current investor's KYC status and submitted data
    /// </summary>
    [HttpGet("me/kyc")]
    [ProducesResponseType(typeof(ApiResponse<InvestorKYCStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetKYCStatus()
    {
        var userId = GetCurrentUserId();
        var result = await _investorService.GetKYCStatusAsync(userId);
        return result.ToActionResult();
    }

    /// <summary>
    /// Submit investor profile and documents for KYC approval
    /// </summary>
    [HttpPost("me/kyc/submit")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<InvestorKYCStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitKYC([FromForm] SubmitInvestorKYCRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _investorService.SubmitKYCAsync(userId, request);
        return result.ToActionResult();
    }

    /// <summary>
    /// Save investor KYC draft
    /// </summary>
    [HttpPost("me/kyc/draft")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<InvestorKYCStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveKYCDraft([FromForm] SaveInvestorKYCDraftRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _investorService.SaveKYCDraftAsync(userId, request);
        return result.ToActionResult();
    }

    // ================================================================
    // PREFERENCES
    // ================================================================

    [HttpGet("me/preferences")]
    [ProducesResponseType(typeof(ApiResponse<PreferencesDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPreferences()
    {
        var userId = GetCurrentUserId();
        var result = await _investorService.GetPreferencesAsync(userId);
        return result.ToActionResult();
    }

    /// <summary>
    /// Update investor's investment preferences
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     PUT /api/investors/me/preferences
    ///     {
    ///       "ticketMin": 50000,
    ///       "ticketMax": 500000,
    ///       "preferredStages": ["Seed", "Series A"],
    ///       "preferredIndustries": ["Fintech", "HealthTech"],
    ///       "preferredGeographies": "Vietnam, Singapore"
    ///     }
    /// </remarks>
    [HttpPut("me/preferences")]
    [ProducesResponseType(typeof(ApiResponse<PreferencesDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PreferencesDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferencesRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _investorService.UpdatePreferencesAsync(userId, request);
        return result.ToActionResult();
    }

    // ================================================================
    // WATCHLIST
    // ================================================================

    /// <summary>
    /// Add a startup to investor's watchlist
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/investors/me/watchlist
    ///     {
    ///       "startupId": 1,
    ///       "watchReason": "Interesting AI product",
    ///       "priority": "High"
    ///     }
    /// </remarks>
    [HttpPost("me/watchlist")]
    [ProducesResponseType(typeof(ApiResponse<WatchlistItemDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<WatchlistItemDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<WatchlistItemDto>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddToWatchlist([FromBody] WatchlistAddRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _investorService.AddToWatchlistAsync(userId, request);
        if (!result.Success) return result.ToErrorResult();
        return result.ToCreatedEnvelope();
    }

    /// <summary>
    /// Get investor's watchlist (paginated)
    /// </summary>
    /// <param name="page">Page number (default 1)</param>
    /// <param name="pageSize">Items per page (default 20, max 100)</param>
    [HttpGet("me/watchlist")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<WatchlistItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWatchlist([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        var result = await _investorService.GetWatchlistAsync(userId, page, pageSize);
        return result.ToActionResult();
    }

    /// <summary>
    /// Remove a startup from investor's watchlist
    /// </summary>
    [HttpDelete("me/watchlist/{startupId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFromWatchlist(int startupId)
    {
        var userId = GetCurrentUserId();
        var result = await _investorService.RemoveFromWatchlistAsync(userId, startupId);
        if (!result.Success) return result.ToErrorResult();
        return ApiEnvelopeExtensions.DeletedEnvelope("Removed from watchlist");
    }

    // ================================================================
    // SEARCH STARTUPS
    // ================================================================

    /// <summary>
    /// Search/filter startups for investment opportunities
    /// </summary>
    /// <param name="q">Keyword to search in company name, one-liner, description</param>
    /// <param name="industryId">Filter by industry ID (from master data)</param>
    /// <param name="stage">Filter by stage (e.g. Seed, Series A)</param>
    /// <param name="location">Filter by location/country</param>
    /// <param name="minScore">Min AI score (currently ignored - AI module pending)</param>
    /// <param name="sortBy">Sort field: updatedAt (default), createdAt, name</param>
    /// <param name="page">Page number (default 1)</param>
    /// <param name="pageSize">Items per page (default 20, max 100)</param>
    [HttpGet("search")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<StartupSearchItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchStartups(
        [FromQuery] string? q,
        [FromQuery] int? industryId,
        [FromQuery] string? stage,
        [FromQuery] string? location,
        [FromQuery] float? minScore,
        [FromQuery] string? sortBy,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // minScore is accepted but ignored until AI module is implemented
        var result = await _investorService.SearchStartupsAsync(q, industryId, stage, location, sortBy, page, pageSize);
        return result.ToActionResult();
    }

    // ================================================================
    // RECOMMENDATIONS (PLACEHOLDER)
    // ================================================================

    [AllowAnonymous]
    [HttpGet("debug-investor")]
    public IActionResult DebugInvestor([FromServices] AISEP.Infrastructure.Data.ApplicationDbContext db, [FromQuery] string email = "investor@aisep.local")
    {
        var user = db.Users.FirstOrDefault(u => u.Email == email);
        if (user == null) return NotFound($"User {email} not found");

        var investor = db.Investors.FirstOrDefault(i => i.UserID == user.UserID);
        if (investor == null) return NotFound($"Investor profile for {email} not found");

        return Ok(new {
            User = new { user.UserID, user.Email, user.IsActive, user.UserType },
            Investor = new {
                investor.InvestorID,
                investor.FullName,
                investor.FirmName,
                investor.Title,
                investor.Bio,
                investor.ProfileStatus,
                investor.Location,
                investor.CreatedAt,
                investor.UpdatedAt
            }
        });
    }


    // ================================================================
    // INDUSTRY FOCUS
    // ================================================================

    /// <summary>Get investor's industry focus list.</summary>
    [HttpGet("me/industry-focus")]
    public async Task<IActionResult> GetIndustryFocus()
    {
        var result = await _investorService.GetIndustryFocusAsync(GetCurrentUserId());
        return result.ToActionResult();
    }

    /// <summary>Add an industry focus.</summary>
    [HttpPost("me/industry-focus")]
    public async Task<IActionResult> AddIndustryFocus([FromBody] AddIndustryFocusRequest request)
    {
        var result = await _investorService.AddIndustryFocusAsync(GetCurrentUserId(), request);
        if (!result.Success) return result.ToErrorResult();
        return result.ToCreatedEnvelope();
    }

    /// <summary>Remove an industry focus.</summary>
    [HttpDelete("me/industry-focus/{focusId:int}")]
    public async Task<IActionResult> RemoveIndustryFocus(int focusId)
    {
        var result = await _investorService.RemoveIndustryFocusAsync(GetCurrentUserId(), focusId);
        if (!result.Success) return result.ToErrorResult();
        return ApiEnvelopeExtensions.DeletedEnvelope("Industry focus removed");
    }

    // ================================================================
    // STAGE FOCUS
    // ================================================================

    /// <summary>Get investor's stage focus list.</summary>
    [HttpGet("me/stage-focus")]
    public async Task<IActionResult> GetStageFocus()
    {
        var result = await _investorService.GetStageFocusAsync(GetCurrentUserId());
        return result.ToActionResult();
    }

    /// <summary>Add a stage focus.</summary>
    [HttpPost("me/stage-focus")]
    public async Task<IActionResult> AddStageFocus([FromBody] AddStageFocusRequest request)
    {
        var result = await _investorService.AddStageFocusAsync(GetCurrentUserId(), request);
        if (!result.Success) return result.ToErrorResult();
        return result.ToCreatedEnvelope();
    }

    /// <summary>Remove a stage focus.</summary>
    [HttpDelete("me/stage-focus/{stageFocusId:int}")]
    public async Task<IActionResult> RemoveStageFocus(int stageFocusId)
    {
        var result = await _investorService.RemoveStageFocusAsync(GetCurrentUserId(), stageFocusId);
        if (!result.Success) return result.ToErrorResult();
        return ApiEnvelopeExtensions.DeletedEnvelope("Stage focus removed");
    }

    // ================================================================
    // ACCEPTING CONNECTIONS
    // ================================================================

    /// <summary>
    /// Enable or disable receiving new connection requests from startups.
    /// Requires an approved profile and completed KYC.
    /// Existing pending or accepted connections are not affected.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     PATCH /api/investors/me/accepting-connections
    ///     {
    ///       "acceptingConnections": false
    ///     }
    ///
    /// </remarks>
    [HttpPatch("me/accepting-connections")]
    [ProducesResponseType(typeof(ApiResponse<AcceptingConnectionsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AcceptingConnectionsDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<AcceptingConnectionsDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetAcceptingConnections([FromBody] SetAcceptingConnectionsRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _investorService.SetAcceptingConnectionsAsync(userId, request.AcceptingConnections);
        return result.ToActionResult();
    }

    // ================================================================
    // COMPARE STARTUPS
    // ================================================================

    /// <summary>Compare 2-5 startups side by side.</summary>
    [HttpGet("compare")]
    public async Task<IActionResult> CompareStartups([FromQuery] List<int> startupIds)
    {
        var result = await _investorService.CompareStartupsAsync(startupIds);
        return result.ToActionResult();
    }
}
