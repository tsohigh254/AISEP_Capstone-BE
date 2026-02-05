namespace AISEP.Domain.Entities;

public class InvestorPreferences
{
    public int PreferenceID { get; set; }
    public int InvestorID { get; set; }
    public float? MinPotentialScore { get; set; }
    public string? PreferredStages { get; set; }
    public string? PreferredIndustries { get; set; }
    public string? PreferredGeographies { get; set; }
    public decimal? MinInvestmentSize { get; set; }
    public decimal? MaxInvestmentSize { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Investor Investor { get; set; } = null!;
}
