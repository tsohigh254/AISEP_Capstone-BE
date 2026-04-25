using AISEP.Application.DTOs.AI;
using AISEP.Application.DTOs.Common;
using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AISEP.Infrastructure.Services;

public class RecommendationService : IRecommendationService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<RecommendationService> _logger;

    public RecommendationService(ApplicationDbContext db, ILogger<RecommendationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ApiResponse<RecommendationListResult>> GetStartupRecommendationsAsync(int investorId, int topN)
    {
        try
        {
            var result = await GetStartupRecommendationsLocalAsync(investorId, topN);
            return ApiResponse<RecommendationListResult>.SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error computing recommendations for investor {InvestorId}", investorId);
            return ApiResponse<RecommendationListResult>.ErrorResponse("SERVER_ERROR", "Không thể lấy danh sách gợi ý.");
        }
    }

    public async Task<ApiResponse<RecommendationExplanationResult>> GetMatchExplanationAsync(int investorId, int startupId)
    {
        try
        {
            var recommendations = await GetStartupRecommendationsLocalAsync(investorId, 100);
            var match = recommendations.Matches.FirstOrDefault(m => m.StartupId == startupId);

            if (match == null)
            {
                return ApiResponse<RecommendationExplanationResult>.ErrorResponse("NOT_FOUND", "Không tìm thấy dữ liệu phù hợp.");
            }

            var result = new RecommendationExplanationResult
            {
                InvestorId = investorId,
                StartupId = startupId,
                GeneratedAt = DateTime.UtcNow,
                Explanation = new
                {
                    summary = match.FitSummaryLabel,
                    reasons = match.PositiveReasons,
                    score = Math.Round(match.FinalMatchScore * 100, 1) + "%"
                }
            };

            return ApiResponse<RecommendationExplanationResult>.SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error explaining match for {InvestorId}-{StartupId}", investorId, startupId);
            return ApiResponse<RecommendationExplanationResult>.ErrorResponse("SERVER_ERROR", "Không thể hiển thị giải thích.");
        }
    }

    private async Task<RecommendationListResult> GetStartupRecommendationsLocalAsync(int investorId, int topN)
    {
        var investor = await _db.Investors
            .AsNoTracking()
            .Include(i => i.Preferences)
            .Include(i => i.IndustryFocus).ThenInclude(f => f.IndustryRef)
            .Include(i => i.StageFocus).ThenInclude(f => f.StageRef)
            .FirstOrDefaultAsync(i => i.InvestorID == investorId);

        if (investor == null) return new RecommendationListResult { InvestorId = investorId };

        var startups = await _db.Startups
            .AsNoTracking()
            .Include(s => s.Industry)
            .Include(s => s.StageRef)
            .Where(s => s.IsVisible && s.ProfileStatus == Domain.Enums.ProfileStatus.Approved)
            .ToListAsync();

        var matches = new List<RecommendationMatchResult>();
        
        static List<string> SplitCsv(string? val) => string.IsNullOrWhiteSpace(val) 
            ? new List<string>() 
            : val.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        var preferredIndustries = investor.IndustryFocus.Select(ifoc => ifoc.IndustryRef?.IndustryName ?? "").ToList();
        var preferredStages = investor.StageFocus.Select(sf => sf.StageRef?.StageName ?? "").ToList();

        var preferredGeos = SplitCsv(investor.Preferences?.PreferredGeographies);

        foreach (var s in startups)
        {
            double score = 0;
            var positive = new List<string>();

            // Industry Match (40 pts)
            if (s.Industry != null && preferredIndustries.Any(pi => pi.Contains(s.Industry.IndustryName, StringComparison.OrdinalIgnoreCase) || s.Industry.IndustryName.Contains(pi, StringComparison.OrdinalIgnoreCase)))
            {
                score += 40;
                positive.Add($"Lĩnh vực phù hợp: {s.Industry.IndustryName}");
            }

            // Stage Match (30 pts)
            if (s.StageRef != null && preferredStages.Any(ps => ps.Equals(s.StageRef.StageName, StringComparison.OrdinalIgnoreCase)))
            {
                score += 30;
                positive.Add($"Giai đoạn phù hợp: {s.StageRef.StageName}");
            }

            // Geo Match (20 pts)
            if (!string.IsNullOrEmpty(s.Country) && preferredGeos.Any(pg => pg.Contains(s.Country, StringComparison.OrdinalIgnoreCase)))
            {
                score += 20;
                positive.Add($"Khu vực ưu tiên: {s.Country}");
            }

            score += 10; // Base score for active profile

            if (score > 10)
            {
                matches.Add(new RecommendationMatchResult
                {
                    StartupId = s.StartupID,
                    StartupName = s.CompanyName,
                    FinalMatchScore = score / 100.0,
                    MatchBand = score >= 70 ? "Excellent" : (score >= 40 ? "Good" : "Fair"),
                    FitSummaryLabel = score >= 70 ? "Rất phù hợp" : (score >= 40 ? "Khá phù hợp" : "Có tiềm năng"),
                    PositiveReasons = positive,
                    MatchReasons = positive
                });
            }
        }

        return new RecommendationListResult
        {
            InvestorId = investorId,
            Matches = matches.OrderByDescending(m => m.FinalMatchScore).Take(topN).ToList(),
            GeneratedAt = DateTime.UtcNow
        };
    }
}
