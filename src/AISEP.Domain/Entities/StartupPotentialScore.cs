namespace AISEP.Domain.Entities;

public class StartupPotentialScore
{
    public int ScoreID { get; set; }
    public int StartupID { get; set; }
    public int? ConfigID { get; set; }
    public float OverallScore { get; set; }
    public float TeamScore { get; set; }
    public float MarketScore { get; set; }
    public float ProductScore { get; set; }
    public float TractionScore { get; set; }
    public float FinancialScore { get; set; }
    public DateTime CalculatedAt { get; set; }
    public bool IsCurrentScore { get; set; }
    public float? PitchDeckOverallScore { get; set; }
    public float? BusinessPlanOverallScore { get; set; }
    public int? EvaluationRunID { get; set; }

    // Navigation properties
    public Startup Startup { get; set; } = null!;
    public ScoringModelConfiguration? ScoringConfiguration { get; set; }
    public ICollection<ScoreSubMetric> SubMetrics { get; set; } = new List<ScoreSubMetric>();
    public ICollection<ScoreImprovementRecommendation> ImprovementRecommendations { get; set; } = new List<ScoreImprovementRecommendation>();
}
