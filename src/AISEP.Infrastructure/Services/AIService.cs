using System.Net.Http.Json;
using System.Text.Json;
using AISEP.Application.DTOs.AI;
using AISEP.Application.DTOs.Common;
using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Domain.Enums;
using AISEP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AISEP.Infrastructure.Services;

public class AIService : IAIService
{
    private readonly HttpClient _http;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AIService> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public AIService(HttpClient http, ApplicationDbContext db, ILogger<AIService> logger)
    {
        _http = http;
        _db = db;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Evaluation — proxy to Python AI service
    // ═══════════════════════════════════════════════════════════════

    public async Task<ApiResponse<EvaluationSubmitResponse>> TriggerEvaluationAsync(
        int documentId, int userId, CancellationToken ct = default)
    {
        var startup = await _db.Startups.FirstOrDefaultAsync(s => s.UserID == userId, ct);
        if (startup == null)
            return ApiResponse<EvaluationSubmitResponse>.ErrorResponse(
                "STARTUP_NOT_FOUND", "No startup profile found for this user.");

        var startupId = startup.StartupID;
        // If documentId == 0, collect all startup documents that are pitch deck / business plan
        // and that have been anchored (verified) on-chain. Otherwise use the single document.
        List<Document> docsToSend = new();

        if (documentId == 0)
        {
            docsToSend = await _db.Documents
                .Include(d => d.BlockchainProof)
                .Where(d => d.StartupID == startupId
                            && !d.IsArchived
                            && (d.DocumentType == DocumentType.Pitch_Deck || d.DocumentType == DocumentType.Bussiness_Plan)
                            && d.BlockchainProof != null
                            && d.BlockchainProof.ProofStatus == ProofStatus.Anchored)
                .ToListAsync(ct);

            if (docsToSend == null || docsToSend.Count == 0)
                return ApiResponse<EvaluationSubmitResponse>.ErrorResponse(
                    "NO_VERIFIED_DOCUMENTS", "No pitch deck or business plan documents anchored on-chain were found for this startup.");
        }
        else
        {
            var doc = await _db.Documents
                .Include(d => d.BlockchainProof)
                .FirstOrDefaultAsync(d => d.DocumentID == documentId && d.StartupID == startupId, ct);

            if (doc == null)
                return ApiResponse<EvaluationSubmitResponse>.ErrorResponse(
                    "DOCUMENT_NOT_FOUND", "Document not found or does not belong to this startup.");

            if (doc.DocumentType != DocumentType.Pitch_Deck && doc.DocumentType != DocumentType.Bussiness_Plan)
                return ApiResponse<EvaluationSubmitResponse>.ErrorResponse(
                    "INVALID_DOCUMENT_TYPE", "Only pitch deck or business plan documents can be evaluated.");

            if (doc.BlockchainProof == null || doc.BlockchainProof.ProofStatus != ProofStatus.Anchored)
                return ApiResponse<EvaluationSubmitResponse>.ErrorResponse(
                    "DOCUMENT_NOT_VERIFIED_ON_CHAIN", "Document must be anchored on the blockchain before evaluation.");

            docsToSend.Add(doc);
        }

        var docInputs = docsToSend.Select(d => new
        {
            document_id = d.DocumentID.ToString(),
            document_type = d.DocumentType == DocumentType.Pitch_Deck ? "pitch_deck" : "business_plan",
            file_url_or_path = d.FileURL
        }).ToArray();

        var payload = new
        {
            startup_id = startupId.ToString(),
            documents = docInputs
        };

        try
        {
            var resp = await _http.PostAsJsonAsync("/api/v1/evaluations/", payload, JsonOpts, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("AI evaluation submit failed: {Status} {Body}", resp.StatusCode, body);
                return ApiResponse<EvaluationSubmitResponse>.ErrorResponse(
                    "AI_SERVICE_ERROR", $"AI service returned {(int)resp.StatusCode}.");
            }

            var aiResp = JsonSerializer.Deserialize<JsonElement>(body, JsonOpts);

            var result = new EvaluationSubmitResponse
            {
                EvaluationRunId = aiResp.GetProperty("evaluation_run_id").GetInt32(),
                Status = aiResp.GetProperty("status").GetString() ?? "queued",
                Message = aiResp.TryGetProperty("message", out var msg) ? msg.GetString() ?? "" : "Evaluation queued"
            };

            // Mark documents as being analyzed
            foreach (var d in docsToSend)
            {
                d.AnalysisStatus = AnalysisStatus.NOTANALYZE;
            }
            await _db.SaveChangesAsync(ct);

            return ApiResponse<EvaluationSubmitResponse>.Ok(result, "Evaluation submitted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call AI evaluation service");
            return ApiResponse<EvaluationSubmitResponse>.ErrorResponse(
                "AI_SERVICE_UNAVAILABLE", "AI service is currently unavailable.");
        }
    }

    public async Task<ApiResponse<EvaluationStatusResponse>> GetEvaluationStatusAsync(
        int evaluationRunId, CancellationToken ct = default)
    {
        try
        {
            var resp = await _http.GetAsync($"/api/v1/evaluations/{evaluationRunId}", ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                return ApiResponse<EvaluationStatusResponse>.ErrorResponse(
                    "EVALUATION_NOT_FOUND", "Evaluation run not found.");

            if (!resp.IsSuccessStatusCode)
                return ApiResponse<EvaluationStatusResponse>.ErrorResponse(
                    "AI_SERVICE_ERROR", $"AI service returned {(int)resp.StatusCode}.");

            var result = JsonSerializer.Deserialize<EvaluationStatusResponse>(body, JsonOpts);
            return ApiResponse<EvaluationStatusResponse>.Ok(result!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get evaluation status");
            return ApiResponse<EvaluationStatusResponse>.ErrorResponse(
                "AI_SERVICE_UNAVAILABLE", "AI service is currently unavailable.");
        }
    }

    public async Task<ApiResponse<EvaluationReportResponse>> GetEvaluationReportAsync(
        int evaluationRunId, CancellationToken ct = default)
    {
        try
        {
            var resp = await _http.GetAsync($"/api/v1/evaluations/{evaluationRunId}/report", ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                return ApiResponse<EvaluationReportResponse>.ErrorResponse(
                    "EVALUATION_NOT_FOUND", "Evaluation run not found.");

            if (resp.StatusCode == System.Net.HttpStatusCode.Accepted)
                return ApiResponse<EvaluationReportResponse>.ErrorResponse(
                    "REPORT_NOT_READY", "Report is not ready yet. Please retry shortly.");

            if (!resp.IsSuccessStatusCode)
                return ApiResponse<EvaluationReportResponse>.ErrorResponse(
                    "AI_SERVICE_ERROR", $"AI service returned {(int)resp.StatusCode}.");

            var result = JsonSerializer.Deserialize<EvaluationReportResponse>(body, JsonOpts);
            return ApiResponse<EvaluationReportResponse>.Ok(result!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get evaluation report");
            return ApiResponse<EvaluationReportResponse>.ErrorResponse(
                "AI_SERVICE_UNAVAILABLE", "AI service is currently unavailable.");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Scores — from local DB
    // ═══════════════════════════════════════════════════════════════

    public async Task<ApiResponse<AIScoreLatestResponse>> GetLatestScoreAsync(
        int userId, CancellationToken ct = default)
    {
        var startupId = await ResolveStartupIdAsync(userId, ct);
        if (startupId == 0)
            return ApiResponse<AIScoreLatestResponse>.ErrorResponse(
                "STARTUP_NOT_FOUND", "No startup profile found for this user.");

        var score = await _db.StartupPotentialScores
            .Include(s => s.SubMetrics)
            .Include(s => s.ImprovementRecommendations)
            .Where(s => s.StartupID == startupId && s.IsCurrentScore)
            .FirstOrDefaultAsync(ct);

        if (score == null)
            return ApiResponse<AIScoreLatestResponse>.ErrorResponse(
                "SCORE_NOT_FOUND", "No AI score found for this startup.");

        // Look up the evaluated document types from the associated run
        List<string> evalDocTypes = new();
        if (score.EvaluationRunID.HasValue)
        {
            var run = await _db.AiEvaluationRuns
                .AsNoTracking()
                .Where(r => r.Id == score.EvaluationRunID.Value)
                .Select(r => r.EvaluatedDocumentTypes)
                .FirstOrDefaultAsync(ct);
            if (!string.IsNullOrWhiteSpace(run))
                evalDocTypes = run.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }

        var mapped = MapScore(score);
        mapped.EvaluatedDocumentTypes = evalDocTypes;
        return ApiResponse<AIScoreLatestResponse>.Ok(mapped);
    }

    public async Task<ApiResponse<AIScoreHistoryResponse>> GetScoreHistoryAsync(
        int userId, CancellationToken ct = default)
    {
        var startupId = await ResolveStartupIdAsync(userId, ct);
        if (startupId == 0)
            return ApiResponse<AIScoreHistoryResponse>.ErrorResponse(
                "STARTUP_NOT_FOUND", "No startup profile found for this user.");

        var scores = await _db.StartupPotentialScores
            .Include(s => s.SubMetrics)
            .Include(s => s.ImprovementRecommendations)
            .Where(s => s.StartupID == startupId)
            .OrderByDescending(s => s.CalculatedAt)
            .ToListAsync(ct);

        var result = new AIScoreHistoryResponse
        {
            Scores = scores.Select(MapScore).ToList()
        };

        return ApiResponse<AIScoreHistoryResponse>.Ok(result);
    }

    public async Task<ApiResponse<EvaluationReportResponse>> GetStartupReportAsync(
        int startupId, CancellationToken ct = default)
    {
        // Find the latest completed evaluation for this startup
        try
        {
            var resp = await _http.GetAsync($"/api/v1/evaluations/history?startup_id={startupId}", ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
                return ApiResponse<EvaluationReportResponse>.ErrorResponse(
                    "AI_SERVICE_ERROR", $"AI service returned {(int)resp.StatusCode}.");

            var history = JsonSerializer.Deserialize<List<JsonElement>>(body, JsonOpts);
            var completed = history?
                .Where(h => h.GetProperty("status").GetString() == "completed")
                .OrderByDescending(h => h.GetProperty("submitted_at").GetString())
                .FirstOrDefault();

            if (completed == null || completed.Value.ValueKind == JsonValueKind.Undefined)
                return ApiResponse<EvaluationReportResponse>.ErrorResponse(
                    "REPORT_NOT_FOUND", "No completed evaluation found for this startup.");

            var runId = completed.Value.GetProperty("id").GetInt32();
            return await GetEvaluationReportAsync(runId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get startup report");
            return ApiResponse<EvaluationReportResponse>.ErrorResponse(
                "AI_SERVICE_UNAVAILABLE", "AI service is currently unavailable.");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Recommendations — proxy to Python AI service
    // ═══════════════════════════════════════════════════════════════

    public async Task<ApiResponse<RecommendationListResponse>> GetRecommendationsAsync(
        int investorId, int topN, CancellationToken ct = default)
    {
        try
        {
            var resp = await _http.GetAsync(
                $"/api/v1/recommendations/startups?investor_id={investorId}&top_n={topN}", ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("AI recommendations failed: {Status} {Body}", resp.StatusCode, body);
                return ApiResponse<RecommendationListResponse>.ErrorResponse(
                    "AI_SERVICE_ERROR", $"AI service returned {(int)resp.StatusCode}.");
            }

            var result = JsonSerializer.Deserialize<RecommendationListResponse>(body, JsonOpts);
            return ApiResponse<RecommendationListResponse>.Ok(result!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get AI recommendations");
            return ApiResponse<RecommendationListResponse>.ErrorResponse(
                "AI_SERVICE_UNAVAILABLE", "AI service is currently unavailable.");
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Scoring Model Config — local DB CRUD
    // ═══════════════════════════════════════════════════════════════

    public async Task<ApiResponse<List<ScoringModelConfigDto>>> GetScoringConfigsAsync(CancellationToken ct = default)
    {
        var configs = await _db.ScoringModelConfigurations
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

        var dtos = configs.Select(MapConfig).ToList();
        return ApiResponse<List<ScoringModelConfigDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<ScoringModelConfigDto>> CreateScoringConfigAsync(
        CreateScoringModelConfigRequest request, int userId, CancellationToken ct = default)
    {
        var config = new ScoringModelConfiguration
        {
            Version = request.Version,
            TeamWeight = request.TeamWeight,
            MarketWeight = request.MarketWeight,
            ProductWeight = request.ProductWeight,
            TractionWeight = request.TractionWeight,
            FinancialWeight = request.FinancialWeight,
            ApplicableStage = request.ApplicableStage,
            ChangeNotes = request.ChangeNotes,
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        _db.ScoringModelConfigurations.Add(config);
        await _db.SaveChangesAsync(ct);

        return ApiResponse<ScoringModelConfigDto>.Ok(MapConfig(config), "Scoring config created.");
    }

    public async Task<ApiResponse<ScoringModelConfigDto>> UpdateScoringConfigAsync(
        int configId, UpdateScoringModelConfigRequest request, CancellationToken ct = default)
    {
        var config = await _db.ScoringModelConfigurations.FindAsync(new object[] { configId }, ct);
        if (config == null)
            return ApiResponse<ScoringModelConfigDto>.ErrorResponse(
                "CONFIG_NOT_FOUND", "Scoring model configuration not found.");

        if (request.TeamWeight.HasValue) config.TeamWeight = request.TeamWeight.Value;
        if (request.MarketWeight.HasValue) config.MarketWeight = request.MarketWeight.Value;
        if (request.ProductWeight.HasValue) config.ProductWeight = request.ProductWeight.Value;
        if (request.TractionWeight.HasValue) config.TractionWeight = request.TractionWeight.Value;
        if (request.FinancialWeight.HasValue) config.FinancialWeight = request.FinancialWeight.Value;
        if (request.ApplicableStage != null) config.ApplicableStage = request.ApplicableStage;
        if (request.ChangeNotes != null) config.ChangeNotes = request.ChangeNotes;

        await _db.SaveChangesAsync(ct);
        return ApiResponse<ScoringModelConfigDto>.Ok(MapConfig(config), "Scoring config updated.");
    }

    public async Task<ApiResponse<ScoringModelConfigDto>> ActivateScoringConfigAsync(
        int configId, CancellationToken ct = default)
    {
        var config = await _db.ScoringModelConfigurations.FindAsync(new object[] { configId }, ct);
        if (config == null)
            return ApiResponse<ScoringModelConfigDto>.ErrorResponse(
                "CONFIG_NOT_FOUND", "Scoring model configuration not found.");

        // Deactivate all others
        var activeConfigs = await _db.ScoringModelConfigurations
            .Where(c => c.IsActive)
            .ToListAsync(ct);
        foreach (var c in activeConfigs)
            c.IsActive = false;

        config.IsActive = true;
        config.ActivatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return ApiResponse<ScoringModelConfigDto>.Ok(MapConfig(config), "Scoring config activated.");
    }

    // ═══════════════════════════════════════════════════════════════
    //  Mapping helpers
    // ═══════════════════════════════════════════════════════════════

    private async Task<int> ResolveStartupIdAsync(int userId, CancellationToken ct)
    {
        var startup = await _db.Startups
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserID == userId, ct);
        return startup?.StartupID ?? 0;
    }

    private static AIScoreLatestResponse MapScore(StartupPotentialScore s) => new()
    {
        ScoreId = s.ScoreID,
        StartupId = s.StartupID,
        OverallScore = s.OverallScore,
        TeamScore = s.TeamScore < 0 ? null : s.TeamScore,
        MarketScore = s.MarketScore < 0 ? null : s.MarketScore,
        ProductScore = s.ProductScore < 0 ? null : s.ProductScore,
        TractionScore = s.TractionScore < 0 ? null : s.TractionScore,
        FinancialScore = s.FinancialScore < 0 ? null : s.FinancialScore,
        PitchDeckScore = s.PitchDeckOverallScore,
        BusinessPlanScore = s.BusinessPlanOverallScore,
        CalculatedAt = s.CalculatedAt,
        EvaluationRunId = s.EvaluationRunID,
        SubMetrics = s.SubMetrics.Select(m => {
            var nameUpper = (m.MetricName ?? "").ToUpperInvariant();
            string pillar = "OTHER";
            if (nameUpper.Contains("TEAM")) pillar = "TEAM";
            else if (nameUpper.Contains("MARKET")) pillar = "MARKET";
            else if (nameUpper.Contains("SOLUTION") || nameUpper.Contains("PRODUCT") || nameUpper.Contains("DIFFERENTIATION")) pillar = "PRODUCT";
            else if (nameUpper.Contains("TRACTION") || nameUpper.Contains("VALIDATION") || nameUpper.Contains("GROWTH") || nameUpper.Contains("MILESTONE") || nameUpper.Contains("ADOPTION") || nameUpper.Contains("RETENTION")) pillar = "TRACTION";
            else if (nameUpper.Contains("BUSINESS") || nameUpper.Contains("FINANCIAL") || nameUpper.Contains("REVENUE") || 
                     nameUpper.Contains("GTM") || nameUpper.Contains("MONETIZATION") || nameUpper.Contains("MODEL") || 
                     nameUpper.Contains("ECONOMICS") || nameUpper.Contains("SCALABILITY") || nameUpper.Contains("SALES") || 
                     nameUpper.Contains("COMMERCIAL") || nameUpper.Contains("PROJECTION") || nameUpper.Contains("COST") || 
                     nameUpper.Contains("BUDGET")) pillar = "FINANCIAL";

            return new SubMetricDto
            {
                Pillar = pillar,
                Category = m.Category,
                MetricName = m.MetricName,
                MetricValue = m.MetricValue,
                MetricScore = m.MetricScore,
                Explanation = m.Explanation
            };
        }).ToList(),
        Recommendations = s.ImprovementRecommendations.Select(r => new ImprovementRecommendationDto
        {
            Category = r.Category,
            Priority = r.Priority.ToString(),
            RecommendationText = r.RecommendationText,
            ExpectedImpact = r.ExpectedImpact
        }).ToList()
    };

    private static ScoringModelConfigDto MapConfig(ScoringModelConfiguration c) => new()
    {
        ConfigId = c.ConfigID,
        Version = c.Version,
        TeamWeight = c.TeamWeight,
        MarketWeight = c.MarketWeight,
        ProductWeight = c.ProductWeight,
        TractionWeight = c.TractionWeight,
        FinancialWeight = c.FinancialWeight,
        ApplicableStage = c.ApplicableStage,
        ChangeNotes = c.ChangeNotes,
        IsActive = c.IsActive,
        CreatedAt = c.CreatedAt
    };
}
