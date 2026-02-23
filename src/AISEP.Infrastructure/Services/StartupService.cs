using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Startup;
using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AISEP.Infrastructure.Services;

public class StartupService : IStartupService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ILogger<StartupService> _logger;

    public StartupService(ApplicationDbContext context, IAuditService auditService, ILogger<StartupService> logger)
    {
        _context = context;
        _auditService = auditService;
        _logger = logger;
    }

    // ========== STARTUP PROFILE ==========

    public async Task<ApiResponse<StartupMeDto>> CreateStartupAsync(int userId, CreateStartupRequest request)
    {
        // Check if user already has a startup profile
        var exists = await _context.Startups.AnyAsync(s => s.UserID == userId);
        if (exists)
        {
            return ApiResponse<StartupMeDto>.ErrorResponse("STARTUP_PROFILE_EXISTS",
                "You already have a startup profile. Each user can only create one startup.");
        }

        // Validate industry exists in master data (optional - store as string)
        if (!string.IsNullOrWhiteSpace(request.Industry))
        {
            var industryExists = await _context.Industries
                .AsNoTracking()
                .AnyAsync(i => i.IndustryName == request.Industry);

            if (!industryExists)
            {
                return ApiResponse<StartupMeDto>.ErrorResponse("INVALID_INDUSTRY",
                    $"Industry '{request.Industry}' does not exist in master data.");
            }
        }

        var startup = new Domain.Entities.Startup
        {
            UserID = userId,
            CompanyName = request.CompanyName,
            OneLiner = request.OneLiner,
            Description = request.Description,
            Industry = request.Industry,
            SubIndustry = request.SubIndustry,
            Stage = request.Stage,
            FoundedDate = request.FoundedDate.HasValue
                ? DateTime.SpecifyKind(request.FoundedDate.Value, DateTimeKind.Utc)
                : null,
            TeamSize = request.TeamSize,
            Location = request.Location,
            Country = request.Country,
            Website = request.Website,
            FundingStage = request.FundingStage,
            FundingAmountSought = request.FundingAmountSought,
            CurrentFundingRaised = request.CurrentFundingRaised,
            Valuation = request.Valuation,
            ProfileStatus = "Draft",
            ProfileCompleteness = CalculateProfileCompleteness(request),
            CreatedAt = DateTime.UtcNow
        };

        _context.Startups.Add(startup);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("CREATE_STARTUP", "Startup", startup.StartupID,
            $"CompanyName: {startup.CompanyName}");

        return ApiResponse<StartupMeDto>.SuccessResponse(MapToMeDto(startup), "Startup profile created successfully");
    }

    public async Task<ApiResponse<StartupMeDto>> GetMyStartupAsync(int userId)
    {
        var startup = await _context.Startups
            .AsNoTracking()
            .Include(s => s.TeamMembers)
            .FirstOrDefaultAsync(s => s.UserID == userId);

        if (startup == null)
        {
            return ApiResponse<StartupMeDto>.ErrorResponse("STARTUP_PROFILE_NOT_FOUND",
                "You haven't created a startup profile yet.");
        }

        return ApiResponse<StartupMeDto>.SuccessResponse(MapToMeDto(startup));
    }

    public async Task<ApiResponse<StartupMeDto>> UpdateStartupAsync(int userId, UpdateStartupRequest request)
    {
        var startup = await _context.Startups
            .Include(s => s.TeamMembers)
            .FirstOrDefaultAsync(s => s.UserID == userId);

        if (startup == null)
        {
            return ApiResponse<StartupMeDto>.ErrorResponse("STARTUP_PROFILE_NOT_FOUND",
                "You haven't created a startup profile yet.");
        }

        // Validate industry if provided
        if (request.Industry != null && !string.IsNullOrWhiteSpace(request.Industry))
        {
            var industryExists = await _context.Industries
                .AsNoTracking()
                .AnyAsync(i => i.IndustryName == request.Industry);

            if (!industryExists)
            {
                return ApiResponse<StartupMeDto>.ErrorResponse("INVALID_INDUSTRY",
                    $"Industry '{request.Industry}' does not exist in master data.");
            }
        }

        // Apply partial updates (only non-null fields)
        if (request.CompanyName != null) startup.CompanyName = request.CompanyName;
        if (request.OneLiner != null) startup.OneLiner = request.OneLiner;
        if (request.Description != null) startup.Description = request.Description;
        if (request.Industry != null) startup.Industry = request.Industry;
        if (request.SubIndustry != null) startup.SubIndustry = request.SubIndustry;
        if (request.Stage != null) startup.Stage = request.Stage;
        if (request.FoundedDate.HasValue) startup.FoundedDate = DateTime.SpecifyKind(request.FoundedDate.Value, DateTimeKind.Utc);
        if (request.TeamSize.HasValue) startup.TeamSize = request.TeamSize;
        if (request.Location != null) startup.Location = request.Location;
        if (request.Country != null) startup.Country = request.Country;
        if (request.Website != null) startup.Website = request.Website;
        if (request.LogoURL != null) startup.LogoURL = request.LogoURL;
        if (request.CoverImageURL != null) startup.CoverImageURL = request.CoverImageURL;
        if (request.FundingStage != null) startup.FundingStage = request.FundingStage;
        if (request.FundingAmountSought.HasValue) startup.FundingAmountSought = request.FundingAmountSought;
        if (request.CurrentFundingRaised.HasValue) startup.CurrentFundingRaised = request.CurrentFundingRaised;
        if (request.Valuation.HasValue) startup.Valuation = request.Valuation;

        startup.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("UPDATE_STARTUP", "Startup", startup.StartupID,
            $"Updated fields for {startup.CompanyName}");

        return ApiResponse<StartupMeDto>.SuccessResponse(MapToMeDto(startup), "Startup profile updated successfully");
    }

    public async Task<ApiResponse<StartupMeDto>> SubmitForApprovalAsync(int userId)
    {
        var startup = await _context.Startups
            .Include(s => s.TeamMembers)
            .FirstOrDefaultAsync(s => s.UserID == userId);

        if (startup == null)
        {
            return ApiResponse<StartupMeDto>.ErrorResponse("STARTUP_PROFILE_NOT_FOUND",
                "You haven't created a startup profile yet.");
        }

        if (startup.ProfileStatus == "PendingApproval")
        {
            return ApiResponse<StartupMeDto>.ErrorResponse("ALREADY_PENDING",
                "Your startup profile is already pending approval.");
        }

        if (startup.ProfileStatus == "Approved")
        {
            return ApiResponse<StartupMeDto>.ErrorResponse("ALREADY_APPROVED",
                "Your startup profile is already approved.");
        }

        startup.ProfileStatus = "PendingApproval";
        startup.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("SUBMIT_STARTUP_APPROVAL", "Startup", startup.StartupID,
            $"{startup.CompanyName} submitted for approval");

        return ApiResponse<StartupMeDto>.SuccessResponse(MapToMeDto(startup), "Startup submitted for approval");
    }

    // ========== PUBLIC ENDPOINTS ==========

    public async Task<ApiResponse<StartupPublicDto>> GetStartupByIdAsync(int startupId)
    {
        var startup = await _context.Startups
            .AsNoTracking()
            .Include(s => s.TeamMembers)
            .FirstOrDefaultAsync(s => s.StartupID == startupId);

        if (startup == null)
        {
            return ApiResponse<StartupPublicDto>.ErrorResponse("STARTUP_NOT_FOUND",
                "Startup not found.");
        }

        return ApiResponse<StartupPublicDto>.SuccessResponse(MapToPublicDto(startup));
    }

    public async Task<ApiResponse<PagedResponse<StartupListItemDto>>> SearchStartupsAsync(
        string? keyword, string? industry, string? stage,
        int page, int pageSize)
    {
        // Clamp pageSize
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;
        if (page < 1) page = 1;

        var query = _context.Startups.AsNoTracking().AsQueryable();

        // Keyword search on CompanyName
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var lowerKeyword = keyword.ToLower();
            query = query.Where(s => s.CompanyName.ToLower().Contains(lowerKeyword)
                                  || (s.OneLiner != null && s.OneLiner.ToLower().Contains(lowerKeyword)));
        }

        // Filter by industry
        if (!string.IsNullOrWhiteSpace(industry))
        {
            query = query.Where(s => s.Industry == industry);
        }

        // Filter by stage
        if (!string.IsNullOrWhiteSpace(stage))
        {
            query = query.Where(s => s.Stage == stage);
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var items = await query
            .OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new StartupListItemDto
            {
                StartupID = s.StartupID,
                CompanyName = s.CompanyName,
                OneLiner = s.OneLiner,
                Industry = s.Industry,
                SubIndustry = s.SubIndustry,
                Stage = s.Stage,
                Location = s.Location,
                Country = s.Country,
                LogoURL = s.LogoURL,
                FundingStage = s.FundingStage,
                ProfileStatus = s.ProfileStatus,
                UpdatedAt = s.UpdatedAt
            })
            .ToListAsync();

        var result = new PagedResponse<StartupListItemDto>
        {
            Items = items,
            Paging = new PagingInfo
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            }
        };

        return ApiResponse<PagedResponse<StartupListItemDto>>.SuccessResponse(result);
    }

    // ========== TEAM MEMBERS ==========

    public async Task<ApiResponse<List<TeamMemberDto>>> GetTeamMembersAsync(int userId)
    {
        var startup = await _context.Startups
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserID == userId);

        if (startup == null)
        {
            return ApiResponse<List<TeamMemberDto>>.ErrorResponse("STARTUP_PROFILE_NOT_FOUND",
                "You haven't created a startup profile yet.");
        }

        var members = await _context.TeamMembers
            .AsNoTracking()
            .Where(tm => tm.StartupID == startup.StartupID)
            .OrderBy(tm => tm.CreatedAt)
            .Select(tm => MapToTeamMemberDto(tm))
            .ToListAsync();

        return ApiResponse<List<TeamMemberDto>>.SuccessResponse(members);
    }

    public async Task<ApiResponse<TeamMemberDto>> AddTeamMemberAsync(int userId, CreateTeamMemberRequest request)
    {
        var startup = await _context.Startups.FirstOrDefaultAsync(s => s.UserID == userId);

        if (startup == null)
        {
            return ApiResponse<TeamMemberDto>.ErrorResponse("STARTUP_PROFILE_NOT_FOUND",
                "You haven't created a startup profile yet.");
        }

        var member = new TeamMember
        {
            StartupID = startup.StartupID,
            FullName = request.FullName,
            Role = request.Role,
            Title = request.Title,
            LinkedInURL = request.LinkedInURL,
            Bio = request.Bio,
            PhotoURL = request.PhotoURL,
            IsFounder = request.IsFounder,
            YearsOfExperience = request.YearsOfExperience,
            CreatedAt = DateTime.UtcNow
        };

        _context.TeamMembers.Add(member);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("CREATE_TEAM_MEMBER", "TeamMember", member.TeamMemberID,
            $"Added {member.FullName} to startup {startup.StartupID}");

        return ApiResponse<TeamMemberDto>.SuccessResponse(MapToTeamMemberDto(member), "Team member added");
    }

    public async Task<ApiResponse<TeamMemberDto>> UpdateTeamMemberAsync(int userId, int teamMemberId, UpdateTeamMemberRequest request)
    {
        var startup = await _context.Startups.AsNoTracking().FirstOrDefaultAsync(s => s.UserID == userId);
        if (startup == null)
        {
            return ApiResponse<TeamMemberDto>.ErrorResponse("STARTUP_PROFILE_NOT_FOUND",
                "You haven't created a startup profile yet.");
        }

        var member = await _context.TeamMembers.FirstOrDefaultAsync(
            tm => tm.TeamMemberID == teamMemberId && tm.StartupID == startup.StartupID);

        if (member == null)
        {
            return ApiResponse<TeamMemberDto>.ErrorResponse("TEAM_MEMBER_NOT_FOUND",
                "Team member not found or does not belong to your startup.");
        }

        // Apply partial updates
        if (request.FullName != null) member.FullName = request.FullName;
        if (request.Role != null) member.Role = request.Role;
        if (request.Title != null) member.Title = request.Title;
        if (request.LinkedInURL != null) member.LinkedInURL = request.LinkedInURL;
        if (request.Bio != null) member.Bio = request.Bio;
        if (request.PhotoURL != null) member.PhotoURL = request.PhotoURL;
        if (request.IsFounder.HasValue) member.IsFounder = request.IsFounder.Value;
        if (request.YearsOfExperience.HasValue) member.YearsOfExperience = request.YearsOfExperience;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("UPDATE_TEAM_MEMBER", "TeamMember", member.TeamMemberID,
            $"Updated {member.FullName} in startup {startup.StartupID}");

        return ApiResponse<TeamMemberDto>.SuccessResponse(MapToTeamMemberDto(member), "Team member updated");
    }

    public async Task<ApiResponse<string>> DeleteTeamMemberAsync(int userId, int teamMemberId)
    {
        var startup = await _context.Startups.AsNoTracking().FirstOrDefaultAsync(s => s.UserID == userId);
        if (startup == null)
        {
            return ApiResponse<string>.ErrorResponse("STARTUP_PROFILE_NOT_FOUND",
                "You haven't created a startup profile yet.");
        }

        var member = await _context.TeamMembers.FirstOrDefaultAsync(
            tm => tm.TeamMemberID == teamMemberId && tm.StartupID == startup.StartupID);

        if (member == null)
        {
            return ApiResponse<string>.ErrorResponse("TEAM_MEMBER_NOT_FOUND",
                "Team member not found or does not belong to your startup.");
        }

        _context.TeamMembers.Remove(member);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("DELETE_TEAM_MEMBER", "TeamMember", teamMemberId,
            $"Removed {member.FullName} from startup {startup.StartupID}");

        return ApiResponse<string>.SuccessResponse("Team member deleted", "Team member removed successfully");
    }

    // ========== MAPPING HELPERS ==========

    private static StartupMeDto MapToMeDto(Domain.Entities.Startup s)
    {
        return new StartupMeDto
        {
            StartupID = s.StartupID,
            UserID = s.UserID,
            CompanyName = s.CompanyName,
            OneLiner = s.OneLiner,
            Description = s.Description,
            Industry = s.Industry,
            SubIndustry = s.SubIndustry,
            Stage = s.Stage,
            FoundedDate = s.FoundedDate,
            TeamSize = s.TeamSize,
            Location = s.Location,
            Country = s.Country,
            Website = s.Website,
            LogoURL = s.LogoURL,
            CoverImageURL = s.CoverImageURL,
            FundingStage = s.FundingStage,
            FundingAmountSought = s.FundingAmountSought,
            CurrentFundingRaised = s.CurrentFundingRaised,
            Valuation = s.Valuation,
            ProfileStatus = s.ProfileStatus,
            ProfileCompleteness = s.ProfileCompleteness,
            ApprovedAt = s.ApprovedAt,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt,
            TeamMembers = s.TeamMembers?.Select(MapToTeamMemberDto).ToList() ?? new()
        };
    }

    private static StartupPublicDto MapToPublicDto(Domain.Entities.Startup s)
    {
        return new StartupPublicDto
        {
            StartupID = s.StartupID,
            CompanyName = s.CompanyName,
            OneLiner = s.OneLiner,
            Description = s.Description,
            Industry = s.Industry,
            SubIndustry = s.SubIndustry,
            Stage = s.Stage,
            FoundedDate = s.FoundedDate,
            TeamSize = s.TeamSize,
            Location = s.Location,
            Country = s.Country,
            Website = s.Website,
            LogoURL = s.LogoURL,
            CoverImageURL = s.CoverImageURL,
            FundingStage = s.FundingStage,
            FundingAmountSought = s.FundingAmountSought,
            CurrentFundingRaised = s.CurrentFundingRaised,
            ProfileStatus = s.ProfileStatus,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt,
            TeamMembers = s.TeamMembers?.Select(tm => new TeamMemberPublicDto
            {
                FullName = tm.FullName,
                Role = tm.Role,
                Title = tm.Title,
                LinkedInURL = tm.LinkedInURL,
                Bio = tm.Bio,
                PhotoURL = tm.PhotoURL,
                IsFounder = tm.IsFounder
            }).ToList() ?? new()
        };
    }

    private static TeamMemberDto MapToTeamMemberDto(TeamMember tm)
    {
        return new TeamMemberDto
        {
            TeamMemberID = tm.TeamMemberID,
            FullName = tm.FullName,
            Role = tm.Role,
            Title = tm.Title,
            LinkedInURL = tm.LinkedInURL,
            Bio = tm.Bio,
            PhotoURL = tm.PhotoURL,
            IsFounder = tm.IsFounder,
            YearsOfExperience = tm.YearsOfExperience,
            CreatedAt = tm.CreatedAt
        };
    }

    private static int CalculateProfileCompleteness(CreateStartupRequest r)
    {
        var fields = new object?[]
        {
            r.CompanyName, r.OneLiner, r.Description, r.Industry,
            r.Stage, r.Location, r.Country, r.Website,
            r.FoundedDate, r.TeamSize, r.FundingStage
        };
        var filled = fields.Count(f => f != null && f.ToString() != string.Empty);
        return (int)Math.Round(filled * 100.0 / fields.Length);
    }
}
