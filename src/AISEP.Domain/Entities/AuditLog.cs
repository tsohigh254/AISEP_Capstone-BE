namespace AISEP.Domain.Entities;

public class AuditLog
{
    public int LogID { get; set; }
    public int? UserID { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityID { get; set; }
    public string? ActionDetails { get; set; }
    public string IPAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public User? User { get; set; }
}
