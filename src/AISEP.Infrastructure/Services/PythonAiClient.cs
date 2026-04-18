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

    /// <summary>Exposes the resolved options so callers can read timeout values (e.g. StreamTimeoutSeconds).</summary>
    public PythonAiOptions Options => _options;

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

        // Python wraps the canonical report inside a { "report": {...}, "report_mode": "..." } object.
        // Deserialize the wrapper first, then unwrap the inner report.
        var wrapper = await resp.Content.ReadFromJsonAsync<PythonReportWrapperResponse>(JsonOpts, ct);
        var report = wrapper?.Report;
        return (report, resp.StatusCode);
    }

    /// <summary>
    /// Fetches the per-source report for a specific document type (combined-mode runs only).
    /// <paramref name="documentType"/> must be snake_case — e.g. <c>pitch_deck</c> or <c>business_plan</c>.
    /// Returns null + 202 if not ready. Throws PythonAiException on 404 (doc not found) / 400 (invalid type) / 500.
    /// </summary>
    public async Task<(PythonCanonicalReport? Report, HttpStatusCode StatusCode)> GetSourceReportAsync(
        int pythonRunId, string documentType, string? correlationId = null, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/evaluations/{pythonRunId}/report/source/{documentType}");
        AttachHeaders(req, correlationId);

        using var resp = await _http.SendAsync(req, ct);

        if (resp.StatusCode == HttpStatusCode.Accepted)
            return (null, HttpStatusCode.Accepted);

        await EnsureSuccessOrThrow(resp, ct);

        // Same ReportEnvelope wrapper as /report — report_mode will be "source".
        var wrapper = await resp.Content.ReadFromJsonAsync<PythonReportWrapperResponse>(JsonOpts, ct);
        return (wrapper?.Report, resp.StatusCode);
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
    //  Recommendation — Internal Reindex
    // ═══════════════════════════════════════════════════════════

    public async Task<PythonReindexResponse> ReindexStartupAsync(
        int startupId, PythonReindexStartupRequest payload, string? correlationId = null, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, $"/internal/recommendations/reindex/startup/{startupId}");
        req.Content = JsonContent.Create(payload, options: JsonOpts);
        AttachHeaders(req, correlationId);

        using var resp = await _http.SendAsync(req, ct);
        await EnsureSuccessOrThrow(resp, ct);

        var result = await resp.Content.ReadFromJsonAsync<PythonReindexResponse>(JsonOpts, ct);
        return result ?? new PythonReindexResponse { Status = "ok" };
    }

    public async Task<PythonReindexResponse> ReindexInvestorAsync(
        int investorId, PythonReindexInvestorRequest payload, string? correlationId = null, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, $"/internal/recommendations/reindex/investor/{investorId}");
        req.Content = JsonContent.Create(payload, options: JsonOpts);
        AttachHeaders(req, correlationId);

        using var resp = await _http.SendAsync(req, ct);
        await EnsureSuccessOrThrow(resp, ct);

        var result = await resp.Content.ReadFromJsonAsync<PythonReindexResponse>(JsonOpts, ct);
        return result ?? new PythonReindexResponse { Status = "ok" };
    }

    // ═══════════════════════════════════════════════════════════
    //  Recommendation — Public Read
    // ═══════════════════════════════════════════════════════════

    public async Task<PythonRecommendationListResponse> GetStartupRecommendationsAsync(
        int investorId, int topN = 5, string? correlationId = null, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get,
            $"/api/v1/recommendations/startups?investor_id={investorId}&top_n={topN}");
        AttachHeaders(req, correlationId);

        using var resp = await _http.SendAsync(req, ct);
        await EnsureSuccessOrThrow(resp, ct);

        var result = await resp.Content.ReadFromJsonAsync<PythonRecommendationListResponse>(JsonOpts, ct);
        return result ?? throw new InvalidOperationException("Python returned null recommendation list.");
    }

    public async Task<PythonRecommendationExplanation> GetMatchExplanationAsync(
        int investorId, int startupId, string? correlationId = null, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get,
            $"/api/v1/recommendations/startups/{startupId}/explanation?investor_id={investorId}");
        AttachHeaders(req, correlationId);

        using var resp = await _http.SendAsync(req, ct);
        await EnsureSuccessOrThrow(resp, ct);

        var result = await resp.Content.ReadFromJsonAsync<PythonRecommendationExplanation>(JsonOpts, ct);
        return result ?? throw new InvalidOperationException("Python returned null explanation.");
    }

    // ═══════════════════════════════════════════════════════════
    //  Investor Agent — Chat non-stream (consumes SSE internally)
    //  Python only exposes /chat/stream — this method opens the
    //  stream, reads all SSE events, and assembles a single response.
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Calls Python /api/v1/investor-agent/chat/stream, consumes all SSE events,
    /// and returns an assembled <see cref="PythonAgentChatResponse"/>.
    /// Uses LongTimeoutSeconds as the overall deadline.
    /// </summary>
    public async Task<PythonAgentChatResponse> ConsumeStreamToResponseAsync(
        PythonAgentChatRequest request, string? correlationId = null, CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(_options.LongTimeoutSeconds));

        using var httpResp = await InvestorAgentChatStreamRawAsync(request, correlationId, cts.Token);

        var result = new PythonAgentChatResponse();
        var answerBuilder = new System.Text.StringBuilder();

        using var stream = await httpResp.Content.ReadAsStreamAsync(cts.Token);
        using var reader = new System.IO.StreamReader(stream, System.Text.Encoding.UTF8);

        while (!reader.EndOfStream && !cts.Token.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cts.Token);
            if (line == null) break;
            if (!line.StartsWith("data: ", StringComparison.Ordinal)) continue;

            var payload = line["data: ".Length..];
            if (payload == "[DONE]") break;

            SseEvent? evt = null;
            try { evt = JsonSerializer.Deserialize<SseEvent>(payload, JsonOpts); }
            catch { /* ignore malformed event */ }
            if (evt == null) continue;

            switch (evt.Type)
            {
                case "answer_chunk":
                    answerBuilder.Append(evt.Content);
                    break;

                case "final_answer":
                    // Prefer full final_answer over concatenated chunks if present
                    if (!string.IsNullOrEmpty(evt.Content))
                        result.FinalAnswer = evt.Content;
                    break;

                case "final_metadata":
                    result.References = evt.References;
                    result.Caveats = evt.Caveats;
                    result.WriterNotes = evt.WriterNotes;
                    result.ProcessingWarnings = evt.ProcessingWarnings;
                    result.GroundingSummary = evt.GroundingSummary;
                    break;

                case "error":
                    throw new PythonAiException(
                        "AGENT_STREAM_ERROR",
                        evt.Content ?? "Unknown agent error",
                        System.Net.HttpStatusCode.InternalServerError,
                        false,
                        correlationId);
            }
        }

        // Fall back to concatenated chunks if final_answer event had no content
        if (string.IsNullOrEmpty(result.FinalAnswer))
            result.FinalAnswer = answerBuilder.ToString();

        return result;
    }

    // ═══════════════════════════════════════════════════════════
    //  Investor Agent — Chat SSE stream (raw)
    //  Returns the HttpResponseMessage — caller owns disposal and
    //  must read the response body as an SSE stream.
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Opens the SSE stream from Python. The returned <see cref="HttpResponseMessage"/>
    /// is NOT disposed by this method — the caller must dispose it after reading the
    /// stream to completion.  Uses <see cref="HttpCompletionOption.ResponseHeadersRead"/>
    /// so no buffering occurs.
    /// </summary>
    public async Task<HttpResponseMessage> InvestorAgentChatStreamRawAsync(
        PythonAgentChatRequest request, string? correlationId = null, CancellationToken ct = default)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/investor-agent/chat/stream");
        req.Content = JsonContent.Create(request, options: JsonOpts);
        req.Headers.Accept.ParseAdd("text/event-stream");
        AttachHeaders(req, correlationId);

        // ResponseHeadersRead = do NOT buffer — stream line-by-line.
        // Timeout is controlled by the CancellationToken passed in from StreamChatAsync
        // (which uses StreamTimeoutSeconds), NOT by HttpClient.Timeout.
        var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

        // If Python returned an error status BEFORE the stream started, parse and throw.
        if (!resp.IsSuccessStatusCode)
        {
            // We must dispose resp on error since the caller won't get it.
            using (resp)
            {
                await EnsureSuccessOrThrow(resp, ct);
            }
        }

        return resp; // caller owns disposal
    }

    // Research is not a separate Python endpoint — ResearchAsync in the service
    // delegates to ConsumeStreamToResponseAsync with a fresh thread_id.

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
