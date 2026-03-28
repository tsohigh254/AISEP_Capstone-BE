using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Mentorship;
using AISEP.Application.DTOs.Slot;
using AISEP.Application.Interfaces;
using AISEP.Application.QueryParams;
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
        if (!result.Success) return result.ToErrorResult();
        return result.ToCreatedEnvelope();
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
    public async Task<IActionResult> GetMyMentorships([FromQuery] MentorshipQueryParams mentorshipQuery)
    {
        var userId = GetCurrentUserId();
        var userType = GetCurrentUserType();
        var result = await _mentorshipService.GetMyMentorshipsAsync(userId, userType, mentorshipQuery);
        return result.ToPagedEnvelope();
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
        return result.ToActionResult();
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
        return result.ToActionResult();
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
        if (!result.Success) return result.ToErrorResult();
        return result.ToCreatedEnvelope();
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
        return result.ToActionResult();
    }

    /// <summary>
    /// Get a paginated list of sessions for the current user.
    /// </summary>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<MentorshipListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySessions([FromQuery] SessionQueryParams sessionQuery)
    {
        var userId = GetCurrentUserId();
        var userType = GetCurrentUserType();
        var result = await _mentorshipService.GetSessions(userId, userType, sessionQuery);
        return result.ToPagedEnvelope();
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
        if (!result.Success) return result.ToErrorResult();
        return result.ToCreatedEnvelope();
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
        return result.ToActionResult();
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
        if (!result.Success) return result.ToErrorResult();
        return result.ToCreatedEnvelope();
    }



    // ================================================================
    // WEEKLY SCHEDULE TEMPLATES (Advisor)
    // ================================================================
    /// <summary>
    /// Create a single available time slot for a specific date/time.
    /// Example: April 1st, 2026 from 1am-1:30am
    /// </summary>
    [HttpPost("available-slots")]
    [Authorize(Policy = "AdvisorOnly")]
    [ProducesResponseType(typeof(ApiResponse<AvailableSlotDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<AvailableSlotDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAvailableSlot([FromBody] CreateAvailableSlotRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _mentorshipService.CreateAvailableSlotAsync(userId, request);
        if (!result.Success) return result.ToErrorResult();
        return result.ToCreatedEnvelope();
    }

    /// <summary>
    /// Create multiple available slots in bulk.
    /// Useful for setting up entire month of slots at once.
    /// </summary>
    [HttpPost("available-slots/bulk")]
    [Authorize(Policy = "AdvisorOnly")]
    [ProducesResponseType(typeof(ApiResponse<List<AvailableSlotDto>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<List<AvailableSlotDto>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateMultipleAvailableSlots([FromBody] CreateMultipleAvailableSlotsRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _mentorshipService.CreateMultipleAvailableSlotsAsync(userId, request);
        if (!result.Success) return result.ToErrorResult();
        return result.ToCreatedEnvelope();
    }

    /// <summary>
    /// Get my available slots (Advisor) - all slots I created
    /// </summary>
    [HttpGet("my-available-slots")]
    [Authorize(Policy = "AdvisorOnly")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<AvailableSlotDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyAvailableSlots([FromQuery] AvailableSlotQueryParams queryParams)
    {
        var userId = GetCurrentUserId();
        var result = await _mentorshipService.GetMyAvailableSlotsAsync(userId, queryParams);
        return result.ToPagedEnvelope();
    }

    /// <summary>
    /// Update an available slot (change time or notes)
    /// Cannot update if already booked
    /// </summary>
    [HttpPut("available-slots/{slotId:int}")]
    [Authorize(Policy = "AdvisorOnly")]
    [ProducesResponseType(typeof(ApiResponse<AvailableSlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AvailableSlotDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AvailableSlotDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAvailableSlot(int slotId, [FromBody] UpdateAvailableSlotRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _mentorshipService.UpdateAvailableSlotAsync(userId, slotId, request);
        return result.ToActionResult();
    }

    /// <summary>
    /// Delete an available slot
    /// Cannot delete if already booked
    /// </summary>
    [HttpDelete("available-slots/{slotId:int}")]
    [Authorize(Policy = "AdvisorOnly")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAvailableSlot(int slotId)
    {
        var userId = GetCurrentUserId();
        var result = await _mentorshipService.DeleteAvailableSlotAsync(userId, slotId);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get available slots for a specific advisor (public endpoint).
    /// Startups view this to find slots to book.
    /// </summary>
    [HttpGet("advisors/{advisorId:int}/available-slots")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<AvailableSlotDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<AvailableSlotDto>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAdvisorAvailableSlots(int advisorId, [FromQuery] AvailableSlotQueryParams queryParams)
    {
        var result = await _mentorshipService.GetAdvisorAvailableSlotsAsync(advisorId, queryParams);
        return result.ToPagedEnvelope();
    }

    /// <summary>
    /// Startup books a session by selecting an available slot
    /// </summary>
    [HttpPost("book-session")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> BookSessionFromSlot([FromBody] BookSessionFromSlotRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _mentorshipService.BookSessionFromSlotAsync(userId, request);
        if (!result.Success) return result.ToErrorResult();
        return result.ToCreatedEnvelope();
    }
}
