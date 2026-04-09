namespace AISEP.Application.DTOs.Investor;

// ========== STAFF: INVESTOR KYC SUBMISSION DTO (mirrors StartupKycSubmissionDto) ==========

public class InvestorKycSubmissionDto
{
    public int Id { get; set; }
    public int InvestorId { get; set; }
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public string WorkflowStatus { get; set; } = string.Empty;
    public string ResultLabel { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int? ReviewedBy { get; set; }
    public string? Remarks { get; set; }
    public bool RequiresNewEvidence { get; set; }

    // Basic investor profile context for staff display
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ProfileStatus { get; set; }
    public string? ProfilePhotoURL { get; set; }

    public InvestorKYCSubmissionSummaryDto? SubmissionSummary { get; set; }
}

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

public class SubmitInvestorKYCRequest
{
    public string InvestorCategory { get; set; } = null!; // "INSTITUTIONAL" | "INDIVIDUAL_ANGEL"
    public string FullName { get; set; } = null!;
    public string ContactEmail { get; set; } = null!;
    public string? OrganizationName { get; set; }
    public string? CurrentRoleTitle { get; set; }
    public string? Location { get; set; }
    public string? Website { get; set; }
    public string? LinkedInURL { get; set; }
    public string? SubmitterRole { get; set; }
    public string? TaxIdOrBusinessCode { get; set; }
    public List<Microsoft.AspNetCore.Http.IFormFile> EvidenceFiles { get; set; } = new();
    public List<string> EvidenceFileKinds { get; set; } = new();
}

public class SaveInvestorKYCDraftRequest
{
    // All fields nullable — draft allows partial save
    public string? InvestorCategory { get; set; }
    public string? FullName { get; set; }
    public string? ContactEmail { get; set; }
    public string? OrganizationName { get; set; }
    public string? CurrentRoleTitle { get; set; }
    public string? Location { get; set; }
    public string? Website { get; set; }
    public string? LinkedInURL { get; set; }
    public string? SubmitterRole { get; set; }
    public string? TaxIdOrBusinessCode { get; set; }
    public List<Microsoft.AspNetCore.Http.IFormFile>? EvidenceFiles { get; set; }
    public List<string>? EvidenceFileKinds { get; set; }
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
    public List<string>? PreferredMarketScopes { get; set; }
    public List<string>? SupportOffered { get; set; }
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
    public int UserID { get; set; }
    public string Email { get; set; } = string.Empty;
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
    public string ProfileStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // KYC Information (populated from active submission when available)
    public string? InvestorType { get; set; }
    public string? ContactEmail { get; set; }
    public string? CurrentOrganization { get; set; }
    public string? CurrentRoleTitle { get; set; }
    public string? BusinessCode { get; set; }
    public string? SubmitterRole { get; set; }
    public string? IDProofFileURL { get; set; }
    public string? InvestmentProofFileURL { get; set; }
    public string? Remarks { get; set; }
}

public class InvestorKYCStatusDto
{
    public string WorkflowStatus { get; set; } = null!;
    public string VerificationLabel { get; set; } = null!;
    public string Explanation { get; set; } = string.Empty;
    public DateTime? LastUpdated { get; set; }
    public string? Remarks { get; set; }
    public bool RequiresNewEvidence { get; set; }
    public int? SubmissionId { get; set; }
    public int? Version { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<string>? FlaggedFields { get; set; }
    public InvestorKYCSubmissionSummaryDto? SubmissionSummary { get; set; }
    public List<InvestorKYCHistoryItemDto> History { get; set; } = new();
}

public class InvestorKYCHistoryItemDto
{
    public int SubmissionId { get; set; }
    public int Version { get; set; }
    public string WorkflowStatus { get; set; } = string.Empty;
    public string ResultLabel { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? Remarks { get; set; }
    public bool RequiresNewEvidence { get; set; }
}

public class InvestorKYCSubmissionSummaryDto
{
    public string? FullName { get; set; }
    public string? InvestorCategory { get; set; }
    public string? ContactEmail { get; set; }
    public string? OrganizationName { get; set; }
    public string? CurrentRoleTitle { get; set; }
    public string? Location { get; set; }
    public string? Website { get; set; }
    public string? LinkedInURL { get; set; }
    public string? SubmitterRole { get; set; }
    public string? TaxIdOrBusinessCode { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public int Version { get; set; }
    public List<InvestorKYCEvidenceFileDto> EvidenceFiles { get; set; } = new();
}

public class InvestorKYCEvidenceFileDto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? FileType { get; set; }
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string? StorageKey { get; set; }
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

// ========== INDUSTRY FOCUS DTOs ==========

public class IndustryFocusDto
{
    public int FocusId { get; set; }
    public string Industry { get; set; } = string.Empty;
}

public class AddIndustryFocusRequest
{
    public string Industry { get; set; } = string.Empty;
}

// ========== STAGE FOCUS DTOs ==========

public class StageFocusDto
{
    public int StageFocusId { get; set; }
    public string Stage { get; set; } = string.Empty;
}

public class AddStageFocusRequest
{
    public AISEP.Domain.Enums.StartupStage Stage { get; set; }
}

// ========== COMPARE DTOs ==========

public class StartupCompareDto
{
    public int StartupID { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? OneLiner { get; set; }
    public string? Stage { get; set; }
    public string? IndustryName { get; set; }
    public decimal? FundingAmountSought { get; set; }
    public decimal? CurrentFundingRaised { get; set; }
    public decimal? Valuation { get; set; }
    public int TeamSize { get; set; }
    public string? LogoURL { get; set; }
    public DateTime? FoundedDate { get; set; }
    public string? ProfileStatus { get; set; }
}

// ========== PREFERENCES DTO ==========

public class PreferencesDto
{
    public decimal? TicketMin { get; set; }
    public decimal? TicketMax { get; set; }
    public List<string> PreferredStages { get; set; } = new();
    public List<string> PreferredIndustries { get; set; } = new();
    public string? PreferredGeographies { get; set; }
    public float? MinPotentialScore { get; set; }
    public List<string> PreferredMarketScopes { get; set; } = new();
    public List<string> SupportOffered { get; set; } = new();
    public DateTime? UpdatedAt { get; set; }
}

// ========== WATCHLIST DTO ==========

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
