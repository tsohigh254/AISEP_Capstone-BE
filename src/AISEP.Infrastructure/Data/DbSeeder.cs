using AISEP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AISEP.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await SeedRolesAsync(context);
        await SeedPermissionsAsync(context);
    }

    private static async Task SeedRolesAsync(ApplicationDbContext context)
    {
        if (await context.Roles.AnyAsync()) return;

        var roles = new List<Role>
        {
            new() { RoleName = "Startup", Description = "Startup company user", CreatedAt = DateTime.UtcNow },
            new() { RoleName = "Investor", Description = "Investor user", CreatedAt = DateTime.UtcNow },
            new() { RoleName = "Advisor", Description = "Advisor/Mentor user", CreatedAt = DateTime.UtcNow },
            new() { RoleName = "Staff", Description = "Platform staff/operations", CreatedAt = DateTime.UtcNow },
            new() { RoleName = "Admin", Description = "Platform administrator", CreatedAt = DateTime.UtcNow }
        };

        context.Roles.AddRange(roles);
        await context.SaveChangesAsync();
    }

    private static async Task SeedPermissionsAsync(ApplicationDbContext context)
    {
        if (await context.Permissions.AnyAsync()) return;

        var permissions = new List<Permission>
        {
            // User permissions
            new() { PermissionName = "users.view", Description = "View users", Category = "Users" },
            new() { PermissionName = "users.manage", Description = "Manage users", Category = "Users" },
            new() { PermissionName = "users.lock", Description = "Lock/unlock users", Category = "Users" },
            
            // Startup permissions
            new() { PermissionName = "startups.create", Description = "Create startup profile", Category = "Startups" },
            new() { PermissionName = "startups.view", Description = "View startups", Category = "Startups" },
            new() { PermissionName = "startups.approve", Description = "Approve startups", Category = "Startups" },
            
            // Document permissions
            new() { PermissionName = "documents.upload", Description = "Upload documents", Category = "Documents" },
            new() { PermissionName = "documents.view", Description = "View documents", Category = "Documents" },
            new() { PermissionName = "documents.approve", Description = "Approve documents", Category = "Documents" },
            
            // Mentorship permissions
            new() { PermissionName = "mentorships.create", Description = "Create mentorship", Category = "Mentorships" },
            new() { PermissionName = "mentorships.manage", Description = "Manage mentorships", Category = "Mentorships" },
            
            // Connection permissions
            new() { PermissionName = "connections.create", Description = "Create connections", Category = "Connections" },
            new() { PermissionName = "connections.manage", Description = "Manage connections", Category = "Connections" },
            
            // Moderation permissions
            new() { PermissionName = "moderation.view", Description = "View flagged content", Category = "Moderation" },
            new() { PermissionName = "moderation.action", Description = "Take moderation actions", Category = "Moderation" },
            
            // Admin permissions
            new() { PermissionName = "admin.audit", Description = "View audit logs", Category = "Admin" },
            new() { PermissionName = "admin.config", Description = "Manage system configuration", Category = "Admin" },
            new() { PermissionName = "admin.roles", Description = "Manage roles and permissions", Category = "Admin" }
        };

        context.Permissions.AddRange(permissions);
        await context.SaveChangesAsync();
    }
}
