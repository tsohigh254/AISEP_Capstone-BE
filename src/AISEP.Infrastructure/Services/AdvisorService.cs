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

        try
        {
            var advisor = new Advisor
            {
                UserID = userId,
                FullName = request.FullName,
                Title = request.Title,
                Company = request.Company,
                Bio = request.Bio,
                ProfilePhotoURL = profilePhotoUrl,
                LinkedInURL = request.LinkedInURL,
                GoogleMeetLink = request.GoogleMeetLink,
                MsTeamsLink = request.MsTeamsLink,
                Website = request.Website,
                MentorshipPhilosophy = request.MentorshipPhilosophy,
                ProfileStatus = ProfileStatus.Approved,
                IsVerified = false,
                YearsOfExperience = request.YearsOfExperience,
                HourlyRate = request.HourlyRate,
                Expertise = request.Expertise,
                DomainTags = request.DomainTags,
                SuitableFor = request.SuitableFor,
                SupportedDurations = request.SupportedDurations,
                ExperiencesJson = request.ExperiencesJson,
                Skills = request.Skills,
                CreatedAt = DateTime.UtcNow
            };

            if (request.AdvisorIndustryFocus != null && request.AdvisorIndustryFocus.Count > 0)
            {
                foreach (var industry in request.AdvisorIndustryFocus)
                {
                    var industryFocus = new AdvisorIndustryFocus
                    {
                        AdvisorID = advisor.AdvisorID,
                        IndustryFocusID = industry.IndustryId
                    };
                    advisor.IndustryFocus.Add(industryFocus);
                }
            }

            var wallet = new AdvisorWallet
            {
                Advisor = advisor,
                CreatedAt = DateTime.UtcNow
            };

            await _db.AdvisorWallets.AddAsync(wallet);
            await _db.Advisors.AddAsync(advisor);
            await _db.SaveChangesAsync();

            await _audit.LogAsync("CREATE_ADVISOR_PROFILE", "Advisor", advisor.AdvisorID, null);
            _logger.LogInformation("Advisor profile {AdvisorId} created for user {UserId}", advisor.AdvisorID, userId);

            return ApiResponse<AdvisorMeDto>.SuccessResponse(
                MapToMeDto(advisor, null, advisor.IndustryFocus));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating advisor profile for user {UserId}", userId);
            return ApiResponse<AdvisorMeDto>.ErrorResponse("CREATE_PROFILE_ERROR",
                $"Failed to create advisor profile: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    public async Task<ApiResponse<AdvisorMeDto>> GetMyProfileAsync(int userId)
    {
        var advisor = await _db.Advisors
            .AsNoTracking()
            .AsSplitQuery()
            .Include(a => a.Availability)
            .Include(a => a.IndustryFocus)
                .ThenInclude(i => i.Industry)
            .FirstOrDefaultAsync(a => a.UserID == userId);

        if (advisor == null)
            return ApiResponse<AdvisorMeDto>.SuccessResponse(null, "Profile has not been created yet.");

        return ApiResponse<AdvisorMeDto>.SuccessResponse(
            MapToMeDto(advisor, advisor.Availability, advisor.IndustryFocus));
    }

    public async Task<ApiResponse<AdvisorMeDto>> UpdateProfileAsync(int userId, UpdateAdvisorRequest request)
    {
        var advisor = await _db.Advisors
            .AsSplitQuery()
            .Include(a => a.Availability)
            .Include(a => a.IndustryFocus)
                .ThenInclude(i => i.Industry)
            .FirstOrDefaultAsync(a => a.UserID == userId);

        if (advisor == null)
            return ApiResponse<AdvisorMeDto>.ErrorResponse("ADVISOR_PROFILE_NOT_FOUND",
                "Advisor profile not found.");

        if (request.FullName != null) advisor.FullName = request.FullName;
        if (request.Title != null) advisor.Title = request.Title;
        if (request.Company != null) advisor.Company = request.Company;
        if (request.Bio != null) advisor.Bio = request.Bio;
        if (request.LinkedInURL != null) advisor.LinkedInURL = request.LinkedInURL;
        if (request.GoogleMeetLink != null) advisor.GoogleMeetLink = request.GoogleMeetLink;
        if (request.MsTeamsLink != null) advisor.MsTeamsLink = request.MsTeamsLink;
        if (request.Website != null) advisor.Website = request.Website;
        if (request.MentorshipPhilosophy != null) advisor.MentorshipPhilosophy = request.MentorshipPhilosophy;
        if (request.YearsOfExperience.HasValue) advisor.YearsOfExperience = request.YearsOfExperience;
        if (request.HourlyRate.HasValue) advisor.HourlyRate = request.HourlyRate;
        if (request.Expertise != null) advisor.Expertise = request.Expertise;
        if (request.DomainTags != null) advisor.DomainTags = request.DomainTags;
        if (request.SuitableFor != null) advisor.SuitableFor = request.SuitableFor;
        if (request.SupportedDurations != null) advisor.SupportedDurations = request.SupportedDurations;
        if (request.ExperiencesJson != null) advisor.ExperiencesJson = request.ExperiencesJson;
        if (request.Skills != null) advisor.Skills = request.Skills;
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
                    IndustryID = industry.IndustryId
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

        if (request.IsAcceptingNewMentees == true && advisor.ProfileStatus != ProfileStatus.Approved)
            return ApiResponse<AvailabilityDto>.ErrorResponse("VALIDATION_ERROR",
                "Profile must be Approved before enabling accepting new mentees. Please complete KYC verification first.");

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
    private static List<string> SplitCsv(string? csv)
        => string.IsNullOrEmpty(csv) ? new() : csv.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();

    private static AdvisorMeDto MapToMeDto(
        Advisor a,
        AdvisorAvailability? availability,
        IEnumerable<AdvisorIndustryFocus> industryFocus) => new()
    {
        AdvisorID = a.AdvisorID,
        UserId = a.UserID,
        FullName = a.FullName,
        Title = a.Title,
        Company = a.Company,
        Bio = a.Bio,
        ProfilePhotoURL = a.ProfilePhotoURL,
        MentorshipPhilosophy = a.MentorshipPhilosophy,
        LinkedInURL = a.LinkedInURL,
        GoogleMeetLink = a.GoogleMeetLink,
        MsTeamsLink = a.MsTeamsLink,
        Website = a.Website,
        ProfileStatus = a.ProfileStatus.ToString(),
        YearsOfExperience = a.YearsOfExperience,
        HourlyRate = a.HourlyRate,
        Expertise = SplitCsv(a.Expertise),
        DomainTags = SplitCsv(a.DomainTags),
        SuitableFor = SplitCsv(a.SuitableFor),
        SupportedDurations = SplitCsv(a.SupportedDurations),
        ExperiencesJson = a.ExperiencesJson,
        Skills = SplitCsv(a.Skills),
        CurrentOrganization = a.CurrentOrganization,
        BasicExpertiseProofFile = a.BasicExpertiseProofFileURL,
        ContactEmail = a.ContactEmail,
        TotalMentees = a.TotalMentees,
        TotalSessionHours = a.TotalSessionHours,
        AverageRating = a.AverageRating,
        CreatedAt = a.CreatedAt,
        UpdatedAt = a.UpdatedAt,
        Availability = availability != null ? MapAvailabilityDto(availability) : null,
        IndustryFocus = a.IndustryFocus.Select(i => new AdvisorIndustryFocusDto
        {
            IndustryId = i.IndustryID,
            Industry = i.Industry?.IndustryName ?? string.Empty
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
            return ApiResponse<AdvisorKYCStatusDto>.ErrorResponse("NOT_FOUND", "Profile not found.");

        var dto = new AdvisorKYCStatusDto
        {
            LastUpdated = advisor.UpdatedAt ?? advisor.CreatedAt
        };

        // Priority order:
        // 1. Staff flagged for more info (PendingMoreInfo tag) — must beat generic Pending check
        // 2. Actively under review (PendingKYC / Pending with no special tag)
        // 3. Advisor is saving/editing a draft
        // 4. Truly verified tags
        // 5. Failed / Rejected
        // 6. Not started (fresh account, never touched KYC form)
        if (advisor.AdvisorTag == AdvisorTag.PendingMoreInfo)
        {
            dto.WorkflowStatus = "PENDING_MORE_INFO";
            dto.Explanation = "Hồ sơ cần bổ sung thêm thông tin. Vui lòng xem ghi chú từ Staff và nộp lại.";
        }
        else if (advisor.ProfileStatus == ProfileStatus.PendingKYC || advisor.ProfileStatus == ProfileStatus.Pending)
        {
            dto.WorkflowStatus = "PENDING_REVIEW";
            dto.Explanation = "Hồ sơ của bạn đang được đội ngũ AISEP xem xét. Thường mất 1–3 ngày làm việc.";
        }
        else if (advisor.ProfileStatus == ProfileStatus.Draft)
        {
            dto.WorkflowStatus = "DRAFT";
            dto.Explanation = "Bạn đang lưu nháp hồ sơ xác thực. Hãy hoàn thiện và gửi để được xem xét.";
        }
        else if (advisor.AdvisorTag == AdvisorTag.VerifiedAdvisor || advisor.AdvisorTag == AdvisorTag.BasicVerified)
        {
            dto.WorkflowStatus = "VERIFIED";
            dto.VerificationLabel = advisor.AdvisorTag.ToString();
            dto.Explanation = "Chúc mừng! Hồ sơ của bạn đã được xác thực đầy đủ. Huy hiệu VERIFIED ADVISOR đã được kích hoạt trên profile.";
        }
        else if (advisor.AdvisorTag == AdvisorTag.VerificationFailed || advisor.ProfileStatus == ProfileStatus.Rejected)
        {
            dto.WorkflowStatus = "VERIFICATION_FAILED";
            dto.RequiresNewEvidence = advisor.RequiresNewEvidence;
            dto.Remarks = advisor.RejectionRemarks;
            dto.Explanation = string.IsNullOrEmpty(advisor.RejectionRemarks)
                ? "Hồ sơ không đáp ứng tiêu chuẩn xác thực. Vui lòng xem lại ghi chú và gửi lại."
                : advisor.RejectionRemarks;
        }
        else
        {
            dto.WorkflowStatus = "NOT_STARTED";
            dto.Explanation = "Chào mừng! Hãy xác thực tài khoản để tăng uy tín của bạn trong hệ sinh thái AISEP.";
        }

        dto.SubmissionSummary = new AdvisorKYCSubmissionSummaryDto
        {
            FullName = advisor.FullName,
            SubmittedAt = advisor.UpdatedAt ?? advisor.CreatedAt,
            Version = 1,
            EvidenceFiles = advisor.BasicExpertiseProofFileURL != null
                ? new List<AdvisorKYCEvidenceFileDto>
                  {
                      new AdvisorKYCEvidenceFileDto
                      {
                          Id = 1,
                          Url = _cloudinaryService.GenerateSignedDocumentUrl(null, advisor.BasicExpertiseProofFileURL),
                          FileName = advisor.BasicExpertiseProofFileName
                              ?? System.IO.Path.GetFileName(advisor.BasicExpertiseProofFileURL),
                          FileType = System.IO.Path.GetExtension(advisor.BasicExpertiseProofFileURL)?.ToLowerInvariant() switch
                          {
                              ".pdf"  => "application/pdf",
                              ".png"  => "image/png",
                              ".jpg" or ".jpeg" => "image/jpeg",
                              ".gif"  => "image/gif",
                              ".webp" => "image/webp",
                              _       => "application/octet-stream"
                          }
                      }
                  }
                : new List<AdvisorKYCEvidenceFileDto>()
        };

        dto.History = new List<AdvisorKYCHistoryDto>();
        if (advisor.ProfileStatus == ProfileStatus.PendingKYC || advisor.ProfileStatus == ProfileStatus.Pending)
        {
            dto.History.Add(new AdvisorKYCHistoryDto 
            { 
                Action = "Gửi hồ sơ xác thực", 
                Date = (advisor.UpdatedAt ?? advisor.CreatedAt).ToString("dd/MM/yyyy HH:mm"), 
                Status = "PENDING_REVIEW" 
            });
        }
        else if ((advisor.AdvisorTag == AdvisorTag.VerificationFailed || advisor.ProfileStatus == ProfileStatus.Rejected)
                 && !string.IsNullOrEmpty(advisor.RejectionRemarks))
        {
            dto.History.Add(new AdvisorKYCHistoryDto
            {
                Action = "Hồ sơ bị từ chối",
                Date = (advisor.UpdatedAt ?? advisor.CreatedAt).ToString("dd/MM/yyyy HH:mm"),
                Status = "VERIFICATION_FAILED",
                Remark = advisor.RejectionRemarks
            });
        }
        else if (advisor.AdvisorTag == AdvisorTag.PendingMoreInfo
                 && !string.IsNullOrEmpty(advisor.RejectionRemarks))
        {
            dto.History.Add(new AdvisorKYCHistoryDto
            {
                Action = "Yêu cầu bổ sung thông tin",
                Date = (advisor.UpdatedAt ?? advisor.CreatedAt).ToString("dd/MM/yyyy HH:mm"),
                Status = "PENDING_MORE_INFO",
                Remark = advisor.RejectionRemarks
            });
        }

        dto.CurrentSubmission = new AdvisorKYCCurrentSubmissionDto
        {
            FullName = advisor.FullName,
            ContactEmail = advisor.ContactEmail,
            CurrentRoleTitle = advisor.Title,
            CurrentOrganization = advisor.CurrentOrganization,
            PrimaryExpertise = advisor.Expertise,
            Bio = advisor.Bio,
            ProfessionalProfileLink = advisor.LinkedInURL,
            BasicExpertiseProofFileURL = advisor.BasicExpertiseProofFileURL,
            BasicExpertiseProofFileName = advisor.BasicExpertiseProofFileName,
            YearsOfExperience = advisor.YearsOfExperience,
            MentorshipPhilosophy = advisor.MentorshipPhilosophy
        };

        return ApiResponse<AdvisorKYCStatusDto>.SuccessResponse(dto);
    }

    public async Task<ApiResponse<AdvisorKYCStatusDto>> SubmitKYCAsync(int userId, SubmitAdvisorKYCRequest request)
    {
        var advisor = await _db.Advisors.FirstOrDefaultAsync(a => a.UserID == userId);
        if (advisor == null) return ApiResponse<AdvisorKYCStatusDto>.ErrorResponse("NOT_FOUND", "Profile not found.");

        // Validate file requirement
        if (request.BasicExpertiseProofFile == null)
        {
            if (string.IsNullOrEmpty(advisor.BasicExpertiseProofFileURL))
            {
                return ApiResponse<AdvisorKYCStatusDto>.ErrorResponse("EVIDENCE_FILES_REQUIRED",
                    "A proof document is required when submitting KYC.");
            }

            if (advisor.RequiresNewEvidence)
            {
                return ApiResponse<AdvisorKYCStatusDto>.ErrorResponse("EVIDENCE_FILES_REQUIRED",
                    "New proof document is required before you can resubmit this KYC case.");
            }
        }

        advisor.FullName = request.FullName;
        advisor.Title = request.Title ?? advisor.Title;
        advisor.Bio = request.Bio ?? advisor.Bio;
        advisor.LinkedInURL = request.LinkedInURL ?? advisor.LinkedInURL;
        advisor.MentorshipPhilosophy = request.MentorshipPhilosophy ?? advisor.MentorshipPhilosophy;
        if (!string.IsNullOrEmpty(request.CurrentOrganization))
            advisor.CurrentOrganization = request.CurrentOrganization;
        if (!string.IsNullOrEmpty(request.ContactEmail))
            advisor.ContactEmail = request.ContactEmail;
        if (request.BasicExpertiseProofFile != null)
        {
            advisor.BasicExpertiseProofFileURL = await _cloudinaryService.UploadDocument(request.BasicExpertiseProofFile, CloudinaryFolderSaving.DocumentStorage);
            advisor.BasicExpertiseProofFileName = request.BasicExpertiseProofFile.FileName;
        }

        advisor.ProfileStatus = ProfileStatus.PendingKYC;
        advisor.AdvisorTag = AdvisorTag.None;
        advisor.RequiresNewEvidence = false;
        advisor.RejectionRemarks = null;
        advisor.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _audit.LogAsync("SUBMIT_ADVISOR_KYC", "Advisor", advisor.AdvisorID, "Advisor submitted KYC details");

        return await GetKYCStatusAsync(userId);
    }

    public async Task<ApiResponse<AdvisorKYCStatusDto>> SaveKYCDraftAsync(int userId, SaveAdvisorKYCDraftRequest request)
    {
        var advisor = await _db.Advisors
            .Include(a => a.IndustryFocus)
            .FirstOrDefaultAsync(a => a.UserID == userId);
        if (advisor == null) return ApiResponse<AdvisorKYCStatusDto>.ErrorResponse("NOT_FOUND", "Profile not found.");

        // Base fields (from SubmitAdvisorKYCRequest)
        if (!string.IsNullOrEmpty(request.FullName)) advisor.FullName = request.FullName;
        if (!string.IsNullOrEmpty(request.Title)) advisor.Title = request.Title;
        if (!string.IsNullOrEmpty(request.Bio)) advisor.Bio = request.Bio;
        if (!string.IsNullOrEmpty(request.LinkedInURL)) advisor.LinkedInURL = request.LinkedInURL;
        if (!string.IsNullOrEmpty(request.MentorshipPhilosophy)) advisor.MentorshipPhilosophy = request.MentorshipPhilosophy;
        if (!string.IsNullOrEmpty(request.CurrentOrganization)) advisor.CurrentOrganization = request.CurrentOrganization;
        if (!string.IsNullOrEmpty(request.ContactEmail)) advisor.ContactEmail = request.ContactEmail;
        if (request.BasicExpertiseProofFile != null)
        {
            advisor.BasicExpertiseProofFileURL = await _cloudinaryService.UploadDocument(request.BasicExpertiseProofFile, CloudinaryFolderSaving.DocumentStorage);
            advisor.BasicExpertiseProofFileName = request.BasicExpertiseProofFile.FileName;
        }

        // Extended draft-only fields — these were previously never saved despite being in the request
        if (request.YearsOfExperience.HasValue) advisor.YearsOfExperience = request.YearsOfExperience;
        if (request.HourlyRate.HasValue) advisor.HourlyRate = request.HourlyRate;
        if (!string.IsNullOrEmpty(request.Expertise)) advisor.Expertise = request.Expertise;
        if (!string.IsNullOrEmpty(request.DomainTags)) advisor.DomainTags = request.DomainTags;
        if (!string.IsNullOrEmpty(request.SuitableFor)) advisor.SuitableFor = request.SuitableFor;
        if (!string.IsNullOrEmpty(request.SupportedDurations)) advisor.SupportedDurations = request.SupportedDurations;
        if (!string.IsNullOrEmpty(request.ExperiencesJson)) advisor.ExperiencesJson = request.ExperiencesJson;
        if (!string.IsNullOrEmpty(request.Skills)) advisor.Skills = request.Skills;

        if (request.AdvisorIndustryFocus is { Count: > 0 })
        {
            _db.AdvisorIndustryFocuses.RemoveRange(advisor.IndustryFocus);
            foreach (var industry in request.AdvisorIndustryFocus)
            {
                advisor.IndustryFocus.Add(new AdvisorIndustryFocus
                {
                    AdvisorID = advisor.AdvisorID,
                    IndustryID = industry.IndustryId
                });
            }
        }

        // Only set Draft when the advisor has never submitted (truly in Draft state).
        // Do NOT reset Rejected (VERIFICATION_FAILED) or Pending (PENDING_MORE_INFO) —
        // those states must be preserved until the advisor explicitly calls /submit.
        if (advisor.ProfileStatus == ProfileStatus.Draft)
        {
            advisor.ProfileStatus = ProfileStatus.Draft;
        }

        advisor.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return await GetKYCStatusAsync(userId);
    }

    #endregion
}
