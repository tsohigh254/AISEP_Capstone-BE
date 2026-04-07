namespace AISEP.Domain.Enums;

// ───────────────────────── Shared ─────────────────────────

/// <summary>Profile workflow status shared by Advisor, Investor, Startup.</summary>
public enum ProfileStatus : short
{
    Draft = 0,
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    PendingKYC = 4
}

// ───────────────────────── Advisor ────────────────────────

public enum AchievementType : short
{
    Activity = 0,
    Contribution = 1,
    Milestone = 2,
    Rank = 3,
    Special = 4
}

public enum ProficiencyLevel : short
{
    Beginner = 0,
    Elementary = 1,
    Intermediate = 2,
    Advanced = 3,
    Expert = 4
}

// ───────────────────────── Chat ───────────────────────────

public enum ConversationStatus : short
{
    Active = 0,
    Archived = 1,
    Closed = 2,
    Deleted = 3
}

// ───────────────────────── Blockchain ─────────────────────

public enum ProofStatus : short
{
    Anchored = 0,       // Confirmed on-chain (final happy state)
    Revoked = 1,        // Explicitly revoked
    HashComputed = 2,   // Local hash created, not yet submitted
    Pending = 3         // Submitted to chain, awaiting confirmation
}

// ───────────────────────── Moderation ─────────────────────

/// <summary>Moderation decision/status for flagged content.</summary>
public enum ModerationStatus : short
{
    None = 0,
    Approve = 1,
    Reject = 2,
    Flag = 3,
    Remove = 4,
    Warn = 5,
    Suspend = 6,
    Ban = 7
}

// ───────────────────────── Information Request ────────────

public enum RequestStatus : short
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3,
    Expired = 4
}

// ───────────────────────── Investor ───────────────────────

/// <summary>Stages used in InvestorPreferences.PreferredStages (JSON list).</summary>
public enum PreferredStage : short
{
    Idea = 0,
    Validation = 1,
    MVP = 2,
    EarlyStage = 3,
    Growth = 4,
    Scale = 5,
    Mature = 6
}

public enum WatchlistPriority : short
{
    Low = 0,
    Medium = 1,
    High = 2
}

public enum InvestorType : short
{
    Institutional = 0,
    IndividualAngel = 1
}

// ───────────────────────── Startup / Investor Stage ───────

/// <summary>
/// Unified stage taxonomy shared by Startup.Stage and InvestorStageFocus.Stage.
/// </summary>
public enum StartupStage : short
{
    Idea = 0,
    PreSeed = 1,
    Seed = 2,
    SeriesA = 3,
    SeriesB = 4,
    SeriesC = 5,
    Growth = 6
}

// ───────────────────────── Portfolio Company ──────────────

public enum InvestmentStage : short
{
    PreSeed = 0,
    Seed = 1,
    SeriesA = 2,
    SeriesB = 3,
    SeriesC = 4,
    Growth = 5,
    LateStage = 6
}

public enum PortfolioCompanyStatus : short
{
    Active = 0,
    Acquired = 1,
    IPO = 2,
    Closed = 3,
    Unknown = 4
}

public enum ExitType : short
{
    Acquisition = 0,
    IPO = 1,
    SecondarySale = 2,
    WriteOff = 3,
    Unknown = 4
}

// ───────────────────────── Saved Report ───────────────────

public enum ReportType : short
{
    StartupAnalytics = 0,
    InvestorPortfolio = 1,
    AdvisorPerformance = 2,
    PlatformStatistics = 3,
    FundingTrends = 4
}

// ───────────────────────── Score Recommendation ──────────

public enum RecommendationPriority : short
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

// ───────────────────────── Connection ─────────────────────

public enum ConnectionStatus : short
{
    Requested = 0,
    Accepted = 1,
    Rejected = 2,
    Withdrawn = 3,
    InDiscussion = 4,
    Closed = 5
}

// ───────────────────────── Mentorship ─────────────────────

public enum MentorshipStatus : short
{
    Requested = 0,
    Rejected = 1,
    Accepted = 2,
    InProgress = 3,
    Completed = 4,
    InDispute = 5,
    Resolved = 6,
    Cancelled = 7,
    Expired = 8
}

// ───────────────────────── Document ───────────────────────────
public enum DocumentType : short
{
    Pitch_Deck = 0,
    Bussiness_Plan = 1
}

public enum AnalysisStatus : short
{
    NOTANALYZE = 0,
    COMPLETED = 1,
    FAILED = 2
}

public enum StartupTag : short
{
    None = 0,
    VerifiedCompany = 1,
    BasicVerified = 2,
    PendingMoreInfo = 3,
    VerificationFailed = 4
}

public enum StartupKycWorkflowStatus : short
{
    NotSubmitted = 0,
    Draft = 1,
    UnderReview = 2,
    PendingMoreInfo = 3,
    Approved = 4,
    Rejected = 5,
    Superseded = 6
}

public enum StartupKycResultLabel : short
{
    None = 0,
    PendingMoreInfo = 1,
    BasicVerified = 2,
    VerifiedCompany = 3,
    VerificationFailed = 4
}

public enum StartupVerificationType : short
{
    WithLegalEntity = 0,
    WithoutLegalEntity = 1
}

public enum StartupKycEvidenceKind : short
{
    BusinessRegistrationCertificate = 0,
    ProofOfOperation = 1,
    ProductMaterials = 2,
    Other = 3
}

public enum AdvisorTag : short
{
    None = 0,
    VerifiedAdvisor = 1,
    BasicVerified = 2,
    PendingMoreInfo = 3,
    VerificationFailed = 4
}

public enum InvestorTag : short
{
    None = 0,
    VerifiedInvestorEntity = 1,
    VerifiedAngelInvestor = 2,
    BasicVerified = 3,
    PendingMoreInfo = 4,
    VerificationFailed = 5
}
public enum InvestorKycWorkflowStatus : short
{
    NotSubmitted = 0,
    Draft = 1,
    UnderReview = 2,
    PendingMoreInfo = 3,
    Approved = 4,
    Rejected = 5,
    Superseded = 6
}

public enum InvestorKycResultLabel : short
{
    None = 0,
    PendingMoreInfo = 1,
    BasicVerified = 2,
    VerifiedInvestorEntity = 3,
    VerifiedAngelInvestor = 4,
    VerificationFailed = 5
}

public enum InvestorKycEvidenceKind : short
{
    IDProof = 0,
    InvestmentProof = 1,
    Other = 2
}

public enum TransactionType : short
{
    Deposit = 0,    // Tiền từ session
    Withdrawal = 1  // Rút tiền
}

public enum PaymentStatus : short
{
    Pending = 0,      // Đang chờ thanh toán
    Completed = 1,    // Đã thanh toán thành công
    Failed = 2,       // Thanh toán thất bại
}

public enum TransactionStatus : short
{
    Pending = 0,      // Đang chờ
    Completed = 1,    // Hoàn thành
    Failed = 2        // Thất bại
}
// ───────────────────────── Document Review ───────────────────

public enum DocumentReviewStatus : short
{
    Pending = 0,
    Verified = 1,
    Approved = 2,
    Rejected = 3
}

// ───────────────────────── Incident ──────────────────────────

public enum IncidentSeverity : short
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum IncidentStatus : short
{
    Open = 0,
    Investigating = 1,
    Resolved = 2,
    RolledBack = 3
}
