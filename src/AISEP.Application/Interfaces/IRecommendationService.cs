using AISEP.Application.DTOs.AI;
using AISEP.Application.DTOs.Common;

namespace AISEP.Application.Interfaces;

/// <summary>
/// Service for matching startups and investors based on profile fields.
/// This is the primary recommendation logic handled directly within the Backend.
/// </summary>
public interface IRecommendationService
{
    /// <summary>
    /// Gets a list of startups that match the investor's preferences.
    /// </summary>
    Task<ApiResponse<RecommendationListResult>> GetStartupRecommendationsAsync(int investorId, int topN);

    /// <summary>
    /// Explains the match between a specific investor and startup.
    /// </summary>
    Task<ApiResponse<RecommendationExplanationResult>> GetMatchExplanationAsync(int investorId, int startupId);
}
