using AISEP.Domain.Enums;

namespace AISEP.Domain.Entities;

public class AdvisorExpertise
{
    public int ExpertiseID { get; set; }
    public int AdvisorID { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? SubTopic { get; set; }
    public ProficiencyLevel? ProficiencyLevel { get; set; }
    public int? YearsOfExperience { get; set; }

    // Navigation properties
    public Advisor Advisor { get; set; } = null!;
}
