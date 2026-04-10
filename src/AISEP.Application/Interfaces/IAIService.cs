using AISEP.Application.DTOs.AI;
using AISEP.Application.DTOs.Common;

namespace AISEP.Application.Interfaces;

public interface IAIService
{
    /// <summary>Trigger AI evaluation for a document (calls Python AI service).</summary>
    Task<ApiResponse<EvaluationSubmitResponse>> TriggerEvaluationAsync(int documentId, int startupId, CancellationToken ct = default);

    /// <summary>Get evaluation run status from AI service.</summary>
    Task<ApiResponse<EvaluationStatusResponse>> GetEvaluationStatusAsync(int evaluationRunId, CancellationToken ct = default);

    /// <summary>Get evaluation report from AI service.</summary>
    Task<ApiResponse<EvaluationReportResponse>> GetEvaluationReportAsync(int evaluationRunId, CancellationToken ct = default);

    /// <summary>Get latest AI score for a startup from local DB.</summary>
    Task<ApiResponse<AIScoreLatestResponse>> GetLatestScoreAsync(int startupId, CancellationToken ct = default);

    /// <summary>Get AI scoring history for a startup from local DB.</summary>
    Task<ApiResponse<AIScoreHistoryResponse>> GetScoreHistoryAsync(int startupId, CancellationToken ct = default);

    /// <summary>Get detailed AI report for a startup.</summary>
    Task<ApiResponse<EvaluationReportResponse>> GetStartupReportAsync(int startupId, CancellationToken ct = default);

    /// <summary>Get AI-powered investor recommendations for a startup list.</summary>
    Task<ApiResponse<RecommendationListResponse>> GetRecommendationsAsync(int investorId, int topN, CancellationToken ct = default);

    // ── Admin: Scoring Model Config ──────────────────────────────

    Task<ApiResponse<List<ScoringModelConfigDto>>> GetScoringConfigsAsync(CancellationToken ct = default);
    Task<ApiResponse<ScoringModelConfigDto>> CreateScoringConfigAsync(CreateScoringModelConfigRequest request, int userId, CancellationToken ct = default);
    Task<ApiResponse<ScoringModelConfigDto>> UpdateScoringConfigAsync(int configId, UpdateScoringModelConfigRequest request, CancellationToken ct = default);
    Task<ApiResponse<ScoringModelConfigDto>> ActivateScoringConfigAsync(int configId, CancellationToken ct = default);
}
