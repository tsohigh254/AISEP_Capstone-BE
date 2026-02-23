namespace AISEP.Application.DTOs;

// User profile DTOs
public record UserProfileResponse(
    int UserId,
    string Email,
    string UserType,
    bool IsActive,
    bool EmailVerified,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    IEnumerable<string> Roles
);

public record UpdateUserProfileRequest(
    string? Email
);

// Admin user management DTOs
public record UserListResponse(
    int UserId,
    string Email,
    string UserType,
    bool IsActive,
    bool EmailVerified,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    IEnumerable<string> Roles
);

public record CreateUserRequest(
    string Email,
    string Password,
    string UserType
);

public record UpdateUserStatusRequest(
    bool IsActive
);

public record AssignRoleRequest(
    int RoleId
);

public record RemoveRoleRequest(
    int RoleId
);
