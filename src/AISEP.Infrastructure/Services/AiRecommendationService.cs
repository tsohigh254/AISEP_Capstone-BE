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

    public async Task ReindexStartupAsync(int startupId)
    {
        var correlationId = Guid.NewGuid().ToString();
        try
        {
            var startup = await _db.Startups
                .AsNoTracking()
                .Include(s => s.Industry)
                .Include(s => s.StageRef)
                .FirstOrDefaultAsync(s => s.StartupID == startupId);

            if (startup == null) return;

            var latestRun = await _db.AiEvaluationRuns
                .AsNoTracking()
                .Where(r => r.StartupId == startupId)
                .OrderByDescending(r => r.SubmittedAt)
                .FirstOrDefaultAsync();

            var payload = BuildStartupReindexPayload(startup, latestRun);
            await _pythonClient.ReindexStartupAsync(startupId, payload, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Startup reindex failed for {StartupId}", startupId);
        }
    }

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
                .FirstOrDefaultAsync(i => i.InvestorID == investorId);

            if (investor == null) return;

            var payload = BuildInvestorReindexPayload(investor);
            await _pythonClient.ReindexInvestorAsync(investorId, payload, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Investor reindex failed for {InvestorId}", investorId);
        }
    }

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
                Matches = pythonResult.Matches.Select(m => new RecommendationMatchResult
                {
                    StartupId = int.TryParse(m.StartupId, out var sid) ? sid : 0,
                    StartupName = m.StartupName,
                    FinalMatchScore = m.FinalMatchScore,
                    MatchBand = m.MatchBand,
                    FitSummaryLabel = m.FitSummaryLabel,
                    PositiveReasons = m.PositiveReasons,
                    MatchReasons = m.MatchReasons
                }).ToList()
            };
            return ApiResponse<RecommendationListResult>.SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI Recommendation failed for {InvestorId}", investorId);
            return ApiResponse<RecommendationListResult>.ErrorResponse("AI_ERROR", "AI Service unavailable.");
        }
    }

    public async Task<ApiResponse<RecommendationExplanationResult>> GetMatchExplanationAsync(int investorId, int startupId)
    {
        var correlationId = Guid.NewGuid().ToString();
        try
        {
            var pythonResult = await _pythonClient.GetMatchExplanationAsync(investorId, startupId, correlationId);
            var result = new RecommendationExplanationResult
            {
                InvestorId = investorId,
                StartupId = startupId,
                GeneratedAt = pythonResult.GeneratedAt,
                Explanation = pythonResult.Explanation
            };
            return ApiResponse<RecommendationExplanationResult>.SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI Explanation failed for {InvestorId}", investorId);
            return ApiResponse<RecommendationExplanationResult>.ErrorResponse("AI_ERROR", "AI Service unavailable.");
        }
    }

    private static PythonReindexStartupRequest BuildStartupReindexPayload(Startup startup, AiEvaluationRun? latestRun)
    {
        return new PythonReindexStartupRequest
        {
            StartupId = startup.StartupID.ToString(),
            StartupName = startup.CompanyName,
            Stage = startup.StageRef?.StageName,
            PrimaryIndustry = startup.Industry?.IndustryName,
            Location = startup.Location,
            Country = startup.Country
        };
    }

    private static PythonReindexInvestorRequest BuildInvestorReindexPayload(Investor investor)
    {
        return new PythonReindexInvestorRequest
        {
            InvestorName = investor.FullName,
            Location = investor.Location
        };
    }
}
