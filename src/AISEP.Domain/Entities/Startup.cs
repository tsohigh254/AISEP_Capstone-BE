using AISEP.Domain.Enums;

namespace AISEP.Domain.Entities;

public class Startup
{
    public int StartupID { get; set; }
    public int UserID { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string OneLiner { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? IndustryID { get; set; }
    public StartupStage? Stage { get; set; }
    public DateTime? FoundedDate { get; set; }
    public string? Website { get; set; }
    public string? LogoURL { get; set; }
    public decimal? FundingAmountSought { get; set; }
    public decimal? CurrentFundingRaised { get; set; }
    public decimal? Valuation { get; set; }
    
    // UI Profile View requirements
    public string? SubIndustry { get; set; }
    public string? MarketScope { get; set; }
    public string? ProductStatus { get; set; }
    public string? Location { get; set; }
    public string? Country { get; set; }
    public string? ProblemStatement { get; set; }
    public string? SolutionSummary { get; set; }
    public string? CurrentNeeds { get; set; } // Stored as comma-separated or JSON string
    public string? MetricSummary { get; set; }
    public string? LinkedInURL { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public int TeamSize { get; set; }
    public bool IsVisible { get; set; } = true;
    
    public ProfileStatus ProfileStatus { get; set; } = ProfileStatus.Draft;
    public DateTime? ApprovedAt { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime CreatedAt { get; set; } 
    public DateTime? UpdatedAt { get; set; }

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
