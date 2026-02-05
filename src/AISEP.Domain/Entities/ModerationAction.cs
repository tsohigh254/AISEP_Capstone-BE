namespace AISEP.Domain.Entities;

public class ModerationAction
{
    public int ActionID { get; set; }
    public int FlagID { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public int? TargetUserID { get; set; }
    public string? ActionDetails { get; set; }
    public string? MessageToUser { get; set; }
    public int? ActionTakenBy { get; set; }
    public DateTime ActionTakenAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    // Navigation properties
    public FlaggedContent FlaggedContent { get; set; } = null!;
    public User? TargetUser { get; set; }
    public User? ActionTakenByUser { get; set; }
}
