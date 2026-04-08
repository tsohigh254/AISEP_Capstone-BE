using AISEP.Application.DTOs.Admin;
using AISEP.Application.DTOs.Common;
using AISEP.Application.QueryParams;

namespace AISEP.Application.Interfaces;

public interface IAdminService
{
    // Roles Matrix
    Task<ApiResponse<RolesMatrixDto>> GetRolesMatrixAsync();
    Task<ApiResponse<RolesMatrixDto>> UpdateRolesMatrixAsync(int adminId, UpdateRolesMatrixRequest request);

    // System Config
    Task<ApiResponse<SystemConfigDto>> GetConfigAsync(string group);
    Task<ApiResponse<SystemConfigDto>> UpdateConfigAsync(int adminId, string group, UpdateSystemConfigRequest request);

    // Workflows
    Task<ApiResponse<WorkflowConfigDto>> GetWorkflowConfigAsync();
    Task<ApiResponse<WorkflowConfigDto>> UpdateWorkflowConfigAsync(int adminId, UpdateWorkflowConfigRequest request);

    // System Health
    Task<ApiResponse<SystemHealthDto>> GetSystemHealthAsync();

    // Violation Reports
    Task<ApiResponse<PagedResponse<ViolationReportDto>>> GetViolationReportsAsync(ViolationQueryParams query);
    Task<ApiResponse<ViolationReportDto>> ResolveViolationAsync(int adminId, int flagId, ResolveViolationRequest request);

    // Incidents
    Task<ApiResponse<List<IncidentDto>>> GetIncidentsAsync();
    Task<ApiResponse<IncidentDto>> CreateIncidentAsync(int adminId, CreateIncidentRequest request);
    Task<ApiResponse<IncidentDto>> RollbackIncidentAsync(int adminId, int incidentId, RollbackIncidentRequest request);

    // Audit Logs
    Task<ApiResponse<PagedResponse<AuditLogDto>>> GetAuditLogsAsync(AuditLogQueryParams query);
    Task<ApiResponse<AuditLogDetailDto>> GetAuditLogByIdAsync(int logId);
}
