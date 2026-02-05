namespace AISEP.Domain.Entities;

public class IndustryTrend
{
    public int TrendID { get; set; }
    public int IndustryID { get; set; }
    public string TrendPeriod { get; set; } = string.Empty;
    public int StartupCount { get; set; }
    public float? AveragePotentialScore { get; set; }
    public decimal? TotalFundingRaised { get; set; }
    public decimal? AverageRoundSize { get; set; }
    public string? TopStrengths { get; set; }
    public string? CommonWeaknesses { get; set; }
    public string? AIGeneratedInsights { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Industry Industry { get; set; } = null!;
}
