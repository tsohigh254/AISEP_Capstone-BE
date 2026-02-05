namespace AISEP.Domain.Entities;

public class SavedReport
{
    public int ReportID { get; set; }
    public string ReportName { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public string? Parameters { get; set; } // JSON
    public int CreatedBy { get; set; }
    public bool IsScheduled { get; set; }
    public string? ScheduleFrequency { get; set; }
    public DateTime? LastGeneratedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public User CreatedByUser { get; set; } = null!;
}
