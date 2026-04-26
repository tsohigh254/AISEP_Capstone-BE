using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Readiness;
using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Domain.Enums;
using AISEP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AISEP.Infrastructure.Services;

public class ReadinessService : IReadinessService
{
    private readonly ApplicationDbContext _db;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public ReadinessService(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<ApiResponse<ReadinessResultDto>> GetReadinessAsync(int userId)
        => CalculateAndPersistAsync(userId);

    public Task<ApiResponse<ReadinessResultDto>> RecalculateReadinessAsync(int userId)
        => CalculateAndPersistAsync(userId);

    // ══════════════════════════════════════════════════════════
    //  CORE CALCULATION
    // ══════════════════════════════════════════════════════════

    private async Task<ApiResponse<ReadinessResultDto>> CalculateAndPersistAsync(int userId)
    {
        var startup = await _db.Startups
            .AsNoTracking()
            .Include(s => s.TeamMembers)
            .Include(s => s.Documents.Where(d => !d.IsArchived))
                .ThenInclude(d => d.BlockchainProof)
            .Include(s => s.PotentialScores.Where(ps => ps.IsCurrentScore))
            .Include(s => s.KycSubmissions.Where(k => k.IsActive))
            .FirstOrDefaultAsync(s => s.UserID == userId);

        if (startup == null)
            return ApiResponse<ReadinessResultDto>.ErrorResponse(
                "STARTUP_NOT_FOUND", "Startup profile not found.");

        var missingItems = new List<MissingItemDto>();
        var nextActions = new List<NextActionDto>();
        var appliedCaps = new List<AppliedCapDto>();

        // ── Dimension 1: Profile (max 25) ──
        int profileScore = CalcProfileScore(startup, missingItems, nextActions);

        // ── Dimension 2: KYC (max 20) ──
        int kycScore = CalcKycScore(startup, missingItems, nextActions);

        // ── Dimension 3: Documents (max 20) ──
        int docScore = CalcDocumentScore(startup, missingItems, nextActions);

        // ── Dimension 4: AI Evaluation (max 20) ──
        int aiScore = CalcAiScore(startup, missingItems, nextActions);

        // ── Dimension 5: Trust / Proof (max 15) ──
        int trustScore = CalcTrustScore(startup, missingItems, nextActions);

        // ── Raw total ──
        int rawScore = profileScore + kycScore + docScore + aiScore + trustScore;

        // ── Apply business-rule caps ──
        int finalScore = ApplyBusinessCaps(rawScore, startup, aiScore, appliedCaps);

        // ── Derive status ──
        var status = DeriveStatus(finalScore);

        // ── Persist snapshot (upsert — always 1 row per startup) ──
        var now = DateTime.UtcNow;

        // Must re-query with tracking for upsert
        var snapshot = await _db.StartupReadinessSnapshots
            .FirstOrDefaultAsync(s => s.StartupID == startup.StartupID);

        if (snapshot == null)
        {
            snapshot = new StartupReadinessSnapshot { StartupID = startup.StartupID };
            _db.StartupReadinessSnapshots.Add(snapshot);
        }

        snapshot.OverallScore = finalScore;
        snapshot.Status = status;
        snapshot.ProfileScore = profileScore;
        snapshot.KycScore = kycScore;
        snapshot.DocumentScore = docScore;
        snapshot.AiScore = aiScore;
        snapshot.TrustScore = trustScore;
        snapshot.MissingItemsJson = JsonSerializer.Serialize(missingItems, JsonOpts);
        snapshot.RecommendationsJson = JsonSerializer.Serialize(nextActions, JsonOpts);
        snapshot.CalculatedAt = now;

        await _db.SaveChangesAsync();

        // ── Build response ──
        var dto = new ReadinessResultDto
        {
            OverallScore = finalScore,
            Status = status switch
            {
                ReadinessStatus.NotReady => "NOTREADY",
                ReadinessStatus.NeedsWork => "NEEDSWORK",
                ReadinessStatus.AlmostReady => "ALMOSTREADY",
                ReadinessStatus.InvestorReady => "INVESTORREADY",
                _ => "NOTREADY"
            },
            Dimensions = new ReadinessDimensionsDto
            {
                Profile = profileScore,
                Kyc = kycScore,
                Documents = docScore,
                Ai = aiScore,
                Trust = trustScore
            },
            MissingItems = missingItems,
            NextActions = nextActions,
            AppliedCaps = appliedCaps,
            CalculatedAt = now
        };

        return ApiResponse<ReadinessResultDto>.SuccessResponse(dto);
    }

    // ══════════════════════════════════════════════════════════
    //  DIMENSION CALCULATORS
    // ══════════════════════════════════════════════════════════

    /// <summary>Profile completeness (max 25).</summary>
    private static int CalcProfileScore(Startup s, List<MissingItemDto> missing, List<NextActionDto> actions)
    {
        int score = 0;

        // CompanyName + OneLiner (3)
        if (!string.IsNullOrWhiteSpace(s.CompanyName)) score += 1;
        if (!string.IsNullOrWhiteSpace(s.OneLiner)) score += 2;
        else AddMissing(missing, "MISSING_ONELINER", "profile", "Add a tagline / one-liner for your startup");

        // Stage + Industry (3)
        if (s.StageID.HasValue) score += 1;
        else AddMissing(missing, "MISSING_STAGE", "profile", "Select your startup stage");
        if (s.IndustryID.HasValue) score += 2;
        else AddMissing(missing, "MISSING_INDUSTRY", "profile", "Select your primary industry");

        // ProblemStatement + SolutionSummary (4)
        if (!string.IsNullOrWhiteSpace(s.ProblemStatement)) score += 2;
        else AddMissing(missing, "MISSING_PROBLEM", "profile", "Describe the problem you are solving");
        if (!string.IsNullOrWhiteSpace(s.SolutionSummary)) score += 2;
        else AddMissing(missing, "MISSING_SOLUTION", "profile", "Describe your solution");

        // Website / Demo URL (3)
        if (!string.IsNullOrWhiteSpace(s.Website)) score += 3;
        else AddMissing(missing, "MISSING_WEBSITE", "profile", "Add your website or demo URL");

        // Location + MarketScope (3)
        if (!string.IsNullOrWhiteSpace(s.Location)) score += 1;
        if (!string.IsNullOrWhiteSpace(s.MarketScope)) score += 2;
        else AddMissing(missing, "MISSING_MARKET_SCOPE", "profile", "Describe your target market scope");

        // ProductStatus + CurrentNeeds + ValidationStatus (4)
        if (!string.IsNullOrWhiteSpace(s.ProductStatus)) score += 1;
        if (!string.IsNullOrWhiteSpace(s.CurrentNeeds)) score += 1;
        // ValidationStatus → use StartupTag as proxy
        score += s.StartupTag switch
        {
            StartupTag.VerifiedCompany => 2,
            StartupTag.BasicVerified => 1,
            _ => 0
        };

        // TeamSize + TeamMembers >= 1 (3)
        if (!string.IsNullOrWhiteSpace(s.TeamSize)) score += 1;
        if (s.TeamMembers != null && s.TeamMembers.Count >= 1) score += 2;
        else AddMissing(missing, "MISSING_TEAM", "profile", "Add at least one team member");

        // MetricSummary (2)
        if (!string.IsNullOrWhiteSpace(s.MetricSummary)) score += 2;

        // Cap at 25
        score = Math.Min(score, 25);

        if (score < 20)
        {
            actions.Add(new NextActionDto
            {
                Code = "COMPLETE_PROFILE",
                Label = "Complete your startup profile",
                Target = "/startup/startup-profile"
            });
        }

        return score;
    }

    /// <summary>KYC & trust identity (max 20).</summary>
    private static int CalcKycScore(Startup s, List<MissingItemDto> missing, List<NextActionDto> actions)
    {
        var activeKyc = s.KycSubmissions?
            .OrderByDescending(k => k.Version)
            .FirstOrDefault();

        if (activeKyc == null || activeKyc.WorkflowStatus == StartupKycWorkflowStatus.NotSubmitted
                              || activeKyc.WorkflowStatus == StartupKycWorkflowStatus.Draft)
        {
            AddMissing(missing, "MISSING_KYC", "kyc", "Submit KYC verification");
            actions.Add(new NextActionDto
            {
                Code = "SUBMIT_KYC",
                Label = "Submit KYC verification",
                Target = "/startup/verification"
            });
            return 0;
        }

        if (activeKyc.WorkflowStatus == StartupKycWorkflowStatus.UnderReview)
        {
            // Give partial credit for submission effort
            return 4;
        }

        if (activeKyc.WorkflowStatus == StartupKycWorkflowStatus.PendingMoreInfo)
        {
            AddMissing(missing, "KYC_PENDING_INFO", "kyc", "Provide additional KYC information requested by staff");
            actions.Add(new NextActionDto
            {
                Code = "RESUBMIT_KYC",
                Label = "Provide additional KYC information",
                Target = "/startup/verification"
            });
            return 6;
        }

        if (activeKyc.WorkflowStatus == StartupKycWorkflowStatus.Rejected)
        {
            AddMissing(missing, "KYC_REJECTED", "kyc", "KYC verification was rejected — resubmit with correct information");
            actions.Add(new NextActionDto
            {
                Code = "RESUBMIT_KYC",
                Label = "Resubmit KYC verification",
                Target = "/startup/verification"
            });
            return 0;
        }

        // Approved → score by ResultLabel
        return activeKyc.ResultLabel switch
        {
            StartupKycResultLabel.VerifiedCompany => 20,
            StartupKycResultLabel.BasicVerified => 14,
            StartupKycResultLabel.PendingMoreInfo => 6,
            StartupKycResultLabel.VerificationFailed => 0,
            _ => 14 // Approved but label not set → assume BasicVerified
        };
    }

    /// <summary>Document package (max 20).</summary>
    private static int CalcDocumentScore(Startup s, List<MissingItemDto> missing, List<NextActionDto> actions)
    {
        var docs = s.Documents?.Where(d => !d.IsArchived).ToList() ?? new List<Document>();
        int score = 0;

        // Pitch Deck (+8) — identified by DocumentType enum, fallback to PitchDeckUrl
        bool hasPitchDeck = docs.Any(d => d.DocumentType == DocumentType.Pitch_Deck)
                         || !string.IsNullOrWhiteSpace(s.PitchDeckUrl);
        if (hasPitchDeck) score += 8;
        else
        {
            AddMissing(missing, "MISSING_PITCH_DECK", "documents", "Upload a Pitch Deck");
            actions.Add(new NextActionDto
            {
                Code = "UPLOAD_PITCH_DECK",
                Label = "Upload Pitch Deck",
                Target = "/startup/documents"
            });
        }

        // Business Plan (+6) — identified by DocumentType enum
        bool hasBP = docs.Any(d => d.DocumentType == DocumentType.Bussiness_Plan);
        if (hasBP) score += 6;
        else
        {
            AddMissing(missing, "MISSING_BP", "documents", "Upload a Business Plan");
            actions.Add(new NextActionDto
            {
                Code = "UPLOAD_BP",
                Label = "Upload Business Plan",
                Target = "/startup/documents"
            });
        }

        // At least 1 supporting doc (+3)
        bool hasSupporting = docs.Any(d => d.DocumentType == DocumentType.Other);
        if (hasSupporting) score += 3;

        // Metadata & visibility (+3)
        // Rule: Latest version of key docs must be visible to investors
        var latestKeyDocs = docs
            .Where(d => d.DocumentType != DocumentType.Other)
            .GroupBy(d => d.DocumentType)
            .Select(g => g.OrderByDescending(d => d.UploadedAt).First())
            .ToList();

        if (latestKeyDocs.Count > 0)
        {
            bool allHaveVisibility = latestKeyDocs.All(d => d.Visibility.HasFlag(DocumentVisibility.Investor));
            if (allHaveVisibility) score += 3;
            else
            {
                AddMissing(missing, "DOC_VISIBILITY", "documents", "Set document visibility to include investors");
                actions.Add(new NextActionDto
                {
                    Code = "SET_DOC_VISIBILITY",
                    Label = "Enable investor visibility for key documents",
                    Target = "/startup/documents"
                });
            }
        }

        return Math.Min(score, 20);
    }

    /// <summary>AI evaluation readiness (max 20).</summary>
    private static int CalcAiScore(Startup s, List<MissingItemDto> missing, List<NextActionDto> actions)
    {
        // Only considers completed evaluations (IsCurrentScore = true)
        var latestScore = s.PotentialScores?
            .Where(ps => ps.IsCurrentScore)
            .OrderByDescending(ps => ps.CalculatedAt)
            .FirstOrDefault();

        if (latestScore == null)
        {
            AddMissing(missing, "MISSING_AI_EVAL", "ai", "Request an AI evaluation for your startup");
            actions.Add(new NextActionDto
            {
                Code = "REQUEST_AI_EVAL",
                Label = "Request AI Evaluation",
                Target = "/startup/ai-evaluation/request"
            });
            return 0;
        }

        int score = 0;

        // Has at least 1 completed evaluation (+8)
        score += 8;

        // Normalize AI overall score to 0-100 then map to max 8 points
        // Handles both 0-10 and 0-100 scales from the Python AI service
        float normalizedAi = NormalizeAiScore(latestScore.OverallScore);
        score += (int)Math.Round(normalizedAi / 100.0 * 8.0);

        // Sub-metric quality check (+4)
        // All dimensional scores must be > 0 (Team, Market, Product, Traction)
        bool hasStrongDimensions =
            latestScore.TeamScore > 0 &&
            latestScore.MarketScore > 0 &&
            latestScore.ProductScore > 0 &&
            latestScore.TractionScore > 0;
        if (hasStrongDimensions) score += 4;

        return Math.Min(score, 20);
    }

    /// <summary>Proof & freshness (max 15).</summary>
    private static int CalcTrustScore(Startup s, List<MissingItemDto> missing, List<NextActionDto> actions)
    {
        var docs = s.Documents?.Where(d => !d.IsArchived).ToList() ?? new List<Document>();
        int score = 0;

        // At least 1 key doc verified on-chain (+8)
        // Requires ProofStatus == Anchored (confirmed on-chain)
        bool hasAnchoredProof = docs.Any(d =>
            d.BlockchainProof != null &&
            d.BlockchainProof.ProofStatus == ProofStatus.Anchored);

        if (hasAnchoredProof) score += 8;
        else
        {
            AddMissing(missing, "MISSING_BLOCKCHAIN_PROOF", "trust", "Verify at least one key document on blockchain");
            actions.Add(new NextActionDto
            {
                Code = "VERIFY_DOC",
                Label = "Verify key document on blockchain",
                Target = "/startup/documents"
            });
        }

        // Key docs are latest version (+4)
        // A doc is "latest" if it has no child versions (no newer version uploaded)
        var keyDocs = docs.Where(d => d.DocumentType != DocumentType.Other).ToList();
        if (keyDocs.Count > 0)
        {
            bool allLatest = keyDocs.All(d => d.ChildVersions == null || d.ChildVersions.Count == 0);
            if (allLatest) score += 4;
        }

        // Profile / documents updated recently — within 90 days (+3)
        var recentThreshold = DateTime.UtcNow.AddDays(-90);
        bool recentlyUpdated = (s.UpdatedAt.HasValue && s.UpdatedAt.Value >= recentThreshold)
                            || docs.Any(d => d.UploadedAt >= recentThreshold);
        if (recentlyUpdated) score += 3;
        else
        {
            AddMissing(missing, "STALE_DATA", "trust", "Update your profile or documents (last update > 90 days ago)");
        }

        return Math.Min(score, 15);
    }

    // ══════════════════════════════════════════════════════════
    //  BUSINESS RULES
    // ══════════════════════════════════════════════════════════

    /// <summary>Apply hard caps based on business rules. Populates appliedCaps list.</summary>
    private static int ApplyBusinessCaps(int rawScore, Startup s, int aiScore, List<AppliedCapDto> caps)
    {
        int finalScore = rawScore;

        // Rule 1: No Pitch Deck → cap at 69 (Needs Work)
        var docs = s.Documents?.Where(d => !d.IsArchived).ToList() ?? new List<Document>();
        bool hasPitchDeck = docs.Any(d => d.DocumentType == DocumentType.Pitch_Deck)
                         || !string.IsNullOrWhiteSpace(s.PitchDeckUrl);
        if (!hasPitchDeck && finalScore > 69)
        {
            finalScore = 69;
            caps.Add(new AppliedCapDto
            {
                Rule = "NO_PITCH_DECK",
                Description = "Missing Pitch Deck limits maximum score to 69",
                CappedAt = 69
            });
        }

        // Rule 2: No AI evaluation → cap at 84 (cannot reach Investor Ready)
        if (aiScore == 0 && finalScore > 84)
        {
            finalScore = 84;
            caps.Add(new AppliedCapDto
            {
                Rule = "NO_AI_EVALUATION",
                Description = "Missing AI evaluation limits maximum score to 84",
                CappedAt = 84
            });
        }

        // Rule 3: KYC failed → cap at 69
        var activeKyc = s.KycSubmissions?
            .OrderByDescending(k => k.Version)
            .FirstOrDefault();
        if (activeKyc != null && activeKyc.ResultLabel == StartupKycResultLabel.VerificationFailed && finalScore > 69)
        {
            finalScore = 69;
            caps.Add(new AppliedCapDto
            {
                Rule = "KYC_FAILED",
                Description = "Failed KYC verification limits maximum score to 69",
                CappedAt = 69
            });
        }

        return Math.Clamp(finalScore, 0, 100);
    }

    /// <summary>Derive status from final score.</summary>
    private static ReadinessStatus DeriveStatus(int finalScore)
    {
        return finalScore switch
        {
            >= 85 => ReadinessStatus.InvestorReady,
            >= 70 => ReadinessStatus.AlmostReady,
            >= 40 => ReadinessStatus.NeedsWork,
            _ => ReadinessStatus.NotReady
        };
    }

    // ══════════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Normalize AI score to 0-100 scale.
    /// Handles both 0-10 and 0-100 ranges from the Python AI service.
    /// </summary>
    private static float NormalizeAiScore(float raw)
    {
        if (raw <= 0) return 0;
        if (raw <= 10) return raw * 10;  // 0-10 scale → multiply
        return Math.Clamp(raw, 0, 100);  // 0-100 scale → keep as-is
    }

    private static void AddMissing(List<MissingItemDto> list, string code, string dimension, string label)
    {
        list.Add(new MissingItemDto { Code = code, Dimension = dimension, Label = label });
    }
}
