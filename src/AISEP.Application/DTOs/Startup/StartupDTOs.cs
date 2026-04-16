using AISEP.Domain.Enums;
using Microsoft.AspNetCore.Http;
using AISEP.Application.DTOs.Document;
using System.Linq;

namespace AISEP.Application.DTOs.Startup;

// ========== REQUEST DTOs ==========

public class CreateStartupRequest
{
    public string CompanyName { get; set; } = null!;
    public string OneLiner { get; set; } = null!;
    public string? Description { get; set; }
    /// <summary>FK to Industries table</summary>
    public int? IndustryID { get; set; }
    public string? SubIndustry { get; set; }
    /// <summary>Enum name: Idea, PreSeed, Seed, SeriesA, SeriesB, SeriesC, Growth</summary>
    public StartupStage Stage { get; set; }
    public DateTime? FoundedDate { get; set; }
    public string? Website { get; set; }
    public IFormFile? LogoUrl { get; set; }
    public decimal? FundingAmountSought { get; set; }
    public decimal? CurrentFundingRaised { get; set; }
    public decimal? Valuation { get; set; }

    public string? BusinessCode { get; set; }
    public string? FullNameOfApplicant { get; set; } 
    public string? RoleOfApplicant { get; set; } 
    public string? ContactEmail { get; set; } 
    public string? ContactPhone { get; set; }

    // UI Extra Requirements
    public string? MarketScope { get; set; }
    public string? ProductStatus { get; set; }
    public string? Location { get; set; }
    public string? Country { get; set; }
    public string? ProblemStatement { get; set; }
    public string? SolutionSummary { get; set; }
    public List<string> CurrentNeeds { get; set; } = new();
    public string? MetricSummary { get; set; }
    public string? TeamSize { get; set; }
    public string? PitchDeckUrl { get; set; }
    public string? LinkedInURL { get; set; }
    public IFormFile? FileCertificateBusiness { get; set; }
}

public class UpdateStartupRequest
{
    public string? CompanyName { get; set; }
    public string? OneLiner { get; set; }
    public string? Description { get; set; }
    /// <summary>FK to Industries table</summary>
    public int? IndustryID { get; set; }
    public string? SubIndustry { get; set; }
    /// <summary>Enum name: Idea, PreSeed, Seed, SeriesA, SeriesB, SeriesC, Growth</summary>
    public StartupStage? Stage { get; set; }
    public DateTime? FoundedDate { get; set; }
    public string? Website { get; set; }
    public IFormFile? LogoUrl { get; set; }
    public decimal? FundingAmountSought { get; set; }
    public decimal? CurrentFundingRaised { get; set; }
    public decimal? Valuation { get; set; }

    public string? BusinessCode { get; set; }
    public string? FullNameOfApplicant { get; set; }
    public string? RoleOfApplicant { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }

    // UI Extra Requirements
    public string? MarketScope { get; set; }
    public string? ProductStatus { get; set; }
    public string? Location { get; set; }
    public string? Country { get; set; }
    public string? ProblemStatement { get; set; }
    public string? SolutionSummary { get; set; }
    public List<string>? CurrentNeeds { get; set; }
    public string? MetricSummary { get; set; }
    public string? TeamSize { get; set; }
    public string? PitchDeckUrl { get; set; }
    public string? LinkedInURL { get; set; }
    public IFormFile? FileCertificateBusiness { get; set; }
}

public class SubmitStartupKYCRequest
{
    public string StartupVerificationType { get; set; } = string.Empty;
    public string? LegalFullName { get; set; }
    public string? ProjectName { get; set; }
    public string? EnterpriseCode { get; set; }
    public string RepresentativeFullName { get; set; } = string.Empty;
    public string RepresentativeRole { get; set; } = string.Empty;
    public string WorkEmail { get; set; } = string.Empty;
    public string? PublicLink { get; set; }
    public List<IFormFile> EvidenceFiles { get; set; } = new();
    public List<string> EvidenceFileKinds { get; set; } = new();

