namespace AISEP.Application.DTOs;

// Role DTOs
public record RoleResponse(
    int RoleId,
    string RoleName,
    string? Description,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IEnumerable<PermissionResponse>? Permissions
);

public record CreateRoleRequest(
    string RoleName,
    string? Description
);

public record UpdateRoleRequest(
    string? RoleName,
    string? Description
);

public record AssignPermissionRequest(
    int PermissionId
);

// Permission DTOs
public record PermissionResponse(
    int PermissionId,
    string PermissionName,
    string? Description,
    string? Category
);

public record CreatePermissionRequest(
    string PermissionName,
    string? Description,
    string? Category
);

public record UpdatePermissionRequest(
    string? PermissionName,
    string? Description,
    string? Category
);
