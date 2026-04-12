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
/// AI-powered startup recommendations for investors.
/// All endpoints require Investor role.
/// </summary>
[ApiController]
[Route("api/ai/recommendations")]
[Tags("AI – Recommendations")]
[Authorize(Policy = "InvestorOnly")]
public class AiRecommendationController : ControllerBase
{
    private readonly IAiRecommendationService _recommendationService;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AiRecommendationController> _logger;

    public AiRecommendationController(
        IAiRecommendationService recommendationService,
        ApplicationDbContext db,
        ILogger<AiRecommendationController> logger)
    {
        _recommendationService = recommendationService;
        _db = db;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Get AI-recommended startups for the current investor.
    /// </summary>
    /// <param name="topN">Number of recommendations to return (1–50, default 10).</param>
    /// <returns>Ranked list of startup recommendations with match scores.</returns>
    [HttpGet("startups")]
    [ProducesResponseType(typeof(ApiEnvelope<RecommendationListResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetStartupRecommendations([FromQuery] int topN = 10)
    {
        topN = Math.Clamp(topN, 1, 50);

        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized(ApiEnvelope<object>.Error("Unable to identify user.", 401));

        var investor = await _db.Investors
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.UserID == userId);

        if (investor == null)
        {
            return NotFound(ApiEnvelope<object>.Error(
                "No investor profile found. Please create your investor profile first.", 404));
        }

        var result = await _recommendationService.GetStartupRecommendationsAsync(investor.InvestorID, topN);
        return result.ToEnvelope();
    }

    /// <summary>
    /// Get a detailed match explanation for a specific investor–startup pair.
    /// </summary>
    /// <param name="startupId">The ID of the startup to explain the match for.</param>
    /// <returns>Detailed explanation of why this startup was recommended.</returns>
    [HttpGet("startups/{startupId:int}/explanation")]
    [ProducesResponseType(typeof(ApiEnvelope<RecommendationExplanationResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetMatchExplanation([FromRoute] int startupId)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized(ApiEnvelope<object>.Error("Unable to identify user.", 401));

        var investor = await _db.Investors
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.UserID == userId);

        if (investor == null)
        {
            return NotFound(ApiEnvelope<object>.Error(
                "No investor profile found. Please create your investor profile first.", 404));
        }

        var result = await _recommendationService.GetMatchExplanationAsync(investor.InvestorID, startupId);
        return result.ToEnvelope();
    }
}
