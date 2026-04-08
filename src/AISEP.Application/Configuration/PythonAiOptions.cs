namespace AISEP.Application.Configuration;

/// <summary>
/// Configuration for communication with the Python AI Service.
/// Bound from "PythonAi" section in appsettings / env vars.
/// </summary>
public class PythonAiOptions
{
    public const string SectionName = "PythonAi";

    /// <summary>Base URL of the Python AI Service (e.g. http://127.0.0.1:8000).</summary>
    public string BaseUrl { get; set; } = "http://127.0.0.1:8000";

    /// <summary>Optional internal token sent as X-Internal-Token for protected endpoints.</summary>
    public string? InternalToken { get; set; }

    /// <summary>HTTP request timeout in seconds for normal calls.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>HTTP request timeout in seconds for long-running calls (e.g. research).</summary>
    public int LongTimeoutSeconds { get; set; } = 120;

    // ── Webhook / Callback ──────────────────────────────────────

    /// <summary>
    /// The callback URL that Python will POST webhook events to.
    /// Set this to the publicly-reachable URL of the .NET callback endpoint.
    /// For local dev: http://host.docker.internal:5294/api/ai/evaluation/callback
    ///                or http://127.0.0.1:5294/api/ai/evaluation/callback
    /// </summary>
    public string? WebhookCallbackUrl { get; set; }

    /// <summary>Shared secret used to verify HMAC-SHA256 signatures on webhook payloads.</summary>
    public string? WebhookSigningSecret { get; set; }

    // ── Retry / Resilience ──────────────────────────────────────

    /// <summary>Max retries for idempotent GET requests (status/report polling).</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Initial retry delay in milliseconds (exponential backoff base).</summary>
    public int RetryBaseDelayMs { get; set; } = 500;
}
