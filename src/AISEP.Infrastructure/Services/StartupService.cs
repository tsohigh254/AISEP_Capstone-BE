using AISEP.Application.Const;
using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Investor;
using AISEP.Application.DTOs.Startup;
using AISEP.Application.Extensions;
using AISEP.Application.Interfaces;
using AISEP.Application.QueryParams;
using AISEP.Domain.Entities;
using AISEP.Domain.Enums;
using AISEP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AISEP.Infrastructure.Services;

public class StartupService : IStartupService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ILogger<StartupService> _logger;
    private readonly ICloudinaryService _cloudinaryService;

    public StartupService(ApplicationDbContext context, IAuditService auditService, ILogger<StartupService> logger, ICloudinaryService cloudinaryService)
    {
        _context = context;
        _auditService = auditService;
        _logger = logger;
        _cloudinaryService = cloudinaryService;
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

        // Validate industry exists in master data
        if (request.IndustryID.HasValue)
        {
            var industryExists = await _context.Industries
                .AsNoTracking()
                .AnyAsync(i => i.IndustryID == request.IndustryID.Value);

            if (!industryExists)
            {
                return ApiResponse<StartupMeDto>.ErrorResponse("INVALID_INDUSTRY",
                    $"Industry with ID {request.IndustryID} does not exist in master data.");
            }
        }

        var startup = new Startup
        {
            UserID = userId,
            CompanyName = request.CompanyName,
            OneLiner = request.OneLiner,
            Description = request.Description,
            IndustryID = request.IndustryID,
            Stage = request.Stage,
            FoundedDate = request.FoundedDate.HasValue
                ? DateTime.SpecifyKind(request.FoundedDate.Value, DateTimeKind.Utc)
                : null,
            Website = request.Website,
            FundingAmountSought = request.FundingAmountSought,
            CurrentFundingRaised = request.CurrentFundingRaised,
            Valuation = request.Valuation,

            SubIndustry = request.SubIndustry,
            MarketScope = request.MarketScope,
            ProductStatus = request.ProductStatus,
            Location = request.Location,
            Country = request.Country,
            ProblemStatement = request.ProblemStatement,
            SolutionSummary = request.SolutionSummary,
            CurrentNeeds = request.CurrentNeeds != null ? string.Join(",", request.CurrentNeeds) : null,
            MetricSummary = request.MetricSummary,
            LinkedInURL = request.LinkedInURL,
            ContactEmail = request.ContactEmail,
            ContactPhone = request.ContactPhone,
            TeamSize = request.TeamSize ?? 0,

            ProfileStatus = ProfileStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        var logoUrl = request.LogoUrl != null
            ? await _cloudinaryService.UploadImage(request.LogoUrl, CloudinaryFolderSaving.Logo)
            : null;

        startup.LogoURL = logoUrl;

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
            .Include(s => s.Industry)
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
        if (request.IndustryID.HasValue)
        {
            var industryExists = await _context.Industries
                .AsNoTracking()
                .AnyAsync(i => i.IndustryID == request.IndustryID.Value);

            if (!industryExists)
            {
                return ApiResponse<StartupMeDto>.ErrorResponse("INVALID_INDUSTRY",
                    $"Industry with ID {request.IndustryID} does not exist in master data.");
            }
        }

        // Apply partial updates (only non-null fields)
        if (request.CompanyName != null) startup.CompanyName = request.CompanyName;
        if (request.Description != null) startup.Description = request.Description;
        if (request.IndustryID.HasValue) startup.IndustryID = request.IndustryID;
        if (request.Stage != null) startup.Stage = request.Stage;
        if (request.OneLiner != null) startup.OneLiner = request.OneLiner;
        if (request.FoundedDate.HasValue) startup.FoundedDate = DateTime.SpecifyKind(request.FoundedDate.Value, DateTimeKind.Utc);
        if (request.Website != null) startup.Website = request.Website;
        if (request.FundingAmountSought.HasValue) startup.FundingAmountSought = request.FundingAmountSought;
        if (request.CurrentFundingRaised.HasValue) startup.CurrentFundingRaised = request.CurrentFundingRaised;
        if (request.Valuation.HasValue) startup.Valuation = request.Valuation;
        
        if (request.SubIndustry != null) startup.SubIndustry = request.SubIndustry;
        if (request.MarketScope != null) startup.MarketScope = request.MarketScope;
        if (request.ProductStatus != null) startup.ProductStatus = request.ProductStatus;
        if (request.Location != null) startup.Location = request.Location;
        if (request.Country != null) startup.Country = request.Country;
        if (request.ProblemStatement != null) startup.ProblemStatement = request.ProblemStatement;
        if (request.SolutionSummary != null) startup.SolutionSummary = request.SolutionSummary;
        if (request.CurrentNeeds != null) startup.CurrentNeeds = string.Join(",", request.CurrentNeeds);
        if (request.MetricSummary != null) startup.MetricSummary = request.MetricSummary;
        if (request.LinkedInURL != null) startup.LinkedInURL = request.LinkedInURL;
        if (request.ContactEmail != null) startup.ContactEmail = request.ContactEmail;
        if (request.ContactPhone != null) startup.ContactPhone = request.ContactPhone;
        if (request.TeamSize.HasValue) startup.TeamSize = request.TeamSize.Value;

        if (request.LogoUrl != null)
        {
            var logoUrl = await _cloudinaryService.UploadImage(request.LogoUrl, CloudinaryFolderSaving.Logo);
            if (!string.IsNullOrEmpty(startup.LogoURL))
                await _cloudinaryService.DeleteImage(startup.LogoURL);
            startup.LogoURL = logoUrl;
        }

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

        if (startup.ProfileStatus == ProfileStatus.Pending)
        {
            return ApiResponse<StartupMeDto>.ErrorResponse("ALREADY_PENDING",
                "Your startup profile is already pending approval.");
        }

        if (startup.ProfileStatus == ProfileStatus.Approved)
        {
            return ApiResponse<StartupMeDto>.ErrorResponse("ALREADY_APPROVED",
                "Your startup profile is already approved.");
        }

        startup.ProfileStatus = ProfileStatus.Pending;
        startup.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("SUBMIT_STARTUP_APPROVAL", "Startup", startup.StartupID,
            $"{startup.CompanyName} submitted for approval");

        return ApiResponse<StartupMeDto>.SuccessResponse(MapToMeDto(startup), "Startup submitted for approval");
    }

    public async Task<ApiResponse<string>> ToggleVisibilityAsync(int userId, bool isVisible)
    {
        var startup = await _context.Startups
            .FirstOrDefaultAsync(s => s.UserID == userId);

        if (startup == null)
        {
            return ApiResponse<string>.ErrorResponse("STARTUP_PROFILE_NOT_FOUND",
                "You haven't created a startup profile yet.");
        }

        startup.IsVisible = isVisible;
        startup.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        string action = isVisible ? "enabled" : "disabled";
        await _auditService.LogAsync("TOGGLE_VISIBILITY", "Startup", startup.StartupID,
            $"Startup visibility {action}");

        return ApiResponse<string>.SuccessResponse($"Visibility {action}", $"Your profile is now {(isVisible ? "visible" : "hidden")} to investors.");
    }

    // ========== PUBLIC ENDPOINTS ==========

    public async Task<ApiResponse<StartupPublicDto>> GetStartupByIdAsync(int startupId)
    {
        var startup = await _context.Startups
            .AsNoTracking()
            .Include(s => s.TeamMembers)
            .Include(s => s.Industry)
            .FirstOrDefaultAsync(s => s.StartupID == startupId);

        if (startup == null)
        {
            return ApiResponse<StartupPublicDto>.ErrorResponse("STARTUP_NOT_FOUND",
                "Startup not found.");
        }

        return ApiResponse<StartupPublicDto>.SuccessResponse(MapToPublicDto(startup));
    }

    public async Task<ApiResponse<PagedResponse<StartupListItemDto>>> SearchStartupsAsync(StartupQueryParams startupQuery)
    {

        var query = _context.Startups.AsNoTracking().AsQueryable();

        // Keyword search on CompanyName
        if (!string.IsNullOrWhiteSpace(startupQuery.Key))
        {
            query = query.Where(s => s.CompanyName.Trim().ToLower().Contains(startupQuery.Key.Trim().ToLower())
            || s.Industry.IndustryName.Trim().ToLower().Contains(startupQuery.Key.Trim().ToLower()));
        }


        // Filter by stage
        if (startupQuery.Stage.HasValue)
        {
            query = query.Where(s => s.Stage == startupQuery.Stage.Value);
        }


        var items = query
            .OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt)
            .Select(s => new StartupListItemDto
            {
                StartupID = s.StartupID,
                CompanyName = s.CompanyName,
                IndustryName = s.Industry != null ? s.Industry.IndustryName : null,
                Stage = s.Stage != null ? s.Stage.ToString() : null,
                LogoURL = s.LogoURL,
                ProfileStatus = s.ProfileStatus.ToString(),
                UpdatedAt = s.UpdatedAt
            }).Paging(startupQuery.Page, startupQuery.PageSize);

        var result = new PagedResponse<StartupListItemDto>
        {
            Items = await items.ToListAsync(),
            Paging = new PagingInfo
            {
                Page = startupQuery.Page,
                PageSize = startupQuery.PageSize,
                TotalItems = await query.CountAsync(),
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
            IsFounder = request.IsFounder,
            YearsOfExperience = request.YearsOfExperience,
            CreatedAt = DateTime.UtcNow
        };

        var photo = request.PhotoURL != null
            ? await _cloudinaryService.UploadImage(request.PhotoURL, CloudinaryFolderSaving.Member)
            : null;

        member.PhotoURL = photo;

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
        if (request.IsFounder.HasValue) member.IsFounder = request.IsFounder.Value;
        if (request.YearsOfExperience.HasValue) member.YearsOfExperience = request.YearsOfExperience;

        if (request.PhotoURL != null)
        {
            var photo = await _cloudinaryService.UploadImage(request.PhotoURL, CloudinaryFolderSaving.Logo);
            if (!string.IsNullOrEmpty(startup.LogoURL))
                await _cloudinaryService.DeleteImage(member.PhotoURL);
            member.PhotoURL = photo;
        }

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

    private static int CalculateProfileCompleteness(Startup s)
    {
        int totalFields = 16;
        int filledFields = 0;

        if (!string.IsNullOrEmpty(s.CompanyName)) filledFields++;
        if (!string.IsNullOrEmpty(s.OneLiner)) filledFields++;
        if (!string.IsNullOrEmpty(s.Description)) filledFields++;
        if (s.IndustryID.HasValue) filledFields++;
        if (s.Stage.HasValue) filledFields++;
        if (s.FoundedDate.HasValue) filledFields++;
        if (!string.IsNullOrEmpty(s.Website)) filledFields++;
        if (!string.IsNullOrEmpty(s.LogoURL)) filledFields++;
        if (!string.IsNullOrEmpty(s.SubIndustry)) filledFields++;
        if (!string.IsNullOrEmpty(s.MarketScope)) filledFields++;
        if (!string.IsNullOrEmpty(s.ProductStatus)) filledFields++;
        if (!string.IsNullOrEmpty(s.ProblemStatement)) filledFields++;
        if (!string.IsNullOrEmpty(s.SolutionSummary)) filledFields++;
        if (!string.IsNullOrEmpty(s.ContactEmail)) filledFields++;
        if (!string.IsNullOrEmpty(s.CurrentNeeds)) filledFields++;
        if (s.TeamMembers != null && s.TeamMembers.Any()) filledFields++;

        return (int)Math.Round((double)filledFields / totalFields * 100);
    }

    private static StartupMeDto MapToMeDto(Startup s)
    {
        return new StartupMeDto
        {
            StartupID = s.StartupID,
            UserID = s.UserID,
            CompanyName = s.CompanyName,
            OneLiner = s.OneLiner,
            Description = s.Description,
            IndustryID = s.IndustryID,
            IndustryName = s.Industry?.IndustryName,
            Stage = s.Stage?.ToString(),
            FoundedDate = s.FoundedDate,
            Website = s.Website,
            LogoURL = s.LogoURL,
            FundingAmountSought = s.FundingAmountSought,
            CurrentFundingRaised = s.CurrentFundingRaised,
            Valuation = s.Valuation,
            
            SubIndustry = s.SubIndustry,
            MarketScope = s.MarketScope,
            ProductStatus = s.ProductStatus,
            Location = s.Location,
            Country = s.Country,
            ProblemStatement = s.ProblemStatement,
            SolutionSummary = s.SolutionSummary,
            CurrentNeeds = string.IsNullOrEmpty(s.CurrentNeeds) 
                ? new List<string>() 
                : s.CurrentNeeds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(n => n.Trim()).ToList(),
            MetricSummary = s.MetricSummary,
            LinkedInURL = s.LinkedInURL,
            ContactEmail = s.ContactEmail,
            ContactPhone = s.ContactPhone,
            VisibilityStatus = s.IsVisible ? "Visible" : "Hidden",
            ValidationStatus = s.ProfileStatus.ToString() == "Approved" ? "Validated" : (s.ProfileStatus.ToString() == "Pending" ? "In Progress" : "Unverified"),
            ProfileCompleteness = CalculateProfileCompleteness(s),
            TeamSize = s.TeamSize > 0 ? s.TeamSize : (s.TeamMembers?.Count ?? 0),

            ProfileStatus = s.ProfileStatus.ToString(),
            ApprovedAt = s.ApprovedAt,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt,
            TeamMembers = s.TeamMembers?.Select(MapToTeamMemberDto).ToList() ?? new()
        };
    }

    private static StartupPublicDto MapToPublicDto(Startup s)
    {
        return new StartupPublicDto
        {
            StartupID = s.StartupID,
            CompanyName = s.CompanyName,
            OneLiner = s.OneLiner,
            Description = s.Description,
            IndustryID = s.IndustryID,
            IndustryName = s.Industry?.IndustryName,
            Stage = s.Stage?.ToString(),
            FoundedDate = s.FoundedDate,
            Website = s.Website,
            LogoURL = s.LogoURL,
            FundingAmountSought = s.FundingAmountSought,
            CurrentFundingRaised = s.CurrentFundingRaised,

            SubIndustry = s.SubIndustry,
            MarketScope = s.MarketScope,
            ProductStatus = s.ProductStatus,
            Location = s.Location,
            Country = s.Country,
            ProblemStatement = s.ProblemStatement,
            SolutionSummary = s.SolutionSummary,
            CurrentNeeds = string.IsNullOrEmpty(s.CurrentNeeds) 
                ? new List<string>() 
                : s.CurrentNeeds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(n => n.Trim()).ToList(),
            MetricSummary = s.MetricSummary,
            LinkedInURL = s.LinkedInURL,
            ContactEmail = s.ContactEmail,
            ContactPhone = s.ContactPhone,
            TeamSize = s.TeamSize > 0 ? s.TeamSize : (s.TeamMembers?.Count ?? 0),

            ProfileStatus = s.ProfileStatus.ToString(),
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

    // ========== BROWSE INVESTORS (Startup role) ==========

    public async Task<ApiResponse<PagedResponse<InvestorSearchItemDto>>> SearchInvestorsAsync(InvestorQueryParams investorQuery)
    {
        var query = _context.Investors
            .AsNoTracking()
            .Include(i => i.Preferences)
            .Include(i => i.StageFocus)
            .Include(i => i.IndustryFocus)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(investorQuery.Key))
        {
            var kw = investorQuery.Key.Trim().ToLower();
            query = query.Where(i =>
                i.FullName.Trim().ToLower().Contains(kw) ||
                (i.FirmName != null && i.FirmName.ToLower().Contains(kw)));
        }

        query = query.OrderByDescending(i => i.UpdatedAt ?? i.CreatedAt);

        var items = query.Select(i => new InvestorSearchItemDto
        {
            InvestorID = i.InvestorID,
            FullName = i.FullName,
            FirmName = i.FirmName,
            Title = i.Title,
            Bio = i.Bio,
            ProfilePhotoURL = i.ProfilePhotoURL,
            Location = i.Location,
            Country = i.Country,
            LinkedInURL = i.LinkedInURL,
            Website = i.Website,
            PreferredIndustries = i.IndustryFocus.Select(f => f.Industry).ToList(),
            //PreferredGeographies = i.Preferences?.PreferredGeographies,
            //TicketSizeMin = i.Preferences?.MinInvestmentSize,
            //TicketSizeMax = i.Preferences?.MaxInvestmentSize,
            UpdatedAt = i.UpdatedAt
        }).Paging(investorQuery.Page, investorQuery.PageSize);


        return ApiResponse<PagedResponse<InvestorSearchItemDto>>.SuccessResponse(new PagedResponse<InvestorSearchItemDto>
        {
            Items = await items.ToListAsync(),
            Paging = new PagingInfo
            {
                Page = investorQuery.Page,
                PageSize = investorQuery.PageSize,
                TotalItems = await query.CountAsync()
            }
        });
    }

    public async Task<ApiResponse<InvestorDto>> GetInvestorByIdAsync(int investorId)
    {
        var investor = await _context.Investors
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.InvestorID == investorId);

        if (investor == null)
            return ApiResponse<InvestorDto>.ErrorResponse("INVESTOR_NOT_FOUND", "Investor not found.");

        return ApiResponse<InvestorDto>.SuccessResponse(new InvestorDto
        {
            InvestorID = investor.InvestorID,
            FullName = investor.FullName,
            FirmName = investor.FirmName,
            Title = investor.Title,
            Bio = investor.Bio,
            ProfilePhotoURL = investor.ProfilePhotoURL,
            InvestmentThesis = investor.InvestmentThesis,
            Location = investor.Location,
            Country = investor.Country,
            LinkedInURL = investor.LinkedInURL,
            Website = investor.Website,
            CreatedAt = investor.CreatedAt,
            UpdatedAt = investor.UpdatedAt
        });
    }
}
