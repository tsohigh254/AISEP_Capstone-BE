using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Notification;
using AISEP.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using AISEP.WebAPI.Hubs;

namespace AISEP.WebAPI.Services;

public class NotificationDeliveryService : INotificationDeliveryService
{
    private readonly INotificationService _notificationService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationDeliveryService> _logger;

    public NotificationDeliveryService(INotificationService notificationService, IHubContext<NotificationHub> hubContext, ILogger<NotificationDeliveryService> logger)
    {
        _notificationService = notificationService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<ApiResponse<NotificationDto>?> CreateAndPushAsync(CreateNotificationRequest request)
    {
        try
        {
            var created = await _notificationService.CreateAsync(request);
            if (created?.Success == true && created.Data != null)
            {
                try
                {
                    // Map to compact list item DTO for SignalR push to match FE expectations
                    var listItem = new NotificationListItemDto
                    {
                        NotificationId = created.Data.NotificationId,
                        NotificationType = created.Data.NotificationType,
                        Title = created.Data.Title,
                        MessagePreview = created.Data.Message?.Length > 150 
                            ? created.Data.Message[..150] + "..." 
                            : created.Data.Message,
                        IsRead = created.Data.IsRead,
                        CreatedAt = created.Data.CreatedAt,
                        ActionUrl = created.Data.ActionUrl
                    };

                    await _hubContext.Clients.User(request.UserId.ToString())
                        .SendAsync("ReceiveNotification", listItem);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to push notification via NotificationHub for user {UserId}", request.UserId);
                }
            }

            return created;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "NotificationDeliveryService.CreateAndPushAsync failed");
            return null;
        }
    }
}
