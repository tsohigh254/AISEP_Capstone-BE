using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.MasterData;
using AISEP.Infrastructure.Data;
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
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<IndustryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIndustries([FromQuery] string mode = "tree")
    {
        // Single query, load all industries with AsNoTracking
        var allIndustries = await _context.Industries
            .AsNoTracking()
            .OrderBy(i => i.IndustryID)
            .ToListAsync();

        if (mode.Equals("flat", StringComparison.OrdinalIgnoreCase))
        {
            // Flat list - all industries without hierarchy
            var flatList = allIndustries
                .OrderBy(i => i.ParentIndustryID ?? 0)
                .ThenBy(i => i.IndustryName)
                .Select(i => new IndustrySimpleDto
                {
                    IndustryID = i.IndustryID,
                    IndustryName = i.IndustryName,
                    Description = i.Description,
                    ParentIndustryID = i.ParentIndustryID
                });

            return Ok(ApiResponse<IEnumerable<IndustrySimpleDto>>.Ok(flatList));
        }

        // Tree mode (default) - build tree in-memory
        var industryLookup = allIndustries.ToDictionary(i => i.IndustryID);
        
        // Get root industries (ParentIndustryID == null)
        var rootIndustries = allIndustries
            .Where(i => i.ParentIndustryID == null)
            .OrderBy(i => i.IndustryID)
            .ToList();

        // Build tree structure
        var tree = rootIndustries.Select(parent => new IndustryDto
        {
            IndustryID = parent.IndustryID,
            IndustryName = parent.IndustryName,
            Description = parent.Description,
            ParentIndustryID = null,
            SubIndustries = allIndustries
                .Where(sub => sub.ParentIndustryID == parent.IndustryID)
                .OrderBy(sub => sub.IndustryID)
                .Select(sub => new IndustryDto
                {
                    IndustryID = sub.IndustryID,
                    IndustryName = sub.IndustryName,
                    Description = sub.Description,
                    ParentIndustryID = sub.ParentIndustryID,
                    SubIndustries = new List<IndustryDto>() // No deeper nesting in MVP
                })
                .ToList()
        });

        return Ok(ApiResponse<IEnumerable<IndustryDto>>.Ok(tree));
    }

    // Hardcoded startup stages (matches ERD: Stage varchar in Startups table)
    private static readonly List<StartupStageDto> _stages = new()
    {
        new() { StageName = "Idea", Description = "Concept stage - validating the idea" },
        new() { StageName = "Pre-Seed", Description = "Building MVP and early validation" },
        new() { StageName = "Seed", Description = "Product-market fit and early traction" },
        new() { StageName = "Series A", Description = "Scaling product and team" },
        new() { StageName = "Series B", Description = "Expansion and growth" },
        new() { StageName = "Series C+", Description = "Late stage growth and profitability" },
        new() { StageName = "IPO Ready", Description = "Preparing for public offering" }
    };

    /// <summary>
    /// Get all startup stages
    /// </summary>
    [HttpGet("stages")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<IEnumerable<StartupStageDto>>> GetStages()
    {
        return Ok(ApiResponse<IEnumerable<StartupStageDto>>.Ok(_stages));
    }

    /// <summary>
    /// Get all roles (for dropdowns)
    /// </summary>
    [HttpGet("roles")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IEnumerable<RoleSimpleDto>>>> GetRoles()
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

        return Ok(ApiResponse<IEnumerable<RoleSimpleDto>>.Ok(roles));
    }
}
