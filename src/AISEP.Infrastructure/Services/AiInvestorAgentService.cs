using AISEP.Application.DTOs.AI;
using AISEP.Application.DTOs.Common;
using AISEP.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;

namespace AISEP.Infrastructure.Services;

/// <summary>
/// Implements investor-agent chat, SSE streaming proxy, and one-shot research
/// by delegating to the Python AI Service via <see cref="PythonAiClient"/>.
/// </summary>
public class AiInvestorAgentService : IAiInvestorAgentService
{
    private readonly PythonAiClient _pythonClient;
    private readonly ILogger<AiInvestorAgentService> _logger;

    public AiInvestorAgentService(
        PythonAiClient pythonClient,
        ILogger<AiInvestorAgentService> logger)
    {
        _pythonClient = pythonClient;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════
    //  Chat — non-stream
    // ═══════════════════════════════════════════════════════════

    public async Task<ApiResponse<InvestorAgentChatResult>> ChatAsync(
        int investorId, string query, string? threadId)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation(
            "InvestorAgent chat request for investor {InvestorId}, thread={ThreadId}, correlation={CorrelationId}, LongTimeoutSeconds={LongTimeout}",
            investorId, threadId ?? "(new)", correlationId, _pythonClient.Options.LongTimeoutSeconds);

        try
        {
            var request = new PythonAgentChatRequest
            {
                Query = query,
                ThreadId = threadId
            };

            var pythonResp = await _pythonClient.InvestorAgentChatAsync(request, correlationId);

            var result = MapChatResponseToResult(pythonResp);

            return ApiResponse<InvestorAgentChatResult>.Ok(result, "Agent response received.");
        }
        catch (PythonAiException ex) when (ex.HttpStatus == HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning(ex, "Rate-limited by Python for investor {InvestorId} (Correlation={CorrelationId})",
                investorId, correlationId);
            return ApiResponse<InvestorAgentChatResult>.ErrorResponse(
                "RATE_LIMITED", "The AI agent is busy. Please wait a moment and try again.");
        }
        catch (PythonAiException ex) when (ex.HttpStatus == HttpStatusCode.UnprocessableEntity)
        {
            _logger.LogWarning(ex, "Validation error from Python for investor {InvestorId}: {Message} (Correlation={CorrelationId})",
                investorId, ex.Message, correlationId);
            return ApiResponse<InvestorAgentChatResult>.ErrorResponse(
                "VALIDATION_ERROR", ex.Message);
        }
        catch (PythonAiException ex)
        {
            _logger.LogError(ex,
                "Python AI error during chat for investor {InvestorId}: [{Code}] {Message} (Correlation={CorrelationId})",
                investorId, ex.Code, ex.Message, correlationId);
            return ApiResponse<InvestorAgentChatResult>.ErrorResponse(
                ex.Code, $"AI agent error: {ex.Message}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Python AI unreachable during chat for investor {InvestorId} (Correlation={CorrelationId})",
                investorId, correlationId);
            return ApiResponse<InvestorAgentChatResult>.ErrorResponse(
                "AI_SERVICE_UNAVAILABLE", "The AI service is temporarily unavailable.");
        }
        catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex,
                "Python AI timeout during chat for investor {InvestorId} (Correlation={CorrelationId})",
                investorId, correlationId);
            return ApiResponse<InvestorAgentChatResult>.ErrorResponse(
                "AI_TIMEOUT", "The AI agent took too long to respond. Please try again.");
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Chat — SSE stream proxy
    // ═══════════════════════════════════════════════════════════

    public async Task StreamChatAsync(
        int investorId, string query, string? threadId, HttpResponse httpResponse, CancellationToken ct)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation(
            "InvestorAgent stream request for investor {InvestorId}, thread={ThreadId}, correlation={CorrelationId}",
            investorId, threadId ?? "(new)", correlationId);

        HttpResponseMessage? pythonResp = null;

        // Build a stream-specific CancellationToken that respects StreamTimeoutSeconds.
        // This is the ONLY timeout guard for SSE — HttpClient.Timeout is set to Infinite.
        // We link it with the incoming ct (client disconnect) so either side can cancel.
        var streamTimeoutSeconds = _pythonClient.Options.StreamTimeoutSeconds;
        using var streamCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        streamCts.CancelAfter(TimeSpan.FromSeconds(streamTimeoutSeconds));
        var streamToken = streamCts.Token;

