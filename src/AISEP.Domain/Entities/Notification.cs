namespace AISEP.Domain.Entities;

public class Notification
{
    public int NotificationID { get; set; }
    public int UserID { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityID { get; set; }
    public string? ActionURL { get; set; }
    public bool IsRead { get; set; }
    public bool IsSent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
}
