namespace AISEP.Domain.Entities;

public class AdvisorIndustryFocus
{
    public int IndustryFocusID { get; set; }
    public int AdvisorID { get; set; }
    public string Industry { get; set; } = string.Empty;

    // Navigation properties
    public Advisor Advisor { get; set; } = null!;
}
