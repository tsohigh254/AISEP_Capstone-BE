namespace AISEP.Domain.Entities;

public class Message
{
    public int MessageID { get; set; }
    public int ConversationID { get; set; }
    public int SenderUserID { get; set; }
    public string MessageText { get; set; } = string.Empty;
    public string? AttachmentURLs { get; set; } // JSON
    public bool IsRead { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? ReadAt { get; set; }

    // Navigation properties
    public Conversation Conversation { get; set; } = null!;
    public User SenderUser { get; set; } = null!;
}
