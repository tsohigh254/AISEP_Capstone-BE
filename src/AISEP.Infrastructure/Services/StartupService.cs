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
using System.Text.Json;

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
            SubIndustry = request.SubIndustry,
            Stage = request.Stage,
            FoundedDate = request.FoundedDate.HasValue
                ? DateTime.SpecifyKind(request.FoundedDate.Value, DateTimeKind.Utc)
                : null,
            Website = request.Website,
            FundingAmountSought = request.FundingAmountSought,
            CurrentFundingRaised = request.CurrentFundingRaised,
            Valuation = request.Valuation,
            BusinessCode = request.BusinessCode?.Trim() ?? string.Empty,
            FullNameOfApplicant = request.FullNameOfApplicant ?? string.Empty,
            RoleOfApplicant = request.RoleOfApplicant ?? string.Empty,            
            MarketScope = request.MarketScope,
            ProductStatus = request.ProductStatus,
            Location = request.Location,
            Country = request.Country,
            ProblemStatement = request.ProblemStatement,
            SolutionSummary = request.SolutionSummary,
            CurrentNeeds = SerializeCurrentNeeds(request.CurrentNeeds),
            MetricSummary = request.MetricSummary,
            TeamSize = request.TeamSize,
            PitchDeckUrl = request.PitchDeckUrl,
            LinkedInURL = request.LinkedInURL,
            ContactEmail = request.ContactEmail ?? string.Empty,
            ContactPhone = request.ContactPhone,

            ProfileStatus = ProfileStatus.Approved,
            CreatedAt = DateTime.UtcNow
        };

        var logoUrl = request.LogoUrl != null
            ? await _cloudinaryService.UploadImage(request.LogoUrl, CloudinaryFolderSaving.Logo)
            : null;

        startup.LogoURL = logoUrl;

        var fileUrl = request.FileCertificateBusiness != null
            ? await _cloudinaryService.UploadDocument(request.FileCertificateBusiness, CloudinaryFolderSaving.DocumentStorage)
            : null;

        startup.FileCertificateBusiness = fileUrl;

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
            .Include(s => s.ApprovedByUser)
            .FirstOrDefaultAsync(s => s.UserID == userId);

        if (startup == null)
        {
            return ApiResponse<StartupMeDto>.SuccessResponse(null!, "Profile has not been created yet.");
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
        if (request.SubIndustry != null) startup.SubIndustry = request.SubIndustry;
        if (request.Stage != null) startup.Stage = request.Stage;
        if (request.OneLiner != null) startup.OneLiner = request.OneLiner;
        if (request.FoundedDate.HasValue) startup.FoundedDate = DateTime.SpecifyKind(request.FoundedDate.Value, DateTimeKind.Utc);
        if (request.Website != null) startup.Website = request.Website;
        if (request.FundingAmountSought.HasValue) startup.FundingAmountSought = request.FundingAmountSought;
        if (request.CurrentFundingRaised.HasValue) startup.CurrentFundingRaised = request.CurrentFundingRaised;
        if (request.Valuation.HasValue) startup.Valuation = request.Valuation;   
        if (request.MarketScope != null) startup.MarketScope = request.MarketScope;
        if (request.ProductStatus != null) startup.ProductStatus = request.ProductStatus;
        if (request.Location != null) startup.Location = request.Location;
        if (request.Country != null) startup.Country = request.Country;
        if (request.ProblemStatement != null) startup.ProblemStatement = request.ProblemStatement;
        if (request.SolutionSummary != null) startup.SolutionSummary = request.SolutionSummary;
        if (request.CurrentNeeds != null) startup.CurrentNeeds = SerializeCurrentNeeds(request.CurrentNeeds);
        if (request.MetricSummary != null) startup.MetricSummary = request.MetricSummary;
        if (request.TeamSize != null) startup.TeamSize = request.TeamSize;
        if (request.PitchDeckUrl != null) startup.PitchDeckUrl = request.PitchDeckUrl;
        if (request.LinkedInURL != null) startup.LinkedInURL = request.LinkedInURL;
        if (request.BusinessCode != null) startup.BusinessCode = request.BusinessCode;
        if (request.FullNameOfApplicant != null) startup.FullNameOfApplicant = request.FullNameOfApplicant;
        if (request.RoleOfApplicant != null) startup.RoleOfApplicant = request.RoleOfApplicant;
        if (request.ContactEmail != null) startup.ContactEmail = request.ContactEmail;
        if (request.ContactPhone != null) startup.ContactPhone = request.ContactPhone;

        if (request.LogoUrl != null)
        {
            var logoUrl = await _cloudinaryService.UploadImage(request.LogoUrl, CloudinaryFolderSaving.Logo);
            if (!string.IsNullOrEmpty(startup.LogoURL))
                await _cloudinaryService.DeleteImage(startup.LogoURL);
            startup.LogoURL = logoUrl;
        }

        if (request.FileCertificateBusiness != null)
        {
            var fileUrl = await _cloudinaryService.UploadDocument(request.FileCertificateBusiness, CloudinaryFolderSaving.DocumentStorage);            
            startup.FileCertificateBusiness = fileUrl;
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

        // Removed the check that blocked Approved profiles from submitting for KYC.
        // In the new workflow, Approved (normal) profiles can submit for KYC (PendingKYC).

        startup.ProfileStatus = ProfileStatus.PendingKYC;
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

    public async Task<ApiResponse<StartupKYCStatusDto>> GetKYCStatusAsync(int userId)
    {
        var startup = await _context.Startups
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserID == userId);

        if (startup == null)
        {
            return ApiResponse<StartupKYCStatusDto>.ErrorResponse("STARTUP_PROFILE_NOT_FOUND", "You haven't created a startup profile yet.");
        }

        var submission = await _context.StartupKycSubmissions
            .AsNoTracking()
            .Include(s => s.EvidenceFiles)
            .Include(s => s.RequestedAdditionalItems)
            .Where(s => s.StartupID == startup.StartupID)
            .OrderByDescending(s => s.Version)
            .FirstOrDefaultAsync();

        if (submission == null)
        {
            return ApiResponse<StartupKYCStatusDto>.SuccessResponse(new StartupKYCStatusDto
            {
                WorkflowStatus = MapWorkflowStatus(StartupKycWorkflowStatus.NotSubmitted),
                ResultLabel = MapResultLabel(StartupKycResultLabel.None),
                VerificationLabel = MapResultLabel(StartupKycResultLabel.None),
                Explanation = "KYC has not been submitted.",
                RequiresNewEvidence = false,
                LastUpdated = startup.UpdatedAt ?? startup.CreatedAt
            });
        }

        return ApiResponse<StartupKYCStatusDto>.SuccessResponse(MapToKycStatusDto(submission));
#if false
        var dto = new StartupKYCStatusDto
        {
            LastUpdated = startup.UpdatedAt ?? startup.CreatedAt
        };

        // Workflow Status Mapping (Simplified)
        if (startup.StartupTag != StartupTag.None)
        {
            dto.WorkflowStatus = "VERIFIED";
            dto.VerificationLabel = startup.StartupTag.ToString();
            dto.Explanation = "Chúc mừng! Startup của bạn đã được xác minh chính thức trên hệ thống AISEP.";
        }
        else if (startup.ProfileStatus == ProfileStatus.PendingKYC)
        {
            dto.WorkflowStatus = "PENDING_REVIEW";
            dto.Explanation = "Hồ sơ xác thực của bạn đang được đội ngũ Staff xem xét. Quá trình này thường mất 1-3 ngày làm việc.";
        }
        else if (startup.ProfileStatus == ProfileStatus.Rejected)
        {
            dto.WorkflowStatus = "VERIFICATION_FAILED";
            dto.Explanation = "Hồ sơ xác thực bị từ chối hoặc cần bổ sung thông tin. Vui lòng kiểm tra lại.";
        }
        else
        {
            dto.WorkflowStatus = "NOT_STARTED";
            dto.Explanation = "Hãy hoàn tất các thông tin định danh chuyên sâu để được xác minh trên nền tảng.";
        }

        dto.SubmissionSummary = new StartupKYCSubmissionSummaryDto
        {
            CompanyName = startup.CompanyName,
            SubmittedAt = startup.UpdatedAt ?? startup.CreatedAt,
            Version = 1
        };

        dto.History = new List<StartupKYCHistoryDto>();
        if (startup.ProfileStatus == ProfileStatus.PendingKYC)
        {
            dto.History.Add(new StartupKYCHistoryDto
            {
                Action = "Gửi hồ sơ xác thực",
                Date = (startup.UpdatedAt ?? startup.CreatedAt).ToString("dd/MM/yyyy HH:mm"),
                Status = "PENDING_REVIEW"
            });
        }

        return ApiResponse<StartupKYCStatusDto>.SuccessResponse(dto);
#endif
    }

    public async Task<ApiResponse<StartupKYCStatusDto>> SubmitKYCAsync(int userId, SubmitStartupKYCRequest request)
    {
        var startup = await _context.Startups
            .FirstOrDefaultAsync(s => s.UserID == userId);

        if (startup == null)
            return ApiResponse<StartupKYCStatusDto>.ErrorResponse("STARTUP_PROFILE_NOT_FOUND", "You haven't created a startup profile yet.");

        if (!TryParseStartupVerificationType(request.StartupVerificationType, out var verificationType))
        {
            return ApiResponse<StartupKYCStatusDto>.ErrorResponse("INVALID_STARTUP_VERIFICATION_TYPE",
                "StartupVerificationType must be WITH_LEGAL_ENTITY or WITHOUT_LEGAL_ENTITY.");
        }

        if (verificationType == StartupVerificationType.WithLegalEntity && string.IsNullOrWhiteSpace(request.LegalFullName))
        {
            return ApiResponse<StartupKYCStatusDto>.ErrorResponse("LEGAL_FULL_NAME_REQUIRED",
                "LegalFullName is required when StartupVerificationType is WITH_LEGAL_ENTITY.");
        }

        if (verificationType == StartupVerificationType.WithoutLegalEntity && string.IsNullOrWhiteSpace(request.ProjectName))
        {
            return ApiResponse<StartupKYCStatusDto>.ErrorResponse("PROJECT_NAME_REQUIRED",
                "ProjectName is required when StartupVerificationType is WITHOUT_LEGAL_ENTITY.");
        }

        if (string.IsNullOrWhiteSpace(request.RepresentativeFullName))
        {
            return ApiResponse<StartupKYCStatusDto>.ErrorResponse("REPRESENTATIVE_FULL_NAME_REQUIRED",
                "RepresentativeFullName is required.");
        }

        if (string.IsNullOrWhiteSpace(request.RepresentativeRole))
        {
            return ApiResponse<StartupKYCStatusDto>.ErrorResponse("REPRESENTATIVE_ROLE_REQUIRED",
                "RepresentativeRole is required.");
        }

        if (string.IsNullOrWhiteSpace(request.WorkEmail))
        {
            return ApiResponse<StartupKYCStatusDto>.ErrorResponse("WORK_EMAIL_REQUIRED",
                "WorkEmail is required.");
        }

        var latestDraft = await _context.StartupKycSubmissions
            .Include(s => s.EvidenceFiles)
            .Where(s => s.StartupID == startup.StartupID && s.WorkflowStatus == StartupKycWorkflowStatus.Draft && !s.IsActive)
            .OrderByDescending(s => s.Version)
            .FirstOrDefaultAsync();

        var now = DateTime.UtcNow;
        var activeSubmission = await _context.StartupKycSubmissions
            .Include(s => s.EvidenceFiles)
            .FirstOrDefaultAsync(s => s.StartupID == startup.StartupID && s.IsActive);
        var requiresNewEvidence = activeSubmission?.RequiresNewEvidence ?? false;

        if (request.EvidenceFiles.Count == 0)
        {
            var hasDraftEvidence = latestDraft?.EvidenceFiles.Count > 0;
            var hasActiveEvidence = activeSubmission?.EvidenceFiles.Count > 0;

            if (activeSubmission == null && !hasDraftEvidence)
            {
                return ApiResponse<StartupKYCStatusDto>.ErrorResponse("EVIDENCE_FILES_REQUIRED",
                    "At least one evidence file is required when submitting KYC.");
            }

            if (requiresNewEvidence)
            {
                return ApiResponse<StartupKYCStatusDto>.ErrorResponse("EVIDENCE_FILES_REQUIRED",
                    "New evidence files are required before you can resubmit this KYC case.");
            }

            if (!hasDraftEvidence && !hasActiveEvidence)
            {
                return ApiResponse<StartupKYCStatusDto>.ErrorResponse("EVIDENCE_FILES_REQUIRED",
                    "At least one evidence file is required when submitting KYC.");
            }
        }

        StartupKycSubmission submission;
        if (latestDraft != null)
        {
            submission = latestDraft;
        }
        else
        {
            submission = new StartupKycSubmission
            {
                StartupID = startup.StartupID,
                Version = await GetNextSubmissionVersionAsync(startup.StartupID),
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.StartupKycSubmissions.Add(submission);
        }

        if (activeSubmission != null && activeSubmission.SubmissionID != submission.SubmissionID)
        {
            activeSubmission.IsActive = false;
            activeSubmission.UpdatedAt = now;

            if (activeSubmission.WorkflowStatus == StartupKycWorkflowStatus.UnderReview
                || activeSubmission.WorkflowStatus == StartupKycWorkflowStatus.PendingMoreInfo
                || activeSubmission.WorkflowStatus == StartupKycWorkflowStatus.Draft)
            {
                activeSubmission.WorkflowStatus = StartupKycWorkflowStatus.Superseded;
            }
        }

        ApplyKycSubmissionPayload(submission, request, verificationType, now);
        submission.IsActive = true;
        submission.WorkflowStatus = StartupKycWorkflowStatus.UnderReview;
        submission.ResultLabel = StartupKycResultLabel.None;
        submission.SubmittedAt = now;
        submission.Explanation = "KYC submission is under review.";
        submission.Remarks = null;
        submission.RequiresNewEvidence = false;

        if (request.EvidenceFiles.Count > 0)
        {
            await ReplaceEvidenceFilesAsync(submission, request, now);
        }
        else if (submission.EvidenceFiles.Count == 0 && activeSubmission != null)
        {
            CopyEvidenceFiles(activeSubmission, submission);
        }

        startup.ProfileStatus = ProfileStatus.PendingKYC;
        startup.UpdatedAt = now;

        await _context.SaveChangesAsync();
        await _auditService.LogAsync("SUBMIT_STARTUP_KYC", "Startup", startup.StartupID,
            $"Startup submitted KYC version {submission.Version}");

        return await GetKYCStatusAsync(userId);
#if false
        // Update fields
        startup.CompanyName = request.CompanyName;
        startup.FullNameOfApplicant = request.FullNameOfApplicant;
        startup.RoleOfApplicant = request.RoleOfApplicant;
        startup.ContactEmail = request.ContactEmail;
        startup.ContactPhone = request.ContactPhone;
        startup.BusinessCode = request.BusinessCode ?? startup.BusinessCode;
        startup.Website = request.Website ?? startup.Website;
        startup.LinkedInURL = request.LinkedInURL ?? startup.LinkedInURL;
        startup.ProblemStatement = request.ProblemStatement ?? startup.ProblemStatement;
        startup.SolutionSummary = request.SolutionSummary ?? startup.SolutionSummary;

        if (!string.IsNullOrEmpty(startup.FileCertificateBusiness))
        {
            startup.FileCertificateBusiness = startup.FileCertificateBusiness;
        }

        // Set status to PendingKYC
        startup.ProfileStatus = ProfileStatus.PendingKYC;
        startup.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await _auditService.LogAsync("SUBMIT_STARTUP_KYC", "Startup", startup.StartupID, "Startup submitted KYC details");

        return await GetKYCStatusAsync(userId);
#endif
    }

    public async Task<ApiResponse<StartupKYCStatusDto>> SaveKYCDraftAsync(int userId, SaveStartupKYCDraftRequest request)
    {
        var startup = await _context.Startups
            .FirstOrDefaultAsync(s => s.UserID == userId);

        if (startup == null)
            return ApiResponse<StartupKYCStatusDto>.ErrorResponse("STARTUP_PROFILE_NOT_FOUND", "You haven't created a startup profile yet.");

        var now = DateTime.UtcNow;
        var draft = await _context.StartupKycSubmissions
            .Include(s => s.EvidenceFiles)
            .Where(s => s.StartupID == startup.StartupID && s.WorkflowStatus == StartupKycWorkflowStatus.Draft && !s.IsActive)
            .OrderByDescending(s => s.Version)
            .FirstOrDefaultAsync();

        if (draft == null)
        {
            draft = new StartupKycSubmission
            {
                StartupID = startup.StartupID,
                Version = await GetNextSubmissionVersionAsync(startup.StartupID),
                CreatedAt = now,
                UpdatedAt = now,
                WorkflowStatus = StartupKycWorkflowStatus.Draft,
                ResultLabel = StartupKycResultLabel.None,
                StartupVerificationType = StartupVerificationType.WithoutLegalEntity
            };

            _context.StartupKycSubmissions.Add(draft);
        }

        ApplyDraftPayload(draft, request, now);
        draft.IsActive = false;
        draft.WorkflowStatus = StartupKycWorkflowStatus.Draft;
        draft.ResultLabel = StartupKycResultLabel.None;
        draft.Explanation = "KYC draft saved.";

        await ReplaceEvidenceFilesAsync(draft, request, now);

        startup.UpdatedAt = now;
        await _context.SaveChangesAsync();

        return await GetKYCStatusAsync(userId);
#if false
        // Partial update for draft
        if (!string.IsNullOrEmpty(request.CompanyName)) startup.CompanyName = request.CompanyName;
        if (!string.IsNullOrEmpty(request.FullNameOfApplicant)) startup.FullNameOfApplicant = request.FullNameOfApplicant;
        if (!string.IsNullOrEmpty(request.RoleOfApplicant)) startup.RoleOfApplicant = request.RoleOfApplicant;
        if (!string.IsNullOrEmpty(request.ContactEmail)) startup.ContactEmail = request.ContactEmail;
        if (!string.IsNullOrEmpty(request.ContactPhone)) startup.ContactPhone = request.ContactPhone;
        if (!string.IsNullOrEmpty(request.BusinessCode)) startup.BusinessCode = request.BusinessCode;
        if (!string.IsNullOrEmpty(request.Website)) startup.Website = request.Website;
        if (!string.IsNullOrEmpty(request.LinkedInURL)) startup.LinkedInURL = request.LinkedInURL;
        if (!string.IsNullOrEmpty(request.ProblemStatement)) startup.ProblemStatement = request.ProblemStatement;
        if (!string.IsNullOrEmpty(request.SolutionSummary)) startup.SolutionSummary = request.SolutionSummary;

        // DO NOT demote Approved -> Draft
        if (startup.ProfileStatus == ProfileStatus.Draft)
        {
            startup.ProfileStatus = ProfileStatus.Draft;
        }

        startup.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetKYCStatusAsync(userId);
#endif
    }

    // ========== PUBLIC ENDPOINTS ==========

    public async Task<ApiResponse<StartupPublicDto>> GetStartupByIdAsync(int startupId)
    {
        var startup = await _context.Startups
            .AsNoTracking()
            .Include(s => s.TeamMembers)
            .Include(s => s.Industry)
            .FirstOrDefaultAsync(s => s.StartupID == startupId && (s.ProfileStatus == ProfileStatus.Approved || s.ProfileStatus == ProfileStatus.PendingKYC));

        if (startup == null)
        {
            return ApiResponse<StartupPublicDto>.ErrorResponse("STARTUP_NOT_FOUND",
                "Startup not found.");
        }

        return ApiResponse<StartupPublicDto>.SuccessResponse(MapToPublicDto(startup));
    }

    public async Task<ApiResponse<PagedResponse<StartupListItemDto>>> SearchStartupsAsync(StartupQueryParams startupQuery)
    {

        var query = _context.Startups.AsNoTracking().Where(s => s.ProfileStatus == ProfileStatus.Approved || s.ProfileStatus == ProfileStatus.PendingKYC).AsQueryable();

        // Keyword search on CompanyName
        if (!string.IsNullOrWhiteSpace(startupQuery.Key))
        {
            var key = startupQuery.Key.Trim().ToLower();
            query = query.Where(s => s.CompanyName.Trim().ToLower().Contains(key)
            || (s.Industry != null && s.Industry.IndustryName.Trim().ToLower().Contains(key)));
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
            if (!string.IsNullOrEmpty(member.PhotoURL))
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
            CurrentNeeds = DeserializeCurrentNeeds(s.CurrentNeeds),
            MetricSummary = s.MetricSummary,
            TeamSize = s.TeamSize,
            PitchDeckUrl = s.PitchDeckUrl,
            IsVisible = s.IsVisible,
            LinkedInURL = s.LinkedInURL,
            FileCertificateBusiness = s.FileCertificateBusiness,

            FullNameOfApplicant = s.FullNameOfApplicant,
            RoleOfApplicant = s.RoleOfApplicant,
            ContactEmail = s.ContactEmail,
            ContactPhone = s.ContactPhone,
            BusinessCode = s.BusinessCode,

            ProfileStatus = s.ProfileStatus.ToString(),
            ApprovedAt = s.ApprovedAt,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt,
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
            CurrentNeeds = DeserializeCurrentNeeds(s.CurrentNeeds),
            MetricSummary = s.MetricSummary,
            TeamSize = s.TeamSize,
            PitchDeckUrl = s.PitchDeckUrl,
            LinkedInURL = s.LinkedInURL,
            ContactEmail = s.ContactEmail,
            ContactPhone = s.ContactPhone,
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

    private static string? SerializeCurrentNeeds(IEnumerable<string>? currentNeeds)
    {
        if (currentNeeds == null)
        {
            return null;
        }

        var items = currentNeeds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return JsonSerializer.Serialize(items);
    }

    private static List<string> DeserializeCurrentNeeds(string? currentNeeds)
    {
        if (string.IsNullOrWhiteSpace(currentNeeds))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(currentNeeds) ?? new List<string>();
        }
        catch (JsonException)
        {
            return currentNeeds
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .ToList();
        }
    }

    private async Task<int> GetNextSubmissionVersionAsync(int startupId)
    {
        var latestVersion = await _context.StartupKycSubmissions
            .Where(s => s.StartupID == startupId)
            .Select(s => (int?)s.Version)
            .MaxAsync();

        return (latestVersion ?? 0) + 1;
    }

    private async Task ReplaceEvidenceFilesAsync(StartupKycSubmission submission, SubmitStartupKYCRequest request, DateTime now)
    {
        if (request.EvidenceFiles.Count == 0)
        {
            return;
        }

        if (submission.EvidenceFiles.Count > 0)
        {
            _context.StartupKycEvidenceFiles.RemoveRange(submission.EvidenceFiles);
            submission.EvidenceFiles.Clear();
        }

        for (var index = 0; index < request.EvidenceFiles.Count; index++)
        {
            var file = request.EvidenceFiles[index];
            var uploadedFile = await _cloudinaryService.UploadDocumentWithMetadata(file, CloudinaryFolderSaving.DocumentStorage);
            var kind = ParseEvidenceKind(index < request.EvidenceFileKinds.Count ? request.EvidenceFileKinds[index] : null);

            submission.EvidenceFiles.Add(new StartupKycEvidenceFile
            {
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileUrl = uploadedFile.Url,
                StorageKey = uploadedFile.PublicId,
                Kind = kind,
                FileSize = file.Length,
                UploadedAt = now
            });
        }
    }

    private static void ApplyKycSubmissionPayload(StartupKycSubmission submission, SubmitStartupKYCRequest request, StartupVerificationType verificationType, DateTime now)
    {
        submission.StartupVerificationType = verificationType;
        submission.LegalFullName = verificationType == StartupVerificationType.WithLegalEntity ? request.LegalFullName?.Trim() : null;
        submission.ProjectName = verificationType == StartupVerificationType.WithoutLegalEntity ? request.ProjectName?.Trim() : null;
        submission.EnterpriseCode = request.EnterpriseCode?.Trim();
        submission.RepresentativeFullName = request.RepresentativeFullName.Trim();
        submission.RepresentativeRole = request.RepresentativeRole.Trim();
        submission.WorkEmail = request.WorkEmail.Trim();
        submission.PublicLink = request.PublicLink?.Trim();
        submission.UpdatedAt = now;
    }

    private static void ApplyDraftPayload(StartupKycSubmission submission, SaveStartupKYCDraftRequest request, DateTime now)
    {
        if (TryParseStartupVerificationType(request.StartupVerificationType, out var verificationType))
        {
            submission.StartupVerificationType = verificationType;
        }

        if (request.LegalFullName != null)
        {
            submission.LegalFullName = string.IsNullOrWhiteSpace(request.LegalFullName) ? null : request.LegalFullName.Trim();
        }

        if (request.ProjectName != null)
        {
            submission.ProjectName = string.IsNullOrWhiteSpace(request.ProjectName) ? null : request.ProjectName.Trim();
        }

        if (request.EnterpriseCode != null)
        {
            submission.EnterpriseCode = string.IsNullOrWhiteSpace(request.EnterpriseCode) ? null : request.EnterpriseCode.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.RepresentativeFullName))
        {
            submission.RepresentativeFullName = request.RepresentativeFullName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.RepresentativeRole))
        {
            submission.RepresentativeRole = request.RepresentativeRole.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.WorkEmail))
        {
            submission.WorkEmail = request.WorkEmail.Trim();
        }

        if (request.PublicLink != null)
        {
            submission.PublicLink = string.IsNullOrWhiteSpace(request.PublicLink) ? null : request.PublicLink.Trim();
        }

        submission.UpdatedAt = now;
    }

    private StartupKYCStatusDto MapToKycStatusDto(StartupKycSubmission submission)
    {
        return new StartupKYCStatusDto
        {
            WorkflowStatus = MapWorkflowStatus(submission.WorkflowStatus),
            ResultLabel = MapResultLabel(submission.ResultLabel),
            VerificationLabel = MapResultLabel(submission.ResultLabel),
            Explanation = string.IsNullOrWhiteSpace(submission.Explanation)
                ? GetDefaultExplanation(submission.WorkflowStatus, submission.ResultLabel)
                : submission.Explanation,
            Remarks = submission.Remarks,
            RequiresNewEvidence = submission.RequiresNewEvidence,
            SubmissionId = submission.SubmissionID,
            Version = submission.Version,
            SubmittedAt = submission.SubmittedAt,
            UpdatedAt = submission.UpdatedAt,
            LastUpdated = submission.UpdatedAt,
            SubmissionSummary = MapToSubmissionSummaryDto(submission),
            RequestedAdditionalItems = submission.RequestedAdditionalItems
                .OrderBy(i => i.CreatedAt)
                .Select(MapToRequestedItemDto)
                .ToList()
        };
    }

    private StartupKycSubmissionDto MapToKycSubmissionDto(StartupKycSubmission submission)
    {
        return new StartupKycSubmissionDto
        {
            Id = submission.SubmissionID,
            StartupId = submission.StartupID,
            Version = submission.Version,
            IsActive = submission.IsActive,
            WorkflowStatus = MapWorkflowStatus(submission.WorkflowStatus),
            ResultLabel = MapResultLabel(submission.ResultLabel),
            SubmittedAt = submission.SubmittedAt,
            UpdatedAt = submission.UpdatedAt,
            ReviewedAt = submission.ReviewedAt,
            ReviewedBy = submission.ReviewedBy,
            RequiresNewEvidence = submission.RequiresNewEvidence,
            SubmissionSummary = MapToSubmissionSummaryDto(submission),
            RequestedAdditionalItems = submission.RequestedAdditionalItems
                .OrderBy(i => i.CreatedAt)
                .Select(MapToRequestedItemDto)
                .ToList(),
            Explanation = string.IsNullOrWhiteSpace(submission.Explanation)
                ? GetDefaultExplanation(submission.WorkflowStatus, submission.ResultLabel)
                : submission.Explanation,
            Remarks = submission.Remarks
        };
    }

    private StartupKYCSubmissionSummaryDto MapToSubmissionSummaryDto(StartupKycSubmission submission)
    {
        return new StartupKYCSubmissionSummaryDto
        {
            CompanyName = submission.LegalFullName ?? submission.ProjectName ?? string.Empty,
            SubmittedAt = submission.SubmittedAt ?? submission.CreatedAt,
            Version = submission.Version,
            StartupVerificationType = MapVerificationType(submission.StartupVerificationType),
            LegalFullName = submission.LegalFullName,
            EnterpriseCode = submission.EnterpriseCode,
            ProjectName = submission.ProjectName,
            RepresentativeFullName = submission.RepresentativeFullName,
            RepresentativeRole = submission.RepresentativeRole,
            WorkEmail = submission.WorkEmail,
            PublicLink = submission.PublicLink,
            EvidenceFiles = submission.EvidenceFiles
                .OrderBy(f => f.UploadedAt)
                .Select(f => new StartupKycEvidenceFileDto
                {
                    Id = f.EvidenceFileID,
                    FileName = f.FileName,
                    FileType = f.ContentType,
                    FileSize = f.FileSize,
                    UploadedAt = f.UploadedAt,
                    Kind = MapEvidenceKind(f.Kind),
                    Url = _cloudinaryService.GenerateSignedDocumentUrl(f.StorageKey, f.FileUrl, f.FileName),
                    StorageKey = !string.IsNullOrWhiteSpace(f.StorageKey)
                        ? f.StorageKey
                        : _cloudinaryService.ExtractDocumentStorageKeyFromUrl(f.FileUrl)
                })
                .ToList()
        };
    }

    private static StartupKycRequestedItemDto MapToRequestedItemDto(StartupKycRequestedItem item)
    {
        return new StartupKycRequestedItemDto
        {
            Id = item.RequestedItemID,
            FieldKey = item.FieldKey,
            Label = item.Label,
            Reason = item.Reason,
            CreatedAt = item.CreatedAt,
            ResolvedAt = item.ResolvedAt
        };
    }

    private static void CopyEvidenceFiles(StartupKycSubmission sourceSubmission, StartupKycSubmission targetSubmission)
    {
        if (targetSubmission.EvidenceFiles.Count > 0)
        {
            return;
        }

        foreach (var file in sourceSubmission.EvidenceFiles.OrderBy(f => f.UploadedAt))
        {
            targetSubmission.EvidenceFiles.Add(new StartupKycEvidenceFile
            {
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileUrl = file.FileUrl,
                StorageKey = file.StorageKey,
                Kind = file.Kind,
                FileSize = file.FileSize,
                UploadedAt = file.UploadedAt
            });
        }
    }

    private static string GetDefaultExplanation(StartupKycWorkflowStatus workflowStatus, StartupKycResultLabel resultLabel)
    {
        return workflowStatus switch
        {
            StartupKycWorkflowStatus.NotSubmitted => "KYC has not been submitted.",
            StartupKycWorkflowStatus.Draft => "KYC draft saved.",
            StartupKycWorkflowStatus.UnderReview => "KYC submission is under review.",
            StartupKycWorkflowStatus.PendingMoreInfo => "Additional information has been requested for this KYC submission.",
            StartupKycWorkflowStatus.Approved when resultLabel == StartupKycResultLabel.VerifiedCompany => "Startup KYC has been approved as a verified company.",
            StartupKycWorkflowStatus.Approved when resultLabel == StartupKycResultLabel.BasicVerified => "Startup KYC has been approved as basic verified.",
            StartupKycWorkflowStatus.Rejected => "Startup KYC has been rejected.",
            StartupKycWorkflowStatus.Superseded => "This KYC submission has been replaced by a newer version.",
            _ => "KYC status updated."
        };
    }

    private static bool TryParseStartupVerificationType(string? rawValue, out StartupVerificationType verificationType)
    {
        verificationType = StartupVerificationType.WithoutLegalEntity;
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        var normalized = rawValue.Trim().Replace("-", "_").Replace(" ", "_").ToUpperInvariant();
        switch (normalized)
        {
            case "WITH_LEGAL_ENTITY":
            case "WITHLEGALENTITY":
                verificationType = StartupVerificationType.WithLegalEntity;
                return true;
            case "WITHOUT_LEGAL_ENTITY":
            case "WITHOUTLEGALENTITY":
                verificationType = StartupVerificationType.WithoutLegalEntity;
                return true;
            default:
                return false;
        }
    }

    private static StartupKycEvidenceKind ParseEvidenceKind(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return StartupKycEvidenceKind.Other;
        }

        var normalized = rawValue.Trim().Replace("-", "_").Replace(" ", "_").ToUpperInvariant();
        return normalized switch
        {
            "BUSINESS_REGISTRATION_CERTIFICATE" => StartupKycEvidenceKind.BusinessRegistrationCertificate,
            "PROOF_OF_OPERATION" => StartupKycEvidenceKind.ProofOfOperation,
            "PRODUCT_MATERIALS" => StartupKycEvidenceKind.ProductMaterials,
            _ => StartupKycEvidenceKind.Other
        };
    }

    private static string MapWorkflowStatus(StartupKycWorkflowStatus status)
    {
        return status switch
        {
            StartupKycWorkflowStatus.NotSubmitted => "NOT_SUBMITTED",
            StartupKycWorkflowStatus.Draft => "DRAFT",
            StartupKycWorkflowStatus.UnderReview => "UNDER_REVIEW",
            StartupKycWorkflowStatus.PendingMoreInfo => "PENDING_MORE_INFO",
            StartupKycWorkflowStatus.Approved => "APPROVED",
            StartupKycWorkflowStatus.Rejected => "REJECTED",
            StartupKycWorkflowStatus.Superseded => "SUPERSEDED",
            _ => "NOT_SUBMITTED"
        };
    }

    private static string MapResultLabel(StartupKycResultLabel label)
    {
        return label switch
        {
            StartupKycResultLabel.None => "NONE",
            StartupKycResultLabel.PendingMoreInfo => "PENDING_MORE_INFO",
            StartupKycResultLabel.BasicVerified => "BASIC_VERIFIED",
            StartupKycResultLabel.VerifiedCompany => "VERIFIED_COMPANY",
            StartupKycResultLabel.VerificationFailed => "VERIFICATION_FAILED",
            _ => "NONE"
        };
    }

    private static string MapVerificationType(StartupVerificationType verificationType)
    {
        return verificationType switch
        {
            StartupVerificationType.WithLegalEntity => "WITH_LEGAL_ENTITY",
            StartupVerificationType.WithoutLegalEntity => "WITHOUT_LEGAL_ENTITY",
            _ => "WITHOUT_LEGAL_ENTITY"
        };
    }

    private static string MapEvidenceKind(StartupKycEvidenceKind kind)
    {
        return kind switch
        {
            StartupKycEvidenceKind.BusinessRegistrationCertificate => "BUSINESS_REGISTRATION_CERTIFICATE",
            StartupKycEvidenceKind.ProofOfOperation => "PROOF_OF_OPERATION",
            StartupKycEvidenceKind.ProductMaterials => "PRODUCT_MATERIALS",
            _ => "OTHER"
        };
    }

    // ========== BROWSE INVESTORS (Startup role) ==========

    public async Task<ApiResponse<PagedResponse<InvestorSearchItemDto>>> SearchInvestorsAsync(InvestorQueryParams investorQuery)
    {
        var query = _context.Investors
            .AsNoTracking()
            .Where(i => i.ProfileStatus == ProfileStatus.Approved || i.ProfileStatus == ProfileStatus.PendingKYC)
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
