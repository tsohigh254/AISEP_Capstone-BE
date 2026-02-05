namespace AISEP.Domain.Entities;

public class ScoreSubMetric
{
    public int SubMetricID { get; set; }
    public int ScoreID { get; set; }
    public string Category { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public string? MetricValue { get; set; }
    public float MetricScore { get; set; }
    public string? Explanation { get; set; }

    // Navigation properties
    public StartupPotentialScore PotentialScore { get; set; } = null!;
}
