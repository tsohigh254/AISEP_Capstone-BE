using AISEP.Application.DTOs.Advisor;
using AISEP.Application.DTOs.Common;
using AISEP.Application.Interfaces;
using AISEP.Application.QueryParams;
using AISEP.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AISEP.WebAPI.Controllers;

/// <summary>
/// Advisor profile management and search.
/// Profile/expertise/availability endpoints for Advisor role;
/// search endpoint for Startup, Investor, Staff, Admin (any authenticated user).
/// </summary>
[ApiController]
[Route("api/advisors")]
[Tags("Advisors")]
public class AdvisorsController : ControllerBase
{
    private readonly IAdvisorService _advisorService;

    public AdvisorsController(IAdvisorService advisorService)
    {
        _advisorService = advisorService;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    // ================================================================
    // 1) POST /api/advisors — Create advisor profile
    // ================================================================

    /// <summary>
    /// Create advisor profile for the current user.
    /// Returns 409 if profile already exists.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "AdvisorOnly")]
    [ProducesResponseType(typeof(ApiResponse<AdvisorMeDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<AdvisorMeDto>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateProfile(
        [FromForm] CreateAdvisorRequest request,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var result = await _advisorService.CreateProfileAsync(userId, request);
        if (!result.Success) return result.ToErrorResult();
        return result.ToCreatedEnvelope();
    }

    // ================================================================
    // 2) GET /api/advisors/me — Get my profile
    // ================================================================

    /// <summary>
    /// Get the current advisor's profile, including expertise, availability, and industry focus.
    /// </summary>
    [HttpGet("me")]
    [Authorize(Policy = "AdvisorOnly")]
    [ProducesResponseType(typeof(ApiResponse<AdvisorMeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AdvisorMeDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile(CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var result = await _advisorService.GetMyProfileAsync(userId);
        return result.ToActionResult();
    }

    // ================================================================
    // 3) PUT /api/advisors/me — Update my profile
    // ================================================================

    /// <summary>
    /// Update advisor profile fields. Only non-null fields are updated.
    /// </summary>
    [HttpPut("me")]
    [Authorize(Policy = "AdvisorOnly")]
    [ProducesResponseType(typeof(ApiResponse<AdvisorMeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AdvisorMeDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile(
        [FromForm] UpdateAdvisorRequest request,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var result = await _advisorService.UpdateProfileAsync(userId, request);
        return result.ToActionResult();
    }

    [HttpPost("me/kyc/submit")]
    [Authorize(Policy = "AdvisorOnly")]
    [ProducesResponseType(typeof(ApiResponse<AdvisorKYCStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AdvisorKYCStatusDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitKYC([FromForm] SubmitAdvisorKYCRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _advisorService.SubmitKYCAsync(userId, request);
        return result.ToActionResult();
    }

    /// <summary>
    /// Save advisor KYC details as draft
    /// </summary>
    [HttpPatch("me/kyc/draft")]
    [Authorize(Policy = "AdvisorOnly")]
    [ProducesResponseType(typeof(ApiResponse<AdvisorKYCStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveKYCDraft([FromForm] SaveAdvisorKYCDraftRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _advisorService.SaveKYCDraftAsync(userId, request);
        return result.ToActionResult();
    }

    [HttpGet("me/kyc/status")]
    [Authorize(Policy = "AdvisorOnly")]
    [ProducesResponseType(typeof(ApiResponse<AdvisorKYCStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AdvisorKYCStatusDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetKYCStatus()
    {
        var userId = GetCurrentUserId();
        var result = await _advisorService.GetKYCStatusAsync(userId);
        return result.ToActionResult();
    }
    [HttpPut("me/availability")]
    [Authorize(Policy = "AdvisorOnly")]
    [ProducesResponseType(typeof(ApiResponse<AvailabilityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AvailabilityDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAvailability(
        [FromBody] UpdateAvailabilityRequest request,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var result = await _advisorService.UpdateAvailabilityAsync(userId, request);
        return result.ToActionResult();
    }

    // ================================================================
    // TIME SLOTS
    // ================================================================

    [HttpGet("me/timeslots")]
    [Authorize(Policy = "AdvisorOnly")]
    [ProducesResponseType(typeof(ApiResponse<List<TimeSlotDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTimeSlots(CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var result = await _advisorService.GetTimeSlotsAsync(userId);
        return result.ToActionResult();
    }

    [HttpPut("me/timeslots")]
    [Authorize(Policy = "AdvisorOnly")]
    [ProducesResponseType(typeof(ApiResponse<List<TimeSlotDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertTimeSlots(
        [FromBody] UpsertTimeSlotsRequest request,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var result = await _advisorService.UpsertTimeSlotsAsync(userId, request);
        return result.ToActionResult();
    }

    // ================================================================
    // 6) GET /api/advisors/search — Search advisors
    // ================================================================

    /// <summary>
    /// Search advisors by keyword, industry, expertise. Available to any authenticated user.
    /// </summary>
    /// <param name="q">Keyword search on name, title, bio, company.</param>
    /// <param name="industryId">Filter by industry ID (looks up name from Industries table).</param>
    /// <param name="expertise">Keyword search in expertise category/subtopic.</param>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="pageSize">Items per page (default 20, max 100).</param>
    [HttpGet("search")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<AdvisorSearchItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchAdvisors(
        [FromQuery] AdvisorQueryParams advisorQueryParams,
        CancellationToken ct = default)
    {
        var result = await _advisorService.SearchAdvisorsAsync(advisorQueryParams);
        return result.ToActionResult();
    }

    // ================================================================
    // 7) GET /api/advisors/{id} — Get advisor detail
    // ================================================================

    /// <summary>
    /// Get the detailed public profile of an advisor.
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<AdvisorDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AdvisorDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAdvisorDetail(int id, CancellationToken ct = default)
    {
        var result = await _advisorService.GetAdvisorDetailAsync(id);
        return result.ToActionResult();
    }

    // ================================================================
    // FEEDBACK MANAGEMENT (advisor-facing)
    // ================================================================

    [HttpGet("me/feedbacks")]
    [Authorize(Policy = "AdvisorOnly")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<AdvisorFeedbackListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyFeedbacks(
        [FromQuery] int? rating,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var result = await _advisorService.GetMyFeedbacksAsync(userId, rating, sort, page, pageSize);
        return result.ToActionResult();
    }

    [HttpGet("me/feedbacks/summary")]
    [Authorize(Policy = "AdvisorOnly")]
    [ProducesResponseType(typeof(ApiResponse<AdvisorFeedbackSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyFeedbackSummary(CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var result = await _advisorService.GetMyFeedbackSummaryAsync(userId);
        return result.ToActionResult();
    }

    [HttpPost("feedbacks/{feedbackId:int}/response")]
    [Authorize(Policy = "AdvisorOnly")]
    [ProducesResponseType(typeof(ApiResponse<FeedbackResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<FeedbackResponseDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<FeedbackResponseDto>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RespondToFeedback(
        int feedbackId,
        [FromBody] RespondToFeedbackRequest request,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var result = await _advisorService.RespondToFeedbackAsync(userId, feedbackId, request);
        return result.ToActionResult();
    }
}
