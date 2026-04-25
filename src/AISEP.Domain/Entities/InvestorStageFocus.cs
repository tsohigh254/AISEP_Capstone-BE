using AISEP.Domain.Enums;

namespace AISEP.Domain.Entities;

public class InvestorStageFocus
{
    public int StageFocusID { get; set; }
    public int InvestorID { get; set; }
    public int StageID { get; set; }

    // Navigation properties
    public Investor Investor { get; set; } = null!;
    public Stage StageRef { get; set; } = null!;
}
