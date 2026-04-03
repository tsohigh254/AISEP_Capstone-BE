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
    private readonly ICloudinaryService _cloudinary;

    public InvestorsController(IInvestorService investorService, ICloudinaryService cloudinary)
    {
        _investorService = investorService;
        _cloudinary = cloudinary;
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

    // ================================================================
        // KYC / APPROVAL
        // ================================================================     

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
    public async Task<IActionResult> SubmitKYC([FromForm] SubmitInvestorKYCRequest request, IFormFile? idProof, IFormFile? investmentProof)
    {
        var userId = GetCurrentUserId();
        string? idProofUrl = null;
        string? investmentProofUrl = null;

        if (idProof != null)
        {
            idProofUrl = await _cloudinary.UploadDocument(idProof, "investor_kyc/id_proofs");
        }

        if (investmentProof != null)
        {
            investmentProofUrl = await _cloudinary.UploadDocument(investmentProof, "investor_kyc/investment_proofs");
        }

        var result = await _investorService.SubmitKYCAsync(userId, request, idProofUrl, investmentProofUrl);
        return result.ToActionResult();
    }

    /// <summary>
    /// Save investor KYC draft
    /// </summary>
    [HttpPost("me/kyc/draft")]
    [ProducesResponseType(typeof(ApiResponse<InvestorKYCStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveKYCDraft([FromBody] SaveInvestorKYCDraftRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _investorService.SaveKYCDraftAsync(userId, request);
        return result.ToActionResult();
    }

    /// <summary>
    /// Submit investor profile for KYC approval (Legacy endpoint - redirects to status update)
    /// </summary>
    [HttpPost("me/kyc/submit-legacy")]
    [ProducesResponseType(typeof(ApiResponse<InvestorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitForApproval()
    {
        var userId = GetCurrentUserId();
        var result = await _investorService.SubmitForApprovalAsync(userId);
        return result.ToActionResult();
    }

        // ================================================================     
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
        return result.ToPagedEnvelope();
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
        return result.ToPagedEnvelope();
    }

    // ================================================================
    // RECOMMENDATIONS (PLACEHOLDER)
    // ================================================================

    /// <summary>
    /// Get AI-powered startup recommendations (not yet implemented)
    /// </summary>
    [HttpGet("recommendations")]
    [ProducesResponseType(typeof(ApiResponse<List<StartupSearchItemDto>>), StatusCodes.Status501NotImplemented)]
    public IActionResult GetRecommendations()
    {
        return ApiEnvelopeExtensions.ErrorEnvelope(
            "AI recommendation engine is not yet enabled. This feature is coming soon.",
            StatusCodes.Status501NotImplemented);
    }
}
