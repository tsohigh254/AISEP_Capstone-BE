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

public class AiEvaluationService : IAiEvaluationService
{
    private readonly ApplicationDbContext _db;
    private readonly PythonAiClient _pythonClient;
    private readonly ILogger<AiEvaluationService> _logger;

    // Terminal statuses — no further transitions allowed
    private static readonly HashSet<string> TerminalStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "completed", "failed"
    };

    // Status precedence for state reconciliation (higher = more authoritative)
    private static readonly Dictionary<string, int> StatusPrecedence = new(StringComparer.OrdinalIgnoreCase)
    {
        ["queued"] = 1,
        ["processing"] = 2,
        ["partial_completed"] = 3,
        ["retry"] = 2,
        ["completed"] = 10,
        ["failed"] = 10
    };

    public AiEvaluationService(
        ApplicationDbContext db,
        PythonAiClient pythonClient,
        ILogger<AiEvaluationService> logger)
    {
        _db = db;
        _pythonClient = pythonClient;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════
    //  Submit
    // ═══════════════════════════════════════════════════════════

    public async Task<ApiResponse<EvaluationSubmitResult>> SubmitEvaluationAsync(
        int currentUserId, SubmitEvaluationRequest request)
    {
        // Validate: startup exists and belongs to user
        var startup = await _db.Startups.FirstOrDefaultAsync(s => s.StartupID == request.StartupId);
        if (startup == null)
            return ApiResponse<EvaluationSubmitResult>.ErrorResponse("NOT_FOUND", "Startup not found.");
        if (startup.UserID != currentUserId)
            return ApiResponse<EvaluationSubmitResult>.ErrorResponse("ACCESS_DENIED", "You do not own this startup.");

        // Gather documents for evaluation
        var docsQuery = _db.Documents
            .Where(d => d.StartupID == request.StartupId && !d.IsArchived);
        if (request.DocumentIds is { Count: > 0 })
            docsQuery = docsQuery.Where(d => request.DocumentIds.Contains(d.DocumentID));

        var docs = await docsQuery.ToListAsync();
        if (docs.Count == 0)
            return ApiResponse<EvaluationSubmitResult>.ErrorResponse("VALIDATION_ERROR", "No documents available for evaluation.");

        // Build Python request
        var correlationId = Guid.NewGuid().ToString();
        var pythonReq = new PythonSubmitEvaluationRequest
        {
            StartupId = request.StartupId.ToString(),
            Documents = docs.Select(d => new PythonDocumentInput
            {
                DocumentId = d.DocumentID.ToString(),
                DocumentType = MapDocumentType(d.DocumentType),
                FileUrlOrPath = d.FileURL
            }).ToList()
        };

        try
        {
            var pythonResp = await _pythonClient.SubmitEvaluationAsync(pythonReq, correlationId);

            // Create local tracking record
            var run = new AiEvaluationRun
            {
                StartupId = request.StartupId,
                PythonRunId = pythonResp.EvaluationRunId,
                Status = pythonResp.Status,
                CorrelationId = correlationId,
                SubmittedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.AiEvaluationRuns.Add(run);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Evaluation submitted: LocalRunId={RunId}, PythonRunId={PythonRunId}, StartupId={StartupId}, Correlation={CorrelationId}",
                run.Id, run.PythonRunId, run.StartupId, correlationId);

            return ApiResponse<EvaluationSubmitResult>.SuccessResponse(new EvaluationSubmitResult
            {
                RunId = run.Id,
                StartupId = run.StartupId,
                Status = run.Status,
                Message = pythonResp.Message
            });
        }
        catch (PythonAiException ex)
        {
            _logger.LogError(ex, "Python AI submit failed: {Code} {Message}", ex.Code, ex.Message);
            return ApiResponse<EvaluationSubmitResult>.ErrorResponse(
                ex.Code, $"AI Service error: {ex.Message}");
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Status (with reconciliation)
    // ═══════════════════════════════════════════════════════════

    public async Task<ApiResponse<EvaluationStatusResult>> GetEvaluationStatusAsync(int runId)
    {
        var run = await _db.AiEvaluationRuns.FindAsync(runId);
        if (run == null)
            return ApiResponse<EvaluationStatusResult>.ErrorResponse("NOT_FOUND", "Evaluation run not found.");

        // If not terminal, poll Python for fresh status
        if (!TerminalStatuses.Contains(run.Status))
        {
            try
            {
                var pythonStatus = await _pythonClient.GetEvaluationStatusAsync(run.PythonRunId, run.CorrelationId);
                ReconcileStatus(run, pythonStatus.Status, pythonStatus.OverallScore, pythonStatus.FailureReason, "poll");
                await _db.SaveChangesAsync();
            }
            catch (PythonAiException ex)
            {
                _logger.LogWarning(ex, "Failed to poll Python status for run {RunId}: {Code}", runId, ex.Code);
                // Return stale local state — don't fail the user request
            }
        }

        return ApiResponse<EvaluationStatusResult>.SuccessResponse(MapToStatusResult(run));
    }

    // ═══════════════════════════════════════════════════════════
    //  Report (with validation gate)
    // ═══════════════════════════════════════════════════════════

    public async Task<ApiResponse<EvaluationReportResult>> GetEvaluationReportAsync(int runId)
    {
        var run = await _db.AiEvaluationRuns.FindAsync(runId);
        if (run == null)
            return ApiResponse<EvaluationReportResult>.ErrorResponse("NOT_FOUND", "Evaluation run not found.");

        // If we already have a valid cached report, return it
        if (!string.IsNullOrEmpty(run.ReportJson) && run.IsReportValid)
        {
            var cached = JsonSerializer.Deserialize<object>(run.ReportJson);
            return ApiResponse<EvaluationReportResult>.SuccessResponse(new EvaluationReportResult
            {
                RunId = run.Id,
                StartupId = run.StartupId,
                Status = run.Status,
                IsReportValid = true,
                Report = cached
            });
        }

        // Fetch from Python
        try
        {
            var (report, statusCode) = await _pythonClient.GetEvaluationReportAsync(run.PythonRunId, run.CorrelationId);

            if (statusCode == HttpStatusCode.Accepted || report == null)
            {
                return ApiResponse<EvaluationReportResult>.SuccessResponse(new EvaluationReportResult
                {
                    RunId = run.Id,
                    StartupId = run.StartupId,
                    Status = run.Status,
                    IsReportValid = false,
                    ValidationMessage = "Report is not ready yet. Please retry shortly."
                }, "Report not ready");
            }

            // Validate the report
            var (isValid, validationMsg) = ValidateReport(report);

            // Cache locally
            run.ReportJson = JsonSerializer.Serialize(report);
            run.IsReportValid = isValid;
            if (report.OverallResult.HasValue)
            {
                // Try to extract overall_score from the overall_result object
                try
                {
                    if (report.OverallResult.Value.TryGetProperty("overall_score", out var scoreProp))
                        run.OverallScore = scoreProp.GetDouble();
                }
                catch { /* score extraction failed — non-fatal */ }
            }
            run.UpdatedAt = DateTime.UtcNow;

            // Also reconcile status if report says completed
            if (!string.IsNullOrEmpty(report.Status))
                ReconcileStatus(run, report.Status, run.OverallScore, null, "report-fetch");

            await _db.SaveChangesAsync();

            var reportObj = JsonSerializer.Deserialize<object>(run.ReportJson);
            return ApiResponse<EvaluationReportResult>.SuccessResponse(new EvaluationReportResult
            {
                RunId = run.Id,
                StartupId = run.StartupId,
                Status = run.Status,
                IsReportValid = isValid,
                Report = reportObj,
                ValidationMessage = validationMsg
            });
        }
        catch (PythonAiException ex)
        {
            _logger.LogWarning(ex, "Failed to fetch report for run {RunId}: {Code}", runId, ex.Code);

            // Map Python error codes to user-friendly messages
            var msg = ex.HttpStatus switch
            {
                HttpStatusCode.NotFound => "Evaluation run not found on AI service.",
                HttpStatusCode.Conflict => "Evaluation failed or has no report available.",
                _ => $"AI Service error: {ex.Message}"
            };
            return ApiResponse<EvaluationReportResult>.ErrorResponse(ex.Code, msg);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Webhook Processing (idempotent)
    // ═══════════════════════════════════════════════════════════

    public async Task ProcessWebhookAsync(EvaluationWebhookPayload payload)
    {
        // Idempotency check
        var alreadyProcessed = await _db.AiWebhookDeliveries
            .AnyAsync(d => d.DeliveryId == payload.DeliveryId);

        if (alreadyProcessed)
        {
            _logger.LogInformation("Webhook delivery {DeliveryId} already processed, skipping.", payload.DeliveryId);
            return;
        }

        // Find the local evaluation run by Python run id
        var run = await _db.AiEvaluationRuns
            .FirstOrDefaultAsync(r => r.PythonRunId == payload.EvaluationRunId);

        // Record delivery regardless
        var delivery = new AiWebhookDelivery
        {
            DeliveryId = payload.DeliveryId,
            EvaluationRunId = run?.Id,
            PayloadJson = JsonSerializer.Serialize(payload),
            ReceivedAt = DateTime.UtcNow
        };
        _db.AiWebhookDeliveries.Add(delivery);

        if (run == null)
        {
            _logger.LogWarning(
                "Webhook for unknown PythonRunId={PythonRunId}, DeliveryId={DeliveryId}",
                payload.EvaluationRunId, payload.DeliveryId);
            delivery.ProcessingNote = "No matching local evaluation run found.";
            delivery.Processed = false;
            await _db.SaveChangesAsync();
            return;
        }

        // Reconcile state from webhook
        ReconcileStatus(run, payload.TerminalStatus, payload.OverallScore, payload.FailureReason, "webhook");

        delivery.Processed = true;
        delivery.ProcessingNote = $"Updated run {run.Id} to status={run.Status}";
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Webhook processed: DeliveryId={DeliveryId}, RunId={RunId}, Status={Status}",
            payload.DeliveryId, run.Id, run.Status);
    }

    // ═══════════════════════════════════════════════════════════
    //  History
    // ═══════════════════════════════════════════════════════════

    public async Task<ApiResponse<List<EvaluationStatusResult>>> GetEvaluationHistoryAsync(int startupId)
    {
        var runs = await _db.AiEvaluationRuns
            .Where(r => r.StartupId == startupId)
            .OrderByDescending(r => r.SubmittedAt)
            .ToListAsync();

        var results = runs.Select(MapToStatusResult).ToList();
        return ApiResponse<List<EvaluationStatusResult>>.SuccessResponse(results);
    }

    // ═══════════════════════════════════════════════════════════
    //  State Reconciliation
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Reconcile incoming status against local state.
    /// Rules:
    ///   1. Never overwrite a terminal status with a non-terminal status.
    ///   2. Use precedence to prevent stale overwrites (lower precedence cannot override higher).
    ///   3. Always update score/failure_reason if the incoming status wins.
    /// </summary>
    private void ReconcileStatus(AiEvaluationRun run, string incomingStatus, double? score, string? failureReason, string source)
    {
        var currentPrecedence = StatusPrecedence.GetValueOrDefault(run.Status, 0);
        var incomingPrecedence = StatusPrecedence.GetValueOrDefault(incomingStatus, 0);

        // Rule 1: never downgrade from terminal
        if (TerminalStatuses.Contains(run.Status) && !TerminalStatuses.Contains(incomingStatus))
        {
            _logger.LogDebug(
                "Skipping non-terminal status '{Incoming}' from {Source} — run {RunId} is already terminal '{Current}'",
                incomingStatus, source, run.Id, run.Status);
            return;
        }

        // Rule 2: incoming must have >= precedence
        if (incomingPrecedence < currentPrecedence)
        {
            _logger.LogDebug(
                "Skipping lower-precedence status '{Incoming}' (p={IncomingP}) from {Source} — current '{Current}' (p={CurrentP}) for run {RunId}",
                incomingStatus, incomingPrecedence, source, run.Status, currentPrecedence, run.Id);
            return;
        }

        var oldStatus = run.Status;
        run.Status = incomingStatus;
        run.UpdatedAt = DateTime.UtcNow;

        if (score.HasValue)
            run.OverallScore = score.Value;

        if (!string.IsNullOrWhiteSpace(failureReason))
            run.FailureReason = failureReason;

        if (oldStatus != incomingStatus)
        {
            _logger.LogInformation(
                "Evaluation run {RunId} status reconciled: {Old} → {New} (source={Source})",
                run.Id, oldStatus, incomingStatus, source);
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Report Validation Gate
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Validates that a canonical report is actually usable.
    /// Returns (isValid, validationMessage).
    /// </summary>
    private static (bool IsValid, string? Message) ValidateReport(PythonCanonicalReport report)
    {
        // Check 1: overall_result must exist and have a score
        if (!report.OverallResult.HasValue || report.OverallResult.Value.ValueKind == JsonValueKind.Null)
            return (false, "Report is missing overall_result.");

        bool hasScore = false;
        try
        {
            if (report.OverallResult.Value.TryGetProperty("overall_score", out var scoreProp)
                && scoreProp.ValueKind == JsonValueKind.Number)
            {
                hasScore = true;
            }
        }
        catch { /* property access failed */ }

        if (!hasScore)
            return (false, "Report overall_score is null or missing — report is incomplete.");

        // Check 2: criteria_results should exist and not be empty
        if (!report.CriteriaResults.HasValue
            || report.CriteriaResults.Value.ValueKind != JsonValueKind.Array
            || report.CriteriaResults.Value.GetArrayLength() == 0)
            return (false, "Report criteria_results is empty or missing.");

        // Check 3: verify at least one criterion has a non-null score
        bool anyScore = false;
        foreach (var criterion in report.CriteriaResults.Value.EnumerateArray())
        {
            if (criterion.TryGetProperty("score", out var s) && s.ValueKind == JsonValueKind.Number)
            {
                anyScore = true;
                break;
            }
        }
        if (!anyScore)
            return (false, "All criteria scores are null — report appears incomplete.");

        return (true, null);
    }

    // ═══════════════════════════════════════════════════════════
    //  Mapping Helpers
    // ═══════════════════════════════════════════════════════════

    private static EvaluationStatusResult MapToStatusResult(AiEvaluationRun run) => new()
    {
        RunId = run.Id,
        StartupId = run.StartupId,
        Status = run.Status,
        OverallScore = run.OverallScore,
        FailureReason = run.FailureReason,
        IsReportReady = !string.IsNullOrEmpty(run.ReportJson),
        IsReportValid = run.IsReportValid,
        SubmittedAt = run.SubmittedAt,
        UpdatedAt = run.UpdatedAt
    };

    private static string MapDocumentType(Domain.Enums.DocumentType docType) => docType switch
    {
        Domain.Enums.DocumentType.Pitch_Deck => "pitch_deck",
        Domain.Enums.DocumentType.Bussiness_Plan => "business_plan",
        _ => "unknown"
    };
}
