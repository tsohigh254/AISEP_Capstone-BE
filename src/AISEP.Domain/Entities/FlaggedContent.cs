namespace AISEP.Domain.Entities;

public class FlaggedContent
{
    public int FlagID { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public int ContentID { get; set; }
    public int? RelatedUserID { get; set; }
    public string FlagReason { get; set; } = string.Empty;
    public string? FlagSource { get; set; }
    public string? Severity { get; set; }
    public string? FlagDetails { get; set; }
    public string ModerationStatus { get; set; } = string.Empty;
    public DateTime FlaggedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int? ReviewedBy { get; set; }
    public string? ModerationAction { get; set; }
    public string? ModeratorNotes { get; set; }

    // Navigation properties
    public User? RelatedUser { get; set; }
    public User? ReviewedByUser { get; set; }
    public ICollection<ModerationAction> ModerationActions { get; set; } = new List<ModerationAction>();
}
