namespace AISEP.Domain.Entities;

/// <summary>
/// Weekly recurring availability slot for an advisor.
/// DayOfWeek: 0 = Monday ... 6 = Sunday.
/// StartTime / EndTime stored as "HH:mm" strings (local, no timezone).
/// </summary>
public class AdvisorTimeSlot
{
    public int TimeSlotID { get; set; }
    public int AdvisorID { get; set; }

    /// <summary>0 = Monday, 1 = Tuesday, …, 6 = Sunday.</summary>
    public int DayOfWeek { get; set; }

    /// <summary>Format "HH:mm", e.g. "09:00".</summary>
    public string StartTime { get; set; } = string.Empty;

    /// <summary>Format "HH:mm", e.g. "17:00".</summary>
    public string EndTime { get; set; } = string.Empty;

    // Navigation
    public Advisor Advisor { get; set; } = null!;
}