        try
        {
            var request = new PythonAgentChatRequest
            {
                Query = query,
                ThreadId = threadId
            };

            pythonResp = await _pythonClient.InvestorAgentChatStreamRawAsync(request, correlationId, streamToken);

            // Set SSE response headers BEFORE writing any body content
            httpResponse.ContentType = "text/event-stream";
            httpResponse.Headers["Cache-Control"] = "no-cache";
            httpResponse.Headers["Connection"] = "keep-alive";
            httpResponse.Headers["X-Accel-Buffering"] = "no"; // Disable nginx buffering if present

            // Proxy the SSE stream line-by-line from Python → client.
            // ReadLineAsync reads one \n-terminated line at a time — never buffers full body.
            // Empty lines are SSE event delimiters and must be forwarded as-is.
            using var stream = await pythonResp.Content.ReadAsStreamAsync(streamToken);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            while (!reader.EndOfStream && !streamToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(streamToken);
                if (line == null) break;

                // Forward every line (including empty lines which are SSE delimiters)
                await httpResponse.WriteAsync(line + "\n", streamToken);
                await httpResponse.Body.FlushAsync(streamToken);
            }

            _logger.LogInformation(
                "InvestorAgent stream completed for investor {InvestorId} (Correlation={CorrelationId})",
                investorId, correlationId);
        }
        catch (PythonAiException ex)
        {
            _logger.LogError(ex,
                "Python AI error during stream for investor {InvestorId}: [{Code}] {Message} (Correlation={CorrelationId})",
                investorId, ex.Code, ex.Message, correlationId);

            // If we haven't started writing the response body yet, we can set status code
            if (!httpResponse.HasStarted)
            {
                httpResponse.ContentType = "text/event-stream";
                httpResponse.StatusCode = (int)ex.HttpStatus;
            }

            // Write an SSE error event so FE can handle it gracefully
            await WriteSseErrorAsync(httpResponse, ex.Message, ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Python AI unreachable during stream for investor {InvestorId} (Correlation={CorrelationId})",
                investorId, correlationId);

            if (!httpResponse.HasStarted)
            {
                httpResponse.ContentType = "text/event-stream";
                httpResponse.StatusCode = StatusCodes.Status502BadGateway;
            }

            await WriteSseErrorAsync(httpResponse, "AI service is temporarily unavailable.", ct);
        }
        catch (TaskCanceledException) when (ct.IsCancellationRequested)
        {
            // Client disconnected — this is normal for SSE; just log and exit
            _logger.LogDebug(
                "Client disconnected from stream for investor {InvestorId} (Correlation={CorrelationId})",
                investorId, correlationId);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex,
                "Python AI timeout during stream for investor {InvestorId} (Correlation={CorrelationId})",
                investorId, correlationId);

            if (!httpResponse.HasStarted)
            {
                httpResponse.ContentType = "text/event-stream";
                httpResponse.StatusCode = StatusCodes.Status504GatewayTimeout;
            }

