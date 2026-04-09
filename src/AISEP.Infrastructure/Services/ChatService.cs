using AISEP.Application.DTOs.Chat;
using AISEP.Application.DTOs.Common;
using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Domain.Enums;
using AISEP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AISEP.Infrastructure.Services;

public class ChatService : IChatService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly ILogger<ChatService> _logger;

    public ChatService(ApplicationDbContext db, IAuditService audit, ILogger<ChatService> logger)
    {
        _db = db;
        _audit = audit;
        _logger = logger;
    }

    // ════════════════════════════════════════════════════════════
    //  CONVERSATIONS
    // ════════════════════════════════════════════════════════════

    public async Task<ApiResponse<PagedResponse<ConversationListItemDto>>> GetMyConversationsAsync(
        int userId, string? status, int page, int pageSize)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        // Base query: conversations where the user is a participant
        var query = _db.Conversations
            .Include(c => c.Connection).ThenInclude(cn => cn!.Startup)
            .Include(c => c.Connection).ThenInclude(cn => cn!.Investor)
            .Include(c => c.Mentorship).ThenInclude(m => m!.Startup)
            .Include(c => c.Mentorship).ThenInclude(m => m!.Advisor)
            .AsQueryable();

        // Filter to only conversations this user participates in
        query = query.Where(c =>
            (c.ConnectionID != null && (
                c.Connection!.Startup.UserID == userId ||
                c.Connection!.Investor.UserID == userId)) ||
            (c.MentorshipID != null && (
                c.Mentorship!.Startup.UserID == userId ||
                c.Mentorship!.Advisor.UserID == userId))
        );

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ConversationStatus>(status, true, out var statusEnum))
            query = query.Where(c => c.ConversationStatus == statusEnum);

        var totalItems = await query.CountAsync();

        var conversations = await query
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Batch load last messages and unread counts to avoid N+1
        var conversationIds = conversations.Select(c => c.ConversationID).ToList();

        var lastMessages = await _db.Messages
            .Where(m => conversationIds.Contains(m.ConversationID))
            .GroupBy(m => m.ConversationID)
            .Select(g => new
            {
                ConversationID = g.Key,
                LastMessage = g.OrderByDescending(m => m.SentAt).Select(m => m.MessageText).FirstOrDefault()
            })
            .ToDictionaryAsync(x => x.ConversationID, x => x.LastMessage);

        var unreadCounts = await _db.Messages
            .Where(m => conversationIds.Contains(m.ConversationID)
                     && m.SenderUserID != userId
                     && !m.IsRead)
            .GroupBy(m => m.ConversationID)
            .Select(g => new { ConversationID = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ConversationID, x => x.Count);

        var items = new List<ConversationListItemDto>();
        foreach (var c in conversations)
        {
            var title = BuildTitle(c, userId);
            lastMessages.TryGetValue(c.ConversationID, out var lastMsg);
            unreadCounts.TryGetValue(c.ConversationID, out var unread);

            var (participantId, participantName, participantRole) = GetOtherParticipant(c, userId);

            items.Add(new ConversationListItemDto
            {
                ConversationId = c.ConversationID,
                ConnectionId = c.ConnectionID,
                MentorshipId = c.MentorshipID,
                Status = c.ConversationStatus.ToString(),
                Title = title,
                ParticipantId = participantId,
                ParticipantName = participantName,
                ParticipantRole = participantRole,
                LastMessagePreview = lastMsg != null
                    ? (lastMsg.Length > 80 ? lastMsg[..80] + "…" : lastMsg)
                    : null,
                UnreadCount = unread,
                CreatedAt = c.CreatedAt,
                LastMessageAt = c.LastMessageAt
            });
        }

        var paged = new PagedResponse<ConversationListItemDto>
        {
            Items = items,
            Paging = new PagingInfo
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
            }
        };

        return ApiResponse<PagedResponse<ConversationListItemDto>>.SuccessResponse(paged);
    }

    public async Task<ApiResponse<ConversationDto>> CreateConversationAsync(
        int userId, CreateConversationRequest request)
    {
        // Resolve participants and validate ownership
        if (request.ConnectionId.HasValue)
        {
            var conn = await _db.StartupInvestorConnections
                .Include(c => c.Startup)
                .Include(c => c.Investor)
                .FirstOrDefaultAsync(c => c.ConnectionID == request.ConnectionId.Value);

            if (conn == null)
                return ApiResponse<ConversationDto>.ErrorResponse(
                    "CONNECTION_NOT_FOUND", "Connection not found.");

            if (conn.Startup.UserID != userId && conn.Investor.UserID != userId)
                return ApiResponse<ConversationDto>.ErrorResponse(
                    "ACCESS_DENIED", "You are not a participant of this connection.");

            if (conn.ConnectionStatus != ConnectionStatus.Accepted)
                return ApiResponse<ConversationDto>.ErrorResponse(
                    "INVALID_STATUS_TRANSITION",
                    $"Cannot create conversation: connection status is '{conn.ConnectionStatus}', must be 'Accepted'.");

            // Check for existing open conversation — return it (idempotent)
            var existingConv = await _db.Conversations
                .FirstOrDefaultAsync(c => c.ConnectionID == request.ConnectionId.Value
                            && c.ConversationStatus == ConversationStatus.Active);
            if (existingConv != null)
                return ApiResponse<ConversationDto>.SuccessResponse(MapToDto(existingConv));
        }
        else if (request.MentorshipId.HasValue)
        {
            var mentorship = await _db.StartupAdvisorMentorships
                .Include(m => m.Startup)
                .Include(m => m.Advisor)
                .FirstOrDefaultAsync(m => m.MentorshipID == request.MentorshipId.Value);

            if (mentorship == null)
                return ApiResponse<ConversationDto>.ErrorResponse(
                    "MENTORSHIP_NOT_FOUND", "Mentorship not found.");

            if (mentorship.Startup.UserID != userId && mentorship.Advisor.UserID != userId)
                return ApiResponse<ConversationDto>.ErrorResponse(
                    "ACCESS_DENIED", "You are not a participant of this mentorship.");

            if (mentorship.MentorshipStatus != MentorshipStatus.Accepted && mentorship.MentorshipStatus != MentorshipStatus.InProgress)
                return ApiResponse<ConversationDto>.ErrorResponse(
                    "INVALID_STATUS_TRANSITION",
                    $"Cannot create conversation: mentorship status is '{mentorship.MentorshipStatus}', must be 'Accepted' or 'InProgress'.");

            var existingMentorConv = await _db.Conversations
                .FirstOrDefaultAsync(c => c.MentorshipID == request.MentorshipId.Value
                            && c.ConversationStatus == ConversationStatus.Active);
            if (existingMentorConv != null)
                return ApiResponse<ConversationDto>.SuccessResponse(MapToDto(existingMentorConv));
        }
        else
        {
            // FluentValidation should prevent this, but guard anyway
            return ApiResponse<ConversationDto>.ErrorResponse(
                "VALIDATION_ERROR", "Either mentorshipId or connectionId is required.");
        }

        var conversation = new Conversation
        {
            ConnectionID = request.ConnectionId,
            MentorshipID = request.MentorshipId,
            ConversationStatus = ConversationStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _db.Conversations.Add(conversation);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("CREATE_CONVERSATION", "Conversation",
            conversation.ConversationID,
            $"Created conversation (Connection={request.ConnectionId}, Mentorship={request.MentorshipId})");

        return ApiResponse<ConversationDto>.SuccessResponse(MapToDto(conversation),
            "Conversation created successfully.");
    }

    public async Task<ApiResponse<ConversationDetailDto>> GetConversationAsync(
        int userId, int conversationId)
    {
        var conv = await LoadConversationWithParticipants(conversationId);

        if (conv == null)
            return ApiResponse<ConversationDetailDto>.ErrorResponse(
                "CONVERSATION_NOT_FOUND", "Conversation not found.");

        if (!IsParticipant(userId, conv))
            return ApiResponse<ConversationDetailDto>.ErrorResponse(
                "ACCESS_DENIED", "You are not a participant of this conversation.");

        var participants = BuildParticipants(conv);
        var title = BuildTitle(conv, userId);

        var detail = new ConversationDetailDto
        {
            ConversationId = conv.ConversationID,
            ConnectionId = conv.ConnectionID,
            MentorshipId = conv.MentorshipID,
            Status = conv.ConversationStatus.ToString(),
            Title = title,
            Participants = participants,
            CreatedAt = conv.CreatedAt,
            LastMessageAt = conv.LastMessageAt
        };

        return ApiResponse<ConversationDetailDto>.SuccessResponse(detail);
    }

    public async Task<ApiResponse<ConversationDto>> CloseConversationAsync(
        int userId, int conversationId)
    {
        var conv = await LoadConversationWithParticipants(conversationId);

        if (conv == null)
            return ApiResponse<ConversationDto>.ErrorResponse(
                "CONVERSATION_NOT_FOUND", "Conversation not found.");

        if (!IsParticipant(userId, conv))
            return ApiResponse<ConversationDto>.ErrorResponse(
                "ACCESS_DENIED", "You are not a participant of this conversation.");

        if (conv.ConversationStatus == ConversationStatus.Closed)
            return ApiResponse<ConversationDto>.ErrorResponse(
                "INVALID_STATUS_TRANSITION", "Conversation is already closed.");

        conv.ConversationStatus = ConversationStatus.Closed;
        await _db.SaveChangesAsync();

        await _audit.LogAsync("CLOSE_CONVERSATION", "Conversation",
            conv.ConversationID, "Closed conversation");

        return ApiResponse<ConversationDto>.SuccessResponse(MapToDto(conv),
            "Conversation closed successfully.");
    }

    // ════════════════════════════════════════════════════════════
    //  MESSAGES
    // ════════════════════════════════════════════════════════════

    public async Task<ApiResponse<PagedResponse<MessageDto>>> GetMessagesAsync(
        int userId, int conversationId, int page, int pageSize)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        var conv = await LoadConversationWithParticipants(conversationId);

        if (conv == null)
            return ApiResponse<PagedResponse<MessageDto>>.ErrorResponse(
                "CONVERSATION_NOT_FOUND", "Conversation not found.");

        if (!IsParticipant(userId, conv))
            return ApiResponse<PagedResponse<MessageDto>>.ErrorResponse(
                "ACCESS_DENIED", "You are not a participant of this conversation.");

        var query = _db.Messages
            .Where(m => m.ConversationID == conversationId)
            .Include(m => m.SenderUser);

        var totalItems = await query.CountAsync();

        var messages = await query
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = messages.Select(m => MapToMessageDto(m, userId)).ToList();

        var paged = new PagedResponse<MessageDto>
        {
            Items = items,
            Paging = new PagingInfo
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
            }
        };

        return ApiResponse<PagedResponse<MessageDto>>.SuccessResponse(paged);
    }

    public async Task<ApiResponse<MessageDto>> SendMessageAsync(
        int userId, SendMessageRequest request)
    {
        var conv = await LoadConversationWithParticipants(request.ConversationId);

        if (conv == null)
            return ApiResponse<MessageDto>.ErrorResponse(
                "CONVERSATION_NOT_FOUND", "Conversation not found.");

        if (!IsParticipant(userId, conv))
            return ApiResponse<MessageDto>.ErrorResponse(
                "ACCESS_DENIED", "You are not a participant of this conversation.");

        if (conv.ConversationStatus == ConversationStatus.Closed)
            return ApiResponse<MessageDto>.ErrorResponse(
                "INVALID_STATUS_TRANSITION", "Cannot send messages in a closed conversation.");

        var message = new Message
        {
            ConversationID = conv.ConversationID,
            SenderUserID = userId,
            MessageText = request.Content ?? string.Empty,
            AttachmentURLs = request.AttachmentUrl,
            IsRead = false,
            SentAt = DateTime.UtcNow
        };

        _db.Messages.Add(message);

        // Update conversation timestamp
        conv.LastMessageAt = message.SentAt;
        await _db.SaveChangesAsync();

        // Load sender for display name
        await _db.Entry(message).Reference(m => m.SenderUser).LoadAsync();

        await _audit.LogAsync("SEND_MESSAGE", "Message",
            message.MessageID,
            $"Sent message in conversation {conv.ConversationID}");

        return ApiResponse<MessageDto>.SuccessResponse(
            MapToMessageDto(message, userId), "Message sent successfully.");
    }

    public async Task<ApiResponse<string>> MarkReadAsync(int userId, int messageId)
    {
        var message = await _db.Messages
            .Include(m => m.Conversation)
                .ThenInclude(c => c.Connection).ThenInclude(cn => cn!.Startup)
            .Include(m => m.Conversation)
                .ThenInclude(c => c.Connection).ThenInclude(cn => cn!.Investor)
            .Include(m => m.Conversation)
                .ThenInclude(c => c.Mentorship).ThenInclude(ms => ms!.Startup)
            .Include(m => m.Conversation)
                .ThenInclude(c => c.Mentorship).ThenInclude(ms => ms!.Advisor)
            .FirstOrDefaultAsync(m => m.MessageID == messageId);

        if (message == null)
            return ApiResponse<string>.ErrorResponse(
                "MESSAGE_NOT_FOUND", "Message not found.");

        if (!IsParticipant(userId, message.Conversation))
            return ApiResponse<string>.ErrorResponse(
                "ACCESS_DENIED", "You are not a participant of this conversation.");

        // Only the receiver should mark messages as read
        if (message.SenderUserID == userId)
            return ApiResponse<string>.ErrorResponse(
                "VALIDATION_ERROR", "You cannot mark your own message as read.");

        if (message.IsRead)
            return ApiResponse<string>.SuccessResponse("Message is already read.");

        message.IsRead = true;
        message.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return ApiResponse<string>.SuccessResponse("Message marked as read.");
    }

    public async Task<ApiResponse<string>> MarkReadAllAsync(int userId, MarkReadAllRequest request)
    {
        var conv = await LoadConversationWithParticipants(request.ConversationId);

        if (conv == null)
            return ApiResponse<string>.ErrorResponse(
                "CONVERSATION_NOT_FOUND", "Conversation not found.");

        if (!IsParticipant(userId, conv))
            return ApiResponse<string>.ErrorResponse(
                "ACCESS_DENIED", "You are not a participant of this conversation.");

        var unreadMessages = await _db.Messages
            .Where(m => m.ConversationID == request.ConversationId
                     && m.SenderUserID != userId
                     && !m.IsRead)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var msg in unreadMessages)
        {
            msg.IsRead = true;
            msg.ReadAt = now;
        }

        await _db.SaveChangesAsync();

        return ApiResponse<string>.SuccessResponse(
            $"{unreadMessages.Count} message(s) marked as read.");
    }

    public async Task<ApiResponse<string>> MarkConversationReadAsync(int userId, int conversationId)
    {
        var conv = await LoadConversationWithParticipants(conversationId);

        if (conv == null)
            return ApiResponse<string>.ErrorResponse(
                "CONVERSATION_NOT_FOUND", "Conversation not found.");

        if (!IsParticipant(userId, conv))
            return ApiResponse<string>.ErrorResponse(
                "ACCESS_DENIED", "You are not a participant of this conversation.");

        var unreadMessages = await _db.Messages
            .Where(m => m.ConversationID == conversationId
                     && m.SenderUserID != userId
                     && !m.IsRead)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var msg in unreadMessages)
        {
            msg.IsRead = true;
            msg.ReadAt = now;
        }

        await _db.SaveChangesAsync();

        return ApiResponse<string>.SuccessResponse(
            $"{unreadMessages.Count} message(s) marked as read.");
    }

    // ════════════════════════════════════════════════════════════
    //  HELPERS
    // ════════════════════════════════════════════════════════════

    private async Task<Conversation?> LoadConversationWithParticipants(int conversationId)
    {
        return await _db.Conversations
            .Include(c => c.Connection).ThenInclude(cn => cn!.Startup)
            .Include(c => c.Connection).ThenInclude(cn => cn!.Investor)
            .Include(c => c.Mentorship).ThenInclude(m => m!.Startup)
            .Include(c => c.Mentorship).ThenInclude(m => m!.Advisor)
            .FirstOrDefaultAsync(c => c.ConversationID == conversationId);
    }

    private static bool IsParticipant(int userId, Conversation conv)
    {
        if (conv.Connection != null)
            return conv.Connection.Startup.UserID == userId
                || conv.Connection.Investor.UserID == userId;

        if (conv.Mentorship != null)
            return conv.Mentorship.Startup.UserID == userId
                || conv.Mentorship.Advisor.UserID == userId;

        return false;
    }

    private static (int id, string name, string role) GetOtherParticipant(Conversation conv, int userId)
    {
        if (conv.Connection != null)
        {
            if (conv.Connection.Startup.UserID == userId)
                return (conv.Connection.Investor.UserID, conv.Connection.Investor.FullName, "Investor");
            return (conv.Connection.Startup.UserID, conv.Connection.Startup.CompanyName, "Startup");
        }
        if (conv.Mentorship != null)
        {
            if (conv.Mentorship.Startup.UserID == userId)
                return (conv.Mentorship.Advisor.UserID, conv.Mentorship.Advisor.FullName, "Advisor");
            return (conv.Mentorship.Startup.UserID, conv.Mentorship.Startup.CompanyName, "Startup");
        }
        return (0, "Unknown", "Unknown");
    }

    private static string BuildTitle(Conversation conv, int currentUserId)
    {
        if (conv.Connection != null)
        {
            // Show the other party's name
            if (conv.Connection.Startup.UserID == currentUserId)
                return conv.Connection.Investor.FullName;
            return conv.Connection.Startup.CompanyName;
        }

        if (conv.Mentorship != null)
        {
            if (conv.Mentorship.Startup.UserID == currentUserId)
                return conv.Mentorship.Advisor.FullName;
            return conv.Mentorship.Startup.CompanyName;
        }

        return "Conversation";
    }

    private static List<ParticipantDto> BuildParticipants(Conversation conv)
    {
        var list = new List<ParticipantDto>();

        if (conv.Connection != null)
        {
            list.Add(new ParticipantDto
            {
                UserId = conv.Connection.Startup.UserID,
                DisplayName = conv.Connection.Startup.CompanyName,
                UserType = "Startup"
            });
            list.Add(new ParticipantDto
            {
                UserId = conv.Connection.Investor.UserID,
                DisplayName = conv.Connection.Investor.FullName,
                UserType = "Investor"
            });
        }
        else if (conv.Mentorship != null)
        {
            list.Add(new ParticipantDto
            {
                UserId = conv.Mentorship.Startup.UserID,
                DisplayName = conv.Mentorship.Startup.CompanyName,
                UserType = "Startup"
            });
            list.Add(new ParticipantDto
            {
                UserId = conv.Mentorship.Advisor.UserID,
                DisplayName = conv.Mentorship.Advisor.FullName,
                UserType = "Advisor"
            });
        }

        return list;
    }

    private static ConversationDto MapToDto(Conversation c) => new()
    {
        ConversationId = c.ConversationID,
        ConnectionId = c.ConnectionID,
        MentorshipId = c.MentorshipID,
        Status = c.ConversationStatus.ToString(),
        CreatedAt = c.CreatedAt,
        LastMessageAt = c.LastMessageAt
    };

    private static MessageDto MapToMessageDto(Message m, int currentUserId)
    {
        var senderName = GetSenderDisplayName(m.SenderUser);

        return new MessageDto
        {
            MessageId = m.MessageID,
            ConversationId = m.ConversationID,
            SenderUserId = m.SenderUserID,
            SenderDisplayName = senderName,
            IsMine = m.SenderUserID == currentUserId,
            Content = m.MessageText,
            AttachmentUrls = m.AttachmentURLs,
            IsRead = m.IsRead,
            SentAt = m.SentAt,
            ReadAt = m.ReadAt
        };
    }

    private static string GetSenderDisplayName(User user)
    {
        // The sender's display name comes from their profile entity
        if (user.Startup != null) return user.Startup.CompanyName;
        if (user.Investor != null) return user.Investor.FullName;
        if (user.Advisor != null) return user.Advisor.FullName;
        return user.Email;
    }
}
