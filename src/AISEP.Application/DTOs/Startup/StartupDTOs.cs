using AISEP.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace AISEP.Application.DTOs.Startup;

// ========== REQUEST DTOs ==========

public class CreateStartupRequest
{
    public string CompanyName { get; set; } = null!;
    public string OneLiner { get; set; }
    public string? Description { get; set; }
    /// <summary>FK to Industries table</summary>
    public int? IndustryID { get; set; }
    /// <summary>Enum name: Idea, PreSeed, Seed, SeriesA, SeriesB, SeriesC, Growth</summary>
    public StartupStage Stage { get; set; }
    public DateTime? FoundedDate { get; set; }
    public string? Website { get; set; }
    public IFormFile? LogoUrl { get; set; }
    public decimal? FundingAmountSought { get; set; }
    public decimal? CurrentFundingRaised { get; set; }
    public decimal? Valuation { get; set; }
}

public class UpdateStartupRequest
{
    public string? CompanyName { get; set; }
    public string OneLiner { get; set; }
    public string? Description { get; set; }
    public int? IndustryID { get; set; }
    public StartupStage Stage { get; set; }
    public DateTime? FoundedDate { get; set; }
    public string? Website { get; set; }
    public IFormFile? LogoUrl { get; set; }
    public decimal? FundingAmountSought { get; set; }
    public decimal? CurrentFundingRaised { get; set; }
    public decimal? Valuation { get; set; }
}

// ========== RESPONSE DTOs ==========

/// <summary>Full profile for startup owner (/me endpoint)</summary>
public class StartupMeDto
{
    public int StartupID { get; set; }
    public int UserID { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string OneLiner { get; set; }    
    public string? Description { get; set; }
    public int? IndustryID { get; set; }
    public string? IndustryName { get; set; }
    public string? Stage { get; set; }
    public DateTime? FoundedDate { get; set; }
    public string? Website { get; set; }
    public string? LogoURL { get; set; }
    public decimal? FundingAmountSought { get; set; }
    public decimal? CurrentFundingRaised { get; set; }
    public decimal? Valuation { get; set; }
    public string? ProfileStatus { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<TeamMemberDto> TeamMembers { get; set; } = new();
}

/// <summary>Public view for investors/advisors (no sensitive data)</summary>
public class StartupPublicDto
{
    public int StartupID { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string OneLiner { get; set; }
    public string? Description { get; set; }
    public int? IndustryID { get; set; }
    public string? IndustryName { get; set; }
    public string? Stage { get; set; }
    public DateTime? FoundedDate { get; set; }
    public string? Website { get; set; }
    public string? LogoURL { get; set; }
    public decimal? FundingAmountSought { get; set; }
    public decimal? CurrentFundingRaised { get; set; }
    public string? ProfileStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<TeamMemberPublicDto> TeamMembers { get; set; } = new();
}

/// <summary>List item for search results</summary>
public class StartupListItemDto
{
    public int StartupID { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? IndustryName { get; set; }
    public string? Stage { get; set; }
    public string? LogoURL { get; set; }
    public string? ProfileStatus { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

// ========== TEAM MEMBER DTOs ==========

public class TeamMemberDto
{
    public int TeamMemberID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string? Title { get; set; }
    public string? LinkedInURL { get; set; }
    public string? Bio { get; set; }
    public string? PhotoURL { get; set; }
    public bool IsFounder { get; set; }
    public int? YearsOfExperience { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Public team member info (no sensitive data)</summary>
public class TeamMemberPublicDto
{
    public string FullName { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string? Title { get; set; }
    public string? LinkedInURL { get; set; }
    public string? Bio { get; set; }
    public string? PhotoURL { get; set; }
    public bool IsFounder { get; set; }
}

public class CreateTeamMemberRequest
{
    public string FullName { get; set; } = null!;
    public string? Role { get; set; }
    public string? Title { get; set; }
    public string? LinkedInURL { get; set; }
    public string? Bio { get; set; }
    public IFormFile? PhotoURL { get; set; }
    public bool IsFounder { get; set; }
    public int? YearsOfExperience { get; set; }
}

public class UpdateTeamMemberRequest
{
    public string? FullName { get; set; }
    public string? Role { get; set; }
    public string? Title { get; set; }
    public string? LinkedInURL { get; set; }
    public string? Bio { get; set; }
    public IFormFile? PhotoURL { get; set; }
    public bool? IsFounder { get; set; }
    public int? YearsOfExperience { get; set; }
}
