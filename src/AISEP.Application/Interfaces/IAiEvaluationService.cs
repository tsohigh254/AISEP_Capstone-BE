using AISEP.Application.DTOs.AI;
using AISEP.Application.DTOs.Common;

namespace AISEP.Application.Interfaces;

/// <summary>
/// Application service for AI evaluation operations.
/// Orchestrates calls to the Python AI Service and manages local tracking state.
/// </summary>
public interface IAiEvaluationService
{
    /// <summary>Submit a new evaluation run for a startup.</summary>
    Task<ApiResponse<EvaluationSubmitResult>> SubmitEvaluationAsync(int currentUserId, SubmitEvaluationRequest request);

    /// <summary>Get the latest evaluation status (poll Python if needed, reconcile local state).</summary>
    Task<ApiResponse<EvaluationStatusResult>> GetEvaluationStatusAsync(int runId, int currentUserId = 0);

    /// <summary>Get the full evaluation report (fetch from Python if not cached locally).</summary>
    Task<ApiResponse<EvaluationReportResult>> GetEvaluationReportAsync(int runId, int currentUserId = 0);

    /// <summary>
    /// Get the source-specific report for a single document type when the run used combined mode.
    /// <paramref name="documentType"/> must be snake_case: <c>pitch_deck</c> or <c>business_plan</c>.
    /// </summary>
    Task<ApiResponse<EvaluationReportResult>> GetSourceReportAsync(int runId, string documentType, int currentUserId = 0);

    /// <summary>Process an incoming webhook callback from the Python AI Service.</summary>
    Task ProcessWebhookAsync(EvaluationWebhookPayload payload);

    /// <summary>Get evaluation history for a startup.</summary>
    Task<ApiResponse<List<EvaluationStatusResult>>> GetEvaluationHistoryAsync(int startupId, int currentUserId = 0);
}
