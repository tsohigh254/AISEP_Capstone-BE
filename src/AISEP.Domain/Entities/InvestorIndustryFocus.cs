namespace AISEP.Domain.Entities;

public class InvestorIndustryFocus
{
    public int FocusID { get; set; }
    public int InvestorID { get; set; }
    public int IndustryID { get; set; }

    // Navigation properties
    public Investor Investor { get; set; } = null!;
    public Industry IndustryRef { get; set; } = null!;
}
