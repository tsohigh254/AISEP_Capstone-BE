using AISEP.Domain.Enums;

namespace AISEP.Domain.Entities;

public class Investor
{
    public int InvestorID { get; set; }
    public int UserID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? FirmName { get; set; }
    public string? Title { get; set; }
    public string? Bio { get; set; }
    public string? ProfilePhotoURL { get; set; }
    public string? InvestmentThesis { get; set; }
    public string? Location { get; set; }
    public string? Country { get; set; }
    public string? LinkedInURL { get; set; }
    public string? Website { get; set; }
    public ProfileStatus ProfileStatus { get; set; } = ProfileStatus.Draft;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Approval Workflow
    public InvestorTag InvestorTag { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int? ApprovedBy { get; set; }
    public User? ApprovedByUser { get; set; }

    // KYC Information
    public InvestorType? InvestorType { get; set; }
    public string? ContactEmail { get; set; }
    public string? CurrentOrganization { get; set; }
    public string? CurrentRoleTitle { get; set; }
    public string? BusinessCode { get; set; }
    public string? SubmitterRole { get; set; }
    public string? IDProofFileURL { get; set; }
    public string? InvestmentProofFileURL { get; set; }
    public string? Remarks { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public InvestorPreferences? Preferences { get; set; }
    public ICollection<InvestorWatchlist> Watchlists { get; set; } = new List<InvestorWatchlist>();
    public ICollection<InvestorIndustryFocus> IndustryFocus { get; set; } = new List<InvestorIndustryFocus>();
    public ICollection<InvestorStageFocus> StageFocus { get; set; } = new List<InvestorStageFocus>();
    public ICollection<PortfolioCompany> PortfolioCompanies { get; set; } = new List<PortfolioCompany>();
    public ICollection<StartupInvestorConnection> StartupConnections { get; set; } = new List<StartupInvestorConnection>();
    public ICollection<InformationRequest> InformationRequests { get; set; } = new List<InformationRequest>();
}
