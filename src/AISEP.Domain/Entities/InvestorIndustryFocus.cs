namespace AISEP.Domain.Entities;

public class InvestorIndustryFocus
{
    public int FocusID { get; set; }
    public int InvestorID { get; set; }
    public string Industry { get; set; } = string.Empty;

    // Navigation properties
    public Investor Investor { get; set; } = null!;
}
