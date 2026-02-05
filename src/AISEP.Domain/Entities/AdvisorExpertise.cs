namespace AISEP.Domain.Entities;

public class AdvisorExpertise
{
    public int ExpertiseID { get; set; }
    public int AdvisorID { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? SubTopic { get; set; }
    public string? ProficiencyLevel { get; set; }

    // Navigation properties
    public Advisor Advisor { get; set; } = null!;
}
