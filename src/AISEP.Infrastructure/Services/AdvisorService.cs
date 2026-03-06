using AISEP.Application.DTOs.Advisor;
using AISEP.Application.DTOs.Common;
using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Domain.Enums;
using AISEP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AISEP.Infrastructure.Services;

public class AdvisorService : IAdvisorService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly ILogger<AdvisorService> _logger;
    private readonly ICloudinaryService _cloudinaryService;
    private const string Folder = "ProfilePic";
    public AdvisorService(ApplicationDbContext db, IAuditService audit, ILogger<AdvisorService> logger, ICloudinaryService cloudinaryService)
    {
        _db = db;
        _audit = audit;
        _logger = logger;
        _cloudinaryService = cloudinaryService;
    }

    // ================================================================
    // PROFILE
    // ================================================================

    public async Task<ApiResponse<AdvisorMeDto>> CreateProfileAsync(int userId, CreateAdvisorRequest request)
    {
        var exists = await _db.Advisors.AnyAsync(a => a.UserID == userId);
        if (exists)
            return ApiResponse<AdvisorMeDto>.ErrorResponse("ADVISOR_PROFILE_EXISTS",
                "Advisor profile already exists for this user.");

        var profilePhotoUrl = await _cloudinaryService.UploadImage(request.ProfilePhotoURL, Folder);

        var advisor = new Advisor
        {
            UserID = userId,
            FullName = request.FullName,
            Title = request.Title,
            Company = request.Company,
            Bio = request.Bio,
            ProfilePhotoURL = profilePhotoUrl,
            Website = request.Website,
            LinkedInURL = request.LinkedInURL,
            MentorshipPhilosophy = request.MentorshipPhilosophy,
            ProfileStatus = ProfileStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        foreach(var item in request.Items)
        {
            var newItem = new AdvisorExpertise
            {
                AdvisorID = advisor.AdvisorID,
                Category = item.Category,
                SubTopic = item.SubTopic,
                ProficiencyLevel = item.ProficiencyLevel,
                YearsOfExperience = item.YearsOfExperience
            };

            advisor.Expertise.Add(newItem);
        }
    
        await _db.Advisors.AddAsync(advisor);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("CREATE_ADVISOR_PROFILE", "Advisor", advisor.AdvisorID, null);
        _logger.LogInformation("Advisor profile {AdvisorId} created for user {UserId}", advisor.AdvisorID, userId);

        return ApiResponse<AdvisorMeDto>.SuccessResponse(
            MapToMeDto(advisor, null, Array.Empty<AdvisorExpertise>(), Array.Empty<AdvisorIndustryFocus>()));
    }

    public async Task<ApiResponse<AdvisorMeDto>> GetMyProfileAsync(int userId)
    {
        var advisor = await _db.Advisors
            .AsNoTracking()
            .AsSplitQuery()
            .Include(a => a.Availability)
            .Include(a => a.Expertise)
            .Include(a => a.IndustryFocus)
            .FirstOrDefaultAsync(a => a.UserID == userId);

        if (advisor == null)
            return ApiResponse<AdvisorMeDto>.ErrorResponse("ADVISOR_PROFILE_NOT_FOUND",
                "Advisor profile not found. Please create your profile first.");

        return ApiResponse<AdvisorMeDto>.SuccessResponse(
            MapToMeDto(advisor, advisor.Availability, advisor.Expertise, advisor.IndustryFocus));
    }

    public async Task<ApiResponse<AdvisorMeDto>> UpdateProfileAsync(int userId, UpdateAdvisorRequest request)
    {
        var advisor = await _db.Advisors
            .AsSplitQuery()
            .Include(a => a.Availability)
            .Include(a => a.Expertise)
            .Include(a => a.IndustryFocus)
            .FirstOrDefaultAsync(a => a.UserID == userId);

        if (advisor == null)
            return ApiResponse<AdvisorMeDto>.ErrorResponse("ADVISOR_PROFILE_NOT_FOUND",
                "Advisor profile not found.");

        var profilePhotoUrl = await _cloudinaryService.UploadImage(request.ProfilePhotoURL, Folder);

        if (request.FullName != null) advisor.FullName = request.FullName;
        if (request.Title != null) advisor.Title = request.Title;
        if (request.Company != null) advisor.Company = request.Company;
        if (request.Bio != null) advisor.Bio = request.Bio;
        if (request.Website != null) advisor.Website = request.Website;
        if (request.LinkedInURL != null) advisor.LinkedInURL = request.LinkedInURL;
        if (request.MentorshipPhilosophy != null) advisor.MentorshipPhilosophy = request.MentorshipPhilosophy;
        advisor.UpdatedAt = DateTime.UtcNow;

        if (request.ProfilePhotoURL != null)
        {
            await _cloudinaryService.DeleteImage(advisor.ProfilePhotoURL);
            advisor.ProfilePhotoURL = profilePhotoUrl;
        }

        foreach (var item in request.Items)
        {
            var newItem = new AdvisorExpertise
            {
                AdvisorID = advisor.AdvisorID,
                Category = item.Category,
                SubTopic = item.SubTopic,
                ProficiencyLevel = Enum.TryParse<ProficiencyLevel>(item.ProficiencyLevel, true, out var pl) ? pl : null,
                YearsOfExperience = item.YearsOfExperience
            };

            advisor.Expertise.Add(newItem);
        }

        _db.Advisors.Update(advisor);

        var result = newItems.Select(e => new ExpertiseItemDto
        {
            Category = e.Category,
            SubTopic = e.SubTopic,
            ProficiencyLevel = e.ProficiencyLevel?.ToString(),
            YearsOfExperience = e.YearsOfExperience
        }).ToList();

        await _audit.LogAsync("UPDATE_ADVISOR_PROFILE", "Advisor", advisor.AdvisorID, null);
        _logger.LogInformation("Advisor profile {AdvisorId} updated", advisor.AdvisorID);

        return ApiResponse<AdvisorMeDto>.SuccessResponse(
            MapToMeDto(advisor, advisor.Availability, advisor.Expertise, advisor.IndustryFocus));
    }


    // ================================================================
    // AVAILABILITY (upsert one-to-one)
    // ================================================================

    public async Task<ApiResponse<AvailabilityDto>> UpdateAvailabilityAsync(int userId, UpdateAvailabilityRequest request)
    {
        var advisor = await _db.Advisors
            .Include(a => a.Availability)
            .FirstOrDefaultAsync(a => a.UserID == userId);

        if (advisor == null)
            return ApiResponse<AvailabilityDto>.ErrorResponse("ADVISOR_PROFILE_NOT_FOUND",
                "Advisor profile not found.");

        if (advisor.Availability == null)
        {
            // Create new availability record
            advisor.Availability = new AdvisorAvailability
            {
                AdvisorID = advisor.AdvisorID
            };
            _db.AdvisorAvailabilities.Add(advisor.Availability);
        }

        if (request.SessionFormats != null) advisor.Availability.SessionFormats = request.SessionFormats;
        if (request.TypicalSessionDuration.HasValue) advisor.Availability.TypicalSessionDuration = request.TypicalSessionDuration.Value;
        if (request.WeeklyAvailableHours.HasValue) advisor.Availability.WeeklyAvailableHours = request.WeeklyAvailableHours.Value;
        if (request.MaxConcurrentMentees.HasValue) advisor.Availability.MaxConcurrentMentees = request.MaxConcurrentMentees.Value;
        if (request.ResponseTimeCommitment != null) advisor.Availability.ResponseTimeCommitment = request.ResponseTimeCommitment;
        if (request.IsAcceptingNewMentees.HasValue) advisor.Availability.IsAcceptingNewMentees = request.IsAcceptingNewMentees.Value;
        advisor.Availability.UpdatedAt = DateTime.UtcNow;
        advisor.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _audit.LogAsync("UPDATE_ADVISOR_AVAILABILITY", "AdvisorAvailability", advisor.AdvisorID, null);
        _logger.LogInformation("Advisor {AdvisorId} availability updated", advisor.AdvisorID);

        return ApiResponse<AvailabilityDto>.SuccessResponse(MapAvailabilityDto(advisor.Availability));
    }

    // ================================================================
    // SEARCH
    // ================================================================

    public async Task<ApiResponse<PagedResponse<AdvisorSearchItemDto>>> SearchAdvisorsAsync(
        string? q, int? industryId, string? expertise, int page, int pageSize)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        var query = _db.Advisors
            .AsNoTracking()
            .AsSplitQuery()
            .Include(a => a.Expertise)
            .Include(a => a.IndustryFocus)
            .Include(a => a.Availability)
            .AsQueryable();

        // Keyword search on FullName / Title / Bio / Company
        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim().ToLower();
            query = query.Where(a =>
                a.FullName.ToLower().Contains(keyword) ||
                (a.Title != null && a.Title.ToLower().Contains(keyword)) ||
                (a.Bio != null && a.Bio.ToLower().Contains(keyword)) ||
                (a.Company != null && a.Company.ToLower().Contains(keyword)));
        }

        // Filter by industry (look up name from Industries table)
        if (industryId.HasValue)
        {
            var industryName = await _db.Industries
                .AsNoTracking()
                .Where(i => i.IndustryID == industryId.Value)
                .Select(i => i.IndustryName)
                .FirstOrDefaultAsync();

            if (industryName != null)
            {
                query = query.Where(a =>
                    a.IndustryFocus.Any(f => f.Industry == industryName));
            }
        }

        // Filter by expertise keyword
        if (!string.IsNullOrWhiteSpace(expertise))
        {
            var expKeyword = expertise.Trim().ToLower();
            query = query.Where(a =>
                a.Expertise.Any(e =>
                    e.Category.ToLower().Contains(expKeyword) ||
                    (e.SubTopic != null && e.SubTopic.ToLower().Contains(expKeyword))));
        }

        // Order by UpdatedAt desc
        query = query.OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt);

        var totalItems = await query.CountAsync();

        var advisors = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = advisors.Select(a => new AdvisorSearchItemDto
        {
            AdvisorID = a.AdvisorID,
            DisplayName = a.FullName,
            Title = a.Title,
            Company = a.Company,
            BioShort = TruncateBio(a.Bio, 200),
            Website = a.Website,
            AverageRating = a.AverageRating,
            IsAcceptingNewMentees = a.Availability?.IsAcceptingNewMentees ?? false,
            Industries = a.IndustryFocus.Select(f => f.Industry).ToList(),
            Expertise = a.Expertise.Select(e => new ExpertiseItemDto
            {
                Category = e.Category,
                SubTopic = e.SubTopic,
                ProficiencyLevel = e.ProficiencyLevel?.ToString(),
                YearsOfExperience = e.YearsOfExperience
            }).ToList()
        }).ToList();

        return ApiResponse<PagedResponse<AdvisorSearchItemDto>>.SuccessResponse(new PagedResponse<AdvisorSearchItemDto>
        {
            Items = items,
            Paging = new PagingInfo
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            }
        });
    }

    // ================================================================
    // MAPPING
    // ================================================================

    #region helper method
    private static AdvisorMeDto MapToMeDto(
        Advisor a,
        AdvisorAvailability? availability,
        IEnumerable<AdvisorExpertise> expertise,
        IEnumerable<AdvisorIndustryFocus> industryFocus) => new()
    {
        AdvisorID = a.AdvisorID,
        FullName = a.FullName,
        Title = a.Title,
        Company = a.Company,
        Bio = a.Bio,
        ProfilePhotoURL = a.ProfilePhotoURL,
        MentorshipPhilosophy = a.MentorshipPhilosophy,
        LinkedInURL = a.LinkedInURL,
        Website = a.Website,
        ProfileStatus = a.ProfileStatus.ToString(),
        ProfileCompleteness = a.ProfileCompleteness,
        TotalMentees = a.TotalMentees,
        TotalSessionHours = a.TotalSessionHours,
        AverageRating = a.AverageRating,
        CreatedAt = a.CreatedAt,
        UpdatedAt = a.UpdatedAt,
        Expertise = expertise.Select(e => new ExpertiseItemDto
        {
            Category = e.Category,
            SubTopic = e.SubTopic,
            ProficiencyLevel = e.ProficiencyLevel?.ToString(),
            YearsOfExperience = e.YearsOfExperience
        }).ToList(),
        Availability = availability != null ? MapAvailabilityDto(availability) : null,
        IndustryFocus = industryFocus.Select(f => f.Industry).ToList()
    };

    private static AvailabilityDto MapAvailabilityDto(AdvisorAvailability av) => new()
    {
        SessionFormats = av.SessionFormats,
        TypicalSessionDuration = av.TypicalSessionDuration,
        WeeklyAvailableHours = av.WeeklyAvailableHours,
        MaxConcurrentMentees = av.MaxConcurrentMentees,
        ResponseTimeCommitment = av.ResponseTimeCommitment,
        CalendarConnected = av.CalendarConnected,
        IsAcceptingNewMentees = av.IsAcceptingNewMentees,
        UpdatedAt = av.UpdatedAt
    };

    private static string? TruncateBio(string? bio, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(bio)) return null;
        if (bio.Length <= maxLength) return bio;
        return bio[..maxLength] + "…";
    }
    #endregion
}
