using AISEP.Application.DTOs.AI;
using AISEP.Application.DTOs.Common;
using AISEP.Application.Interfaces;
using AISEP.Infrastructure.Data;
using AISEP.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AISEP.WebAPI.Controllers;

/// <summary>
/// AI Investor Agent — conversational research assistant for investors.
/// Supports non-stream chat, SSE streaming chat, and one-shot research.
/// </summary>
[ApiController]
[Route("api/ai/investor-agent")]
[Tags("AI – Investor Agent")]
[Authorize(Policy = "InvestorOnly")]
public class AiInvestorAgentController : ControllerBase
{
    private readonly IAiInvestorAgentService _agentService;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AiInvestorAgentController> _logger;

    public AiInvestorAgentController(
        IAiInvestorAgentService agentService,
        ApplicationDbContext db,
        ILogger<AiInvestorAgentController> logger)
    {
        _agentService = agentService;
        _db = db;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════════

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    private async Task<(int InvestorId, IActionResult? Error)> ResolveInvestorAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
        {
            return (0, Unauthorized(ApiEnvelope<object>.Error("Unable to identify user.", 401)));
        }

        var investor = await _db.Investors
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.UserID == userId);

        if (investor == null)
        {
            return (0, NotFound(ApiEnvelope<object>.Error(
                "No investor profile found. Please create your investor profile first.", 404)));
        }

        return (investor.InvestorID, null);
    }

    // ═══════════════════════════════════════════════════════════
    //  POST /api/ai/investor-agent/chat
    //  Non-streaming chat — returns full JSON response
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Send a query to the AI investor agent (non-streaming).
    /// Returns the complete response including references, caveats, and grounding summary.
    /// </summary>
    [HttpPost("chat")]
    [ProducesResponseType(typeof(ApiEnvelope<InvestorAgentChatResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Chat([FromBody] InvestorAgentChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(ApiEnvelope<object>.Error("Query is required.", 400));
        }

        if (request.Query.Length > 2000)
        {
            return BadRequest(ApiEnvelope<object>.Error("Query must not exceed 2000 characters.", 400));
        }

        if (request.ThreadId != null && (request.ThreadId.Length == 0 || request.ThreadId.Length > 128))
        {
            return BadRequest(ApiEnvelope<object>.Error("Thread ID must be between 1 and 128 characters.", 400));
        }

        var (investorId, error) = await ResolveInvestorAsync();
        if (error != null) return error;

        try
        {
            var result = await _agentService.ChatAsync(investorId, request.Query, request.ThreadId);
            return result.ToEnvelope();
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "AI investor agent chat timed out for investor {InvestorId}", investorId);
            return StatusCode(StatusCodes.Status504GatewayTimeout,
                ApiEnvelope<object>.Error(
                    "The AI service is taking too long to respond. Please try again in a moment.", 504));
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  POST /api/ai/investor-agent/chat/stream
    //  SSE streaming chat — returns text/event-stream
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Send a query to the AI investor agent with Server-Sent Events streaming.
    /// Returns a text/event-stream with progress, answer_chunk, final_answer,
    /// final_metadata, error, and [DONE] events.
    /// </summary>
    [HttpPost("chat/stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status404NotFound)]
    public async Task ChatStream([FromBody] InvestorAgentChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(
                ApiEnvelope<object>.Error("Query is required.", 400));
            return;
        }

        if (request.Query.Length > 2000)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(
                ApiEnvelope<object>.Error("Query must not exceed 2000 characters.", 400));
            return;
        }

        if (request.ThreadId != null && (request.ThreadId.Length == 0 || request.ThreadId.Length > 128))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsJsonAsync(
                ApiEnvelope<object>.Error("Thread ID must be between 1 and 128 characters.", 400));
            return;
        }

        var (investorId, error) = await ResolveInvestorAsync();
        if (error != null)
        {
            // For stream endpoint we must write error ourselves since we return void
            if (error is ObjectResult objResult)
            {
                Response.StatusCode = objResult.StatusCode ?? StatusCodes.Status400BadRequest;
                await Response.WriteAsJsonAsync(objResult.Value);
            }
            else
            {
                Response.StatusCode = StatusCodes.Status401Unauthorized;
                await Response.WriteAsJsonAsync(
                    ApiEnvelope<object>.Error("Authentication or investor profile error.", 401));
            }
            return;
        }

        // Delegate to service which writes SSE directly to Response
        await _agentService.StreamChatAsync(
            investorId, request.Query, request.ThreadId, Response, HttpContext.RequestAborted);
    }

    // ═══════════════════════════════════════════════════════════
    //  POST /api/ai/investor-agent/research
    //  One-shot research — no thread memory
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// One-shot research query — runs the full research pipeline without conversation memory.
    /// This is a long-running call (may take 30–60 seconds).
    /// </summary>
    [HttpPost("research")]
    [ProducesResponseType(typeof(ApiEnvelope<InvestorAgentChatResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Research([FromBody] InvestorAgentResearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(ApiEnvelope<object>.Error("Query is required.", 400));
        }

        if (request.Query.Length > 2000)
        {
            return BadRequest(ApiEnvelope<object>.Error("Query must not exceed 2000 characters.", 400));
        }

        var (investorId, error) = await ResolveInvestorAsync();
        if (error != null) return error;

        try
        {
            var result = await _agentService.ResearchAsync(investorId, request.Query);
            return result.ToEnvelope();
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "AI investor agent research timed out for investor {InvestorId}", investorId);
            return StatusCode(StatusCodes.Status504GatewayTimeout,
                ApiEnvelope<object>.Error(
                    "The AI service is taking too long to respond. Please try again in a moment.", 504));
        }
    }
}
