using System.Text.Json;
using System.Text.Json.Serialization;

namespace AISEP.Application.DTOs.AI;

// ═══════════════════════════════════════════════════════════════
//  Python AI Service – Shared Error Envelope
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Unified error envelope returned by the Python AI Service.
/// </summary>
public class PythonErrorEnvelope
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>Can be a string or an object depending on the endpoint.</summary>
    [JsonPropertyName("detail")]
    public JsonElement? Detail { get; set; }

    [JsonPropertyName("retryable")]
    public bool Retryable { get; set; }

    [JsonPropertyName("correlation_id")]
    public string? CorrelationId { get; set; }
}

// ═══════════════════════════════════════════════════════════════
//  Submit Evaluation
// ═══════════════════════════════════════════════════════════════

/// <summary>DTO sent FROM .NET TO the frontend for evaluation submission request.</summary>
public class SubmitEvaluationRequest
{
    /// <summary>Startup ID in the .NET system.</summary>
    public int StartupId { get; set; }

    /// <summary>Optional list of specific document IDs to include. If empty, all startup docs are used.</summary>
    public List<int>? DocumentIds { get; set; }
}

/// <summary>Payload sent from .NET to Python POST /api/v1/evaluations/.</summary>
public class PythonSubmitEvaluationRequest
{
    [JsonPropertyName("startup_id")]
    public string StartupId { get; set; } = string.Empty;

    [JsonPropertyName("documents")]
    public List<PythonDocumentInput> Documents { get; set; } = new();
}

public class PythonDocumentInput
{
    [JsonPropertyName("document_id")]
    public string DocumentId { get; set; } = string.Empty;

    [JsonPropertyName("document_type")]
    public string DocumentType { get; set; } = string.Empty;

    [JsonPropertyName("file_url_or_path")]
    public string FileUrlOrPath { get; set; } = string.Empty;
}

/// <summary>Response from Python POST /api/v1/evaluations/.</summary>
public class PythonSubmitEvaluationResponse
{
    [JsonPropertyName("evaluation_run_id")]
    public int EvaluationRunId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

// ═══════════════════════════════════════════════════════════════
//  Evaluation Status (polling)
// ═══════════════════════════════════════════════════════════════

/// <summary>Response from Python GET /api/v1/evaluations/{id}.</summary>
public class PythonEvaluationStatus
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("startup_id")]
    public string StartupId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("submitted_at")]
    public DateTime? SubmittedAt { get; set; }

    [JsonPropertyName("failure_reason")]
    public string? FailureReason { get; set; }

    [JsonPropertyName("overall_score")]
    public double? OverallScore { get; set; }

    [JsonPropertyName("overall_confidence")]
    public string? OverallConfidence { get; set; }

    [JsonPropertyName("documents")]
    public List<PythonEvaluationDocStatus>? Documents { get; set; }
}

public class PythonEvaluationDocStatus
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("document_type")]
    public string? DocumentType { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("extraction_status")]
    public string? ExtractionStatus { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }
}

// ═══════════════════════════════════════════════════════════════
//  Evaluation Report (canonical)
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Canonical evaluation report returned by Python GET /api/v1/evaluations/{id}/report.
/// Stored as JSON in AiEvaluationRun.ReportJson.
/// We keep it weakly typed at top level to avoid coupling to every nested schema change.
/// </summary>
public class PythonCanonicalReport
{
    [JsonPropertyName("startup_id")]
    public string? StartupId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("overall_result")]
    public JsonElement? OverallResult { get; set; }

    [JsonPropertyName("criteria_results")]
    public JsonElement? CriteriaResults { get; set; }

    [JsonPropertyName("classification")]
    public JsonElement? Classification { get; set; }

    [JsonPropertyName("narrative")]
    public JsonElement? Narrative { get; set; }

    [JsonPropertyName("effective_weights")]
    public JsonElement? EffectiveWeights { get; set; }

    [JsonPropertyName("processing_warnings")]
    public List<string>? ProcessingWarnings { get; set; }
}

// ═══════════════════════════════════════════════════════════════
//  Webhook Callback Payload
// ═══════════════════════════════════════════════════════════════

/// <summary>Payload POSTed by Python to the .NET webhook callback endpoint.</summary>
public class EvaluationWebhookPayload
{
    [JsonPropertyName("delivery_id")]
    public string DeliveryId { get; set; } = string.Empty;

    [JsonPropertyName("evaluation_run_id")]
    public int EvaluationRunId { get; set; }

    [JsonPropertyName("startup_id")]
    public string StartupId { get; set; } = string.Empty;

    [JsonPropertyName("terminal_status")]
    public string TerminalStatus { get; set; } = string.Empty;

    [JsonPropertyName("overall_score")]
    public double? OverallScore { get; set; }

    [JsonPropertyName("failure_reason")]
    public string? FailureReason { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("correlation_id")]
    public string? CorrelationId { get; set; }
}

// ═══════════════════════════════════════════════════════════════
//  .NET → Frontend Response DTOs
// ═══════════════════════════════════════════════════════════════

/// <summary>Returned to frontend after submitting an evaluation.</summary>
public class EvaluationSubmitResult
{
    public int RunId { get; set; }
    public int StartupId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>Returned to frontend when polling evaluation status.</summary>
public class EvaluationStatusResult
{
    public int RunId { get; set; }
    public int StartupId { get; set; }
    public string Status { get; set; } = string.Empty;
    public double? OverallScore { get; set; }
    public string? FailureReason { get; set; }
    public bool IsReportReady { get; set; }
    public bool IsReportValid { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Returned to frontend with the full evaluation report.</summary>
public class EvaluationReportResult
{
    public int RunId { get; set; }
    public int StartupId { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsReportValid { get; set; }
    /// <summary>The canonical report object (weakly-typed JSON).</summary>
    public object? Report { get; set; }
    public string? ValidationMessage { get; set; }
}
