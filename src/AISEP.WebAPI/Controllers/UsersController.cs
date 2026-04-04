using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs;
using AISEP.Application.Interfaces;
using AISEP.Infrastructure.Data;
using AISEP.WebAPI.Extensions;
using static AISEP.WebAPI.Extensions.ApiEnvelopeExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AISEP.WebAPI.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public UsersController(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    private int GetCurrentUserId()
    {
        // Try "sub" first (JWT standard), then ClaimTypes.NameIdentifier (ASP.NET mapping)
        var userIdClaim = User.FindFirst("sub")?.Value 
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserID == userId);

        if (user == null)
            return ErrorEnvelope("User not found", StatusCodes.Status404NotFound);

        var response = new UserProfileResponse(
            user.UserID, user.Email, user.UserType, user.IsActive, user.EmailVerified,
            user.CreatedAt, user.LastLoginAt, user.UserRoles.Select(ur => ur.Role.RoleName));

        return OkEnvelope<UserProfileResponse>(response);
    }

    /// <summary>
    /// Update current user profile
    /// </summary>
    [HttpPut("me")]
    public async Task<IActionResult> UpdateCurrentUser([FromBody] UpdateUserProfileRequest request)
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserID == userId);

        if (user == null)
            return ErrorEnvelope("User not found", StatusCodes.Status404NotFound);

        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
        {
            var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email && u.UserID != userId);
            if (emailExists)
                return ErrorEnvelope("Email already in use", StatusCodes.Status409Conflict);
            user.Email = request.Email;
            user.EmailVerified = false;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _auditService.LogAsync("UPDATE_PROFILE", "User", userId, null);

        var response = new UserProfileResponse(
            user.UserID, user.Email, user.UserType, user.IsActive, user.EmailVerified,
            user.CreatedAt, user.LastLoginAt, user.UserRoles.Select(ur => ur.Role.RoleName));

        return OkEnvelope<UserProfileResponse>(response, "Profile updated");
    }

    // Admin endpoints

    /// <summary>
    /// Get all users (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? userType = null,
        [FromQuery] bool? isActive = null)
    {
        var query = _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsQueryable();

        if (!string.IsNullOrEmpty(userType))
            query = query.Where(u => u.UserType == userType);

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        var total = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = users.Select(u => new UserListResponse(
            u.UserID, u.Email, u.UserType, u.IsActive, u.EmailVerified,
            u.CreatedAt, u.LastLoginAt, u.UserRoles.Select(ur => ur.Role.RoleName))).ToList();

        var paged = new PagedResponse<UserListResponse>
        {
            Paging = new PagingInfo
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = total
            },
            Items = items
        };
        return OkEnvelope(paged);
    }

    /// <summary>
    /// Get user by ID (Admin only)
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserID == id);

        if (user == null)
            return ErrorEnvelope("User not found", StatusCodes.Status404NotFound);

        var response = new UserProfileResponse(
            user.UserID, user.Email, user.UserType, user.IsActive, user.EmailVerified,
            user.CreatedAt, user.LastLoginAt, user.UserRoles.Select(ur => ur.Role.RoleName));

        return OkEnvelope<UserProfileResponse>(response);
    }

    /// <summary>
    /// Update user status (lock/unlock) (Admin only)
    /// </summary>
    [HttpPatch("{id}/status")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateUserStatusRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return ErrorEnvelope("User not found", StatusCodes.Status404NotFound);

        var currentUserId = GetCurrentUserId();
        if (id == currentUserId)
            return ErrorEnvelope("Cannot change your own status", StatusCodes.Status400BadRequest);

        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var action = request.IsActive ? "UNLOCK_USER" : "LOCK_USER";
        await _auditService.LogAsync(action, "User", id, null);

        return OkEnvelope<object>(null, request.IsActive ? "User unlocked" : "User locked");
    }

    /// <summary>
    /// Assign role to user (Admin only)
    /// </summary>
    [HttpPost("{id}/roles")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> AssignRole(int id, [FromBody] AssignRoleRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return ErrorEnvelope("User not found", StatusCodes.Status404NotFound);

        var role = await _context.Roles.FindAsync(request.RoleId);
        if (role == null)
            return ErrorEnvelope("Role not found", StatusCodes.Status404NotFound);

        var existingAssignment = await _context.UserRoles
            .AnyAsync(ur => ur.UserID == id && ur.RoleID == request.RoleId);

        if (existingAssignment)
            return ErrorEnvelope("User already has this role", StatusCodes.Status409Conflict);

        var userRole = new Domain.Entities.UserRole
        {
            UserID = id,
            RoleID = request.RoleId,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = GetCurrentUserId()
        };

        _context.UserRoles.Add(userRole);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync("ASSIGN_ROLE", "User", id, $"RoleId: {request.RoleId}");

        return OkEnvelope<object>(null, $"Role '{role.RoleName}' assigned to user");
    }

    /// <summary>
    /// Remove role from user (Admin only)
    /// </summary>
    [HttpDelete("{id}/roles/{roleId}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> RemoveRole(int id, int roleId)
    {
        var userRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserID == id && ur.RoleID == roleId);

        if (userRole == null)
            return ErrorEnvelope("User does not have this role", StatusCodes.Status404NotFound);

        _context.UserRoles.Remove(userRole);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync("REMOVE_ROLE", "User", id, $"RoleId: {roleId}");

        return OkEnvelope<object>(null, "Role removed from user");
    }
}
