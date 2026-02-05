namespace AISEP.Domain.Entities;

public class InvestorStageFocus
{
    public int StageFocusID { get; set; }
    public int InvestorID { get; set; }
    public string Stage { get; set; } = string.Empty;

    // Navigation properties
    public Investor Investor { get; set; } = null!;
}
