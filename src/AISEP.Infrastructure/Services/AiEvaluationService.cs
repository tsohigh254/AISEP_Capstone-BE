using AISEP.Application.DTOs.AI;
using AISEP.Application.DTOs.Common;
using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using AISEP.Application.DTOs.Notification;
using AISEP.Domain.Enums;

namespace AISEP.Infrastructure.Services;

public class AiEvaluationService : IAiEvaluationService
{
    private readonly ApplicationDbContext _db;
    private readonly PythonAiClient _pythonClient;
    private readonly ICloudinaryService _cloudinary;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AiEvaluationService> _logger;
    private readonly INotificationDeliveryService _notifications;

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
        ICloudinaryService cloudinary,
        IServiceScopeFactory scopeFactory,
        ILogger<AiEvaluationService> logger,
        INotificationDeliveryService notifications)
    {
        _db = db;
        _pythonClient = pythonClient;
        _cloudinary = cloudinary;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _notifications = notifications;
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

        // Gather documents for evaluation — only Pitch Deck / Business Plan that are anchored on-chain
        var docsQuery = _db.Documents
            .Include(d => d.BlockchainProof)
            .Where(d => d.StartupID == request.StartupId && !d.IsArchived
                        && (d.DocumentType == Domain.Enums.DocumentType.Pitch_Deck || d.DocumentType == Domain.Enums.DocumentType.Bussiness_Plan)
                        && d.BlockchainProof != null
                        && d.BlockchainProof.ProofStatus == Domain.Enums.ProofStatus.Anchored);

        if (request.DocumentIds is { Count: > 0 })
            docsQuery = docsQuery.Where(d => request.DocumentIds.Contains(d.DocumentID));

        var allDocs = await docsQuery.ToListAsync();
        
        // Python restriction: Only 1 document per type allowed in a single run.
        // If multiple are selected/available, pick the most recent UploadedAt for each type.
        var docs = allDocs
            .GroupBy(d => d.DocumentType)
            .Select(g => g.OrderByDescending(d => d.UploadedAt).First())
            .ToList();

        if (docs.Count == 0)
            return ApiResponse<EvaluationSubmitResult>.ErrorResponse("VALIDATION_ERROR", "No anchored Pitch Deck or Business Plan documents available for evaluation.");

        // Build Python request with signed URLs (time-limited, no auth needed by Python)
        var correlationId = Guid.NewGuid().ToString();

        var pythonReq = new PythonSubmitEvaluationRequest
        {
            StartupId = request.StartupId.ToString(),
            Documents = docs.Select(d => new PythonDocumentInput
            {
                DocumentId = d.DocumentID.ToString(),
                DocumentType = MapDocumentType(d.DocumentType),
                // Prefer signed URL; fall back to plain FileURL if signing fails
                // (e.g. non-Cloudinary URL or extraction failure)
                FileUrlOrPath = GenerateDocumentUrl(d.FileURL)
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
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Python AI service unreachable during submit for StartupId={StartupId}", request.StartupId);
            return ApiResponse<EvaluationSubmitResult>.ErrorResponse(
                "AI_SERVICE_UNAVAILABLE", "The AI evaluation service is currently unavailable. Please try again later.");
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Status (with reconciliation)
    // ═══════════════════════════════════════════════════════════

    public async Task<ApiResponse<EvaluationStatusResult>> GetEvaluationStatusAsync(int runId, int currentUserId = 0)
    {
        var run = await _db.AiEvaluationRuns
            .Include(r => r.Startup)
            .FirstOrDefaultAsync(r => r.Id == runId);
        if (run == null)
            return ApiResponse<EvaluationStatusResult>.ErrorResponse("NOT_FOUND", "Evaluation run not found.");

        if (currentUserId != 0 && run.Startup?.UserID != currentUserId)
            return ApiResponse<EvaluationStatusResult>.ErrorResponse("ACCESS_DENIED", "You do not have access to this evaluation run.");

        // If not terminal, poll Python for fresh status
        if (!TerminalStatuses.Contains(run.Status))
        {
            try
            {
                var pythonStatus = await _pythonClient.GetEvaluationStatusAsync(run.PythonRunId, run.CorrelationId);
                await ReconcileStatus(run, pythonStatus.Status, pythonStatus.OverallScore, pythonStatus.FailureReason, "poll");
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

    public async Task<ApiResponse<EvaluationReportResult>> GetEvaluationReportAsync(int runId, int currentUserId = 0)
    {
        var run = await _db.AiEvaluationRuns
            .Include(r => r.Startup)
            .FirstOrDefaultAsync(r => r.Id == runId);
        if (run == null)
            return ApiResponse<EvaluationReportResult>.ErrorResponse("NOT_FOUND", "Evaluation run not found.");

        if (currentUserId != 0 && run.Startup?.UserID != currentUserId)
            return ApiResponse<EvaluationReportResult>.ErrorResponse("ACCESS_DENIED", "You do not have access to this evaluation run.");

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

            await _db.SaveChangesAsync();

            // Trigger sync to StartupPotentialScore
            await SyncToPotentialScoreAsync(run);

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
    //  Source Report (per-document in combined-mode runs)
    // ═══════════════════════════════════════════════════════════

    private static readonly HashSet<string> ValidDocumentTypes =
        new(StringComparer.OrdinalIgnoreCase) { "pitch_deck", "business_plan" };

    public async Task<ApiResponse<EvaluationReportResult>> GetSourceReportAsync(
        int runId, string documentType, int currentUserId = 0)
    {
        // Validate document type before any DB call (matches Python's validation order)
        if (string.IsNullOrWhiteSpace(documentType) || !ValidDocumentTypes.Contains(documentType))
            return ApiResponse<EvaluationReportResult>.ErrorResponse(
                "INVALID_DOCUMENT_TYPE",
                $"Invalid document type '{documentType}'. Allowed values: pitch_deck, business_plan.");

        var run = await _db.AiEvaluationRuns
            .Include(r => r.Startup)
            .FirstOrDefaultAsync(r => r.Id == runId);
        if (run == null)
            return ApiResponse<EvaluationReportResult>.ErrorResponse("NOT_FOUND", "Evaluation run not found.");

        if (currentUserId != 0 && run.Startup?.UserID != currentUserId)
            return ApiResponse<EvaluationReportResult>.ErrorResponse("ACCESS_DENIED", "You do not have access to this evaluation run.");

        try
        {
            var (report, statusCode) = await _pythonClient.GetSourceReportAsync(
                run.PythonRunId, documentType.ToLowerInvariant(), run.CorrelationId);

            if (statusCode == HttpStatusCode.Accepted || report == null)
                return ApiResponse<EvaluationReportResult>.SuccessResponse(new EvaluationReportResult
                {
                    RunId = run.Id,
                    StartupId = run.StartupId,
                    Status = run.Status,
                    IsReportValid = false,
                    ValidationMessage = "Source report is not ready yet. Please retry shortly."
                }, "Report not ready");

            var (isValid, validationMsg) = ValidateReport(report);

            var reportObj = JsonSerializer.Deserialize<object>(JsonSerializer.Serialize(report));
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
            _logger.LogWarning(ex, "Failed to fetch source report for run {RunId} / {DocType}: {Code}",
                runId, documentType, ex.Code);

            var msg = ex.HttpStatus switch
            {
                HttpStatusCode.NotFound => $"Document '{documentType}' not found for this evaluation run.",
                HttpStatusCode.BadRequest => $"Invalid document type: {ex.Message}",
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
            .Include(r => r.Startup)
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
        await ReconcileStatus(run, payload.TerminalStatus, payload.OverallScore, payload.FailureReason, "webhook");

        delivery.Processed = true;
        delivery.ProcessingNote = $"Updated run {run.Id} to status={run.Status}";
        await _db.SaveChangesAsync();

        // If evaluation completed, reindex startup and SYNC to PotentialScore
        if (string.Equals(run.Status, "completed", StringComparison.OrdinalIgnoreCase))
        {
            var startupIdForEval = run.StartupId;
            var runIdForEval = run.Id;
            
            _ = Task.Run(async () =>
            {
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var evalSvc = scope.ServiceProvider.GetRequiredService<IAiEvaluationService>();
                    var recSvc = scope.ServiceProvider.GetRequiredService<IAiRecommendationService>();
                    
                    // 1. Ensure report is fetched and sync to PotentialScore
                    await evalSvc.GetEvaluationReportAsync(runIdForEval);
                    
                    // 2. Reindex for recommendation engine
                    await recSvc.ReindexStartupAsync(startupIdForEval);
                }
                catch (Exception ex) 
                { 
                    _logger.LogWarning(ex, "Background sync/reindex after evaluation failed for startup {StartupId}", startupIdForEval); 
                }
            });
        }

        _logger.LogInformation(
            "Webhook processed: DeliveryId={DeliveryId}, RunId={RunId}, Status={Status}",
            payload.DeliveryId, run.Id, run.Status);
    }

    // ═══════════════════════════════════════════════════════════
    //  History
    // ═══════════════════════════════════════════════════════════

    public async Task<ApiResponse<List<EvaluationStatusResult>>> GetEvaluationHistoryAsync(int startupId, int currentUserId = 0)
    {
        if (currentUserId != 0)
        {
            var startup = await _db.Startups
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StartupID == startupId);
            if (startup == null)
                return ApiResponse<List<EvaluationStatusResult>>.ErrorResponse("NOT_FOUND", "Startup not found.");
            if (startup.UserID != currentUserId)
                return ApiResponse<List<EvaluationStatusResult>>.ErrorResponse("ACCESS_DENIED", "You do not have access to this startup's evaluation history.");
        }

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
    private async Task ReconcileStatus(AiEvaluationRun run, string incomingStatus, double? score, string? failureReason, string source)
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

            // Notify Startup if completed
            if (string.Equals(incomingStatus, "completed", StringComparison.OrdinalIgnoreCase) && run.Startup != null)
            {
                try
                {
                    await _notifications.CreateAndPushAsync(new CreateNotificationRequest
                    {
                        UserId = run.Startup.UserID,
                        NotificationType = "AI_EVALUATION",
                        Title = "Đánh giá AI hoàn tất",
                        Message = $"Tiến trình đánh giá AI cho startup '{run.Startup.CompanyName}' đã hoàn tất. Điểm số: {run.OverallScore:F1}",
                        RelatedEntityType = "AiEvaluationRun",
                        RelatedEntityId = run.Id,
                        ActionUrl = $"/startup/evaluations/{run.Id}"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send notification for completed evaluation {RunId}", run.Id);
                }
            }
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

        // Check 3: verify at least one SCORED criterion has a non-null final_score.
        // Criteria with status "not_applicable" legitimately have null scores — exclude them.
        bool anyScoredCriterion = false;
        foreach (var criterion in report.CriteriaResults.Value.EnumerateArray())
        {
            // Skip not_applicable criteria
            if (criterion.TryGetProperty("status", out var statusProp)
                && statusProp.GetString()?.Equals("not_applicable", StringComparison.OrdinalIgnoreCase) == true)
                continue;

            // Python uses 'final_score' (not 'score')
            if (criterion.TryGetProperty("final_score", out var s) && s.ValueKind == JsonValueKind.Number)
            {
                anyScoredCriterion = true;
                break;
            }
        }
        if (!anyScoredCriterion)
            return (false, "All scored criteria have null final_score — report appears incomplete.");

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
        Domain.Enums.DocumentType.Other => "other",
        _ => "unknown"
    };

    /// <summary>
    /// Generate a signed Cloudinary URL for Python to download.
    /// Falls back to the raw FileURL if signing fails (e.g. URL is not a Cloudinary URL,
    /// or resource_type mismatch would make the signed URL invalid anyway).
    /// </summary>
    private string GenerateDocumentUrl(string fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            return fileUrl;

        try
        {
            var signed = _cloudinary.GenerateSignedDocumentUrl(
                storageKey: null,
                fallbackUrl: fileUrl,
                fileName: null,
                expiresInMinutes: 120);

            // If signing returned the same original URL (fallback path — public_id extraction failed),
            // log a warning so we know Python will receive an unsigned URL.
            if (signed == fileUrl || string.IsNullOrWhiteSpace(signed))
            {
                _logger.LogWarning(
                    "Could not extract Cloudinary public_id from FileURL '{FileUrl}'. " +
                    "Python will receive unsigned URL — download may fail if resource is private.",
                    fileUrl);
                return fileUrl;
            }

            return signed;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Signed URL generation failed for '{FileUrl}'. Falling back to plain URL.", fileUrl);
            return fileUrl;
        }
    }
    // ═══════════════════════════════════════════════════════════
    //  Sync to StartupPotentialScore
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Synchronizes the results of an AiEvaluationRun into the StartupPotentialScore table.
    /// This ensures that legacy features (Readiness Checklist, Dashboard scores) work with the new AI system.
    /// </summary>
    private async Task SyncToPotentialScoreAsync(AiEvaluationRun run)
    {
        if (string.IsNullOrEmpty(run.ReportJson) || !run.IsReportValid) return;

        try
        {
            var report = JsonSerializer.Deserialize<PythonCanonicalReport>(run.ReportJson);
            if (report == null || !report.OverallResult.HasValue || !report.CriteriaResults.HasValue) return;

            // 1. Map scores from criteria using flexible keyword matching
            // Python AI criterion names vary (e.g. "Team & Execution Readiness" vs "Team_&_Execution_Readiness")
            // so we use case-insensitive Contains instead of exact matching.
            float teamScore = 0, marketScore = 0, productScore = 0, tractionScore = 0, financialScore = 0;
            var subMetrics = new List<ScoreSubMetric>();

            foreach (var criterion in report.CriteriaResults.Value.EnumerateArray())
            {
                string? name = null;
                // Python may use "criterion" or "criterion_name" depending on report version
                if (criterion.TryGetProperty("criterion", out var cProp))
                    name = cProp.GetString();
                else if (criterion.TryGetProperty("criterion_name", out var cnProp))
                    name = cnProp.GetString();

                float scoreValue = 0;
                if (criterion.TryGetProperty("final_score", out var sProp) && sProp.ValueKind == JsonValueKind.Number)
                    scoreValue = (float)sProp.GetDouble();
                else if (criterion.TryGetProperty("score", out var s2Prop) && s2Prop.ValueKind == JsonValueKind.Number)
                    scoreValue = (float)s2Prop.GetDouble();

                // Flexible keyword mapping — case-insensitive, handles both underscored and spaced names
                var nameUpper = (name ?? "").ToUpperInvariant();
                if (nameUpper.Contains("TEAM"))
                    teamScore = scoreValue;
                else if (nameUpper.Contains("MARKET"))
                    marketScore = scoreValue;
                else if (nameUpper.Contains("SOLUTION") || nameUpper.Contains("PRODUCT") || nameUpper.Contains("DIFFERENTIATION"))
                    productScore = scoreValue;
                else if (nameUpper.Contains("TRACTION") || nameUpper.Contains("VALIDATION"))
                    tractionScore = scoreValue;
                else if (nameUpper.Contains("BUSINESS") || nameUpper.Contains("FINANCIAL") || nameUpper.Contains("REVENUE") || 
                         nameUpper.Contains("GO_TO_MARKET") || nameUpper.Contains("GO-TO-MARKET") || nameUpper.Contains("GTM") ||
                         nameUpper.Contains("MONETIZATION"))
                    financialScore = scoreValue;

                // Create sub-metric
                subMetrics.Add(new ScoreSubMetric
                {
                    Category = name ?? "Other",
                    MetricName = name?.Replace("_", " ") ?? "Unknown",
                    MetricScore = scoreValue,
                    Explanation = criterion.TryGetProperty("explanation", out var exp) ? exp.GetString() : null,
                    MetricValue = criterion.TryGetProperty("evidence_strength_summary", out var ess) ? ess.GetString() : null
                });
            }

            // Fallback: if all sub-scores are 0 but overall_result has dimension breakdowns, extract from there
            if (teamScore == 0 && marketScore == 0 && productScore == 0 && tractionScore == 0 && financialScore == 0)
            {
                try
                {
                    var overall = report.OverallResult!.Value;
                    if (overall.TryGetProperty("team_score", out var ts) && ts.ValueKind == JsonValueKind.Number)
                        teamScore = (float)ts.GetDouble();
                    if (overall.TryGetProperty("market_score", out var ms) && ms.ValueKind == JsonValueKind.Number)
                        marketScore = (float)ms.GetDouble();
                    if (overall.TryGetProperty("product_score", out var ps) && ps.ValueKind == JsonValueKind.Number)
                        productScore = (float)ps.GetDouble();
                    if (overall.TryGetProperty("traction_score", out var trs) && trs.ValueKind == JsonValueKind.Number)
                        tractionScore = (float)trs.GetDouble();
                    if (overall.TryGetProperty("financial_score", out var fs) && fs.ValueKind == JsonValueKind.Number)
                        financialScore = (float)fs.GetDouble();

                    // Also try alternative key names used by some Python versions
                    if (teamScore == 0 && overall.TryGetProperty("dimension_scores", out var dims) && dims.ValueKind == JsonValueKind.Object)
                    {
                        if (dims.TryGetProperty("team", out var dt) && dt.ValueKind == JsonValueKind.Number)
                            teamScore = (float)dt.GetDouble();
                        if (dims.TryGetProperty("market", out var dm) && dm.ValueKind == JsonValueKind.Number)
                            marketScore = (float)dm.GetDouble();
                        if (dims.TryGetProperty("product", out var dp) && dp.ValueKind == JsonValueKind.Number)
                            productScore = (float)dp.GetDouble();
                        if (dims.TryGetProperty("traction", out var dtr) && dtr.ValueKind == JsonValueKind.Number)
                            tractionScore = (float)dtr.GetDouble();
                        if (dims.TryGetProperty("financial", out var df) && df.ValueKind == JsonValueKind.Number)
                            financialScore = (float)df.GetDouble();
                    }

                    _logger.LogInformation(
                        "Sub-scores extracted from overall_result fallback for run {RunId}: T={Team} M={Market} P={Product} Tr={Traction} F={Financial}",
                        run.Id, teamScore, marketScore, productScore, tractionScore, financialScore);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to extract sub-scores from overall_result for run {RunId}", run.Id);
                }
            }

            // 2. Map recommendations
            var recommendations = new List<ScoreImprovementRecommendation>();
            if (report.Narrative.HasValue && report.Narrative.Value.TryGetProperty("recommendations", out var recsArray))
            {
                foreach (var rec in recsArray.EnumerateArray())
                {
                    recommendations.Add(new ScoreImprovementRecommendation
                    {
                        Category = rec.TryGetProperty("category", out var cat) ? cat.GetString() ?? "General" : "General",
                        RecommendationText = rec.TryGetProperty("recommendation", out var text) ? text.GetString() : null,
                        ExpectedImpact = rec.TryGetProperty("expected_impact", out var imp) ? imp.GetString() : null,
                        Priority = (RecommendationPriority)Math.Clamp(rec.TryGetProperty("priority", out var prio) ? prio.GetInt32() : 2, 1, 3),
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // 3. Deactivate old scores for this startup
            var oldScores = await _db.StartupPotentialScores
                .Where(s => s.StartupID == run.StartupId && s.IsCurrentScore)
                .ToListAsync();
            foreach (var old in oldScores) old.IsCurrentScore = false;

            // 4. Create new score
            var newScore = new StartupPotentialScore
            {
                StartupID = run.StartupId,
                OverallScore = (float)(run.OverallScore ?? 0),
                TeamScore = teamScore,
                MarketScore = marketScore,
                ProductScore = productScore,
                TractionScore = tractionScore,
                FinancialScore = financialScore,
                CalculatedAt = DateTime.UtcNow,
                IsCurrentScore = true,
                EvaluationRunID = run.Id,
                SubMetrics = subMetrics,
                ImprovementRecommendations = recommendations
            };

            _db.StartupPotentialScores.Add(newScore);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Successfully synced AiEvaluationRun {RunId} to StartupPotentialScore {ScoreId} for Startup {StartupId}",
                run.Id, newScore.ScoreID, run.StartupId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync AiEvaluationRun {RunId} to StartupPotentialScore", run.Id);
        }
    }
}

