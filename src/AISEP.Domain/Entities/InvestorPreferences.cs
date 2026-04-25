namespace AISEP.Domain.Entities;

public class InvestorPreferences
{
    public int PreferenceID { get; set; }
    public int InvestorID { get; set; }
    public float? MinPotentialScore { get; set; }
    public string? PreferredStageIDs { get; set; } // CSV of int
    public string? PreferredIndustryIDs { get; set; } // CSV of int
    public string? PreferredGeographies { get; set; }
    public string? PreferredMarketScopes { get; set; }     // CSV
    public string? SupportOffered { get; set; }             // CSV
    public decimal? MinInvestmentSize { get; set; }
    public decimal? MaxInvestmentSize { get; set; }

    // ── New preference fields (API spec 2026-04-09) ──
    public string? PreferredProductMaturity { get; set; }   // CSV: mvp,beta,launched,...
    public string? PreferredValidationLevel { get; set; }   // CSV: early_validation,traction,...
    public string? PreferredStrengths { get; set; }         // CSV: strong_team,market_traction,...
    public float? PreferredAiScoreMin { get; set; }
    public float? PreferredAiScoreMax { get; set; }
    public string? AiScoreImportance { get; set; }          // low | medium | high
    public string? AcceptingConnectionsStatus { get; set; } // active | paused | closed
    public bool RecentlyActiveBadge { get; set; }
    public bool RequireVerifiedStartups { get; set; }
    public bool RequireVisibleProfiles { get; set; }
    public string? AvoidText { get; set; }
    public string? Tags { get; set; }                       // CSV

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Investor Investor { get; set; } = null!;
}
