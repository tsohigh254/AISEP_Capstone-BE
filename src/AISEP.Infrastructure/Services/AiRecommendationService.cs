using AISEP.Application.DTOs.AI;
using AISEP.Application.DTOs.Common;
using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace AISEP.Infrastructure.Services;

public class AiRecommendationService : IAiRecommendationService
{
    private readonly ApplicationDbContext _db;
    private readonly PythonAiClient _pythonClient;
    private readonly ILogger<AiRecommendationService> _logger;

    public AiRecommendationService(
        ApplicationDbContext db,
        PythonAiClient pythonClient,
        ILogger<AiRecommendationService> logger)
    {
        _db = db;
        _pythonClient = pythonClient;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════
    //  Reindex — Startup (non-blocking, fire-and-forget safe)
    // ═══════════════════════════════════════════════════════════

    public async Task ReindexStartupAsync(int startupId)
    {
        var correlationId = Guid.NewGuid().ToString();
        try
        {
            var startup = await _db.Startups
                .AsNoTracking()
                .Include(s => s.Industry)
                .FirstOrDefaultAsync(s => s.StartupID == startupId);

            if (startup == null)
            {
                _logger.LogWarning("ReindexStartup skipped: Startup {StartupId} not found", startupId);
                return;
            }

            // Load latest AI evaluation run if available
            var latestRun = await _db.AiEvaluationRuns
                .AsNoTracking()
                .Where(r => r.StartupId == startupId)
                .OrderByDescending(r => r.SubmittedAt)
                .FirstOrDefaultAsync();

            var payload = BuildStartupReindexPayload(startup, latestRun);

            await _pythonClient.ReindexStartupAsync(startupId, payload, correlationId);

            _logger.LogInformation(
                "Startup {StartupId} reindexed successfully (Correlation={CorrelationId})",
                startupId, correlationId);
        }
        catch (PythonAiException ex)
        {
            _logger.LogError(ex,
                "Startup reindex failed for {StartupId}: [{Code}] {Message} (Correlation={CorrelationId})",
                startupId, ex.Code, ex.Message, correlationId);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Python AI unreachable during startup reindex for {StartupId} (Correlation={CorrelationId})",
                startupId, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error during startup reindex for {StartupId} (Correlation={CorrelationId})",
                startupId, correlationId);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Reindex — Investor (non-blocking, fire-and-forget safe)
    // ═══════════════════════════════════════════════════════════

    public async Task ReindexInvestorAsync(int investorId)
    {
        var correlationId = Guid.NewGuid().ToString();
        try
        {
            var investor = await _db.Investors
                .AsNoTracking()
                .Include(i => i.Preferences)
                .Include(i => i.StageFocus)
                .Include(i => i.IndustryFocus)
                .Include(i => i.KycSubmissions)
                .FirstOrDefaultAsync(i => i.InvestorID == investorId);

            if (investor == null)
            {
                _logger.LogWarning("ReindexInvestor skipped: Investor {InvestorId} not found", investorId);
                return;
            }

            var payload = BuildInvestorReindexPayload(investor);

            await _pythonClient.ReindexInvestorAsync(investorId, payload, correlationId);

            _logger.LogInformation(
                "Investor {InvestorId} reindexed successfully (Correlation={CorrelationId})",
                investorId, correlationId);
        }
        catch (PythonAiException ex)
        {
            _logger.LogError(ex,
                "Investor reindex failed for {InvestorId}: [{Code}] {Message} (Correlation={CorrelationId})",
                investorId, ex.Code, ex.Message, correlationId);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Python AI unreachable during investor reindex for {InvestorId} (Correlation={CorrelationId})",
                investorId, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error during investor reindex for {InvestorId} (Correlation={CorrelationId})",
                investorId, correlationId);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Read — Startup Recommendations for Investor
    // ═══════════════════════════════════════════════════════════

    public async Task<ApiResponse<RecommendationListResult>> GetStartupRecommendationsAsync(int investorId, int topN)
    {
        var correlationId = Guid.NewGuid().ToString();
        try
        {
            var pythonResult = await _pythonClient.GetStartupRecommendationsAsync(investorId, topN, correlationId);

            var result = new RecommendationListResult
            {
                InvestorId = investorId,
                GeneratedAt = pythonResult.GeneratedAt,
                Warnings = pythonResult.Warnings,
                Matches = pythonResult.Matches.Select(m => new RecommendationMatchResult
                {
                    StartupId = int.TryParse(m.StartupId, out var sid) ? sid : 0,
                    StartupName = m.StartupName,
                    FinalMatchScore = m.FinalMatchScore,
                    MatchBand = m.MatchBand,
                    FitSummaryLabel = m.FitSummaryLabel,
                    MatchReasons = m.MatchReasons,
                    PositiveReasons = m.PositiveReasons,
                    CautionReasons = m.CautionReasons,
                    WarningFlags = m.WarningFlags,
                }).ToList()
            };

            return ApiResponse<RecommendationListResult>.SuccessResponse(result);
        }
        catch (PythonAiException ex) when (ex.HttpStatus == HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning(
                "Recommendation rate-limited for investor {InvestorId}: {Message} (Correlation={CorrelationId})",
                investorId, ex.Message, correlationId);
            return ApiResponse<RecommendationListResult>.ErrorResponse(
                "RATE_LIMIT_EXCEEDED", "Too many recommendation requests. Please try again shortly.");
        }
        catch (PythonAiException ex) when (ex.HttpStatus == HttpStatusCode.NotFound)
        {
            return ApiResponse<RecommendationListResult>.ErrorResponse(
                "NOT_FOUND", "Investor profile not found in recommendation engine. Try updating your profile first.");
        }
        catch (PythonAiException ex)
        {
            _logger.LogError(ex,
                "Recommendation read failed for investor {InvestorId}: [{Code}] {Message} (Correlation={CorrelationId})",
                investorId, ex.Code, ex.Message, correlationId);
            return ApiResponse<RecommendationListResult>.ErrorResponse(
                "AI_SERVICE_ERROR", "Unable to fetch recommendations at this time. Please try again later.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Python AI unreachable during recommendation read for investor {InvestorId} (Correlation={CorrelationId})",
                investorId, correlationId);
            return ApiResponse<RecommendationListResult>.ErrorResponse(
                "AI_SERVICE_UNAVAILABLE", "The recommendation service is currently unavailable. Please try again later.");
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Read — Match Explanation
    // ═══════════════════════════════════════════════════════════

    public async Task<ApiResponse<RecommendationExplanationResult>> GetMatchExplanationAsync(int investorId, int startupId)
    {
        var correlationId = Guid.NewGuid().ToString();
        try
        {
            var pythonResult = await _pythonClient.GetMatchExplanationAsync(investorId, startupId, correlationId);

            object? explanation = null;
            if (pythonResult.Explanation.HasValue &&
                pythonResult.Explanation.Value.ValueKind != JsonValueKind.Null)
            {
                explanation = JsonSerializer.Deserialize<object>(pythonResult.Explanation.Value.GetRawText());
            }

            var result = new RecommendationExplanationResult
            {
                InvestorId = investorId,
                StartupId = startupId,
                Explanation = explanation,
                GeneratedAt = pythonResult.GeneratedAt,
            };

            return ApiResponse<RecommendationExplanationResult>.SuccessResponse(result);
        }
        catch (PythonAiException ex) when (ex.HttpStatus == HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning(
                "Explanation rate-limited for investor {InvestorId}, startup {StartupId} (Correlation={CorrelationId})",
                investorId, startupId, correlationId);
            return ApiResponse<RecommendationExplanationResult>.ErrorResponse(
                "RATE_LIMIT_EXCEEDED", "Too many requests. Please try again shortly.");
        }
        catch (PythonAiException ex) when (ex.HttpStatus == HttpStatusCode.NotFound)
        {
            return ApiResponse<RecommendationExplanationResult>.ErrorResponse(
                "NOT_FOUND", "No match explanation found for this investor–startup pair.");
        }
        catch (PythonAiException ex)
        {
            _logger.LogError(ex,
                "Explanation read failed for investor {InvestorId}, startup {StartupId}: [{Code}] {Message} (Correlation={CorrelationId})",
                investorId, startupId, ex.Code, ex.Message, correlationId);
            return ApiResponse<RecommendationExplanationResult>.ErrorResponse(
                "AI_SERVICE_ERROR", "Unable to fetch match explanation at this time.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Python AI unreachable during explanation for investor {InvestorId}, startup {StartupId} (Correlation={CorrelationId})",
                investorId, startupId, correlationId);
            return ApiResponse<RecommendationExplanationResult>.ErrorResponse(
                "AI_SERVICE_UNAVAILABLE", "The recommendation service is currently unavailable.");
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Payload Builders
    // ═══════════════════════════════════════════════════════════

    private static PythonReindexStartupRequest BuildStartupReindexPayload(Startup startup, AiEvaluationRun? latestRun)
    {
        // Extract AI summary fields from cached report if available
        string? aiSummary = null;
        List<string>? strengthTags = null;
        List<string>? weaknessTags = null;
        Dictionary<string, double>? dimensionScores = null;

        if (latestRun?.IsReportValid == true && !string.IsNullOrEmpty(latestRun.ReportJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(latestRun.ReportJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("narrative", out var narrative) &&
                    narrative.TryGetProperty("executive_summary", out var summary))
                {
                    aiSummary = summary.GetString();
                }

                if (root.TryGetProperty("narrative", out var narr2))
                {
                    if (narr2.TryGetProperty("top_strengths", out var strengths) &&
                        strengths.ValueKind == JsonValueKind.Array)
                    {
                        strengthTags = strengths.EnumerateArray()
                            .Where(e => e.ValueKind == JsonValueKind.String)
                            .Select(e => e.GetString()!)
                            .Take(5)
                            .ToList();
                    }

                    if (narr2.TryGetProperty("top_concerns", out var concerns) &&
                        concerns.ValueKind == JsonValueKind.Array)
                    {
                        weaknessTags = concerns.EnumerateArray()
                            .Where(e => e.ValueKind == JsonValueKind.String)
                            .Select(e => e.GetString()!)
                            .Take(5)
                            .ToList();
                    }
                }

                if (root.TryGetProperty("criteria_results", out var criteria) &&
                    criteria.ValueKind == JsonValueKind.Array)
                {
                    dimensionScores = new Dictionary<string, double>();
                    foreach (var c in criteria.EnumerateArray())
                    {
                        if (c.TryGetProperty("criterion", out var name) &&
                            c.TryGetProperty("final_score", out var score) &&
                            score.ValueKind == JsonValueKind.Number)
                        {
                            dimensionScores[name.GetString() ?? "unknown"] = score.GetDouble();
                        }
                    }
                }
            }
            catch
            {
                // Report parsing failed — send reindex without AI fields
            }
        }

        return new PythonReindexStartupRequest
        {
            StartupId = startup.StartupID.ToString(),
            ProfileVersion = Guid.NewGuid().ToString("N")[..8],
            SourceUpdatedAt = startup.UpdatedAt ?? startup.CreatedAt,
            StartupName = startup.CompanyName,
            Tagline = startup.OneLiner,
            Stage = startup.Stage?.ToString(),
            PrimaryIndustry = startup.Industry?.IndustryName,
            SubIndustry = startup.SubIndustry,
            Description = startup.Description,
            Location = startup.Location,
            Country = startup.Country,
            MarketScope = startup.MarketScope,
            ProductStatus = startup.ProductStatus,
            ProblemStatement = startup.ProblemStatement,
            SolutionSummary = startup.SolutionSummary,
            FundingAmountSought = startup.FundingAmountSought,
            CurrentFundingRaised = startup.CurrentFundingRaised,
            TeamSize = startup.TeamSize,
            IsProfileVisibleToInvestors = startup.IsVisible,
            VerificationLabel = startup.StartupTag.ToString(),
            AccountActive = startup.ProfileStatus == Domain.Enums.ProfileStatus.Approved,
            AiEvaluationStatus = latestRun?.Status,
            AiOverallScore = latestRun?.OverallScore,
            AiSummary = aiSummary,
            AiStrengthTags = strengthTags,
            AiWeaknessTags = weaknessTags,
            AiDimensionScores = dimensionScores,
        };
    }

    private static PythonReindexInvestorRequest BuildInvestorReindexPayload(Investor investor)
    {
        // Helper: split a nullable comma-separated string into a trimmed list, or null if empty/absent
        static List<string>? SplitCsv(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? null
                : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                       .Where(s => !string.IsNullOrEmpty(s))
                       .ToList();

        // Preferred stages from navigation table; fall back to preferences string column
        var preferredStages = investor.StageFocus?.Count > 0
            ? investor.StageFocus.Select(s => s.Stage.ToString()).ToList()
            : SplitCsv(investor.Preferences?.PreferredStages);

        // Preferred industries from navigation table; fall back to preferences string column
        var preferredIndustries = investor.IndustryFocus?.Count > 0
            ? investor.IndustryFocus.Select(i => i.Industry).ToList()
            : SplitCsv(investor.Preferences?.PreferredIndustries);

        // Get active KYC submission for fields that moved from Investor to InvestorKycSubmission
        var activeKyc = investor.KycSubmissions?.FirstOrDefault(s => s.IsActive);

        return new PythonReindexInvestorRequest
        {
            InvestorName      = investor.FullName,
            InvestorType      = activeKyc?.InvestorCategory?.ToLowerInvariant(),
            Organization      = investor.FirmName,
            RoleTitle         = activeKyc?.CurrentRoleTitle ?? investor.Title,
            Location          = investor.Location,
            Website           = investor.Website,
            VerificationLabel = investor.InvestorTag.ToString().ToLowerInvariant(),
            LogoUrl           = investor.ProfilePhotoURL,
            ShortThesisSummary = investor.InvestmentThesis,
            PreferredIndustries      = preferredIndustries,
            PreferredStages          = preferredStages,
            PreferredGeographies     = SplitCsv(investor.Preferences?.PreferredGeographies),
            PreferredMarketScopes    = SplitCsv(investor.Preferences?.PreferredMarketScopes),
            PreferredProductMaturity = SplitCsv(investor.Preferences?.PreferredProductMaturity),
            PreferredValidationLevel = SplitCsv(investor.Preferences?.PreferredValidationLevel),
            PreferredStrengths       = SplitCsv(investor.Preferences?.PreferredStrengths),
            SupportOffered           = SplitCsv(investor.Preferences?.SupportOffered),
            RequireVerifiedStartups  = investor.Preferences?.RequireVerifiedStartups,
            RequireVisibleProfiles   = investor.Preferences?.RequireVisibleProfiles,
            Tags                     = SplitCsv(investor.Preferences?.Tags),
            PreferredAiScoreRange    = (investor.Preferences?.PreferredAiScoreMin != null || investor.Preferences?.PreferredAiScoreMax != null)
                ? new PythonAiScoreRange { Min = investor.Preferences?.PreferredAiScoreMin, Max = investor.Preferences?.PreferredAiScoreMax }
                : null,
            AiScoreImportance            = investor.Preferences?.AiScoreImportance,
            AcceptingConnectionsStatus   = investor.Preferences?.AcceptingConnectionsStatus,
            RecentlyActiveBadge          = investor.Preferences?.RecentlyActiveBadge,
            AvoidText                    = investor.Preferences?.AvoidText,
        };
    }
}
