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
    public async Task<IActionResult> GetMyMentorships(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        var userType = GetCurrentUserType();
        var result = await _mentorshipService.GetMyMentorshipsAsync(userId, userType, status, page, pageSize);
        return result.ToActionResult();
    }

    // ================================================================
    // GET /api/mentorships/sessions — List my sessions (Startup/Advisor/Staff/Admin)
    // ================================================================
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<object>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySessions(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        var userType = GetCurrentUserType();
        var result = await _mentorshipService.GetMySessionsAsync(userId, userType, status, page, pageSize);
        return result.ToActionResult();
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
    // 5.5) PUT /api/mentorships/{id}/cancel — Cancel
    // ================================================================

    /// <summary>
    /// Cancel a mentorship request by Startup or Advisor.
    /// </summary>
    [HttpPut("{id:int}/cancel")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<MentorshipDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MentorshipDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<MentorshipDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(int id, [FromBody] RejectMentorshipRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _mentorshipService.CancelAsync(userId, id, request.Reason);
        return result.ToActionResult();
    }

    // ================================================================
    // 5b) PUT /api/mentorships/{id}/complete — Complete (Advisor)
    // ================================================================

    /// <summary>
    /// Mark a mentorship as completed after the session has occurred. Advisor-only.
    /// </summary>
    [HttpPut("{id:int}/complete")]
    [Authorize(Policy = "AdvisorOnly")]
    [ProducesResponseType(typeof(ApiResponse<MentorshipDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Complete(int id)
    {
        var userId = GetCurrentUserId();
        var result = await _mentorshipService.CompleteAsync(userId, id);
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
    // 6b) POST /api/mentorships/{id}/sessions/{sessionId}/confirm — Startup chọn slot
    // ================================================================

    /// <summary>
    /// Startup confirms one ProposedByAdvisor slot as the final scheduled session.
    /// All other ProposedByAdvisor and ProposedByStartup slots for this mentorship are cancelled.
    /// Mentorship advances to InProgress atomically.
    /// </summary>
    [HttpPost("{id:int}/sessions/{sessionId:int}/confirm")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmSession(int id, int sessionId)
    {
        var userId = GetCurrentUserId();
        var result = await _mentorshipService.ConfirmSessionAsync(userId, id, sessionId);
        return result.ToActionResult();
    }

    // ================================================================
    // 6c) POST /api/mentorships/{id}/sessions/{sessionId}/accept — Advisor chọn slot startup đề xuất
    // ================================================================

    /// <summary>
    /// Advisor accepts one ProposedByStartup slot as the final scheduled session.
    /// All other ProposedByStartup and ProposedByAdvisor slots for this mentorship are cancelled.
    /// Mentorship advances to InProgress atomically.
    /// </summary>
    [HttpPost("{id:int}/sessions/{sessionId:int}/accept")]
    [Authorize(Policy = "AdvisorOnly")]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcceptSession(int id, int sessionId)
    {
        var userId = GetCurrentUserId();
        var result = await _mentorshipService.AcceptSessionAsync(userId, id, sessionId);
        return result.ToActionResult();
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

    // ================================================================
    // 8) POST /api/mentorships/{id}/reports — Create report (Advisor)
    // ================================================================

    /// <summary>
    /// Create a mentorship report, optionally linked to a specific session. Advisor-only.
    /// </summary>
    [HttpPost("{id:int}/reports")]
    [Authorize(Policy = "AdvisorOnly")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<ReportDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ReportDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ReportDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateReport(int id, [FromForm] CreateReportRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _mentorshipService.CreateReportAsync(userId, id, request);
        if (!result.Success) return result.ToErrorResult();
        return result.ToCreatedEnvelope();
    }

    // ================================================================
    // 8b) PATCH /api/mentorships/{id}/reports/{reportId} — Update draft (Advisor)
    // ================================================================

    /// <summary>
    /// Update a draft report. Only allowed when reviewStatus = "Draft".
    /// Set isDraft = false in the body to submit officially for staff review.
    /// </summary>
    [HttpPatch("{id:int}/reports/{reportId:int}")]
    [Authorize(Policy = "AdvisorOnly")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<ReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ReportDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ReportDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateReport(int id, int reportId, [FromForm] UpdateReportRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _mentorshipService.UpdateReportAsync(userId, id, reportId, request);
        return result.ToActionResult();
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
    // 12) GET /api/mentorships/{id}/sessions — List sessions
    // ================================================================

    /// <summary>List sessions for a specific mentorship.</summary>
    [HttpGet("{id:int}/sessions")]
    public async Task<IActionResult> GetSessions(int id)
    {
        var result = await _mentorshipService.GetMentorshipSessionsAsync(
            GetCurrentUserId(), GetCurrentUserType(), id);
        return result.ToEnvelope();
    }

    // ================================================================
    // 13) GET /api/mentorships/{id}/feedbacks — List feedbacks
    // ================================================================

    /// <summary>List feedbacks for a specific mentorship.</summary>
    [HttpGet("{id:int}/feedbacks")]
    public async Task<IActionResult> GetFeedbacks(int id)
    {
        var result = await _mentorshipService.GetMentorshipFeedbacksAsync(
            GetCurrentUserId(), GetCurrentUserType(), id);
        return result.ToEnvelope();
    }

    // ================================================================
    // OVERSIGHT — GET /api/mentorships/oversight/reports (Staff/Admin)
    // ================================================================

    /// <summary>
    /// Get paginated reports for staff oversight/review.
    /// Default filter: PendingReview. Pass reviewStatus=all to see all.
    /// </summary>
    [HttpGet("oversight/reports")]
    [Authorize(Policy = "StaffOrAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ReportOversightDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReportsForOversight(
        [FromQuery] string? reviewStatus,
        [FromQuery] int? advisorId,
        [FromQuery] int? startupId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mentorshipService.GetReportsForOversightAsync(
            reviewStatus, advisorId, startupId, from, to, page, pageSize);
        return result.ToActionResult();
    }

    // ================================================================
    // OVERSIGHT — PUT /api/mentorships/reports/{reportId}/review (Staff/Admin)
    // ================================================================

    /// <summary>
    /// Review a mentorship report: Passed, Failed, or NeedsMoreInfo.
    /// </summary>
    [HttpPut("reports/{reportId:int}/review")]
    [Authorize(Policy = "StaffOrAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ReportReviewResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReviewReport(int reportId, [FromBody] ReviewReportRequest request)
    {
        var staffUserId = GetCurrentUserId();
        var result = await _mentorshipService.ReviewReportAsync(staffUserId, reportId, request);
        return result.ToActionResult();
    }

    // ================================================================
    // OVERSIGHT — POST /api/mentorships/{id}/sessions/{sessionId}/confirm-conducted (Startup)
    // ================================================================

    /// <summary>
    /// Startup confirms a session was actually conducted.
    /// Session moves from Scheduled/InProgress → Conducted.
    /// </summary>
    [HttpPost("{id:int}/sessions/{sessionId:int}/confirm-conducted")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfirmConducted(int id, int sessionId)
    {
        var userId = GetCurrentUserId();
        var result = await _mentorshipService.ConfirmConductedAsync(userId, id, sessionId);
        return result.ToActionResult();
    }

    // ================================================================
    // OVERSIGHT — PUT /api/mentorships/{id}/sessions/{sessionId}/mark-completed (Staff/Admin)
    // ================================================================

    /// <summary>
    /// Staff marks a session as completed after verifying reports are all Passed.
    /// Triggers RecalculateMentorshipStatus + RecalculatePayoutEligibility.
    /// </summary>
    [HttpPut("{id:int}/sessions/{sessionId:int}/mark-completed")]
    [Authorize(Policy = "StaffOrAdmin")]
    [ProducesResponseType(typeof(ApiResponse<SessionOversightResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkSessionCompleted(int id, int sessionId,
        [FromBody] StaffSessionNoteRequest? request)
    {
        var staffUserId = GetCurrentUserId();
        var result = await _mentorshipService.MarkSessionCompletedAsync(
            staffUserId, id, sessionId, request?.Note);
        return result.ToActionResult();
    }

    // ================================================================
    // OVERSIGHT — PUT /api/mentorships/{id}/sessions/{sessionId}/mark-dispute (Staff/Admin)
    // ================================================================

    /// <summary>
    /// Staff marks a session as in dispute. Can be opened from multiple statuses.
    /// </summary>
    [HttpPut("{id:int}/sessions/{sessionId:int}/mark-dispute")]
    [Authorize(Policy = "StaffOrAdmin")]
    [ProducesResponseType(typeof(ApiResponse<SessionOversightResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkSessionDispute(int id, int sessionId,
        [FromBody] StaffMarkDisputeRequest request)
    {
        var staffUserId = GetCurrentUserId();
        var result = await _mentorshipService.MarkSessionDisputeAsync(
            staffUserId, id, sessionId, request.Reason);
        return result.ToActionResult();
    }

    // ================================================================
    // OVERSIGHT — PUT /api/mentorships/{id}/sessions/{sessionId}/mark-resolved (Staff/Admin)
    // ================================================================

    /// <summary>
    /// Staff resolves a dispute. If restoreCompleted=true → Completed, else → Resolved.
    /// </summary>
    [HttpPut("{id:int}/sessions/{sessionId:int}/mark-resolved")]
    [Authorize(Policy = "StaffOrAdmin")]
    [ProducesResponseType(typeof(ApiResponse<SessionOversightResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkSessionResolved(int id, int sessionId,
        [FromBody] ResolveDisputeRequest request)
    {
        var staffUserId = GetCurrentUserId();
        var result = await _mentorshipService.MarkSessionResolvedAsync(
            staffUserId, id, sessionId, request);
        return result.ToActionResult();
    }
}
