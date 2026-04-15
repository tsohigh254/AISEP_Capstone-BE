using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Notification;

namespace AISEP.Application.Interfaces;

public interface INotificationService
{
    /// <summary>List notifications for the current user (paged, optional filters).</summary>
    Task<ApiResponse<PagedResponse<NotificationListItemDto>>> GetMyNotificationsAsync(
        int userId, bool? unreadOnly, string? type, int page, int pageSize);

    /// <summary>Get single notification detail (owner only).</summary>
    Task<ApiResponse<NotificationDto>> GetMyNotificationAsync(int userId, int notificationId);

    /// <summary>Mark a notification as read or unread.</summary>
    Task<ApiResponse<NotificationDto>> MarkReadAsync(int userId, int notificationId, bool isRead);

    /// <summary>Mark all unread notifications as read for the user.</summary>
    Task<ApiResponse<MarkAllResultDto>> MarkReadAllAsync(int userId);

    /// <summary>Delete a notification (owner only, hard delete).</summary>
    Task<ApiResponse<string>> DeleteAsync(int userId, int notificationId);

    /// <summary>Create a notification (system-generated or manual).</summary>
    Task<ApiResponse<NotificationDto>> CreateAsync(CreateNotificationRequest request);
}
