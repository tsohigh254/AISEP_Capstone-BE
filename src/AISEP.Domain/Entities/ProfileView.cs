namespace AISEP.Domain.Entities;

public class ProfileView
{
    public int ViewID { get; set; }
    public int ViewerUserID { get; set; }
    public int? ViewedStartupID { get; set; }
    public int? ViewedInvestorID { get; set; }
    public int? ViewedAdvisorID { get; set; }
    public DateTime ViewedAt { get; set; }

    // Navigation properties
    public User ViewerUser { get; set; } = null!;
    public Startup? ViewedStartup { get; set; }
    public Investor? ViewedInvestor { get; set; }
    public Advisor? ViewedAdvisor { get; set; }
}
