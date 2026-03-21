using AISEP.Application.DTOs.Chat;
using AISEP.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AISEP.WebAPI.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IChatService chatService, ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    // ─── Connection lifecycle ────────────────────────────────────

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("SignalR connected: userId={UserId} connectionId={ConnectionId}",
            GetUserId(), Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("SignalR disconnected: userId={UserId} connectionId={ConnectionId}",
            GetUserId(), Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    // ─── Hub methods (invoked by client) ────────────────────────

    /// <summary>Client đăng ký nhận tin nhắn của một conversation.</summary>
    public async Task JoinConversation(int conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(conversationId));
        _logger.LogDebug("userId={UserId} joined group conv_{ConversationId}", GetUserId(), conversationId);
    }

    /// <summary>Client rời khỏi group của conversation.</summary>
    public async Task LeaveConversation(int conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(conversationId));
        _logger.LogDebug("userId={UserId} left group conv_{ConversationId}", GetUserId(), conversationId);
    }

    /// <summary>
    /// Client gửi tin nhắn. Hub lưu vào DB qua ChatService,
    /// sau đó broadcast "ReceiveMessage" tới tất cả client trong group.
    /// </summary>
    public async Task SendMessage(SendMessageRequest request)
    {
        var userId = GetUserId();
        if (userId == 0)
        {
            await Clients.Caller.SendAsync("Error", "Unauthorized: cannot resolve user identity.");
            return;
        }

        var result = await _chatService.SendMessageAsync(userId, request);

        if (!result.Success)
        {
            await Clients.Caller.SendAsync("Error", result.Error?.Message ?? "Failed to send message.");
            return;
        }

        var saved = result.Data!;

        var payload = new SignalRMessageDto
        {
            MessageId    = saved.MessageId,
            ConversationId = saved.ConversationId,
            SenderId     = saved.SenderUserId,
            Content      = saved.Content,
            AttachmentUrl = saved.AttachmentUrls,
            CreatedAt    = saved.SentAt
        };

        // Broadcast tới tất cả client đang ở trong group (kể cả người gửi)
        await Clients.Group(GroupName(request.ConversationId))
                     .SendAsync("ReceiveMessage", payload);
    }

    // ─── Helpers ────────────────────────────────────────────────

    private int GetUserId()
    {
        var claim = Context.User?.FindFirst("sub")?.Value
                 ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    private static string GroupName(int conversationId) => $"conv_{conversationId}";
}