    // Legacy fields kept for backward compatibility during migration.
    public string? CompanyName { get; set; }
    public string? IndustryName { get; set; }
    public string? Stage { get; set; }
    public string? FullNameOfApplicant { get; set; }
    public string? RoleOfApplicant { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? BusinessCode { get; set; }
    public string? Website { get; set; }
    public string? LinkedInURL { get; set; }
    public string? ProblemStatement { get; set; }
    public string? SolutionSummary { get; set; }
}

public class SaveStartupKYCDraftRequest : SubmitStartupKYCRequest
{
    // All fields are optional on the frontend for drafts
}

public class ToggleVisibilityRequest
{
    public bool IsVisible { get; set; }
}

// ========== RESPONSE DTOs ==========

/// <summary>Full profile for startup owner (/me endpoint)</summary>
public class StartupMeDto
{
    public int StartupID { get; set; }
    public int UserID { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string OneLiner { get; set; } = null!;    
    public string? Description { get; set; }
    public int? IndustryID { get; set; }
    public string? IndustryName { get; set; }
    public string? Stage { get; set; }
    public DateTime? FoundedDate { get; set; }
    public string? Website { get; set; }
    public string? LogoURL { get; set; }
    
    // Original DB Fields 
    public decimal? FundingAmountSought { get; set; }
    public decimal? CurrentFundingRaised { get; set; }  
    public decimal? Valuation { get; set; }

    public string FullNameOfApplicant { get; set; } = string.Empty;
    public string RoleOfApplicant { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string BusinessCode { get; set; } = string.Empty;
    // UI Additions
    public string? SubIndustry { get; set; }
    public string? MarketScope { get; set; }
    public string? ProductStatus { get; set; }
    public string? Location { get; set; }
    public string? Country { get; set; }
    public string? ProblemStatement { get; set; }
    public string? SolutionSummary { get; set; }
    public bool IsVisible { get; set; }
    public List<string> CurrentNeeds { get; set; } = new();
    public string? MetricSummary { get; set; }
    public string? TeamSize { get; set; }
    public string? PitchDeckUrl { get; set; }

    public string? FileCertificateBusiness { get; set; }
    public string? LinkedInURL { get; set; }
    public string? ProfileStatus { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    // Documents & IP (owner-facing)
    public List<DocumentDto> Documents { get; set; } = new();
    public bool HasDocuments => Documents != null && Documents.Count > 0;
}

/// <summary>Public view for investors/advisors (no sensitive data)</summary>
public class StartupPublicDto
{
    public int StartupID { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string OneLiner { get; set; } = null!;
    public string? Description { get; set; }
    public int? IndustryID { get; set; }
    public string? IndustryName { get; set; }
    public string? ParentIndustryName { get; set; }
    public string? Stage { get; set; }
    public DateTime? FoundedDate { get; set; }
    public string? Website { get; set; }
    public string? LogoURL { get; set; }
    
    // Original DB Fields 
    public decimal? FundingAmountSought { get; set; }
    public decimal? CurrentFundingRaised { get; set; }
    
    // UI Format aliases
    public decimal? TargetFunding => FundingAmountSought;
    public decimal? RaisedAmount => CurrentFundingRaised;

    // Public UI additions
    public string? SubIndustry { get; set; }
    public string? MarketScope { get; set; }
    public string? ProductStatus { get; set; }
    public string? Location { get; set; }
    public string? Country { get; set; }
    public string? ProblemStatement { get; set; }
    public string? SolutionSummary { get; set; }
    public List<string> CurrentNeeds { get; set; } = new();
    public string? MetricSummary { get; set; }
    public string? PitchDeckUrl { get; set; }
    public string? LinkedInURL { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? TeamSize { get; set; }
    public double? AiScore { get; set; }

    /// <summary>
    /// Mã đăng ký doanh nghiệp từ KYC đã được duyệt (Approved).
    /// Chỉ có giá trị khi startup có pháp nhân (WithLegalEntity) và đã KYC approved.
    /// Trả về null nếu startup WithoutLegalEntity hoặc chưa qua KYC.
    /// </summary>
    public string? EnterpriseCode { get; set; }

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
    public string? StartupVerificationType { get; set; }
    /// <summary>NONE | REQUESTED | ACCEPTED | IN_DISCUSSION. Null when caller is not an investor.</summary>
    public string? ConnectionStatus { get; set; }
    /// <summary>ID of the active connection record. Null when ConnectionStatus is NONE or caller is not investor.</summary>
    public int? ConnectionId { get; set; }
    /// <summary>Whether the current investor can send a new connection request.</summary>
    public bool CanRequestConnection { get; set; }
    /// <summary>"INVESTOR" if investor initiated, "STARTUP" if startup initiated. Null when no active connection.</summary>
    public string? InitiatedByRole { get; set; }
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
    public int? YearsOfExperience { get; set; }
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

public class StartupDto
{
    public int StartupID { get; set; }
    public int UserID { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string OneLiner { get; set; } = null!;
    public string? Description { get; set; }
    public int? IndustryID { get; set; }
    public string? IndustryName { get; set; }
    public string? Stage { get; set; }
    public DateTime? FoundedDate { get; set; }
    public string? Website { get; set; }
    public string? LogoURL { get; set; }

    // Original DB Fields 
    public decimal? FundingAmountSought { get; set; }
    public decimal? CurrentFundingRaised { get; set; }
    public decimal? Valuation { get; set; }

    public string FullNameOfApplicant { get; set; } = string.Empty;
    public string RoleOfApplicant { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string BusinessCode { get; set; } = string.Empty;

    // Public UI additions
    public string? SubIndustry { get; set; }
    public string? MarketScope { get; set; }
    public string? ProblemStatement { get; set; }
    public string? ProductStatus { get; set; }
    public string? Location { get; set; }
    public string? Country { get; set; }
    public string? SolutionSummary { get; set; }
    public List<string> CurrentNeeds { get; set; } = new();
    public string? MetricSummary { get; set; }
    public string? PitchDeckUrl { get; set; }
    public string? LinkedInURL { get; set; }
    public string? TeamSize { get; set; }

    public string? ProfileStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<TeamMemberPublicDto> TeamMembers { get; set; } = new();
}



