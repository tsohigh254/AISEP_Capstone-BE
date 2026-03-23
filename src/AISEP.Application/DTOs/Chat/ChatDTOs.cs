namespace AISEP.Application.DTOs.Chat;

// ─── Request DTOs ──────────────────────────────────────────────

public class CreateConversationRequest
{
    public int? MentorshipId { get; set; }
    public int? ConnectionId { get; set; }
}

public class SendMessageRequest
{
    public int ConversationId { get; set; }
    public string? Content { get; set; }
    public string? AttachmentUrl { get; set; }
}

public class MarkReadAllRequest
{
    public int ConversationId { get; set; }
}

// ─── Response DTOs ─────────────────────────────────────────────

public class ConversationDto
{
    public int ConversationId { get; set; }
    public int? ConnectionId { get; set; }
    public int? MentorshipId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastMessageAt { get; set; }
}

public class ConversationListItemDto
{
    public int ConversationId { get; set; }
    public int? ConnectionId { get; set; }
    public int? MentorshipId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    // Other participant info (for FE display)
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantRole { get; set; } = string.Empty;   // "Startup" | "Investor" | "Advisor"
    public string? ParticipantAvatarUrl { get; set; }
    public string? LastMessagePreview { get; set; }
    public int UnreadCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastMessageAt { get; set; }
}

public class ConversationDetailDto
{
    public int ConversationId { get; set; }
    public int? ConnectionId { get; set; }
    public int? MentorshipId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<ParticipantDto> Participants { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? LastMessageAt { get; set; }
}

public class ParticipantDto
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty;
}

public class MessageDto
{
    public int MessageId { get; set; }
    public int ConversationId { get; set; }
    public int SenderUserId { get; set; }
    public string SenderDisplayName { get; set; } = string.Empty;
    public bool IsMine { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? AttachmentUrls { get; set; }
    public bool IsRead { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

/// <summary>Payload broadcast qua SignalR event "ReceiveMessage" — aligned với FE IIncomingMessage.</summary>
public class SignalRMessageDto
{
    public int MessageId { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
