namespace AISEP.Application.DTOs.Investor;

// ========== REQUEST DTOs ==========

public class CreateInvestorRequest
{
    public string FullName { get; set; } = null!;
    public string? FirmName { get; set; }
    public string? Title { get; set; }
    public string? Bio { get; set; }
    public string? InvestmentThesis { get; set; }
    public string? Location { get; set; }
    public string? Country { get; set; }
    public string? LinkedInURL { get; set; }
    public string? Website { get; set; }
}

public class UpdateInvestorRequest
{
    public string? FullName { get; set; }
    public string? FirmName { get; set; }
    public string? Title { get; set; }
    public string? Bio { get; set; }
    public string? InvestmentThesis { get; set; }
    public string? Location { get; set; }
    public string? Country { get; set; }
    public string? LinkedInURL { get; set; }
    public string? Website { get; set; }
}

public class UpdatePreferencesRequest
{
    public decimal? TicketMin { get; set; }
    public decimal? TicketMax { get; set; }
    /// <summary>Preferred stages, e.g. ["Seed", "Series A"]</summary>
    public List<string>? PreferredStages { get; set; }
    /// <summary>Preferred industry names, e.g. ["Fintech", "HealthTech"]</summary>
    public List<string>? PreferredIndustries { get; set; }
    public string? PreferredGeographies { get; set; }
    public float? MinPotentialScore { get; set; }
}

public class WatchlistAddRequest
{
    public int StartupId { get; set; }
    public string? WatchReason { get; set; }
    public string? Priority { get; set; } // Low, Medium, High
}

// ========== RESPONSE DTOs ==========

public class InvestorDto
{
    public int InvestorID { get; set; }
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
    public string ProfileStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class PreferencesDto
{
    public decimal? TicketMin { get; set; }
    public decimal? TicketMax { get; set; }
    public List<string> PreferredStages { get; set; } = new();
    public List<string> PreferredIndustries { get; set; } = new();
    public string? PreferredGeographies { get; set; }
    public float? MinPotentialScore { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class WatchlistItemDto
{
    public int WatchlistID { get; set; }
    public int StartupID { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public string? Stage { get; set; }
    public string? LogoURL { get; set; }
    public string Priority { get; set; } = "Medium";
    public DateTime AddedAt { get; set; }
}

/// <summary>Investor search result DTO — used by Startup role to browse investors</summary>
public class InvestorSearchItemDto
{
    public int InvestorID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? FirmName { get; set; }
    public string? Title { get; set; }
    public string? Bio { get; set; }
    public string? ProfilePhotoURL { get; set; }
    public string? Location { get; set; }
    public string? Country { get; set; }
    public string? LinkedInURL { get; set; }
    public string? Website { get; set; }
    public List<string> PreferredIndustries { get; set; } = new();
    public List<string> PreferredStages { get; set; } = new();
    public string? PreferredGeographies { get; set; }
    public decimal? TicketSizeMin { get; set; }
    public decimal? TicketSizeMax { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Startup search result DTO (no sensitive data exposed)</summary>
public class StartupSearchItemDto
{
    public int StartupID { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? Stage { get; set; }
    public string? IndustryName { get; set; }
    public string? SubIndustry { get; set; }
    public string? LogoURL { get; set; }
    public string? ProfileStatus { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