            await WriteSseErrorAsync(httpResponse, "The AI agent took too long to respond.", ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Client disconnected — normal for SSE
            _logger.LogDebug(
                "Client disconnected (OCE) from stream for investor {InvestorId} (Correlation={CorrelationId})",
                investorId, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error during stream for investor {InvestorId} (Correlation={CorrelationId})",
                investorId, correlationId);

            if (!httpResponse.HasStarted)
            {
                httpResponse.ContentType = "text/event-stream";
                httpResponse.StatusCode = StatusCodes.Status500InternalServerError;
            }

            await WriteSseErrorAsync(httpResponse, "An unexpected error occurred.", ct);
        }
        finally
        {
            pythonResp?.Dispose();
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Research — one-shot (no thread memory)
    // ═══════════════════════════════════════════════════════════

    public async Task<ApiResponse<InvestorAgentChatResult>> ResearchAsync(
        int investorId, string query)
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation(
            "InvestorAgent research request for investor {InvestorId}, correlation={CorrelationId}",
            investorId, correlationId);

        try
        {
            var request = new PythonAgentResearchRequest { Query = query };

            var pythonResp = await _pythonClient.InvestorAgentResearchAsync(request, correlationId);

            var result = MapResearchResponseToResult(pythonResp);

            return ApiResponse<InvestorAgentChatResult>.Ok(result, "Research completed.");
        }
        catch (PythonAiException ex) when (ex.HttpStatus == HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning(ex, "Rate-limited during research for investor {InvestorId} (Correlation={CorrelationId})",
                investorId, correlationId);
            return ApiResponse<InvestorAgentChatResult>.ErrorResponse(
                "RATE_LIMITED", "The AI agent is busy. Please wait a moment and try again.");
        }
        catch (PythonAiException ex)
        {
            _logger.LogError(ex,
                "Python AI error during research for investor {InvestorId}: [{Code}] {Message} (Correlation={CorrelationId})",
                investorId, ex.Code, ex.Message, correlationId);
            return ApiResponse<InvestorAgentChatResult>.ErrorResponse(
                ex.Code, $"AI agent error: {ex.Message}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Python AI unreachable during research for investor {InvestorId} (Correlation={CorrelationId})",
                investorId, correlationId);
            return ApiResponse<InvestorAgentChatResult>.ErrorResponse(
                "AI_SERVICE_UNAVAILABLE", "The AI service is temporarily unavailable.");
        }
        catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex,
                "Python AI timeout during research for investor {InvestorId} (Correlation={CorrelationId})",
                investorId, correlationId);
            return ApiResponse<InvestorAgentChatResult>.ErrorResponse(
                "AI_TIMEOUT", "The AI agent took too long to respond. Please try again.");
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Private helpers
    // ═══════════════════════════════════════════════════════════

    private static InvestorAgentChatResult MapChatResponseToResult(PythonAgentChatResponse py)
    {
        var result = MapBaseResponseToResult(py);
        result.ResolvedQuery = py.ResolvedQuery;
        result.FallbackTriggered = py.FallbackTriggered;
        return result;
    }

    private static InvestorAgentChatResult MapResearchResponseToResult(PythonAgentResearchResponse py)
    {
        return MapBaseResponseToResult(py);
    }

    private static InvestorAgentChatResult MapBaseResponseToResult(PythonAgentResearchResponse py)
    {
        return new InvestorAgentChatResult
        {
            Intent = py.Intent,
            FinalAnswer = py.FinalAnswer,
            References = py.References?.Select(r => new AgentReferenceResult
            {
                Title = r.Title,
                Url = r.Url,
                SourceDomain = r.SourceDomain
            }).ToList(),
            Caveats = py.Caveats,
            WriterNotes = py.WriterNotes,
            ProcessingWarnings = py.ProcessingWarnings,
            GroundingSummary = py.GroundingSummary is not null ? new AgentGroundingSummaryResult
            {
                VerifiedClaimCount = py.GroundingSummary.VerifiedClaimCount,
                WeaklySupportedClaimCount = py.GroundingSummary.WeaklySupportedClaimCount,
                ConflictingClaimCount = py.GroundingSummary.ConflictingClaimCount,
                UnsupportedClaimCount = py.GroundingSummary.UnsupportedClaimCount,
                ReferenceCount = py.GroundingSummary.ReferenceCount,
                CoverageStatus = py.GroundingSummary.CoverageStatus
            } : null
        };
    }

    /// <summary>
    /// Writes a synthetic SSE error event and [DONE] sentinel to the response body.
    /// Safe to call even if the response has already started streaming.
    /// </summary>
    private static async Task WriteSseErrorAsync(HttpResponse response, string message, CancellationToken ct)
    {
        try
        {
            var errorEvent = $"data: {{\"type\":\"error\",\"content\":\"{EscapeJsonString(message)}\"}}\n\n";
            await response.WriteAsync(errorEvent, ct);
            await response.WriteAsync("data: [DONE]\n\n", ct);
            await response.Body.FlushAsync(ct);
        }
        catch
        {
            // If the client is already gone, swallow write errors
        }
    }

    /// <summary>Minimal JSON-string escaping for inline SSE payloads.</summary>
    private static string EscapeJsonString(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
}
