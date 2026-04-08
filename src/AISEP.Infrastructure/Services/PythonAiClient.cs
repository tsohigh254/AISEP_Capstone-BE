using AISEP.Application.Configuration;
using AISEP.Application.DTOs.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AISEP.Infrastructure.Services;

/// <summary>
/// Exception thrown when the Python AI Service returns a structured error envelope.
/// </summary>
public class PythonAiException : Exception
{
    public string Code { get; }
    public bool Retryable { get; }
    public HttpStatusCode HttpStatus { get; }
    public string? CorrelationId { get; }

    public PythonAiException(string code, string message, HttpStatusCode httpStatus, bool retryable, string? correlationId)
        : base(message)
    {
        Code = code;
        HttpStatus = httpStatus;
        Retryable = retryable;
        CorrelationId = correlationId;
    }
}

/// <summary>
/// Typed HttpClient for communicating with the Python AI Service.
/// Registered via IHttpClientFactory with named configuration.
/// </summary>
public class PythonAiClient
{
    private readonly HttpClient _http;
    private readonly PythonAiOptions _options;
    private readonly ILogger<PythonAiClient> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public PythonAiClient(HttpClient http, IOptions<PythonAiOptions> options, ILogger<PythonAiClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════
    //  Evaluation endpoints
    // ═══════════════════════════════════════════════════════════

    public async Task<PythonSubmitEvaluationResponse> SubmitEvaluationAsync(
        PythonSubmitEvaluationRequest request, string? correlationId = null, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/evaluations/");
        req.Content = JsonContent.Create(request, options: JsonOpts);
        AttachHeaders(req, correlationId);

        using var resp = await _http.SendAsync(req, ct);
        await EnsureSuccessOrThrow(resp, ct);

        var result = await resp.Content.ReadFromJsonAsync<PythonSubmitEvaluationResponse>(JsonOpts, ct);
        return result ?? throw new InvalidOperationException("Python returned null submit response.");
    }

    public async Task<PythonEvaluationStatus> GetEvaluationStatusAsync(
        int pythonRunId, string? correlationId = null, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/evaluations/{pythonRunId}");
        AttachHeaders(req, correlationId);

        using var resp = await _http.SendAsync(req, ct);
        await EnsureSuccessOrThrow(resp, ct);

        var result = await resp.Content.ReadFromJsonAsync<PythonEvaluationStatus>(JsonOpts, ct);
        return result ?? throw new InvalidOperationException("Python returned null evaluation status.");
    }

    /// <summary>
    /// Fetches the canonical evaluation report.
    /// Returns null if 202 (not ready yet) — caller should retry later.
    /// Throws PythonAiException on 404/409/500.
    /// </summary>
    public async Task<(PythonCanonicalReport? Report, HttpStatusCode StatusCode)> GetEvaluationReportAsync(
        int pythonRunId, string? correlationId = null, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/evaluations/{pythonRunId}/report");
        AttachHeaders(req, correlationId);

        using var resp = await _http.SendAsync(req, ct);

        // 202 = report not ready yet
        if (resp.StatusCode == HttpStatusCode.Accepted)
            return (null, HttpStatusCode.Accepted);

        await EnsureSuccessOrThrow(resp, ct);

        var report = await resp.Content.ReadFromJsonAsync<PythonCanonicalReport>(JsonOpts, ct);
        return (report, resp.StatusCode);
    }

    // ═══════════════════════════════════════════════════════════
    //  Health check
    // ═══════════════════════════════════════════════════════════

    public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
    {
        try
        {
            using var resp = await _http.GetAsync("/health", ct);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Python AI health check failed");
            return false;
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════════

    private void AttachHeaders(HttpRequestMessage req, string? correlationId)
    {
        if (!string.IsNullOrWhiteSpace(correlationId))
            req.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);

        if (!string.IsNullOrWhiteSpace(_options.InternalToken))
            req.Headers.TryAddWithoutValidation("X-Internal-Token", _options.InternalToken);
    }

    private async Task EnsureSuccessOrThrow(HttpResponseMessage resp, CancellationToken ct)
    {
        if (resp.IsSuccessStatusCode)
        {
            // Capture correlation id from response for logging
            if (resp.Headers.TryGetValues("X-Correlation-Id", out var vals))
                _logger.LogDebug("Python X-Correlation-Id: {CorrelationId}", vals.FirstOrDefault());
            return;
        }

        string? correlationId = null;
        if (resp.Headers.TryGetValues("X-Correlation-Id", out var cvals))
            correlationId = cvals.FirstOrDefault();

        // Try to parse Python unified error envelope
        PythonErrorEnvelope? envelope = null;
        try
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!string.IsNullOrWhiteSpace(body))
                envelope = JsonSerializer.Deserialize<PythonErrorEnvelope>(body, JsonOpts);
        }
        catch
        {
            // Not a JSON body — fall through to generic error
        }

        if (envelope is not null && !string.IsNullOrEmpty(envelope.Code))
        {
            _logger.LogWarning(
                "Python AI error {StatusCode} [{Code}]: {Message} (retryable={Retryable}, correlation={CorrelationId})",
                (int)resp.StatusCode, envelope.Code, envelope.Message, envelope.Retryable, envelope.CorrelationId);

            throw new PythonAiException(
                envelope.Code,
                envelope.Message,
                resp.StatusCode,
                envelope.Retryable,
                envelope.CorrelationId ?? correlationId);
        }

        // Generic fallback
        var rawBody = await resp.Content.ReadAsStringAsync(ct);
        _logger.LogWarning("Python AI non-success {StatusCode}: {Body}", (int)resp.StatusCode, rawBody);
        throw new PythonAiException(
            "PYTHON_AI_ERROR",
            $"Python AI returned {(int)resp.StatusCode}: {rawBody}",
            resp.StatusCode,
            false,
            correlationId);
    }
}
