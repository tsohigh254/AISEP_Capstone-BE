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

public class SetAcceptingConnectionsRequest
{
    public bool AcceptingConnections { get; set; }
}

public class AcceptingConnectionsDto
{
    public bool AcceptingConnections { get; set; }
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

    // ── New fields (API spec 2026-04-09) ──
    public List<string>? PreferredProductMaturity { get; set; }
    public List<string>? PreferredValidationLevel { get; set; }
    public List<string>? PreferredStrengths { get; set; }

    /// <summary>AI score filter range, e.g. { "min": 40, "max": 100 }</summary>
    public AiScoreRangeDto? PreferredAiScoreRange { get; set; }
    /// <summary>low | medium | high</summary>
    public string? AiScoreImportance { get; set; }

    /// <summary>active | paused | closed</summary>
    public string? AcceptingConnectionsStatus { get; set; }
    public bool? RecentlyActiveBadge { get; set; }

    public bool? RequireVerifiedStartups { get; set; }
    public bool? RequireVisibleProfiles { get; set; }

    /// <summary>Free-text describing what the investor does NOT want</summary>
    public string? AvoidText { get; set; }
    public List<string>? Tags { get; set; }
}

/// <summary>Nested AI score range filter.</summary>
public class AiScoreRangeDto
{
    public float? Min { get; set; }
    public float? Max { get; set; }
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
    public bool AcceptingConnections { get; set; }
    public string KycStatus { get; set; } = "NOT_STARTED";
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
    // Identity
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

    // KYC / Type
    /// <summary>"INDIVIDUAL_ANGEL" | "INSTITUTIONAL" — from active KYC submission</summary>
    public string? InvestorType { get; set; }
    /// <summary>True if investor has a non-None InvestorTag (KYC approved)</summary>
    public bool KycVerified { get; set; }

    // Matching / Filter
    public List<string> PreferredIndustries { get; set; } = new();
    public List<string> PreferredStages { get; set; } = new();
    public List<string> PreferredGeographies { get; set; } = new();
    public decimal? TicketSizeMin { get; set; }
    public decimal? TicketSizeMax { get; set; }

    // CTA / Connection state (relative to the requesting startup)
    /// <summary>True if the investor's profile is Approved (general availability)</summary>
    public bool AcceptingConnections { get; set; }
    /// <summary>True if the requesting startup can send a new connection request</summary>
    public bool CanRequestConnection { get; set; }
    /// <summary>"NONE" | "REQUESTED" | "ACCEPTED" | "IN_DISCUSSION"</summary>
    public string ConnectionStatus { get; set; } = "NONE";
    /// <summary>"STARTUP" | "INVESTOR" — who initiated the current connection</summary>
    public string? InitiatedByRole { get; set; }
    public int? ConnectionId { get; set; }

    // Stats
    public int AcceptedConnectionCount { get; set; }
    public int PortfolioCount { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Investor detail DTO for Startup role — includes visibility semantics</summary>
public class InvestorDetailForStartupDto
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
    public string? InvestorType { get; set; } // "INDIVIDUAL_ANGEL" | "INSTITUTIONAL"
    public List<string> PreferredIndustries { get; set; } = new();
    public List<string> PreferredStages { get; set; } = new();
    public decimal? TicketSizeMin { get; set; }
    public decimal? TicketSizeMax { get; set; }
    public int? PortfolioCount { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Visibility semantics — FE uses these to decide render mode
    // OPEN: discoverable=true, canRequestConnection=true
    // INVESTOR_PAUSED_DISCOVERY: discoverable=false, canRequestConnection=false, detail still accessible read-only
    public bool DiscoverableForStartups { get; set; }
    public bool CanRequestConnection { get; set; }
    public string ProfileAvailabilityReason { get; set; } = "OPEN";
}

/// <summary>Investor profile DTO for Staff/Admin review — no ProfileStatus filter applied.</summary>
public class InvestorProfileForStaffDto
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
    public string? InvestorType { get; set; }
    public bool AcceptingConnections { get; set; }
    public string ProfileStatus { get; set; } = string.Empty;
    public List<string> PreferredIndustries { get; set; } = new();
    public List<string> PreferredStages { get; set; } = new();
    public decimal? TicketSizeMin { get; set; }
    public decimal? TicketSizeMax { get; set; }
    public int? PortfolioCount { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Startup search result DTO (no sensitive data exposed)</summary>
public class StartupSearchItemDto
{
    public int StartupID { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? Stage { get; set; }
    public string? IndustryName { get; set; }
    public string? ParentIndustryName { get; set; }
    public string? SubIndustry { get; set; }
    public string? LogoURL { get; set; }
    public string? ProfileStatus { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public decimal? FundingAmountSought { get; set; }
    public decimal? CurrentFundingRaised { get; set; }
    /// <summary>NONE | REQUESTED | ACCEPTED | IN_DISCUSSION. Null when caller is not investor.</summary>
    public string? ConnectionStatus { get; set; }
    /// <summary>ID of the active connection record. Null when no active connection.</summary>
    public int? ConnectionId { get; set; }
    /// <summary>Whether the current investor can send a new connection request.</summary>
    public bool CanRequestConnection { get; set; }
    /// <summary>"INVESTOR" if investor initiated, "STARTUP" if startup initiated.</summary>
    public string? InitiatedByRole { get; set; }
    public double? AiScore { get; set; }
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

    // ── New fields ──
    public List<string> PreferredProductMaturity { get; set; } = new();
    public List<string> PreferredValidationLevel { get; set; } = new();
    public List<string> PreferredStrengths { get; set; } = new();
    public AiScoreRangeDto? PreferredAiScoreRange { get; set; }
    public string? AiScoreImportance { get; set; }
    public string? AcceptingConnectionsStatus { get; set; }
    public bool RecentlyActiveBadge { get; set; }
    public bool RequireVerifiedStartups { get; set; }
    public bool RequireVisibleProfiles { get; set; }
    public string? AvoidText { get; set; }
    public List<string> Tags { get; set; } = new();

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
    public double? AiScore { get; set; }
}

// ========== INTERESTED INVESTORS (Startup view) ==========

/// <summary>
/// An investor who has added this startup to their watchlist (expressed passive interest).
/// Returned by GET /api/startups/me/interested-investors.
/// </summary>
public class InterestedInvestorDto
{
    public int InvestorId { get; set; }
    /// <summary>Headline name for display: FirmName nếu có, fallback về FullName (cá nhân).</summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>Tên người đại diện — investor.FullName</summary>
    public string RepresentativeName { get; set; } = string.Empty;
    /// <summary>Tên tổ chức/quỹ — investor.FirmName. Null nếu là investor cá nhân.</summary>
    public string? FundName { get; set; }
    public string? ProfilePhotoURL { get; set; }

    /// <summary>
    /// Short, pre-built summary for direct FE display.
    /// Built from InvestmentThesis or Bio truncated to 200 chars.
    /// </summary>
    public string? ShortSummary { get; set; }

    /// <summary>"VERIFIED" | "BASIC_VERIFIED" | "PENDING" | "UNVERIFIED"</summary>
    public string VerificationStatus { get; set; } = "UNVERIFIED";

    /// <summary>Human-readable badge label e.g. "Verified Investor Entity"</summary>
    public string? VerificationBadge { get; set; }

    /// <summary>
    /// Timestamp when the investor added this startup to their watchlist.
    /// </summary>
    public DateTime? DateOfInterest { get; set; }
}
