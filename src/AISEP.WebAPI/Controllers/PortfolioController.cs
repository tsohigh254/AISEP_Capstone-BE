using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Connection;
using AISEP.Application.Interfaces;
using AISEP.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AISEP.WebAPI.Controllers;

/// <summary>
/// Investor portfolio management — track invested companies.
/// </summary>
[ApiController]
[Route("api/investors/me/portfolio")]
[Tags("Portfolio")]
[Authorize(Policy = "InvestorOnly")]
public class PortfolioController : ControllerBase
{
    private readonly IConnectionsService _svc;

    public PortfolioController(IConnectionsService svc) => _svc = svc;

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    // ================================================================
    // 1) GET /api/investors/me/portfolio
    // ================================================================

    /// <summary>Get the current investor's portfolio companies (paged).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<PortfolioCompanyDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPortfolio([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _svc.GetPortfolioAsync(GetCurrentUserId(), page, pageSize);
        return result.ToActionResult();
    }

    // ================================================================
    // 2) POST /api/investors/me/portfolio
    // ================================================================

    /// <summary>Add a company to portfolio.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PortfolioCompanyDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreatePortfolioCompanyRequest request)
    {
        var result = await _svc.CreatePortfolioAsync(GetCurrentUserId(), request);
        if (!result.Success) return result.ToErrorResult();
        return StatusCode(StatusCodes.Status201Created, result);
    }

    // ================================================================
    // 3) PUT /api/investors/me/portfolio/{id}
    // ================================================================

    /// <summary>Update a portfolio company record.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<PortfolioCompanyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PortfolioCompanyDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePortfolioCompanyRequest request)
    {
        var result = await _svc.UpdatePortfolioAsync(GetCurrentUserId(), id, request);
        return result.ToActionResult();
    }

    // ================================================================
    // 4) DELETE /api/investors/me/portfolio/{id}
    // ================================================================

    /// <summary>Remove a company from portfolio.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<PortfolioCompanyDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _svc.DeletePortfolioAsync(GetCurrentUserId(), id);
        if (!result.Success) return result.ToErrorResult();
        return NoContent();
    }
}
