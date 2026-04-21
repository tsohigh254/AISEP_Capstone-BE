using AISEP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AISEP.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await SeedRolesAsync(context);
        await SeedPermissionsAsync(context);
        await SeedRolePermissionsAsync(context);
        await SeedIndustriesAsync(context);
        await SeedUsersAsync(context);
        await FixMissingStartupDataAsync(context);
    }

    private static async Task FixMissingStartupDataAsync(ApplicationDbContext context)
    {
        var malformedStartups = await context.Startups
            .Where(s => string.IsNullOrEmpty(s.BusinessCode) || string.IsNullOrEmpty(s.Website) || string.IsNullOrEmpty(s.Description))
            .ToListAsync();

        foreach (var s in malformedStartups)
        {
            if (string.IsNullOrEmpty(s.BusinessCode))
                s.BusinessCode = "TAX-" + Guid.NewGuid().ToString().Substring(0, 6).ToUpper();
                
            if (string.IsNullOrEmpty(s.Description))
                s.Description = "Công ty khởi nghiệp cung cấp giải pháp công nghệ tự động hóa cho các doanh nghiệp, tối ưu hóa quy trình quản lý.";
                
            if (string.IsNullOrEmpty(s.Website))
                s.Website = "https://example-startup.com";
                
            if (string.IsNullOrEmpty(s.ProblemStatement))
                s.ProblemStatement = "Các doanh nghiệp vừa và nhỏ thiếu đi công cụ hiệu quả.";

            if (string.IsNullOrEmpty(s.SolutionSummary))
                s.SolutionSummary = "Xây dựng hệ sinh thái SaaS all-in-one linh hoạt.";

            if (string.IsNullOrEmpty(s.MarketScope))
                s.MarketScope = "Khách hàng B2B nội địa và khu vực Đông Nam Á.";
        }
        
        if (malformedStartups.Any()) 
            await context.SaveChangesAsync();
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

    private static async Task SeedRolePermissionsAsync(ApplicationDbContext context)
    {
        var roles = await context.Roles.ToListAsync();
        var perms = await context.Permissions.ToListAsync();

        if (!roles.Any() || !perms.Any()) return;

        var existing = await context.RolePermissions.ToListAsync();

        // Permission mapping per role based on SRS doc
        var rolePermMap = new Dictionary<string, string[]>
        {
            ["Admin"] = perms.Select(p => p.PermissionName).ToArray(), // All permissions
            ["Staff"] = new[]
            {
                "users.view", "startups.view", "startups.approve",
                "documents.view", "documents.approve",
                "moderation.view", "moderation.action"
            },
            ["Startup"] = new[]
            {
                "startups.create", "startups.view",
                "documents.upload", "documents.view",
                "mentorships.create", "connections.create"
            },
            ["Investor"] = new[]
            {
                "startups.view", "documents.view",
                "connections.create", "connections.manage"
            },
            ["Advisor"] = new[]
            {
                "startups.view", "mentorships.create",
                "mentorships.manage", "connections.create"
            }
        };

        var toAdd = new List<RolePermission>();

        foreach (var (roleName, permNames) in rolePermMap)
        {
            var role = roles.FirstOrDefault(r => r.RoleName == roleName);
            if (role == null) continue;

            foreach (var permName in permNames)
            {
                var perm = perms.FirstOrDefault(p => p.PermissionName == permName);
                if (perm == null) continue;

                if (existing.Any(e => e.RoleID == role.RoleID && e.PermissionID == perm.PermissionID))
                    continue;

                toAdd.Add(new RolePermission
                {
                    RoleID = role.RoleID,
                    PermissionID = perm.PermissionID,
                    AssignedAt = DateTime.UtcNow
                });
            }
        }

        if (toAdd.Any())
        {
            context.RolePermissions.AddRange(toAdd);
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedIndustriesAsync(ApplicationDbContext context)
    {
        // Check if correct MVP industries exist (Fintech with ID=1)
        var hasCorrectData = await context.Industries.AnyAsync(i => i.IndustryID == 1 && i.IndustryName == "FinTech");
        
        if (hasCorrectData) return;

        // Delete old industries data if exists (children first due to FK)
        if (await context.Industries.AnyAsync())
        {
            // Delete sub-industries first (ParentIndustryID != null)
            var subIndustries = await context.Industries.Where(i => i.ParentIndustryID != null).ToListAsync();
            context.Industries.RemoveRange(subIndustries);
            await context.SaveChangesAsync();

            // Then delete parent industries
            var parentIndustries = await context.Industries.Where(i => i.ParentIndustryID == null).ToListAsync();
            context.Industries.RemoveRange(parentIndustries);
            await context.SaveChangesAsync();
        }

        var industries = new List<Industry>
        {
            // ===== Top-level (5 industries) =====
            new() { IndustryID = 1, IndustryName = "FinTech",                    Description = "Financial Technology",               ParentIndustryID = null },
            new() { IndustryID = 2, IndustryName = "E-Commerce",                  Description = "Electronic Commerce",                 ParentIndustryID = null },
            new() { IndustryID = 3, IndustryName = "EdTech",                      Description = "Education Technology",                ParentIndustryID = null },
            new() { IndustryID = 4, IndustryName = "HealthTech / MedTech",        Description = "Healthcare Technology",               ParentIndustryID = null },
            new() { IndustryID = 5, IndustryName = "Agri & FoodTech",             Description = "Agriculture and Food Technology",     ParentIndustryID = null },

            // ===== Fintech sub-industries =====
            new() { IndustryID = 101, IndustryName = "Payments & Digital Wallets",   Description = "E-wallets and digital payments",        ParentIndustryID = 1 },
            new() { IndustryID = 102, IndustryName = "Online Lending & Credit",        Description = "P2P lending, BNPL",                    ParentIndustryID = 1 },
            new() { IndustryID = 103, IndustryName = "Blockchain & Digital Assets",    Description = "Blockchain and crypto-related fintech", ParentIndustryID = 1 },
            new() { IndustryID = 104, IndustryName = "InsurTech",                      Description = "Insurance technology",                  ParentIndustryID = 1 },
            new() { IndustryID = 105, IndustryName = "Personal Finance & Investing", Description = "PFM and investment tools", ParentIndustryID = 1 },

            // ===== E-commerce sub-industries =====
            new() { IndustryID = 201, IndustryName = "B2C Marketplace", Description = "B2C ecommerce marketplaces", ParentIndustryID = 2 },
            new() { IndustryID = 202, IndustryName = "B2B Commerce", Description = "B2B ecommerce platforms", ParentIndustryID = 2 },
            new() { IndustryID = 203, IndustryName = "Social Commerce", Description = "Commerce via social channels", ParentIndustryID = 2 },
            new() { IndustryID = 204, IndustryName = "Fulfillment & Delivery",   Description = "Delivery and logistics services",       ParentIndustryID = 2 },
            new() { IndustryID = 205, IndustryName = "Food & Grocery Delivery",    Description = "Food and grocery ordering/delivery",    ParentIndustryID = 2 },

            // ===== Edtech sub-industries =====
            new() { IndustryID = 301, IndustryName = "Language Learning",         Description = "Language learning platforms",         ParentIndustryID = 3 },
            new() { IndustryID = 302, IndustryName = "K-12 Learning Support", Description = "Apps for K-12 learning support", ParentIndustryID = 3 },
            new() { IndustryID = 303, IndustryName = "MOOCs & Skills Courses",  Description = "MOOC and skills course platforms",    ParentIndustryID = 3 },
            new() { IndustryID = 304, IndustryName = "Coding & STEM Education", Description = "Coding and STEM learning", ParentIndustryID = 3 },
            new() { IndustryID = 305, IndustryName = "Tutor Matching Platforms", Description = "Tutor-student matching", ParentIndustryID = 3 },

            // ===== Health/Medtech sub-industries =====
            new() { IndustryID = 401, IndustryName = "Telehealth", Description = "Remote consultation services", ParentIndustryID = 4 },
            new() { IndustryID = 402, IndustryName = "Appointment & Health Records", Description = "Booking and patient record management", ParentIndustryID = 4 },
            new() { IndustryID = 403, IndustryName = "Online Pharmacy", Description = "Online pharmacy and medicine delivery", ParentIndustryID = 4 },
            new() { IndustryID = 404, IndustryName = "Wearables & Health Tracking", Description = "Wearables and health monitoring", ParentIndustryID = 4 },
            new() { IndustryID = 405, IndustryName = "AI in Diagnosis & Clinical Decision Support", Description = "AI diagnosis and medical analytics", ParentIndustryID = 4 },

            // ===== Agri/Foodtech sub-industries =====
            new() { IndustryID = 501, IndustryName = "Precision Agriculture", Description = "IoT and data-driven farming", ParentIndustryID = 5 },
            new() { IndustryID = 502, IndustryName = "Farm Automation & Robotics", Description = "Automation and agricultural robotics", ParentIndustryID = 5 },
            new() { IndustryID = 503, IndustryName = "Farmer-to-Market Platforms", Description = "Market linkage platforms", ParentIndustryID = 5 },
            new() { IndustryID = 504, IndustryName = "Cold Chain Infrastructure",  Description = "Cold supply chain and logistics",     ParentIndustryID = 5 },
            new() { IndustryID = 505, IndustryName = "Traceability & Food Safety", Description = "Traceability and food safety tech", ParentIndustryID = 5 },

            // ===== SaaS & Enterprise Software sub-industries =====
            new() { IndustryID = 6,   IndustryName = "SaaS & Enterprise Software", Description = "Software-as-a-Service and B2B enterprise tools", ParentIndustryID = null },
            new() { IndustryID = 601, IndustryName = "CRM & Sales Tools",         Description = "Customer relationship management software",     ParentIndustryID = 6 },
            new() { IndustryID = 602, IndustryName = "ERP & Operations",          Description = "Enterprise resource planning and ops tools",    ParentIndustryID = 6 },
            new() { IndustryID = 603, IndustryName = "Marketing Tech",            Description = "Marketing automation and analytics platforms", ParentIndustryID = 6 },
            new() { IndustryID = 604, IndustryName = "Developer Tools & DevOps",  Description = "Tools for software development and CI/CD",     ParentIndustryID = 6 },
            new() { IndustryID = 605, IndustryName = "Data Analytics & BI",       Description = "Business intelligence and data visualization", ParentIndustryID = 6 }
        };

        context.Industries.AddRange(industries);
        await context.SaveChangesAsync();
    }

    private static async Task SeedUsersAsync(ApplicationDbContext context)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("12345.nN");

        await SeedSingleUserAsync(context, "staff@aisep.local", passwordHash, "Staff");
        await SeedSingleUserAsync(context, "admin@aisep.local", passwordHash, "Admin");
        // await SeedSingleUserAsync(context, "startup@aisep.local", passwordHash, "Startup"); // Đã đóng theo yêu cầu để tự test tạo mới
        await SeedSingleUserAsync(context, "advisor@aisep.local", passwordHash, "Advisor");
        await SeedSingleUserAsync(context, "investor@aisep.local", passwordHash, "Investor");

        // Additional fresh accounts for testing Onboarding/KYC
        await SeedSingleUserAsync(context, "investor2@aisep.local", passwordHash, "Investor");
        await SeedSingleUserAsync(context, "startup2@aisep.local", passwordHash, "Startup");
        await SeedSingleUserAsync(context, "advisor2@aisep.local", passwordHash, "Advisor");
    }

    private static async Task SeedSingleUserAsync(ApplicationDbContext context, string email, string passHash, string roleName)
    {
        if (await context.Users.AnyAsync(u => u.Email == email)) return;

        var role = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
        if (role == null) return;

        var user = new User
        {
            Email = email,
            PasswordHash = passHash,
            UserType = roleName,
            IsActive = true,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var userRole = new UserRole
        {
            UserID = user.UserID,
            RoleID = role.RoleID,
            AssignedAt = DateTime.UtcNow
        };

        context.UserRoles.Add(userRole);
        await context.SaveChangesAsync();
    }
}
