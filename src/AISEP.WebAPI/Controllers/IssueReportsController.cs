using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.IssueReport;
using AISEP.Application.Interfaces;
using AISEP.Domain.Enums;
using AISEP.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AISEP.WebAPI.Controllers;

[ApiController]
[Route("api/issue-reports")]
[Tags("Issue Reports")]
[Authorize]
public class IssueReportsController : ControllerBase
{
    private readonly IIssueReportService _service;

    public IssueReportsController(IIssueReportService service)
    {
        _service = service;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    private string GetCurrentUserType()
        => User.FindFirst("userType")?.Value ?? string.Empty;

    // ── POST /api/issue-reports — Submit issue report (any authenticated user)
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] CreateIssueReportRequest request)
    {
        var result = await _service.CreateAsync(GetCurrentUserId(), request);
        if (!result.Success) return result.ToErrorResult();
        return result.ToCreatedEnvelope();
    }

    // ── GET /api/issue-reports/me — Reporter views their own reports
    [HttpGet("me")]
    public async Task<IActionResult> GetMyReports(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] IssueReportStatus? status = null,
        [FromQuery] IssueCategory? category = null)
    {
        var result = await _service.GetMyReportsAsync(GetCurrentUserId(), page, pageSize, status, category);
        return result.ToActionResult();
    }

    // ── GET /api/issue-reports/{id} — View report (reporter or Staff/Admin)
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(GetCurrentUserId(), GetCurrentUserType(), id);
        return result.ToActionResult();
    }

    // ── GET /api/issue-reports — List all (Staff/Admin only)
    [HttpGet]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] IssueReportStatus? status = null,
        [FromQuery] IssueCategory? category = null,
        [FromQuery] int? reporterUserId = null)
    {
        var result = await _service.GetListAsync(page, pageSize, status, category, reporterUserId);
        return result.ToActionResult();
    }

    // ── PATCH /api/issue-reports/{id}/status — Update status (Staff/Admin only)
    [HttpPatch("{id:int}/status")]
    [Authorize(Policy = "StaffOrAdmin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateIssueReportStatusRequest request)
    {
        var result = await _service.UpdateStatusAsync(GetCurrentUserId(), id, request);
        return result.ToActionResult();
    }
}
