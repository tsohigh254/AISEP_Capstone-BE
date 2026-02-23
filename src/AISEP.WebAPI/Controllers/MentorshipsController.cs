using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Mentorship;
using AISEP.Application.Interfaces;
using AISEP.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AISEP.WebAPI.Controllers;

/// <summary>
/// Mentorship / Consulting module — Startup ↔ Advisor engagement.
/// Covers mentorship lifecycle, sessions, reports, and feedback.
/// </summary>
[ApiController]
[Route("api/mentorships")]
[Tags("Mentorships")]
[Authorize]
public class MentorshipsController : ControllerBase
{
    private readonly IMentorshipService _mentorshipService;

    public MentorshipsController(IMentorshipService mentorshipService)
    {
        _mentorshipService = mentorshipService;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    private string GetCurrentUserType()
        => User.FindFirst("userType")?.Value ?? string.Empty;

    // ================================================================
    // 1) POST /api/mentorships — Request mentorship (Startup)
    // ================================================================

    /// <summary>
    /// Create a mentorship request from the current startup user to a specified advisor.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<MentorshipDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<MentorshipDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<MentorshipDto>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateRequest([FromBody] CreateMentorshipRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _mentorshipService.CreateRequestAsync(userId, request);

        if (!result.Success && result.Error?.Code == "MENTORSHIP_ALREADY_EXISTS")
            return Conflict(result);

        if (!result.Success)
            return BadRequest(result);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    // ================================================================
    // 2) GET /api/mentorships — List my mentorships (Startup/Advisor/Staff/Admin)
    // ================================================================

    /// <summary>
    /// Get a paginated list of mentorships for the current user.
    /// Startups see their own, Advisors see theirs, Staff/Admin see all.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<MentorshipListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyMentorships(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        var userType = GetCurrentUserType();
        var result = await _mentorshipService.GetMyMentorshipsAsync(userId, userType, status, page, pageSize);

        if (!result.Success && result.Error?.Code == "ACCESS_DENIED")
            return StatusCode(StatusCodes.Status403Forbidden, result);

        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    // ================================================================
    // 3) GET /api/mentorships/{id} — Get mentorship detail
    // ================================================================

    /// <summary>
    /// Get full mentorship detail including sessions, reports, and feedback.
    /// Only participants (startup/advisor) or Staff/Admin have access.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<MentorshipDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MentorshipDetailDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<MentorshipDetailDto>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDetail(int id)
    {
        var userId = GetCurrentUserId();
        var userType = GetCurrentUserType();
        var result = await _mentorshipService.GetDetailAsync(userId, userType, id);
        return result.ToActionResult();
    }

    // ================================================================
    // 4) POST /api/mentorships/{id}/accept — Accept (Advisor)
    // ================================================================

    /// <summary>
    /// Accept a mentorship request. Advisor-only. Mentorship must have status 'Requested'.
    /// </summary>
    [HttpPost("{id:int}/accept")]
    [Authorize(Policy = "AdvisorOnly")]
    [ProducesResponseType(typeof(ApiResponse<MentorshipDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MentorshipDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<MentorshipDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Accept(int id)
    {
        var userId = GetCurrentUserId();
        var result = await _mentorshipService.AcceptAsync(userId, id);

        if (!result.Success && result.Error?.Code == "MENTORSHIP_NOT_FOUND")
            return NotFound(result);

        if (!result.Success && result.Error?.Code == "MENTORSHIP_NOT_OWNED")
            return StatusCode(StatusCodes.Status403Forbidden, result);

        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    // ================================================================
    // 5) POST /api/mentorships/{id}/reject — Reject (Advisor)
    // ================================================================

    /// <summary>
    /// Reject a mentorship request with optional reason. Advisor-only. Status must be 'Requested'.
    /// </summary>
    [HttpPost("{id:int}/reject")]
    [Authorize(Policy = "AdvisorOnly")]
    [ProducesResponseType(typeof(ApiResponse<MentorshipDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MentorshipDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<MentorshipDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectMentorshipRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _mentorshipService.RejectAsync(userId, id, request.Reason);

        if (!result.Success && result.Error?.Code == "MENTORSHIP_NOT_FOUND")
            return NotFound(result);

        if (!result.Success && result.Error?.Code == "MENTORSHIP_NOT_OWNED")
            return StatusCode(StatusCodes.Status403Forbidden, result);

        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    // ================================================================
    // 6) POST /api/mentorships/{id}/sessions — Create session (Advisor)
    // ================================================================

    /// <summary>
    /// Create a session within an accepted/in-progress mentorship. Advisor-only.
    /// Automatically moves mentorship to 'InProgress' if currently 'Accepted'.
    /// </summary>
    [HttpPost("{id:int}/sessions")]
    [Authorize(Policy = "AdvisorOnly")]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateSession(int id, [FromBody] CreateSessionRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _mentorshipService.CreateSessionAsync(userId, id, request);

        if (!result.Success && result.Error?.Code == "MENTORSHIP_NOT_FOUND")
            return NotFound(result);

        if (!result.Success && (result.Error?.Code == "MENTORSHIP_NOT_OWNED" || result.Error?.Code == "ADVISOR_PROFILE_NOT_FOUND"))
            return StatusCode(StatusCodes.Status403Forbidden, result);

        if (!result.Success) return BadRequest(result);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    // ================================================================
    // 7) PUT /api/mentorships/sessions/{sessionId} — Update session (Advisor)
    // ================================================================

    /// <summary>
    /// Update an existing session (reschedule, add notes, change status). Advisor-only.
    /// </summary>
    [HttpPut("sessions/{sessionId:int}")]
    [Authorize(Policy = "AdvisorOnly")]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSession(int sessionId, [FromBody] UpdateSessionRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _mentorshipService.UpdateSessionAsync(userId, sessionId, request);

        if (!result.Success && result.Error?.Code == "SESSION_NOT_FOUND")
            return NotFound(result);

        if (!result.Success && result.Error?.Code == "MENTORSHIP_NOT_OWNED")
            return StatusCode(StatusCodes.Status403Forbidden, result);

        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    // ================================================================
    // 8) POST /api/mentorships/{id}/reports — Create report (Advisor)
    // ================================================================

    /// <summary>
    /// Create a mentorship report, optionally linked to a specific session. Advisor-only.
    /// </summary>
    [HttpPost("{id:int}/reports")]
    [Authorize(Policy = "AdvisorOnly")]
    [ProducesResponseType(typeof(ApiResponse<ReportDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ReportDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ReportDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateReport(int id, [FromBody] CreateReportRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _mentorshipService.CreateReportAsync(userId, id, request);

        if (!result.Success && result.Error?.Code == "MENTORSHIP_NOT_FOUND")
            return NotFound(result);

        if (!result.Success && (result.Error?.Code == "MENTORSHIP_NOT_OWNED" || result.Error?.Code == "ADVISOR_PROFILE_NOT_FOUND"))
            return StatusCode(StatusCodes.Status403Forbidden, result);

        if (!result.Success) return BadRequest(result);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    // ================================================================
    // 9) GET /api/mentorships/reports/{reportId} — Get report
    // ================================================================

    /// <summary>
    /// Get a single mentorship report. Accessible by participants or Staff/Admin.
    /// </summary>
    [HttpGet("reports/{reportId:int}")]
    [ProducesResponseType(typeof(ApiResponse<ReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ReportDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReport(int reportId)
    {
        var userId = GetCurrentUserId();
        var userType = GetCurrentUserType();
        var result = await _mentorshipService.GetReportAsync(userId, userType, reportId);

        if (!result.Success && result.Error?.Code == "REPORT_NOT_FOUND")
            return NotFound(result);

        if (!result.Success && result.Error?.Code == "MENTORSHIP_NOT_OWNED")
            return StatusCode(StatusCodes.Status403Forbidden, result);

        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    // ================================================================
    // 10) POST /api/mentorships/{id}/feedbacks — Create feedback (Startup)
    // ================================================================

    /// <summary>
    /// Create feedback on a mentorship (or a specific session). Startup-only. Rating 1-5.
    /// </summary>
    [HttpPost("{id:int}/feedbacks")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<FeedbackDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<FeedbackDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<FeedbackDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateFeedback(int id, [FromBody] CreateFeedbackRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _mentorshipService.CreateFeedbackAsync(userId, id, request);

        if (!result.Success && result.Error?.Code == "MENTORSHIP_NOT_FOUND")
            return NotFound(result);

        if (!result.Success && result.Error?.Code == "MENTORSHIP_NOT_OWNED")
            return StatusCode(StatusCodes.Status403Forbidden, result);

        if (!result.Success) return BadRequest(result);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
