using AISEP.Application.DTOs;
using AISEP.Application.DTOs.Common;
using AISEP.Application.Interfaces;
using AISEP.WebAPI.Extensions;
using static AISEP.WebAPI.Extensions.ApiEnvelopeExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AISEP.WebAPI.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AdminController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    /// <summary>
    /// Get audit logs with optional filtering and pagination.
    /// </summary>
    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? search,
        [FromQuery] string? actionType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var result = await _auditService.GetLogsAsync(search, actionType, page, pageSize, ct);

        return OkEnvelope(result, "Audit logs retrieved");
    }
}
