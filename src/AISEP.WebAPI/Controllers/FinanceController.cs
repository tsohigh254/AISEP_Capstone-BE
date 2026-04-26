using AISEP.Application.Interfaces;
using AISEP.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AISEP.WebAPI.Controllers;

[ApiController]
[Route("api/staff/finance")]
[Tags("Staff Finance")]
[Authorize(Policy = "StaffOrAdmin")]
public class FinanceController : ControllerBase
{
    private readonly IStaffFinanceService _financeService;

    public FinanceController(IStaffFinanceService financeService)
    {
        _financeService = financeService;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetFinanceOverview(
        [FromQuery] string period = "30D", 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10)
    {
        var result = await _financeService.GetFinanceOverviewAsync(period, page, pageSize);
        return result.ToActionResult();
    }
}
