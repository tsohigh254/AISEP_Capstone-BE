using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Investor;
using AISEP.Application.Interfaces;
using AISEP.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AISEP.WebAPI.Controllers;

/// <summary>
/// Staff dashboard: stats, KYC trend, activity feed.
/// Requires StaffOrAdmin policy.
/// </summary>
[ApiController]
[Route("api/staff")]
[Tags("Staff")]
[Authorize(Policy = "StaffOrAdmin")]
public class StaffController : ControllerBase
{
    private readonly IStaffDashboardService _svc;
    private readonly IInvestorService _investorService;

    public StaffController(IStaffDashboardService svc, IInvestorService investorService)
    {
        _svc = svc;
        _investorService = investorService;
    }

    /// <summary>Overview stats: total users, locked accounts, pending KYC, AI uptime.</summary>
    [HttpGet("dashboard/stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var result = await _svc.GetDashboardStatsAsync();
        return result.ToActionResult();
    }

    /// <summary>KYC submission trend grouped by day.</summary>
    /// <param name="period">7D or 30D (default 7D)</param>
    [HttpGet("dashboard/kyc-trend")]
    public async Task<IActionResult> GetKycTrend([FromQuery] string period = "7D")
    {
        var result = await _svc.GetKycTrendAsync(period);
        return result.ToActionResult();
    }

    /// <summary>Recent platform activity from audit log.</summary>
    /// <param name="limit">Number of entries (1–50, default 10)</param>
    [HttpGet("activity/feed")]
    public async Task<IActionResult> GetActivityFeed([FromQuery] int limit = 10)
    {
        var result = await _svc.GetActivityFeedAsync(limit);
        return result.ToActionResult();
    }

    /// <summary>Get investor profile by ID — bypasses ProfileStatus filter. For KYC review use.</summary>
    [HttpGet("investors/{investorId:int}")]
    [ProducesResponseType(typeof(ApiResponse<InvestorProfileForStaffDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InvestorProfileForStaffDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInvestorProfile(int investorId)
    {
        var result = await _investorService.GetInvestorProfileForStaffAsync(investorId);
        return result.ToActionResult();
    }
}
