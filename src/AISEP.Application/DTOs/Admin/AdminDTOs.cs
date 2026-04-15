using AISEP.Domain.Enums;

namespace AISEP.Application.DTOs.Admin;

// ───────────────────────── Roles Matrix ──────────────────────

public class RolesMatrixDto
{
    public List<RoleMatrixItemDto> Roles { get; set; } = new();
    public List<PermissionItemDto> AllPermissions { get; set; } = new();
}

public class RoleMatrixItemDto
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public List<int> PermissionIds { get; set; } = new();
}

public class PermissionItemDto
{
    public int PermissionId { get; set; }
    public string PermissionName { get; set; } = string.Empty;
    public string? Category { get; set; }
}

public class UpdateRolesMatrixRequest
{
    public List<RolePermissionAssignment> Assignments { get; set; } = new();
}

public class RolePermissionAssignment
{
    public int RoleId { get; set; }
    public List<int> PermissionIds { get; set; } = new();
}

// ───────────────────────── System Config ─────────────────────

public class SystemConfigDto
{
    public string Group { get; set; } = string.Empty;
    public List<ConfigEntryDto> Entries { get; set; } = new();
}

public class ConfigEntryDto
{
    public int SettingId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string SettingType { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateSystemConfigRequest
{
    public List<ConfigUpdateEntry> Entries { get; set; } = new();
}

public class ConfigUpdateEntry
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

// ───────────────────────── Workflows ─────────────────────────

public class WorkflowConfigDto
{
    public List<ConfigEntryDto> Entries { get; set; } = new();
}

public class UpdateWorkflowConfigRequest
{
    public List<ConfigUpdateEntry> Entries { get; set; } = new();
}

// ───────────────────────── System Health ─────────────────────

public class SystemHealthDto
{
    public bool DatabaseConnected { get; set; }
    public int TotalUsers { get; set; }
    public int TotalStartups { get; set; }
    public int TotalInvestors { get; set; }
    public int TotalAdvisors { get; set; }
    public int PendingApprovals { get; set; }
    public int OpenIncidents { get; set; }
    public int UnresolvedFlags { get; set; }
    public DateTime CheckedAt { get; set; }
}

// ───────────────────────── Server Logs ──────────────────────

public class LogFileDto
{
    public string FileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime LastModifiedUtc { get; set; }
}

public class LogContentDto
{
    public string FileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime LastModifiedUtc { get; set; }
    public int TotalLinesReturned { get; set; }
    public List<string> Lines { get; set; } = new();
}

// ───────────────────────── Violation Reports ─────────────────

public class ViolationReportDto
{
    public int FlagId { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public int ContentId { get; set; }
    public int? RelatedUserId { get; set; }
    public string? RelatedUserEmail { get; set; }
    public string FlagReason { get; set; } = string.Empty;
    public string? Severity { get; set; }
    public string? FlagDetails { get; set; }
    public string ModerationStatus { get; set; } = string.Empty;
    public DateTime FlaggedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int? ReviewedBy { get; set; }
    public string? ModeratorNotes { get; set; }
}

public class ResolveViolationRequest
{
    public ModerationStatus Decision { get; set; }
    public string? Notes { get; set; }
}

// ───────────────────────── Incidents ─────────────────────────

public class IncidentDto
{
    public int IncidentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int? ResolvedBy { get; set; }
    public string? RollbackNotes { get; set; }
    public bool IsRolledBack { get; set; }
}

public class CreateIncidentRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IncidentSeverity Severity { get; set; }
}

public class RollbackIncidentRequest
{
    public string RollbackNotes { get; set; } = string.Empty;
}

// ───────────────────────── Audit Logs ────────────────────────

public class AuditLogDto
{
    public int LogId { get; set; }
    public int? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string? ActionDetails { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AuditLogDetailDto : AuditLogDto
{
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
}
