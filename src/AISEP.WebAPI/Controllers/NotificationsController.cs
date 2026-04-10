using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Notification;
using AISEP.Application.Interfaces;
using AISEP.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AISEP.WebAPI.Controllers;

/// <summary>
/// Notifications — private per-user notifications (system-generated).
/// </summary>
[ApiController]
[Route("api/notifications")]
[Tags("Notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    // ── 1) GET /api/notifications ─────────────────────────────────

    /// <summary>List my notifications (paged, optional filters).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<NotificationListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] bool? unreadOnly,
        [FromQuery] string? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _notificationService.GetMyNotificationsAsync(
            GetCurrentUserId(), unreadOnly, type, page, pageSize);
        return result.ToActionResult();
    }

    // ── 2) GET /api/notifications/{id} ────────────────────────────

    /// <summary>Get notification detail (owner only).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<NotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<NotificationDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<NotificationDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetail(int id)
    {
        var result = await _notificationService.GetMyNotificationAsync(GetCurrentUserId(), id);
        return result.ToActionResult();
    }

    // ── 3) PUT /api/notifications/{id}/read ───────────────────────

    /// <summary>Mark a notification as read or unread.</summary>
    [HttpPut("{id:int}/read")]
    [ProducesResponseType(typeof(ApiResponse<NotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<NotificationDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<NotificationDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(int id, [FromBody] MarkReadRequest request)
    {
        var isRead = request.IsRead ?? true;
        var result = await _notificationService.MarkReadAsync(GetCurrentUserId(), id, isRead);
        return result.ToActionResult();
    }

    // ── 4) PUT /api/notifications/read-all ────────────────────────

    /// <summary>Mark all unread notifications as read.</summary>
    [HttpPut("read-all")]
    [ProducesResponseType(typeof(ApiResponse<MarkAllResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkReadAll()
    {
        var result = await _notificationService.MarkReadAllAsync(GetCurrentUserId());
        return result.ToActionResult();
    }

    // ── 5) DELETE /api/notifications/{id} ─────────────────────────

    /// <summary>Delete a notification (owner only, hard delete).</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _notificationService.DeleteAsync(GetCurrentUserId(), id);

        if (!result.Success)
            return result.ToErrorResult();

        return ApiEnvelopeExtensions.DeletedEnvelope("Notification deleted");
    }
}
