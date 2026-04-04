using AISEP.Domain.Enums;
using System.Xml.Linq;

namespace AISEP.Domain.Entities;

public class Startup
{
    public int StartupID { get; set; }
    public int UserID { get; set; }

    // Company Information
    public string CompanyName { get; set; } = string.Empty;
    public string OneLiner { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? IndustryID { get; set; }
    public string? SubIndustry { get; set; }
    public StartupStage? Stage { get; set; }
    public DateTime? FoundedDate { get; set; }
    public string? Website { get; set; }
    public string? LogoURL { get; set; }

    // Funding Information
    public decimal? FundingAmountSought { get; set; }
    public decimal? CurrentFundingRaised { get; set; }
    public decimal? Valuation { get; set; }

    // Applicant Information (Required for staff review)
    public string FullNameOfApplicant { get; set; } = string.Empty;
    public string RoleOfApplicant { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string BusinessCode { get; set; }

    // Registration Documents (Required for staff review)
    public string? FileCertificateBusiness { get; set; }
    public string? LinkedInURL { get; set; }

    // Business Details
    public string? MarketScope { get; set; }
    public string? ProductStatus { get; set; }
    public string? Location { get; set; }
    public string? Country { get; set; }
    public string? ProblemStatement { get; set; }
    public string? SolutionSummary { get; set; }
    public string? CurrentNeeds { get; set; }
    public string? MetricSummary { get; set; }
    public string? TeamSize { get; set; }
    public string? PitchDeckUrl { get; set; }

    public bool IsVisible { get; set; }              

    // Approval Workflow
    public ProfileStatus ProfileStatus { get; set; } = ProfileStatus.Draft;
    public DateTime? ApprovedAt { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Assign tag to startup
    public StartupTag StartupTag { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public User? ApprovedByUser { get; set; }
    public Industry? Industry { get; set; }
    public ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<StartupPotentialScore> PotentialScores { get; set; } = new List<StartupPotentialScore>();
    public ICollection<StartupAdvisorMentorship> Mentorships { get; set; } = new List<StartupAdvisorMentorship>();
    public ICollection<StartupInvestorConnection> InvestorConnections { get; set; } = new List<StartupInvestorConnection>();
    public ICollection<InvestorWatchlist> WatchedByInvestors { get; set; } = new List<InvestorWatchlist>();
    public ICollection<AdvisorTestimonial> AdvisorTestimonials { get; set; } = new List<AdvisorTestimonial>();
}
