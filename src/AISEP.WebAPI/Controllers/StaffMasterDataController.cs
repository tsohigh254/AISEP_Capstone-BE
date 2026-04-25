using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.MasterData;
using AISEP.Application.Interfaces;
using AISEP.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AISEP.WebAPI.Controllers;

/// <summary>
/// Staff endpoints for managing master data (industries, stages)
/// </summary>
[ApiController]
[Route("api/staff/master")]
[Tags("Staff Management")]
[Authorize(Policy = "StaffOrAdmin")]
public class StaffMasterDataController : ControllerBase
{
    private readonly IStaffMasterDataService _svc;

    public StaffMasterDataController(IStaffMasterDataService svc)
    {
        _svc = svc;
    }

    // ================================================================
    // INDUSTRIES
    // ================================================================

    [HttpGet("industries")]
    public async Task<IActionResult> GetAllIndustries()
    {
        var result = await _svc.GetAllIndustriesAsync();
        return result.ToActionResult();
    }

    [HttpPost("industries")]
    public async Task<IActionResult> CreateIndustry([FromBody] ManageIndustryRequest request)
    {
        var result = await _svc.CreateIndustryAsync(request);
        return result.ToActionResult();
    }

    [HttpPut("industries/{id:int}")]
    public async Task<IActionResult> UpdateIndustry(int id, [FromBody] ManageIndustryRequest request)
    {
        var result = await _svc.UpdateIndustryAsync(id, request);
        return result.ToActionResult();
    }

    [HttpDelete("industries/{id:int}")]
    public async Task<IActionResult> DeleteIndustry(int id)
    {
        var result = await _svc.DeleteIndustryAsync(id);
        return result.ToActionResult();
    }

    // ================================================================
    // STAGES
    // ================================================================

    [HttpGet("stages")]
    public async Task<IActionResult> GetAllStages()
    {
        var result = await _svc.GetAllStagesAsync();
        return result.ToActionResult();
    }

    [HttpPost("stages")]
    public async Task<IActionResult> CreateStage([FromBody] ManageStageRequest request)
    {
        var result = await _svc.CreateStageAsync(request);
        return result.ToActionResult();
    }

    [HttpPut("stages/{id:int}")]
    public async Task<IActionResult> UpdateStage(int id, [FromBody] ManageStageRequest request)
    {
        var result = await _svc.UpdateStageAsync(id, request);
        return result.ToActionResult();
    }

    [HttpDelete("stages/{id:int}")]
    public async Task<IActionResult> DeleteStage(int id)
    {
        var result = await _svc.DeleteStageAsync(id);
        return result.ToActionResult();
    }

    [HttpPost("stages/reorder")]
    public async Task<IActionResult> ReorderStages([FromBody] ReorderStageRequest request)
    {
        var result = await _svc.ReorderStagesAsync(request);
        return result.ToActionResult();
    }
}
