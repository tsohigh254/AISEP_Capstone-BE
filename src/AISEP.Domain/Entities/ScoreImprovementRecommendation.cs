using AISEP.Domain.Enums;

namespace AISEP.Domain.Entities;

public class ScoreImprovementRecommendation
{
    public int RecommendationID { get; set; }
    public int ScoreID { get; set; }
    public string Category { get; set; } = string.Empty;
    public RecommendationPriority Priority { get; set; }
    public string? RecommendationText { get; set; }
    public string? ExpectedImpact { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public StartupPotentialScore PotentialScore { get; set; } = null!;
}
