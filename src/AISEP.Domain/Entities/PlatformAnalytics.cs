namespace AISEP.Domain.Entities;

public class PlatformAnalytics
{
    public int AnalyticID { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public float MetricValue { get; set; }
    public DateTime MetricDate { get; set; }
    public string? Category { get; set; }
    public string? Metadata { get; set; } // JSON
    public DateTime CreatedAt { get; set; }
}
