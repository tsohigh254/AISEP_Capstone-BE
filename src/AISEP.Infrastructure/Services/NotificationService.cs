using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Notification;
using AISEP.Application.Interfaces;
using AISEP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AISEP.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;

    public NotificationService(ApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    // ── GET list (paged) ──────────────────────────────────────────

    public async Task<ApiResponse<PagedResponse<NotificationListItemDto>>> GetMyNotificationsAsync(
        int userId, bool? unreadOnly, string? type, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserID == userId);

        if (unreadOnly == true)
            query = query.Where(n => !n.IsRead);

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(n => n.NotificationType == type);

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationListItemDto
            {
                NotificationId = n.NotificationID,
                NotificationType = n.NotificationType,
                Title = n.Title,
                MessagePreview = n.Message != null && n.Message.Length > 100
                    ? n.Message.Substring(0, 100) + "…"
                    : n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                ActionUrl = n.ActionURL
            })
            .ToListAsync();

        var paged = new PagedResponse<NotificationListItemDto>
        {
            Items = items,
            Paging = new PagingInfo
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
            }
        };

        return ApiResponse<PagedResponse<NotificationListItemDto>>.SuccessResponse(paged);
    }

    // ── GET detail ────────────────────────────────────────────────

    public async Task<ApiResponse<NotificationDto>> GetMyNotificationAsync(
        int userId, int notificationId)
    {
        var n = await _db.Notifications
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.NotificationID == notificationId);

        if (n == null)
            return ApiResponse<NotificationDto>.ErrorResponse(
                "NOTIFICATION_NOT_FOUND", "Notification not found.");

        if (n.UserID != userId)
            return ApiResponse<NotificationDto>.ErrorResponse(
                "ACCESS_DENIED", "You do not own this notification.");

        return ApiResponse<NotificationDto>.SuccessResponse(MapToDto(n));
    }

    // ── Mark read / unread ────────────────────────────────────────

    public async Task<ApiResponse<NotificationDto>> MarkReadAsync(
        int userId, int notificationId, bool isRead)
    {
        var n = await _db.Notifications
            .FirstOrDefaultAsync(x => x.NotificationID == notificationId);

        if (n == null)
            return ApiResponse<NotificationDto>.ErrorResponse(
                "NOTIFICATION_NOT_FOUND", "Notification not found.");

        if (n.UserID != userId)
            return ApiResponse<NotificationDto>.ErrorResponse(
                "ACCESS_DENIED", "You do not own this notification.");

        if (isRead)
        {
            n.IsRead = true;
            n.ReadAt ??= DateTime.UtcNow;
        }
        else
        {
            n.IsRead = false;
            n.ReadAt = null;
        }

        await _db.SaveChangesAsync();
        return ApiResponse<NotificationDto>.SuccessResponse(MapToDto(n), "Notification updated.");
    }

    // ── Mark all read ─────────────────────────────────────────────

    public async Task<ApiResponse<MarkAllResultDto>> MarkReadAllAsync(int userId)
    {
        var now = DateTime.UtcNow;

        var count = await _db.Notifications
            .Where(n => n.UserID == userId && !n.IsRead)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, now));

        return ApiResponse<MarkAllResultDto>.SuccessResponse(
            new MarkAllResultDto { UpdatedCount = count },
            $"{count} notification(s) marked as read.");
    }

    // ── Delete (hard) ─────────────────────────────────────────────

    public async Task<ApiResponse<string>> DeleteAsync(int userId, int notificationId)
    {
        var n = await _db.Notifications
            .FirstOrDefaultAsync(x => x.NotificationID == notificationId);

        if (n == null)
            return ApiResponse<string>.ErrorResponse(
                "NOTIFICATION_NOT_FOUND", "Notification not found.");

        if (n.UserID != userId)
            return ApiResponse<string>.ErrorResponse(
                "ACCESS_DENIED", "You do not own this notification.");

        _db.Notifications.Remove(n);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("DELETE_NOTIFICATION", "Notification",
            notificationId, $"Deleted notification '{n.Title}'");

        return ApiResponse<string>.SuccessResponse("Notification deleted.");
    }

    // ── Mapper ────────────────────────────────────────────────────

    private static NotificationDto MapToDto(Domain.Entities.Notification n) => new()
    {
        NotificationId = n.NotificationID,
        NotificationType = n.NotificationType,
        Title = n.Title,
        Message = n.Message,
        RelatedEntityType = n.RelatedEntityType,
        RelatedEntityId = n.RelatedEntityID,
        ActionUrl = n.ActionURL,
        IsRead = n.IsRead,
        IsSent = n.IsSent,
        CreatedAt = n.CreatedAt,
        ReadAt = n.ReadAt
    };

    // ── TODO: Internal helper for system-generated notifications ──
    // Called by other services (ChatService, MentorshipService, etc.)
    // to create notifications on events:
    //   - NewMessage -> notify other conversation participant
    //   - MentorshipAccepted / Rejected -> notify startup
    //   - ConnectionAccepted / Rejected -> notify investor/startup
    // Implementation deferred to next sprint (background jobs / push / email).
}
