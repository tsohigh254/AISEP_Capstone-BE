using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Notification;

namespace AISEP.Application.Interfaces;


public interface INotificationDeliveryService
{
    /// <summary>
    /// Create a notification (persist) and attempt to push it to the user's connected clients.
    /// Returns the created notification response or null on failure.
    /// </summary>
    Task<ApiResponse<NotificationDto>?> CreateAndPushAsync(CreateNotificationRequest request);
}
