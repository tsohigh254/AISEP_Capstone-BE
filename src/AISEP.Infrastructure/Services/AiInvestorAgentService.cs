using AISEP.Application.DTOs.AI;
using AISEP.Application.DTOs.Common;
using AISEP.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;

namespace AISEP.Infrastructure.Services;

/// <summary>
/// Implements investor-agent chat by directly calling Google Gemini API
/// via <see cref="IGeminiService"/>.
/// </summary>
public class AiInvestorAgentService : IAiInvestorAgentService
{
    private readonly IGeminiService _geminiService;
    private readonly ILogger<AiInvestorAgentService> _logger;

    public AiInvestorAgentService(
        IGeminiService geminiService,
        ILogger<AiInvestorAgentService> logger)
    {
        _geminiService = geminiService;
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
            "InvestorAgent chat request (Gemini) for investor {InvestorId}, thread={ThreadId}, correlation={CorrelationId}",
            investorId, threadId ?? "(new)", correlationId);

        try
        {
            var answer = await _geminiService.GenerateContentAsync(query);

            var result = new InvestorAgentChatResult
            {
                FinalAnswer = answer,
                Intent = "chat",
                ResolvedQuery = query
            };

            return ApiResponse<InvestorAgentChatResult>.Ok(result, "Gemini response received.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error during Gemini chat for investor {InvestorId} (Correlation={CorrelationId})",
                investorId, correlationId);
            return ApiResponse<InvestorAgentChatResult>.ErrorResponse(
                "AI_SERVICE_ERROR", "The AI service encountered an error.");
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
            "InvestorAgent stream request (Gemini) for investor {InvestorId}, thread={ThreadId}, correlation={CorrelationId}",
            investorId, threadId ?? "(new)", correlationId);

        try
        {
            // Set SSE response headers BEFORE writing any body content
            httpResponse.ContentType = "text/event-stream";
            httpResponse.Headers["Cache-Control"] = "no-cache";
            httpResponse.Headers["Connection"] = "keep-alive";
            httpResponse.Headers["X-Accel-Buffering"] = "no"; 

            await _geminiService.StreamGenerateContentAsync(query, httpResponse, ct);

            _logger.LogInformation(
                "InvestorAgent stream completed (Gemini) for investor {InvestorId} (Correlation={CorrelationId})",
                investorId, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error during Gemini stream for investor {InvestorId} (Correlation={CorrelationId})",
                investorId, correlationId);

            if (!httpResponse.HasStarted)
            {
                httpResponse.ContentType = "text/event-stream";
                httpResponse.StatusCode = StatusCodes.Status500InternalServerError;
            }

            await WriteSseErrorAsync(httpResponse, "An unexpected error occurred in AI service.", ct);
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
            "InvestorAgent research request (Gemini) for investor {InvestorId}, correlation={CorrelationId}",
            investorId, correlationId);

        try
        {
            var answer = await _geminiService.GenerateContentAsync($"Please conduct detailed research on: {query}");

            var result = new InvestorAgentChatResult
            {
                FinalAnswer = answer,
                Intent = "research"
            };

            return ApiResponse<InvestorAgentChatResult>.Ok(result, "Research completed via Gemini.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error during Gemini research for investor {InvestorId} (Correlation={CorrelationId})",
                investorId, correlationId);
            return ApiResponse<InvestorAgentChatResult>.ErrorResponse(
                "AI_SERVICE_ERROR", "The AI service encountered an error during research.");
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Private helpers
    // ═══════════════════════════════════════════════════════════

    private static async Task WriteSseErrorAsync(HttpResponse response, string message, CancellationToken ct)
    {
        try
        {
            var errorEvent = $"data: {{\"type\":\"error\",\"content\":\"{EscapeJsonString(message)}\"}}\n\n";
            await response.WriteAsync(errorEvent, ct);
            await response.WriteAsync("data: [DONE]\n\n", ct);
            await response.Body.FlushAsync(ct);
        }
        catch { /* ignore */ }
    }

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
