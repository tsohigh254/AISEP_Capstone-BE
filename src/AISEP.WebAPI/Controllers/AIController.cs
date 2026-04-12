using AISEP.Application.DTOs.AI;
using AISEP.Application.Interfaces;
using AISEP.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AISEP.WebAPI.Controllers;

/// <summary>
/// AI evaluation, scoring, reports, trends, and recommendations.
/// Proxies to Python AI service with JWT auth enforcement.
/// </summary>
[ApiController]
[Route("api/ai")]
[Tags("AI")]
[Authorize]
public class AIController : ControllerBase
{
    private readonly IAIService _aiService;

    public AIController(IAIService aiService)
    {
        _aiService = aiService;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Startup AI Evaluation
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Trigger AI evaluation for a document.</summary>
    /// <remarks>Startup role required. Sends document to AI service for async processing.</remarks>
    [HttpPost("evaluate/{documentId}")]
    [Authorize(Policy = "StartupOnly")]
    public async Task<IActionResult> TriggerEvaluation(int documentId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _aiService.TriggerEvaluationAsync(documentId, userId, ct);
        return result.ToEnvelope();
    }

    /// <summary>Get latest AI score for current startup.</summary>
    [HttpGet("scores/latest")]
    [Authorize(Policy = "StartupOnly")]
    public async Task<IActionResult> GetLatestScore(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _aiService.GetLatestScoreAsync(userId, ct);
        return result.ToEnvelope();
    }

    /// <summary>Get AI scoring history for current startup.</summary>
    [HttpGet("history")]
    [Authorize(Policy = "StartupOnly")]
    public async Task<IActionResult> GetScoreHistory(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _aiService.GetScoreHistoryAsync(userId, ct);
        return result.ToEnvelope();
    }

    /// <summary>Get detailed AI report for a startup.</summary>
    /// <remarks>Accessible by Startup owner, Staff, Investor (if shared).</remarks>
    [HttpGet("reports/{startupId}")]
    public async Task<IActionResult> GetStartupReport(int startupId, CancellationToken ct)
    {
        var result = await _aiService.GetStartupReportAsync(startupId, ct);
        return result.ToEnvelope();
    }

    /// <summary>Get evaluation run status.</summary>
    [HttpGet("evaluations/{evaluationRunId}/status")]
    [Authorize(Policy = "StartupOnly")]
    public async Task<IActionResult> GetEvaluationStatus(int evaluationRunId, CancellationToken ct)
    {
        var result = await _aiService.GetEvaluationStatusAsync(evaluationRunId, ct);
        return result.ToEnvelope();
    }

    /// <summary>Get evaluation report by run ID.</summary>
    [HttpGet("evaluations/{evaluationRunId}/report")]
    [Authorize(Policy = "StartupOnly")]
    public async Task<IActionResult> GetEvaluationReport(int evaluationRunId, CancellationToken ct)
    {
        var result = await _aiService.GetEvaluationReportAsync(evaluationRunId, ct);
        return result.ToEnvelope();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Investor AI Features
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Get AI investment trends.</summary>
    [HttpGet("trends")]
    [Authorize(Policy = "InvestorOnly")]
    public async Task<IActionResult> GetTrends(CancellationToken ct)
    {
        // Trends are derived from recommendation data
        var userId = GetCurrentUserId();
        var result = await _aiService.GetRecommendationsAsync(userId, 10, ct);
        return result.ToEnvelope();
    }

    /// <summary>Get AI-powered startup recommendations for an investor.</summary>
    [HttpGet("/api/investors/recommendations")]
    [Authorize(Policy = "InvestorOnly")]
    public async Task<IActionResult> GetRecommendations([FromQuery] int topN = 10, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var result = await _aiService.GetRecommendationsAsync(userId, topN, ct);
        return result.ToEnvelope();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Admin: Scoring Model Configuration
    // ═══════════════════════════════════════════════════════════════

    /// <summary>List all scoring model configurations.</summary>
    [HttpGet("/api/admin/scoring-model-configs")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetScoringConfigs(CancellationToken ct)
    {
        var result = await _aiService.GetScoringConfigsAsync(ct);
        return result.ToEnvelope();
    }

    /// <summary>Create a new scoring model configuration.</summary>
    [HttpPost("/api/admin/scoring-model-configs")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreateScoringConfig(
        [FromBody] CreateScoringModelConfigRequest request, CancellationToken ct)
    {
        var result = await _aiService.CreateScoringConfigAsync(request, GetCurrentUserId(), ct);
        return result.ToCreatedEnvelope();
    }

    /// <summary>Update a scoring model configuration.</summary>
    [HttpPut("/api/admin/scoring-model-configs/{configId}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateScoringConfig(
        int configId, [FromBody] UpdateScoringModelConfigRequest request, CancellationToken ct)
    {
        var result = await _aiService.UpdateScoringConfigAsync(configId, request, ct);
        return result.ToEnvelope();
    }

    /// <summary>Activate a scoring model configuration.</summary>
    [HttpPost("/api/admin/scoring-model-configs/{configId}/activate")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ActivateScoringConfig(int configId, CancellationToken ct)
    {
        var result = await _aiService.ActivateScoringConfigAsync(configId, ct);
        return result.ToEnvelope();
    }
}
