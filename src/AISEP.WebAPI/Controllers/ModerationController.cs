using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Moderation;
using AISEP.Application.Interfaces;
using AISEP.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AISEP.WebAPI.Controllers;

/// <summary>
/// Content Moderation — Staff/Admin review flagged content and perform actions.
/// </summary>
[ApiController]
[Route("api/moderation")]
[Tags("Moderation")]
public class ModerationController : ControllerBase
{
    private readonly IModerationService _svc;

    public ModerationController(IModerationService svc)
    {
        _svc = svc;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    // ══════════════════════════════════════════════════════════════
    // 1) GET /api/moderation/flags — list flagged contents (paged)
    // ══════════════════════════════════════════════════════════════

    /// <summary>List flagged contents (paged, filtered).</summary>
    [HttpGet("flags")]
    [Authorize(Policy = "StaffOrAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<FlaggedContentListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFlags(
        [FromQuery] string? status,
        [FromQuery] string? entityType,
        [FromQuery] string? severity,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _svc.GetFlagsAsync(status, entityType, severity, q, page, pageSize);
        return result.ToActionResult();
    }

    // ══════════════════════════════════════════════════════════════
    // 2) GET /api/moderation/flags/{id} — detail + action history
    // ══════════════════════════════════════════════════════════════

    /// <summary>Get flagged content detail with action history.</summary>
    [HttpGet("flags/{id:int}")]
    [Authorize(Policy = "StaffOrAdmin")]
    [ProducesResponseType(typeof(ApiResponse<FlaggedContentDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<FlaggedContentDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFlagDetail(int id)
    {
        var result = await _svc.GetFlagDetailAsync(id);
        return result.ToActionResult();
    }

    // ══════════════════════════════════════════════════════════════
    // 3) POST /api/moderation/flags/{id}/assign — mark InReview
    // ══════════════════════════════════════════════════════════════

    /// <summary>Assign flag for review (Pending → InReview).</summary>
    [HttpPost("flags/{id:int}/assign")]
    [Authorize(Policy = "StaffOrAdmin")]
    [ProducesResponseType(typeof(ApiResponse<FlaggedContentDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<FlaggedContentDetailDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<FlaggedContentDetailDto>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Assign(int id, [FromBody] AssignFlagRequest request)
    {
        var result = await _svc.AssignAsync(GetCurrentUserId(), id, request.Note);
        return result.ToActionResult();
    }

    // ══════════════════════════════════════════════════════════════
    // 4) POST /api/moderation/flags/{id}/resolve — resolve/reject
    // ══════════════════════════════════════════════════════════════

    /// <summary>Resolve flagged content with decision.</summary>
    [HttpPost("flags/{id:int}/resolve")]
    [Authorize(Policy = "StaffOrAdmin")]
    [ProducesResponseType(typeof(ApiResponse<FlaggedContentDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<FlaggedContentDetailDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<FlaggedContentDetailDto>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Resolve(int id, [FromBody] ResolveFlagRequest request)
    {
        var result = await _svc.ResolveAsync(GetCurrentUserId(), id, request.Decision, request.Note);
        return result.ToActionResult();
    }

    // ══════════════════════════════════════════════════════════════
    // 5) POST /api/moderation/flags/{id}/actions — create action
    // ══════════════════════════════════════════════════════════════

    /// <summary>Create a moderation action on a flag (Warn, Hide, LockUser, etc.).</summary>
    [HttpPost("flags/{id:int}/actions")]
    [Authorize(Policy = "StaffOrAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ModerationActionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ModerationActionDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateAction(int id, [FromBody] CreateModerationActionRequest request)
    {
        var result = await _svc.CreateActionAsync(GetCurrentUserId(), id, request);

        if (!result.Success)
            return result.ToErrorResult();

        return result.ToCreatedEnvelope();
    }

    // ══════════════════════════════════════════════════════════════
    // 6) GET /api/moderation/flags/{id}/actions — action history
    // ══════════════════════════════════════════════════════════════

    /// <summary>List action history for a flag.</summary>
    [HttpGet("flags/{id:int}/actions")]
    [Authorize(Policy = "StaffOrAdmin")]
    [ProducesResponseType(typeof(ApiResponse<List<ModerationActionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<ModerationActionDto>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetActions(int id)
    {
        var result = await _svc.GetActionsAsync(id);
        return result.ToActionResult();
    }

    // ══════════════════════════════════════════════════════════════
    // 7) POST /api/reports — user-initiated report (any auth user)
    // ══════════════════════════════════════════════════════════════

    /// <summary>Report content (any authenticated user).</summary>
    [HttpPost("/api/reports")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<FlaggedContentDetailDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateReport([FromBody] CreateFlagRequest request)
    {
        var result = await _svc.CreateFlagAsync(GetCurrentUserId(), request);

        if (!result.Success)
            return result.ToErrorResult();

        return result.ToCreatedEnvelope();
    }

    // ══════════════════════════════════════════════════════════════
    // 8) GET /api/reports/me — user's own reports
    // ══════════════════════════════════════════════════════════════

    /// <summary>Get my submitted reports (requires ReporterUserId field in DB).</summary>
    [HttpGet("/api/reports/me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<FlaggedContentListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyReports([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _svc.GetMyReportsAsync(GetCurrentUserId(), page, pageSize);

        if (!result.Success && result.Error?.Code == "NOT_IMPLEMENTED")
            return ApiEnvelopeExtensions.ErrorEnvelope(result.Error?.Message ?? "Not implemented", StatusCodes.Status501NotImplemented);

        return result.ToActionResult();
    }
}
