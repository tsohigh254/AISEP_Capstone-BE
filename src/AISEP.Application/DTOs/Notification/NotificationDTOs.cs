namespace AISEP.Application.DTOs.Notification;

// ── Response DTOs ──────────────────────────────────────────────

/// <summary>Full notification detail.</summary>
public class NotificationDto
{
    public int NotificationId { get; set; }
    public string NotificationType { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Message { get; set; }
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public bool IsSent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

/// <summary>Compact notification for list views.</summary>
public class NotificationListItemDto
{
    public int NotificationId { get; set; }
    public string NotificationType { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? MessagePreview { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ActionUrl { get; set; }
}

/// <summary>Result of mark-all-read operation.</summary>
public class MarkAllResultDto
{
    public int UpdatedCount { get; set; }
}

// ── Request DTOs ───────────────────────────────────────────────

/// <summary>Mark a single notification read/unread.</summary>
public class MarkReadRequest
{
    /// <summary>True to mark read, false to unread. Defaults to true when null.</summary>
    public bool? IsRead { get; set; }
}

/// <summary>Request to create a new notification.</summary>
public class CreateNotificationRequest
{
    public int UserId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
    public string? ActionUrl { get; set; }
}
