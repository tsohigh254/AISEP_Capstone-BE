namespace AISEP.Domain.Entities;

/// <summary>
/// Local tracking record for an AI evaluation run submitted to the Python AI Service.
/// </summary>
public class AiEvaluationRun
{
    public int Id { get; set; }

    /// <summary>FK to Startup.StartupID.</summary>
    public int StartupId { get; set; }

    /// <summary>The evaluation_run_id returned by the Python service.</summary>
    public int PythonRunId { get; set; }

    /// <summary>
    /// Latest known status: queued, processing, partial_completed, completed, failed.
    /// </summary>
    public string Status { get; set; } = "queued";

    /// <summary>Reason if the run failed.</summary>
    public string? FailureReason { get; set; }

    /// <summary>Overall score (0-10) once evaluation completes.</summary>
    public double? OverallScore { get; set; }

    /// <summary>JSON blob of the canonical report (cached from Python).</summary>
    public string? ReportJson { get; set; }

    /// <summary>Whether the cached report passed validity checks.</summary>
    public bool IsReportValid { get; set; }

    /// <summary>X-Correlation-Id used when submitting the run.</summary>
    public string? CorrelationId { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Startup? Startup { get; set; }
}
