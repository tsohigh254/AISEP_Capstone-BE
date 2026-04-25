using AISEP.Domain.Enums;

namespace AISEP.Domain.Entities;

/// <summary>
/// Stores the latest readiness assessment for a startup.
/// One snapshot per startup (upserted on each calculation).
/// </summary>
public class StartupReadinessSnapshot
{
    public int Id { get; set; }
    public int StartupID { get; set; }

    // Scores
    public int OverallScore { get; set; }
    public ReadinessStatus Status { get; set; }
    public int ProfileScore { get; set; }
    public int KycScore { get; set; }
    public int DocumentScore { get; set; }
    public int AiScore { get; set; }
    public int TrustScore { get; set; }

    // Structured guidance (stored as JSON)
    public string MissingItemsJson { get; set; } = "[]";
    public string RecommendationsJson { get; set; } = "[]";

    public DateTime CalculatedAt { get; set; }

    // Navigation
    public Startup Startup { get; set; } = null!;
}
