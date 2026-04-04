using AISEP.Application.DTOs.Admin;
using AISEP.Application.DTOs.Common;
using AISEP.Application.Extensions;
using AISEP.Application.Interfaces;
using AISEP.Application.QueryParams;
using AISEP.Domain.Entities;
using AISEP.Domain.Enums;
using AISEP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AISEP.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminService> _logger;
    private readonly IAuditService _audit;

    public AdminService(
        ApplicationDbContext context,
        ILogger<AdminService> logger,
        IAuditService audit)
    {
        _context = context;
        _logger = logger;
        _audit = audit;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Roles Matrix
    // ═══════════════════════════════════════════════════════════════

    public async Task<ApiResponse<RolesMatrixDto>> GetRolesMatrixAsync()
    {
        var roles = await _context.Roles
            .Include(r => r.RolePermissions)
            .AsNoTracking()
            .OrderBy(r => r.RoleID)
            .Select(r => new RoleMatrixItemDto
            {
                RoleId = r.RoleID,
                RoleName = r.RoleName,
                PermissionIds = r.RolePermissions.Select(rp => rp.PermissionID).ToList()
            })
            .ToListAsync();

        var permissions = await _context.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Category).ThenBy(p => p.PermissionName)
            .Select(p => new PermissionItemDto
            {
                PermissionId = p.PermissionID,
                PermissionName = p.PermissionName,
                Category = p.Category
            })
            .ToListAsync();

        return ApiResponse<RolesMatrixDto>.SuccessResponse(new RolesMatrixDto
        {
            Roles = roles,
            AllPermissions = permissions
        });
    }

    public async Task<ApiResponse<RolesMatrixDto>> UpdateRolesMatrixAsync(int adminId, UpdateRolesMatrixRequest request)
    {
        foreach (var assignment in request.Assignments)
        {
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.RoleID == assignment.RoleId);

            if (role == null)
                return ApiResponse<RolesMatrixDto>.ErrorResponse("ROLE_NOT_FOUND",
                    $"Role {assignment.RoleId} not found");

            // Remove existing permissions
            _context.RolePermissions.RemoveRange(role.RolePermissions);

            // Add new permissions
            foreach (var permId in assignment.PermissionIds)
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    RoleID = assignment.RoleId,
                    PermissionID = permId,
                    AssignedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();
        await _audit.LogAsync("UPDATE_ROLES_MATRIX", "Role", null,
            $"Updated roles matrix for {request.Assignments.Count} role(s)");

        return await GetRolesMatrixAsync();
    }

    // ═══════════════════════════════════════════════════════════════
    //  System Config
    // ═══════════════════════════════════════════════════════════════

    public async Task<ApiResponse<SystemConfigDto>> GetConfigAsync(string group)
    {
        var prefix = group + ":";
        var entries = await _context.SystemSettings
            .Where(s => s.SettingKey.StartsWith(prefix))
            .AsNoTracking()
            .OrderBy(s => s.SettingKey)
            .Select(s => new ConfigEntryDto
            {
                SettingId = s.SettingID,
                Key = s.SettingKey,
                Value = s.SettingValue,
                SettingType = s.SettingType,
                Description = s.Description
            })
            .ToListAsync();

        return ApiResponse<SystemConfigDto>.SuccessResponse(new SystemConfigDto
        {
            Group = group,
            Entries = entries
        });
    }

    public async Task<ApiResponse<SystemConfigDto>> UpdateConfigAsync(int adminId, string group, UpdateSystemConfigRequest request)
    {
        var prefix = group + ":";

        foreach (var entry in request.Entries)
        {
            var key = entry.Key.StartsWith(prefix) ? entry.Key : prefix + entry.Key;

            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == key);

            if (setting != null)
            {
                setting.SettingValue = entry.Value;
                setting.UpdatedBy = adminId;
                setting.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.SystemSettings.Add(new SystemSettings
                {
                    SettingKey = key,
                    SettingValue = entry.Value,
                    SettingType = "string",
                    Description = null,
                    IsPublic = false,
                    UpdatedBy = adminId,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();
        await _audit.LogAsync("UPDATE_CONFIG", "SystemSettings", null,
            $"Updated {group} config: {request.Entries.Count} setting(s)");

        return await GetConfigAsync(group);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Workflows
    // ═══════════════════════════════════════════════════════════════

    public async Task<ApiResponse<WorkflowConfigDto>> GetWorkflowConfigAsync()
    {
        var entries = await _context.SystemSettings
            .Where(s => s.SettingKey.StartsWith("workflow:"))
            .AsNoTracking()
            .OrderBy(s => s.SettingKey)
            .Select(s => new ConfigEntryDto
            {
                SettingId = s.SettingID,
                Key = s.SettingKey,
                Value = s.SettingValue,
                SettingType = s.SettingType,
                Description = s.Description
            })
            .ToListAsync();

        return ApiResponse<WorkflowConfigDto>.SuccessResponse(new WorkflowConfigDto
        {
            Entries = entries
        });
    }

    public async Task<ApiResponse<WorkflowConfigDto>> UpdateWorkflowConfigAsync(int adminId, UpdateWorkflowConfigRequest request)
    {
        foreach (var entry in request.Entries)
        {
            var key = entry.Key.StartsWith("workflow:") ? entry.Key : "workflow:" + entry.Key;

            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == key);

            if (setting != null)
            {
                setting.SettingValue = entry.Value;
                setting.UpdatedBy = adminId;
                setting.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.SystemSettings.Add(new SystemSettings
                {
                    SettingKey = key,
                    SettingValue = entry.Value,
                    SettingType = "string",
                    IsPublic = false,
                    UpdatedBy = adminId,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();
        await _audit.LogAsync("UPDATE_WORKFLOW_CONFIG", "SystemSettings", null,
            $"Updated {request.Entries.Count} workflow setting(s)");

        return await GetWorkflowConfigAsync();
    }

    // ═══════════════════════════════════════════════════════════════
    //  System Health
    // ═══════════════════════════════════════════════════════════════

    public async Task<ApiResponse<SystemHealthDto>> GetSystemHealthAsync()
    {
        var dbConnected = await _context.Database.CanConnectAsync();

        var dto = new SystemHealthDto
        {
            DatabaseConnected = dbConnected,
            TotalUsers = dbConnected ? await _context.Users.CountAsync() : 0,
            TotalStartups = dbConnected ? await _context.Startups.CountAsync() : 0,
            TotalInvestors = dbConnected ? await _context.Investors.CountAsync() : 0,
            TotalAdvisors = dbConnected ? await _context.Advisors.CountAsync() : 0,
            PendingApprovals = dbConnected
                ? await _context.Startups.CountAsync(s => s.ProfileStatus == ProfileStatus.Pending)
                + await _context.Investors.CountAsync(i => i.ProfileStatus == ProfileStatus.Pending)
                + await _context.Advisors.CountAsync(a => a.ProfileStatus == ProfileStatus.Pending)
                : 0,
            OpenIncidents = dbConnected
                ? await _context.Incidents.CountAsync(i => i.Status == IncidentStatus.Open || i.Status == IncidentStatus.Investigating)
                : 0,
            UnresolvedFlags = dbConnected
                ? await _context.FlaggedContents.CountAsync(f => f.ModerationStatus == ModerationStatus.None || f.ModerationStatus == ModerationStatus.Flag)
                : 0,
            CheckedAt = DateTime.UtcNow
        };

        return ApiResponse<SystemHealthDto>.SuccessResponse(dto);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Violation Reports
    // ═══════════════════════════════════════════════════════════════

    public async Task<ApiResponse<PagedResponse<ViolationReportDto>>> GetViolationReportsAsync(ViolationQueryParams query)
    {
        var q = _context.FlaggedContents
            .Include(f => f.RelatedUser)
            .AsNoTracking()
            .AsQueryable();

        if (query.Status.HasValue)
            q = q.Where(f => f.ModerationStatus == query.Status.Value);

        if (!string.IsNullOrEmpty(query.Severity))
            q = q.Where(f => f.Severity == query.Severity);

        if (!string.IsNullOrEmpty(query.ContentType))
            q = q.Where(f => f.ContentType == query.ContentType);

        if (!string.IsNullOrEmpty(query.Key))
            q = q.Where(f => f.FlagReason.Contains(query.Key) || (f.FlagDetails != null && f.FlagDetails.Contains(query.Key)));

        var total = await q.CountAsync();

        var items = await q
            .OrderByDescending(f => f.FlaggedAt)
            .Paging(query.Page, query.PageSize)
            .Select(f => new ViolationReportDto
            {
                FlagId = f.FlagID,
                ContentType = f.ContentType,
                ContentId = f.ContentID,
                RelatedUserId = f.RelatedUserID,
                RelatedUserEmail = f.RelatedUser != null ? f.RelatedUser.Email : null,
                FlagReason = f.FlagReason,
                Severity = f.Severity,
                FlagDetails = f.FlagDetails,
                ModerationStatus = f.ModerationStatus.ToString(),
                FlaggedAt = f.FlaggedAt,
                ReviewedAt = f.ReviewedAt,
                ReviewedBy = f.ReviewedBy,
                ModeratorNotes = f.ModeratorNotes
            })
            .ToListAsync();

        return ApiResponse<PagedResponse<ViolationReportDto>>.SuccessResponse(
            new PagedResponse<ViolationReportDto>
            {
                Items = items,
                Paging = new PagingInfo
                {
                    Page = query.Page,
                    PageSize = query.PageSize,
                    TotalItems = total
                }
            });
    }

    public async Task<ApiResponse<ViolationReportDto>> ResolveViolationAsync(int adminId, int flagId, ResolveViolationRequest request)
    {
        var flag = await _context.FlaggedContents
            .Include(f => f.RelatedUser)
            .FirstOrDefaultAsync(f => f.FlagID == flagId);

        if (flag == null)
            return ApiResponse<ViolationReportDto>.ErrorResponse("FLAG_NOT_FOUND", "Flagged content not found");

        flag.ModerationStatus = request.Decision;
        flag.ReviewedBy = adminId;
        flag.ReviewedAt = DateTime.UtcNow;
        flag.ModeratorNotes = request.Notes;

        await _context.SaveChangesAsync();
        await _audit.LogAsync("RESOLVE_VIOLATION", "FlaggedContent", flagId,
            $"Resolved flag #{flagId} with decision: {request.Decision}");

        return ApiResponse<ViolationReportDto>.SuccessResponse(new ViolationReportDto
        {
            FlagId = flag.FlagID,
            ContentType = flag.ContentType,
            ContentId = flag.ContentID,
            RelatedUserId = flag.RelatedUserID,
            RelatedUserEmail = flag.RelatedUser?.Email,
            FlagReason = flag.FlagReason,
            Severity = flag.Severity,
            FlagDetails = flag.FlagDetails,
            ModerationStatus = flag.ModerationStatus.ToString(),
            FlaggedAt = flag.FlaggedAt,
            ReviewedAt = flag.ReviewedAt,
            ReviewedBy = flag.ReviewedBy,
            ModeratorNotes = flag.ModeratorNotes
        }, "Violation resolved");
    }

    // ═══════════════════════════════════════════════════════════════
    //  Incidents
    // ═══════════════════════════════════════════════════════════════

    public async Task<ApiResponse<IncidentDto>> CreateIncidentAsync(int adminId, CreateIncidentRequest request)
    {
        var incident = new Incident
        {
            Title = request.Title,
            Description = request.Description,
            Severity = request.Severity,
            Status = IncidentStatus.Open,
            CreatedBy = adminId,
            CreatedAt = DateTime.UtcNow,
            IsRolledBack = false
        };

        _context.Incidents.Add(incident);
        await _context.SaveChangesAsync();

        await _audit.LogAsync("CREATE_INCIDENT", "Incident", incident.IncidentID,
            $"Created incident: {incident.Title} (Severity: {incident.Severity})");

        return ApiResponse<IncidentDto>.SuccessResponse(MapToDto(incident), "Incident created");
    }

    public async Task<ApiResponse<IncidentDto>> RollbackIncidentAsync(int adminId, int incidentId, RollbackIncidentRequest request)
    {
        var incident = await _context.Incidents.FindAsync(incidentId);

        if (incident == null)
            return ApiResponse<IncidentDto>.ErrorResponse("INCIDENT_NOT_FOUND", "Incident not found");

        incident.Status = IncidentStatus.RolledBack;
        incident.IsRolledBack = true;
        incident.RollbackNotes = request.RollbackNotes;
        incident.ResolvedBy = adminId;
        incident.ResolvedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _audit.LogAsync("ROLLBACK_INCIDENT", "Incident", incidentId,
            $"Rolled back incident #{incidentId}");

        return ApiResponse<IncidentDto>.SuccessResponse(MapToDto(incident), "Incident rolled back");
    }

    private static IncidentDto MapToDto(Incident i) => new()
    {
        IncidentId = i.IncidentID,
        Title = i.Title,
        Description = i.Description,
        Severity = i.Severity.ToString(),
        Status = i.Status.ToString(),
        CreatedBy = i.CreatedBy,
        CreatedAt = i.CreatedAt,
        ResolvedAt = i.ResolvedAt,
        ResolvedBy = i.ResolvedBy,
        RollbackNotes = i.RollbackNotes,
        IsRolledBack = i.IsRolledBack
    };

    // ═══════════════════════════════════════════════════════════════
    //  Audit Logs
    // ═══════════════════════════════════════════════════════════════

    public async Task<ApiResponse<PagedResponse<AuditLogDto>>> GetAuditLogsAsync(AuditLogQueryParams query)
    {
        var q = _context.AuditLogs
            .Include(a => a.User)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrEmpty(query.ActionType))
            q = q.Where(a => a.ActionType == query.ActionType);

        if (!string.IsNullOrEmpty(query.EntityType))
            q = q.Where(a => a.EntityType == query.EntityType);

        if (query.UserId.HasValue)
            q = q.Where(a => a.UserID == query.UserId.Value);

        if (query.DateFrom.HasValue)
            q = q.Where(a => a.CreatedAt >= query.DateFrom.Value);

        if (query.DateTo.HasValue)
            q = q.Where(a => a.CreatedAt <= query.DateTo.Value);

        if (!string.IsNullOrEmpty(query.Key))
            q = q.Where(a => a.ActionType.Contains(query.Key)
                || a.EntityType.Contains(query.Key)
                || (a.ActionDetails != null && a.ActionDetails.Contains(query.Key)));

        var total = await q.CountAsync();

        var items = await q
            .OrderByDescending(a => a.CreatedAt)
            .Paging(query.Page, query.PageSize)
            .Select(a => new AuditLogDto
            {
                LogId = a.LogID,
                UserId = a.UserID,
                UserEmail = a.User != null ? a.User.Email : null,
                ActionType = a.ActionType,
                EntityType = a.EntityType,
                EntityId = a.EntityID,
                ActionDetails = a.ActionDetails,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return ApiResponse<PagedResponse<AuditLogDto>>.SuccessResponse(
            new PagedResponse<AuditLogDto>
            {
                Items = items,
                Paging = new PagingInfo
                {
                    Page = query.Page,
                    PageSize = query.PageSize,
                    TotalItems = total
                }
            });
    }

    public async Task<ApiResponse<AuditLogDetailDto>> GetAuditLogByIdAsync(int logId)
    {
        var log = await _context.AuditLogs
            .Include(a => a.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.LogID == logId);

        if (log == null)
            return ApiResponse<AuditLogDetailDto>.ErrorResponse("AUDIT_LOG_NOT_FOUND", "Audit log not found");

        return ApiResponse<AuditLogDetailDto>.SuccessResponse(new AuditLogDetailDto
        {
            LogId = log.LogID,
            UserId = log.UserID,
            UserEmail = log.User?.Email,
            ActionType = log.ActionType,
            EntityType = log.EntityType,
            EntityId = log.EntityID,
            ActionDetails = log.ActionDetails,
            CreatedAt = log.CreatedAt,
            IpAddress = log.IPAddress,
            UserAgent = log.UserAgent
        });
    }
}
