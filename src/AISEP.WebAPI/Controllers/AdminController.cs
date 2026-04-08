using AISEP.Application.DTOs.Admin;
using AISEP.Application.DTOs.Common;
using AISEP.Application.Interfaces;
using AISEP.Application.QueryParams;
using AISEP.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AISEP.WebAPI.Controllers;

/// <summary>
/// Admin-only endpoints: roles matrix, system config, health, violations, incidents, audit logs.
/// </summary>
[ApiController]
[Route("api/admin")]
[Tags("Admin")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _svc;

    public AdminController(IAdminService svc)
    {
        _svc = svc;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Roles Matrix
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Get roles-permissions matrix.</summary>
    [HttpGet("roles/matrix")]
    public async Task<IActionResult> GetRolesMatrix()
    {
        var result = await _svc.GetRolesMatrixAsync();
        return result.ToEnvelope();
    }

    /// <summary>Bulk update roles-permissions matrix.</summary>
    [HttpPut("roles/matrix")]
    public async Task<IActionResult> UpdateRolesMatrix([FromBody] UpdateRolesMatrixRequest request)
    {
        var result = await _svc.UpdateRolesMatrixAsync(GetCurrentUserId(), request);
        return result.ToEnvelope();
    }

    // ═══════════════════════════════════════════════════════════════
    //  System Config
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Update AI configuration (API key, model, etc.).</summary>
    [HttpPut("config/ai")]
    public async Task<IActionResult> UpdateAiConfig([FromBody] UpdateSystemConfigRequest request)
    {
        var result = await _svc.UpdateConfigAsync(GetCurrentUserId(), "ai", request);
        return result.ToEnvelope();
    }

    /// <summary>Update blockchain configuration (RPC, contract, etc.).</summary>
    [HttpPut("config/blockchain")]
    public async Task<IActionResult> UpdateBlockchainConfig([FromBody] UpdateSystemConfigRequest request)
    {
        var result = await _svc.UpdateConfigAsync(GetCurrentUserId(), "blockchain", request);
        return result.ToEnvelope();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Workflows
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Update approval workflow settings (auto/manual approvals).</summary>
    [HttpPut("workflows")]
    public async Task<IActionResult> UpdateWorkflows([FromBody] UpdateWorkflowConfigRequest request)
    {
        var result = await _svc.UpdateWorkflowConfigAsync(GetCurrentUserId(), request);
        return result.ToEnvelope();
    }

    // ═══════════════════════════════════════════════════════════════
    //  System Health
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Check system health: DB status, user counts, pending items.</summary>
    [HttpGet("system-health")]
    public async Task<IActionResult> GetSystemHealth()
    {
        var result = await _svc.GetSystemHealthAsync();
        return result.ToEnvelope();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Violation Reports
    // ═══════════════════════════════════════════════════════════════

    /// <summary>List violation reports (paged, filtered).</summary>
    [HttpGet("violation-reports")]
    public async Task<IActionResult> GetViolationReports([FromQuery] ViolationQueryParams query)
    {
        var result = await _svc.GetViolationReportsAsync(query);
        return result.ToPagedEnvelope();
    }

    /// <summary>Resolve a violation report.</summary>
    [HttpPut("violation-reports/{id:int}/resolve")]
    public async Task<IActionResult> ResolveViolation(int id, [FromBody] ResolveViolationRequest request)
    {
        var result = await _svc.ResolveViolationAsync(GetCurrentUserId(), id, request);
        return result.ToEnvelope();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Incidents
    // ═══════════════════════════════════════════════════════════════

    /// <summary>List all incidents.</summary>
    [HttpGet("incidents")]
    public async Task<IActionResult> GetIncidents()
    {
        var result = await _svc.GetIncidentsAsync();
        return result.ToEnvelope();
    }

    /// <summary>Create a new incident record.</summary>
    [HttpPost("incidents")]
    public async Task<IActionResult> CreateIncident([FromBody] CreateIncidentRequest request)
    {
        var result = await _svc.CreateIncidentAsync(GetCurrentUserId(), request);
        return result.ToCreatedEnvelope();
    }

    /// <summary>Rollback an incident.</summary>
    [HttpPost("incidents/{id:int}/rollback")]
    public async Task<IActionResult> RollbackIncident(int id, [FromBody] RollbackIncidentRequest request)
    {
        var result = await _svc.RollbackIncidentAsync(GetCurrentUserId(), id, request);
        return result.ToEnvelope();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Audit Logs
    // ═══════════════════════════════════════════════════════════════

    /// <summary>List audit logs (paged, filtered).</summary>
    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs([FromQuery] AuditLogQueryParams query)
    {
        var result = await _svc.GetAuditLogsAsync(query);
        return result.ToPagedEnvelope();
    }

    /// <summary>Get audit log detail.</summary>
    [HttpGet("audit-logs/{id:int}")]
    public async Task<IActionResult> GetAuditLog(int id)
    {
        var result = await _svc.GetAuditLogByIdAsync(id);
        return result.ToEnvelope();
    }
}
