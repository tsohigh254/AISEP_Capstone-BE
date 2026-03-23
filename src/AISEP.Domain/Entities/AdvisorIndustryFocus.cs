namespace AISEP.Domain.Entities;

public class AdvisorIndustryFocus
{
    public int IndustryFocusID { get; set; }
    public int AdvisorID { get; set; }
    public int IndustryID { get; set; }

    // Navigation properties
    public Advisor Advisor { get; set; } = null!;
    public Industry Industry { get; set; } = null!;
}
