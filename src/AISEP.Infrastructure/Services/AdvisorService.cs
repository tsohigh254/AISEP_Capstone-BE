using AISEP.Application.Const;
using AISEP.Application.DTOs.Advisor;
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

public class AdvisorService : IAdvisorService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly ILogger<AdvisorService> _logger;
    private readonly ICloudinaryService _cloudinaryService;
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

        var profilePhotoUrl = request.ProfilePhotoURL != null
            ? await _cloudinaryService.UploadImage(request.ProfilePhotoURL, CloudinaryFolderSaving.Profile)
            : null;

        var advisor = new Advisor
        {
            UserID = userId,
            FullName = request.FullName,
            Title = request.Title,
            Bio = request.Bio,
            ProfilePhotoURL = profilePhotoUrl,
            LinkedInURL = request.LinkedInURL,
            MentorshipPhilosophy = request.MentorshipPhilosophy,
            ProfileStatus = ProfileStatus.Approved,
            IsVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        foreach(var industry in request.AdvisorIndustryFocus)
        {
            var industryFocus = new AdvisorIndustryFocus
            {
                AdvisorID = advisor.AdvisorID,
                IndustryFocusID = industry.IndustryId
            };

            advisor.IndustryFocus.Add(industryFocus);
        }

    
        await _db.Advisors.AddAsync(advisor);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("CREATE_ADVISOR_PROFILE", "Advisor", advisor.AdvisorID, null);
        _logger.LogInformation("Advisor profile {AdvisorId} created for user {UserId}", advisor.AdvisorID, userId);

        return ApiResponse<AdvisorMeDto>.SuccessResponse(
            MapToMeDto(advisor, null, Array.Empty<AdvisorIndustryFocus>()));
    }

    public async Task<ApiResponse<AdvisorMeDto>> GetMyProfileAsync(int userId)
    {
        var advisor = await _db.Advisors
            .AsNoTracking()
            .AsSplitQuery()
            .Include(a => a.Availability)
            .Include(a => a.IndustryFocus)
            .FirstOrDefaultAsync(a => a.UserID == userId);

        if (advisor == null)
            return ApiResponse<AdvisorMeDto>.ErrorResponse("ADVISOR_PROFILE_NOT_FOUND",
                "Advisor profile not found. Please create your profile first.");

        return ApiResponse<AdvisorMeDto>.SuccessResponse(
            MapToMeDto(advisor, advisor.Availability, advisor.IndustryFocus));
    }

    public async Task<ApiResponse<AdvisorMeDto>> UpdateProfileAsync(int userId, UpdateAdvisorRequest request)
    {
        var advisor = await _db.Advisors
            .AsSplitQuery()
            .Include(a => a.Availability)
            .Include(a => a.IndustryFocus)
            .FirstOrDefaultAsync(a => a.UserID == userId);

        if (advisor == null)
            return ApiResponse<AdvisorMeDto>.ErrorResponse("ADVISOR_PROFILE_NOT_FOUND",
                "Advisor profile not found.");

        if (request.FullName != null) advisor.FullName = request.FullName;
        if (request.Title != null) advisor.Title = request.Title;
        if (request.Bio != null) advisor.Bio = request.Bio;
        if (request.LinkedInURL != null) advisor.LinkedInURL = request.LinkedInURL;
        if (request.MentorshipPhilosophy != null) advisor.MentorshipPhilosophy = request.MentorshipPhilosophy;
        advisor.UpdatedAt = DateTime.UtcNow;

        if (request.ProfilePhotoURL != null)
        {
            var profilePhotoUrl = await _cloudinaryService.UploadImage(request.ProfilePhotoURL, CloudinaryFolderSaving.Profile);
            if (!string.IsNullOrEmpty(advisor.ProfilePhotoURL))
                await _cloudinaryService.DeleteImage(advisor.ProfilePhotoURL);
            advisor.ProfilePhotoURL = profilePhotoUrl;
        }

        if (request.AdvisorIndustryFocus is { Count: > 0 })
        {
            _db.AdvisorIndustryFocuses.RemoveRange(advisor.IndustryFocus);

            foreach (var industry in request.AdvisorIndustryFocus)
            {
                advisor.IndustryFocus.Add(new AdvisorIndustryFocus
                {
                    AdvisorID = advisor.AdvisorID,
                    IndustryFocusID = industry.IndustryId
                });
            }
        }

        _db.Advisors.Update(advisor);

        await _audit.LogAsync("UPDATE_ADVISOR_PROFILE", "Advisor", advisor.AdvisorID, null);
        _logger.LogInformation("Advisor profile {AdvisorId} updated", advisor.AdvisorID);

        return ApiResponse<AdvisorMeDto>.SuccessResponse(
            MapToMeDto(advisor, advisor.Availability, advisor.IndustryFocus));
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

    public async Task<ApiResponse<PagedResponse<AdvisorSearchItemDto>>> SearchAdvisorsAsync(AdvisorQueryParams advisorQueryParams)
    {

        var query = _db.Advisors
            .AsNoTracking()
            .AsSplitQuery()
            .Include(a => a.IndustryFocus)
            .Include(a => a.Availability)
            .Where(a => a.ProfileStatus == ProfileStatus.Approved || a.ProfileStatus == ProfileStatus.PendingKYC)
            .AsQueryable();

        // Keyword search on FullName
        if (!string.IsNullOrWhiteSpace(advisorQueryParams.Key))
        {
            query = query.Where(q => q.FullName.ToLower().Trim().Contains(advisorQueryParams.Key.ToLower().Trim()) ||
            q.IndustryFocus.Any(i => i.Industry.IndustryName == advisorQueryParams.Key));
        }


        // Order by UpdatedAt desc
        query = query.OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt);

        var items = query.Select(a => new AdvisorSearchItemDto
        {
            AdvisorID = a.AdvisorID,
            FullName  = a.FullName,
            Title = a.Title,
            Bio = TruncateBio(a.Bio, 200),
            ProfilePhotoURL = a.ProfilePhotoURL,
            AverageRating = a.AverageRating,
            ReviewCount = a.ReviewCount,
            CompletedSessions = a.CompletedSessions,
            YearsOfExperience = a.YearsOfExperience,
            IsVerified = a.IsVerified,
            AvailabilityHint = a.Availability != null ? (a.Availability.IsAcceptingNewMentees ? "Available" : "Not available") : string.Empty,
            HourlyRate = a.HourlyRate,
            Expertise = string.IsNullOrEmpty(a.Expertise) ? new List<string>() : a.Expertise.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            DomainTags = string.IsNullOrEmpty(a.DomainTags) ? new List<string>() : a.DomainTags.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            SuitableFor = string.IsNullOrEmpty(a.SuitableFor) ? new List<string>() : a.SuitableFor.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            SupportedDurations = string.IsNullOrEmpty(a.SupportedDurations) ? new List<string>() : a.SupportedDurations.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            Industry = a.IndustryFocus.Select(i => new AdvisorIndustryFocusDto
            {
                IndustryId = i.IndustryID,
                Industry = i.Industry.IndustryName
            }).ToList()
        }).Paging(advisorQueryParams.Page, advisorQueryParams.PageSize);

        return ApiResponse<PagedResponse<AdvisorSearchItemDto>>.SuccessResponse(new PagedResponse<AdvisorSearchItemDto>
        {
            Items = await items.ToListAsync(),
            Paging = new PagingInfo
            {
                Page = advisorQueryParams.Page,
                PageSize = advisorQueryParams.PageSize,
                TotalItems = await query.CountAsync(),
            }
        });
    }

    public async Task<ApiResponse<AdvisorDetailDto>> GetAdvisorDetailAsync(int advisorId)
    {
        var advisor = await _db.Advisors
            .Include(a => a.IndustryFocus)
                .ThenInclude(i => i.Industry)
            .Include(a => a.Availability)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AdvisorID == advisorId);

        if (advisor == null || (advisor.ProfileStatus != ProfileStatus.Approved && advisor.ProfileStatus != ProfileStatus.PendingKYC))
        {
            return ApiResponse<AdvisorDetailDto>.ErrorResponse("NOT_FOUND", "Advisor not found or profile is not active.");
        }

        var dto = new AdvisorDetailDto
        {
            AdvisorID = advisor.AdvisorID,
            FullName  = advisor.FullName,
            Title = advisor.Title,
            ProfilePhotoURL = advisor.ProfilePhotoURL,
            Bio = advisor.Bio,
            AverageRating = advisor.AverageRating,
            ReviewCount = advisor.ReviewCount,
            CompletedSessions = advisor.CompletedSessions,
            YearsOfExperience = advisor.YearsOfExperience,
            IsVerified = advisor.IsVerified,
            AvailabilityHint = advisor.Availability != null ? (advisor.Availability.IsAcceptingNewMentees ? "Available" : "Not available") : string.Empty,
            HourlyRate = advisor.HourlyRate,
            Expertise = string.IsNullOrEmpty(advisor.Expertise) ? new List<string>() : advisor.Expertise.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            DomainTags = string.IsNullOrEmpty(advisor.DomainTags) ? new List<string>() : advisor.DomainTags.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            SuitableFor = string.IsNullOrEmpty(advisor.SuitableFor) ? new List<string>() : advisor.SuitableFor.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            SupportedDurations = string.IsNullOrEmpty(advisor.SupportedDurations) ? new List<string>() : advisor.SupportedDurations.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            Industry = advisor.IndustryFocus.Select(i => new AdvisorIndustryFocusDto
            {
                IndustryId = i.IndustryID,
                Industry = i.Industry.IndustryName
            }).ToList(),

            MentorshipPhilosophy = advisor.MentorshipPhilosophy,
            ExperiencesJson = advisor.ExperiencesJson,
            Skills = string.IsNullOrEmpty(advisor.Skills) ? new List<string>() : advisor.Skills.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
        };

        return ApiResponse<AdvisorDetailDto>.SuccessResponse(dto);
    }
    
    #region helper method
    private static AdvisorMeDto MapToMeDto(
        Advisor a,
        AdvisorAvailability? availability,
        IEnumerable<AdvisorIndustryFocus> industryFocus) => new()
    {
        AdvisorID = a.AdvisorID,
        UserId = a.UserID,
        FullName = a.FullName,
        Title = a.Title,
        Bio = a.Bio,
        ProfilePhotoURL = a.ProfilePhotoURL,
        MentorshipPhilosophy = a.MentorshipPhilosophy,
        LinkedInURL = a.LinkedInURL,
        ProfileStatus = a.ProfileStatus.ToString(),
        TotalMentees = a.TotalMentees,
        TotalSessionHours = a.TotalSessionHours,
        AverageRating = a.AverageRating,
        CreatedAt = a.CreatedAt,
        UpdatedAt = a.UpdatedAt,
        Availability = availability != null ? MapAvailabilityDto(availability) : null,
        IndustryFocus = a.IndustryFocus.Select(i => new AdvisorIndustryFocusDto
        {
            IndustryId = i.IndustryID,
            Industry = i.Industry.IndustryName
        }).ToList()
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
    public async Task<ApiResponse<AdvisorMeDto>> SubmitForApprovalAsync(int userId)
    {
        var advisor = await _db.Advisors
            .Include(a => a.Availability)
            .Include(a => a.IndustryFocus)
            .ThenInclude(i => i.Industry)
            .FirstOrDefaultAsync(a => a.UserID == userId);

        if (advisor == null)
            return ApiResponse<AdvisorMeDto>.ErrorResponse("ADVISOR_PROFILE_NOT_FOUND", "Advisor profile not found.");

        if (advisor.ProfileStatus == ProfileStatus.Pending)
            return ApiResponse<AdvisorMeDto>.ErrorResponse("ALREADY_PENDING", "Profile is already pending approval.");

        // Removed the check that blocked Approved profiles from submitting for KYC.
        // In the new workflow, Approved (normal) profiles can submit for KYC (PendingKYC).

        // Optionally add validation checks here to ensure profile is complete enough to submit
        
        advisor.ProfileStatus = ProfileStatus.PendingKYC;
        advisor.UpdatedAt = DateTime.UtcNow;

        _db.Advisors.Update(advisor);
        await _db.SaveChangesAsync();

        return ApiResponse<AdvisorMeDto>.SuccessResponse(MapToMeDto(advisor, advisor.Availability, advisor.IndustryFocus));
    }

    public async Task<ApiResponse<AdvisorKYCStatusDto>> GetKYCStatusAsync(int userId)
    {
        var advisor = await _db.Advisors
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserID == userId);

        if (advisor == null)
            return ApiResponse<AdvisorKYCStatusDto>.ErrorResponse("ADVISOR_PROFILE_NOT_FOUND", "Advisor profile not found.");

        var dto = new AdvisorKYCStatusDto
        {
            LastUpdated = advisor.UpdatedAt ?? advisor.CreatedAt
        };

        if (advisor.IsVerified)
        {
            dto.WorkflowStatus = "VERIFIED";
            dto.VerificationLabel = "VERIFIED_ADVISOR";
            dto.Explanation = "Chúc mừng! Hồ sơ của bạn đã được xác thực đầy đủ. Huy hiệu VERIFIED ADVISOR đã được kích hoạt trên profile.";
        }
        else if (advisor.ProfileStatus == ProfileStatus.PendingKYC)
        {
            dto.WorkflowStatus = "PENDING_REVIEW";
            dto.Explanation = "Hồ sơ của bạn đang được đội ngũ AISEP xem xét. Thường mất 1–3 ngày làm việc.";
        }
        else if (advisor.ProfileStatus == ProfileStatus.Rejected)
        {
            dto.WorkflowStatus = "VERIFICATION_FAILED";
            dto.Explanation = "Hồ sơ không đáp ứng tiêu chuẩn xác thực. Vui lòng xem lại ghi chú và gửi lại.";
        }
        else
        {
            dto.WorkflowStatus = "NOT_STARTED";
            dto.Explanation = "Chào mừng! Hãy xác thực tài khoản để tăng uy tín của bạn trong hệ sinh thái AISEP.";
        }

        // Add history entry if applicable
        dto.History = new List<AdvisorKYCHistoryDto>();
        if (advisor.ProfileStatus == ProfileStatus.PendingKYC)
        {
            dto.History.Add(new AdvisorKYCHistoryDto 
            { 
                Action = "Gửi hồ sơ xác thực", 
                Date = (advisor.UpdatedAt ?? advisor.CreatedAt).ToString("dd/MM/yyyy HH:mm"), 
                Status = "PENDING_REVIEW" 
            });
        }
        else if (advisor.IsVerified)
        {
            dto.History.Add(new AdvisorKYCHistoryDto 
            { 
                Action = "Xác thực thành công", 
                Date = (advisor.UpdatedAt ?? advisor.CreatedAt).ToString("dd/MM/yyyy HH:mm"), 
                Status = "VERIFIED" 
            });
        }

        return ApiResponse<AdvisorKYCStatusDto>.SuccessResponse(dto);
    }

    #endregion
}
