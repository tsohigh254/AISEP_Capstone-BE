using AISEP.Domain.Enums;

namespace AISEP.Domain.Entities;

public class InvestorWatchlist
{
    public int WatchlistID { get; set; }
    public int InvestorID { get; set; }
    public int StartupID { get; set; }
    public string? WatchReason { get; set; }
    public WatchlistPriority? Priority { get; set; }
    public bool IsActive { get; set; }
    public DateTime AddedAt { get; set; }
    public DateTime? RemovedAt { get; set; }
    public DateTime? LastNotifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Investor Investor { get; set; } = null!;
    public Startup Startup { get; set; } = null!;
}
