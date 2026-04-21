namespace AISEP.Application.DTOs.Staff;

public class StaffDashboardStatsDto
{
    public int TotalUsers { get; set; }
    public int LockedAccounts { get; set; }
    public int PendingKycCount { get; set; }
    public bool AiServiceOnline { get; set; }
    public DateTime CheckedAt { get; set; }
}

public class KycTrendDto
{
    public string Period { get; set; } = string.Empty;
    public List<KycTrendPointDto> Points { get; set; } = new();
}

public class KycTrendPointDto
{
    public string Date { get; set; } = string.Empty;
    public int Submitted { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
}

public class ActivityFeedItemDto
{
    public int LogId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string? ActionDetails { get; set; }
    public int? UserId { get; set; }
    public string? UserEmail { get; set; }
    public DateTime CreatedAt { get; set; }
}
