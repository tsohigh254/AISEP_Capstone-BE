using AISEP.Application.DTOs.AI;
using AISEP.Application.DTOs.Common;

namespace AISEP.Application.Interfaces;

/// <summary>
/// AI Recommendation integration service.
/// Handles startup/investor reindex triggers and FE-facing recommendation reads.
/// </summary>
public interface IAiRecommendationService
{
    /// <summary>Reindex a startup profile in the recommendation engine. Non-blocking safe.</summary>
    Task ReindexStartupAsync(int startupId);

    /// <summary>Reindex an investor profile in the recommendation engine. Non-blocking safe.</summary>
    Task ReindexInvestorAsync(int investorId);

    /// <summary>Get startup recommendations for an investor (sync read).</summary>
    Task<ApiResponse<RecommendationListResult>> GetStartupRecommendationsAsync(int investorId, int topN);

    /// <summary>Get detailed match explanation for an investor–startup pair.</summary>
    Task<ApiResponse<RecommendationExplanationResult>> GetMatchExplanationAsync(int investorId, int startupId);
}
