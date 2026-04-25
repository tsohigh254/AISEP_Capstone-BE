using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.MasterData;
using AISEP.Infrastructure.Data;
using AISEP.WebAPI.Extensions;
using static AISEP.WebAPI.Extensions.ApiEnvelopeExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AISEP.WebAPI.Controllers;

/// <summary>
/// Master data endpoints for dropdowns (industries, stages, roles)
/// </summary>
[ApiController]
[Route("api/master")]
public class MasterDataController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public MasterDataController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all industries.
    /// Default: hierarchical tree (2 levels: parent -> subIndustries).
    /// Use ?mode=flat for flat list.
    /// </summary>
    /// <param name="mode">Response format: "tree" (default) or "flat"</param>
    [HttpGet("industries")]
    [AllowAnonymous]
    public async Task<IActionResult> GetIndustries([FromQuery] string mode = "tree")
    {
        var allIndustries = await _context.Industries
            .AsNoTracking()
            .Where(i => i.IsActive)
            .OrderBy(i => i.IndustryID)
            .ToListAsync();

        if (mode.Equals("flat", StringComparison.OrdinalIgnoreCase))
        {
            var flatList = allIndustries
                .OrderBy(i => i.ParentIndustryID ?? 0)
                .ThenBy(i => i.IndustryName)
                .Select(i => new IndustrySimpleDto
                {
                    IndustryID = i.IndustryID,
                    IndustryName = i.IndustryName,
                    Description = i.Description,
                    ParentIndustryID = i.ParentIndustryID
                })
                .ToList();

            return OkEnvelope<List<IndustrySimpleDto>>(flatList);
        }

        var rootIndustries = allIndustries
            .Where(i => i.ParentIndustryID == null)
            .OrderBy(i => i.IndustryID)
            .ToList();

        var tree = rootIndustries.Select(parent => new IndustryDto
        {
            IndustryID = parent.IndustryID,
            IndustryName = parent.IndustryName,
            Description = parent.Description,
            ParentIndustryID = null,
            IsActive = parent.IsActive,
            StartupCount = _context.Startups.Count(s => s.IndustryID == parent.IndustryID || s.SubIndustryID == parent.IndustryID),
            InvestorCount = _context.InvestorIndustryFocuses.Count(f => f.IndustryID == parent.IndustryID),
            SubIndustries = allIndustries
                .Where(sub => sub.ParentIndustryID == parent.IndustryID)
                .OrderBy(sub => sub.IndustryID)
                .Select(sub => new IndustryDto
                {
                    IndustryID = sub.IndustryID,
                    IndustryName = sub.IndustryName,
                    Description = sub.Description,
                    ParentIndustryID = sub.ParentIndustryID,
                    IsActive = sub.IsActive,
                    StartupCount = _context.Startups.Count(s => s.IndustryID == sub.IndustryID || s.SubIndustryID == sub.IndustryID),
                    InvestorCount = _context.InvestorIndustryFocuses.Count(f => f.IndustryID == sub.IndustryID),
                    SubIndustries = new List<IndustryDto>()
                })
                .ToList()
        }).ToList();

        return OkEnvelope<List<IndustryDto>>(tree);
    }

    /// <summary>
    /// Get all startup stages
    /// </summary>
    [HttpGet("stages")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStages()
    {
        var stages = await _context.Stages
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.OrderIndex)
            .Select(s => new StartupStageDto
            {
                StageID = s.StageID,
                StageName = s.StageName,
                Description = s.Description
            })
            .ToListAsync();

        return OkEnvelope<List<StartupStageDto>>(stages);
    }

    /// <summary>
    /// Get all roles (for dropdowns)
    /// </summary>
    [HttpGet("roles")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _context.Roles
            .OrderBy(r => r.RoleName)
            .Select(r => new RoleSimpleDto
            {
                RoleID = r.RoleID,
                RoleName = r.RoleName,
                Description = r.Description
            })
            .ToListAsync();

        return OkEnvelope<List<RoleSimpleDto>>(roles);
    }
}
