using AISEP.Application.DTOs.AI;
using AISEP.Application.DTOs.Common;
using Microsoft.AspNetCore.Http;

namespace AISEP.Application.Interfaces;

/// <summary>
/// Service interface for AI Investor Agent integration (Step C).
/// Covers non-stream chat, SSE streaming chat, and one-shot research.
/// </summary>
public interface IAiInvestorAgentService
{
    /// <summary>
    /// Send a query to the investor agent and receive a full response (non-streaming).
    /// </summary>
    Task<ApiResponse<InvestorAgentChatResult>> ChatAsync(int investorId, string query, string? threadId);

    /// <summary>
    /// Open an SSE stream from the investor agent and proxy all events
    /// directly to the client's <see cref="HttpResponse"/>.
    /// </summary>
    Task StreamChatAsync(int investorId, string query, string? threadId, HttpResponse response, CancellationToken ct);

    /// <summary>
    /// One-shot research pipeline (no thread memory).
    /// </summary>
    Task<ApiResponse<InvestorAgentChatResult>> ResearchAsync(int investorId, string query);
}
