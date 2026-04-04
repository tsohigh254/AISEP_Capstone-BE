namespace AISEP.Domain.Entities;

public class AdvisorAvailability
{
    public int AvailabilityID { get; set; }
    public int AdvisorID { get; set; }
    public string? SessionFormats { get; set; }
    public int? TypicalSessionDuration { get; set; }
    public int? WeeklyAvailableHours { get; set; }
    public int? MaxConcurrentMentees { get; set; }
    public string? ResponseTimeCommitment { get; set; }
    public bool IsAcceptingNewMentees { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Advisor Advisor { get; set; } = null!;
}
