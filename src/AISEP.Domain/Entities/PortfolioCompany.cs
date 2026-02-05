namespace AISEP.Domain.Entities;

public class PortfolioCompany
{
    public int PortfolioID { get; set; }
    public int InvestorID { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public string? InvestmentStage { get; set; }
    public DateTime? InvestmentDate { get; set; }
    public decimal? InvestmentAmount { get; set; }
    public string? CurrentStatus { get; set; }
    public string? ExitType { get; set; }
    public DateTime? ExitDate { get; set; }
    public decimal? ExitValue { get; set; }
    public string? Description { get; set; }
    public string? CompanyLogoURL { get; set; }

    // Navigation properties
    public Investor Investor { get; set; } = null!;
}
