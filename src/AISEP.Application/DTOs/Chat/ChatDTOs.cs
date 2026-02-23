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
