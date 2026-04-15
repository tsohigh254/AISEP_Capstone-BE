namespace AISEP.Domain.Entities;

public class DocumentAccessLog
{
    public int LogID { get; set; }
    public int DocumentID { get; set; }
    public int UserID { get; set; }
    public string UserType { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTime AccessedAt { get; set; }
    public string? IpAddress { get; set; }

    // Navigation properties
    public Document Document { get; set; } = null!;
    public User User { get; set; } = null!;
}
