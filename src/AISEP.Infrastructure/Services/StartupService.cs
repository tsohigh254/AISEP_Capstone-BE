using AISEP.Application.Const;
using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Investor;
using AISEP.Application.DTOs.Startup;
using AISEP.Application.DTOs.Document;
using System.Linq;
using AISEP.Application.Extensions;
using AISEP.Application.Interfaces;
using AISEP.Application.QueryParams;
using AISEP.Domain.Entities;
using AISEP.Domain.Enums;
using AISEP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AISEP.Infrastructure.Services;

public class StartupService : IStartupService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ILogger<StartupService> _logger;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IServiceScopeFactory _scopeFactory;

    public StartupService(ApplicationDbContext context, IAuditService auditService, ILogger<StartupService> logger, ICloudinaryService cloudinaryService, IServiceScopeFactory scopeFactory)
    {
        _context = context;
        _auditService = auditService;
        _logger = logger;
        _cloudinaryService = cloudinaryService;
        _scopeFactory = scopeFactory;
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
            IsVisible = false,
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

        // Fire-and-forget: reindex startup in recommendation engine (new scope — avoids disposed DbContext)
        var startupIdForCreate = startup.StartupID;
        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var svc = scope.ServiceProvider.GetRequiredService<IAiRecommendationService>();
                await svc.ReindexStartupAsync(startupIdForCreate);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Background reindex failed for startup {StartupId}", startupIdForCreate); }
        });

        return ApiResponse<StartupMeDto>.SuccessResponse(MapToMeDto(startup), "Startup profile created successfully");
    }

    public async Task<ApiResponse<StartupMeDto>> GetMyStartupAsync(int userId)
    {
        var startup = await _context.Startups
            .Include(s => s.TeamMembers)
            .Include(s => s.Industry)
            .Include(s => s.ApprovedByUser)
            .Include(s => s.Documents)
                .ThenInclude(d => d.BlockchainProof)
            .FirstOrDefaultAsync(s => s.UserID == userId);

        if (startup == null)
            return ApiResponse<StartupMeDto>.SuccessResponse(null!, "Profile has not been created yet.");

        // Auto-heal: if KYC submission is Approved but ProfileStatus is still PendingKYC, sync it
        if (startup.ProfileStatus == ProfileStatus.PendingKYC)
        {
            var approvedSubmission = await _context.StartupKycSubmissions
                .AsNoTracking()
                .AnyAsync(k => k.StartupID == startup.StartupID
                            && k.IsActive
                            && k.WorkflowStatus == StartupKycWorkflowStatus.Approved);

            if (approvedSubmission)
            {
                startup.ProfileStatus = ProfileStatus.Approved;
                startup.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await _auditService.LogAsync("AUTO_HEAL_PROFILE_STATUS", "Startup", startup.StartupID,
                    "ProfileStatus auto-corrected to Approved: KYC was approved but ProfileStatus was PendingKYC");
            }
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

        // Fire-and-forget: reindex startup in recommendation engine (new scope — avoids disposed DbContext)
        var startupIdForUpdate = startup.StartupID;
        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var svc = scope.ServiceProvider.GetRequiredService<IAiRecommendationService>();
                await svc.ReindexStartupAsync(startupIdForUpdate);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Background reindex failed for startup {StartupId}", startupIdForUpdate); }
        });

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
            return ApiResponse<string>.ErrorResponse("STARTUP_PROFILE_NOT_FOUND",
                "You haven't created a startup profile yet.");

        if (isVisible)
        {
            var kycApproved = await _context.StartupKycSubmissions
                .AnyAsync(k => k.StartupID == startup.StartupID
                            && k.IsActive
                            && k.WorkflowStatus == StartupKycWorkflowStatus.Approved);

            if (!kycApproved)
                return ApiResponse<string>.ErrorResponse("STARTUP_VISIBILITY_NOT_ALLOWED",
                    "Your profile must be KYC-approved before it can be made visible.");
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

    public async Task<ApiResponse<StartupPublicDto>> GetStartupByIdAsync(int startupId, int requestingUserId, string userType)
    {
        var startup = await _context.Startups
            .AsNoTracking()
            .Include(s => s.TeamMembers)
            .Include(s => s.Industry)
                .ThenInclude(i => i!.ParentIndustry)
            .FirstOrDefaultAsync(s => s.StartupID == startupId
                && s.ProfileStatus == ProfileStatus.Approved);

        if (startup == null)
            return ApiResponse<StartupPublicDto>.ErrorResponse("STARTUP_NOT_FOUND", "Startup not found.");

        if (!startup.IsVisible)
        {
            var isStaff = userType == "Staff" || userType == "Admin";
            // Owner: Startup user viewing their own profile
            var isOwner = userType == "Startup" && startup.UserID == requestingUserId;

            if (!isStaff && !isOwner)
            {
                bool canView = false;

                if (userType == "Investor")
                {
                    // Investor: có connection Pending hoặc Accepted
                    var investor = await _context.Investors
                        .AsNoTracking()
                        .FirstOrDefaultAsync(i => i.UserID == requestingUserId);

                    canView = investor != null && await _context.StartupInvestorConnections
                        .AnyAsync(c => c.StartupID == startupId
                                    && c.InvestorID == investor.InvestorID
                                    && (c.ConnectionStatus == ConnectionStatus.Requested
                                        || c.ConnectionStatus == ConnectionStatus.Accepted
                                        || c.ConnectionStatus == ConnectionStatus.InDiscussion));
                }
                else if (userType == "Advisor")
                {
                    // Advisor: có mentorship request active (Requested / Accepted / InProgress / InDispute)
                    var advisor = await _context.Advisors
                        .AsNoTracking()
                        .FirstOrDefaultAsync(a => a.UserID == requestingUserId);

                    canView = advisor != null && await _context.StartupAdvisorMentorships
                        .AnyAsync(m => m.StartupID == startupId
                                    && m.AdvisorID == advisor.AdvisorID
                                    && (m.MentorshipStatus == MentorshipStatus.Requested
                                        || m.MentorshipStatus == MentorshipStatus.Accepted
                                        || m.MentorshipStatus == MentorshipStatus.InProgress
                                        || m.MentorshipStatus == MentorshipStatus.InDispute
                                        || m.MentorshipStatus == MentorshipStatus.Completed
                                        || m.MentorshipStatus == MentorshipStatus.Resolved));
                }

                if (!canView)
                    return ApiResponse<StartupPublicDto>.ErrorResponse("STARTUP_NOT_FOUND", "Startup not found.");
            }
        }

        var enterpriseCode = await _context.StartupKycSubmissions
            .AsNoTracking()
            .Where(k => k.StartupID == startupId
                     && k.IsActive
                     && k.WorkflowStatus == StartupKycWorkflowStatus.Approved)
            .Select(k => k.EnterpriseCode)
            .FirstOrDefaultAsync();

        var aiScore = await GetLatestStartupAiScoreAsync(startupId);

        var dto = MapToPublicDto(startup);
        dto.EnterpriseCode = enterpriseCode;
        dto.AiScore = aiScore;
        return ApiResponse<StartupPublicDto>.SuccessResponse(dto);
    }

    public async Task<ApiResponse<PagedResponse<StartupListItemDto>>> SearchStartupsAsync(StartupQueryParams startupQuery, string userType, int callerUserId = 0)
    {
        var isStaff = userType == "Staff" || userType == "Admin";
        var isInvestor = userType == "Investor";

        var query = _context.Startups.AsNoTracking()
            .Where(s => s.ProfileStatus == ProfileStatus.Approved
                     && (isStaff || s.IsVisible))
            .AsQueryable();

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

        var totalItems = await query.CountAsync();

        var rawItems = await query
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
            })
            .Skip((startupQuery.Page <= 0 ? 0 : startupQuery.Page - 1) * (startupQuery.PageSize <= 0 ? 20 : startupQuery.PageSize))
            .Take(startupQuery.PageSize <= 0 ? 20 : startupQuery.PageSize)
            .ToListAsync();

        // Enrich with connection state when caller is an Investor
        if (isInvestor && callerUserId > 0 && rawItems.Count > 0)
        {
            var investorId = await _context.Investors
                .AsNoTracking()
                .Where(i => i.UserID == callerUserId)
                .Select(i => (int?)i.InvestorID)
                .FirstOrDefaultAsync();

            if (investorId.HasValue)
            {
                var startupIds = rawItems.Select(s => s.StartupID).ToList();

                // Fetch all active connections between this investor and the visible startups
                var conns = await _context.StartupInvestorConnections
                    .AsNoTracking()
                    .Where(c => c.InvestorID == investorId.Value
                             && startupIds.Contains(c.StartupID)
                             && (c.ConnectionStatus == ConnectionStatus.Requested
                              || c.ConnectionStatus == ConnectionStatus.Accepted
                              || c.ConnectionStatus == ConnectionStatus.InDiscussion))
                    .Select(c => new { c.StartupID, c.ConnectionID, c.ConnectionStatus, c.InitiatedBy })
                    .ToListAsync();

                var connByStartup = conns.ToDictionary(c => c.StartupID);

                foreach (var item in rawItems)
                {
                    if (connByStartup.TryGetValue(item.StartupID, out var conn))
                    {
                        item.ConnectionStatus = conn.ConnectionStatus switch
                        {
                            ConnectionStatus.Requested    => "REQUESTED",
                            ConnectionStatus.Accepted     => "ACCEPTED",
                            ConnectionStatus.InDiscussion => "IN_DISCUSSION",
                            _                             => "NONE"
                        };
                        item.ConnectionId       = conn.ConnectionID;
                        item.CanRequestConnection = false;
                        item.InitiatedByRole    = conn.InitiatedBy == callerUserId ? "INVESTOR" : "STARTUP";
                    }
                    else
                    {
                        item.ConnectionStatus     = "NONE";
                        item.CanRequestConnection = true;
                    }
                }
            }
        }

        var result = new PagedResponse<StartupListItemDto>
        {
            Items = rawItems,
            Paging = new PagingInfo
            {
                Page = startupQuery.Page,
                PageSize = startupQuery.PageSize,
                TotalItems = totalItems,
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
            SubscriptionPlan = s.SubscriptionPlan.ToString(),
            SubscriptionEndDate = s.SubscriptionEndDate,
            ApprovedAt = s.ApprovedAt,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt,
            Documents = s.Documents?.OrderByDescending(d => d.UploadedAt).Select(d => new DocumentDto
            {
                DocumentID = d.DocumentID,
                StartupID = d.StartupID,
                DocumentType = d.DocumentType.ToString(),
                Title = d.Title ?? string.Empty,
                Version = d.Version,
                FileUrl = d.FileURL,
                IsArchived = d.IsArchived,
                IsAnalyzed = d.IsAnalyzed,
                AnalysisStatus = d.AnalysisStatus.ToString(),
                UploadedAt = d.UploadedAt,
                ProofStatus = d.BlockchainProof?.ProofStatus.ToString(),
                FileHash = d.BlockchainProof?.FileHash,
                TransactionHash = d.BlockchainProof?.TransactionHash,
                AnchoredAt = d.BlockchainProof?.AnchoredAt,
                EtherscanUrl = d.BlockchainProof?.TransactionHash != null ? $"https://etherscan.io/tx/{d.BlockchainProof.TransactionHash}" : null,
                ReviewStatus = d.ReviewStatus.ToString(),
                ReviewedBy = d.ReviewedBy,
                ReviewedAt = d.ReviewedAt
            }).ToList() ?? new List<DocumentDto>(),
        };
    }

    private async Task<double?> GetLatestStartupAiScoreAsync(int startupId)
    {
        var runScore = await _context.AiEvaluationRuns
            .AsNoTracking()
            .Where(r => r.StartupId == startupId
                     && r.OverallScore.HasValue
                     && (r.Status == "completed"
                      || r.Status == "COMPLETED"
                      || r.Status == "partial_completed"
                      || r.Status == "PARTIAL_COMPLETED"))
            .OrderByDescending(r => r.UpdatedAt)
            .Select(r => r.OverallScore)
            .FirstOrDefaultAsync();

        if (runScore.HasValue)
            return runScore.Value;

        var currentPotential = await _context.StartupPotentialScores
            .AsNoTracking()
            .Where(p => p.StartupID == startupId && p.IsCurrentScore)
            .OrderByDescending(p => p.CalculatedAt)
            .Select(p => (double?)p.OverallScore)
            .FirstOrDefaultAsync();

        if (currentPotential.HasValue)
            return currentPotential.Value;

        return await _context.StartupPotentialScores
            .AsNoTracking()
            .Where(p => p.StartupID == startupId)
            .OrderByDescending(p => p.CalculatedAt)
            .Select(p => (double?)p.OverallScore)
            .FirstOrDefaultAsync();
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
            ParentIndustryName = s.Industry?.ParentIndustry?.IndustryName,
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
                IsFounder = tm.IsFounder,
                YearsOfExperience = tm.YearsOfExperience
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

    public async Task<ApiResponse<PagedResponse<InvestorSearchItemDto>>> SearchInvestorsAsync(
        InvestorQueryParams q, int requestingUserId)
    {
        // Resolve the requesting startup
        var myStartupId = await _context.Startups
            .AsNoTracking()
            .Where(s => s.UserID == requestingUserId)
            .Select(s => (int?)s.StartupID)
            .FirstOrDefaultAsync();

        // Fetch active/pending connections for this startup — keyed by InvestorID
        // Statuses: Requested, Accepted, InDiscussion → block new request + show state
        // Rejected/Withdrawn/Closed → treated as NONE (startup may request again)
        var connectionsByInvestor = new Dictionary<int, (int ConnectionId, ConnectionStatus Status, int? InitiatedBy)>();

        if (myStartupId.HasValue)
        {
            var conns = await _context.StartupInvestorConnections
                .AsNoTracking()
                .Where(c => c.StartupID == myStartupId.Value &&
                            (c.ConnectionStatus == ConnectionStatus.Requested ||
                             c.ConnectionStatus == ConnectionStatus.Accepted ||
                             c.ConnectionStatus == ConnectionStatus.InDiscussion))
                .Select(c => new { c.InvestorID, c.ConnectionID, c.ConnectionStatus, c.InitiatedBy })
                .ToListAsync();

            connectionsByInvestor = conns.ToDictionary(
                c => c.InvestorID,
                c => (c.ConnectionID, c.ConnectionStatus, c.InitiatedBy));
        }

        // Base query — only visible profiles
        var query = _context.Investors
            .AsNoTracking()
            .Where(i => (i.ProfileStatus == ProfileStatus.Approved || i.ProfileStatus == ProfileStatus.PendingKYC)
                     && i.AcceptingConnections)
            .AsQueryable();

        // --- Filters ---
        if (!string.IsNullOrWhiteSpace(q.Keyword))
        {
            var kw = q.Keyword.Trim().ToLower();
            query = query.Where(i =>
                i.FullName.ToLower().Contains(kw) ||
                (i.FirmName != null && i.FirmName.ToLower().Contains(kw)) ||
                (i.Title != null && i.Title.ToLower().Contains(kw)));
        }

        if (!string.IsNullOrWhiteSpace(q.Industry))
        {
            var ind = q.Industry.ToLower();
            query = query.Where(i => i.IndustryFocus.Any(f => f.Industry.ToLower().Contains(ind)));
        }

        if (!string.IsNullOrWhiteSpace(q.Stage))
        {
            if (Enum.TryParse<StartupStage>(q.Stage, true, out var stageEnum))
                query = query.Where(i => i.StageFocus.Any(s => s.Stage == stageEnum));
        }

        if (q.TicketSizeMin.HasValue)
            query = query.Where(i => i.Preferences != null && i.Preferences.MaxInvestmentSize >= q.TicketSizeMin.Value);

        if (q.TicketSizeMax.HasValue)
            query = query.Where(i => i.Preferences != null && i.Preferences.MinInvestmentSize <= q.TicketSizeMax.Value);

        if (!string.IsNullOrWhiteSpace(q.Country))
        {
            var country = q.Country.ToLower();
            query = query.Where(i => i.Country != null && i.Country.ToLower().Contains(country));
        }

        if (!string.IsNullOrWhiteSpace(q.InvestorType))
            query = query.Where(i => i.KycSubmissions.Any(k => k.IsActive && k.InvestorCategory == q.InvestorType));

        if (q.KycVerified == true)
            query = query.Where(i => i.InvestorTag != InvestorTag.None);

        // --- Sort ---
        query = q.SortBy switch
        {
            "ticketSizeAsc"   => query.OrderBy(i => i.Preferences != null ? i.Preferences.MinInvestmentSize : null),
            "ticketSizeDesc"  => query.OrderByDescending(i => i.Preferences != null ? i.Preferences.MaxInvestmentSize : null),
            "connectionsDesc" => query.OrderByDescending(i =>
                i.StartupConnections.Count(c => c.ConnectionStatus == ConnectionStatus.Accepted)),
            _                 => query.OrderByDescending(i => i.UpdatedAt ?? i.CreatedAt)
        };

        var totalItems = await query.CountAsync();

        var pageSize = Math.Clamp(q.PageSize <= 0 ? 12 : q.PageSize, 1, 100);
        var page     = q.Page <= 0 ? 1 : q.Page;

        // SQL-side projection (no connection state — done in memory below)
        var raw = await query
            .Select(i => new
            {
                i.InvestorID,
                i.FullName,
                i.FirmName,
                i.Title,
                i.Bio,
                i.ProfilePhotoURL,
                i.Location,
                i.Country,
                i.LinkedInURL,
                i.Website,
                i.UpdatedAt,
                i.InvestorTag,
                i.AcceptingConnections,
                Industries           = i.IndustryFocus.Select(f => f.Industry).ToList(),
                Stages               = i.StageFocus.Select(s => s.Stage.ToString()).ToList(),
                RawGeographies       = i.Preferences != null ? i.Preferences.PreferredGeographies : null,
                TicketSizeMin        = i.Preferences != null ? i.Preferences.MinInvestmentSize : (decimal?)null,
                TicketSizeMax        = i.Preferences != null ? i.Preferences.MaxInvestmentSize : (decimal?)null,
                InvestorType         = i.KycSubmissions.Where(k => k.IsActive).Select(k => k.InvestorCategory).FirstOrDefault(),
                AcceptedConnectionCount = i.StartupConnections.Count(c => c.ConnectionStatus == ConnectionStatus.Accepted),
                PortfolioCount       = i.PortfolioCompanies.Count()
            })
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // In-memory mapping — merge connection state
        var items = raw.Select(i =>
        {
            var hasConn = connectionsByInvestor.TryGetValue(i.InvestorID, out var conn);
            var connStatus = hasConn ? conn.Status switch
            {
                ConnectionStatus.Requested    => "REQUESTED",
                ConnectionStatus.Accepted     => "ACCEPTED",
                ConnectionStatus.InDiscussion => "IN_DISCUSSION",
                _                             => "NONE"
            } : "NONE";

            var accepting  = i.AcceptingConnections;
            var canRequest = accepting && connStatus == "NONE";

            return new InvestorSearchItemDto
            {
                InvestorID              = i.InvestorID,
                FullName                = i.FullName,
                FirmName                = i.FirmName,
                Title                   = i.Title,
                Bio                     = i.Bio,
                ProfilePhotoURL         = i.ProfilePhotoURL,
                Location                = i.Location,
                Country                 = i.Country,
                LinkedInURL             = i.LinkedInURL,
                Website                 = i.Website,
                UpdatedAt               = i.UpdatedAt,
                InvestorType            = i.InvestorType,
                KycVerified             = i.InvestorTag != InvestorTag.None,
                AcceptingConnections    = accepting,
                CanRequestConnection    = canRequest,
                ConnectionStatus        = connStatus,
                InitiatedByRole         = hasConn ? (conn.InitiatedBy == requestingUserId ? "STARTUP" : "INVESTOR") : null,
                ConnectionId            = hasConn ? conn.ConnectionId : (int?)null,
                AcceptedConnectionCount = i.AcceptedConnectionCount,
                PortfolioCount          = i.PortfolioCount,
                PreferredIndustries     = i.Industries,
                PreferredStages         = i.Stages,
                PreferredGeographies    = string.IsNullOrWhiteSpace(i.RawGeographies)
                                          ? new List<string>()
                                          : i.RawGeographies.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                            .Select(g => g.Trim())
                                                            .ToList(),
                TicketSizeMin           = i.TicketSizeMin,
                TicketSizeMax           = i.TicketSizeMax,
            };
        }).ToList();

        return ApiResponse<PagedResponse<InvestorSearchItemDto>>.SuccessResponse(new PagedResponse<InvestorSearchItemDto>
        {
            Items = items,
            Paging = new PagingInfo
            {
                Page       = page,
                PageSize   = pageSize,
                TotalItems = totalItems
            }
        });
    }

    public async Task<ApiResponse<InvestorDetailForStartupDto>> GetInvestorByIdAsync(int investorId)
    {
        var investor = await _context.Investors
            .AsNoTracking()
            .Include(i => i.Preferences)
            .Include(i => i.IndustryFocus)
            .Include(i => i.StageFocus)
            .Include(i => i.PortfolioCompanies)
            .Include(i => i.KycSubmissions)
            .FirstOrDefaultAsync(i => i.InvestorID == investorId
                && (i.ProfileStatus == ProfileStatus.Approved || i.ProfileStatus == ProfileStatus.PendingKYC));

        if (investor == null)
            return ApiResponse<InvestorDetailForStartupDto>.ErrorResponse("INVESTOR_NOT_FOUND", "Investor not found.");

        var discoverable = investor.AcceptingConnections;
        var activeKyc = investor.KycSubmissions.FirstOrDefault(s => s.IsActive);

        return ApiResponse<InvestorDetailForStartupDto>.SuccessResponse(new InvestorDetailForStartupDto
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
            InvestorType = activeKyc?.InvestorCategory,
            PreferredIndustries = investor.IndustryFocus.Select(f => f.Industry).ToList(),
            PreferredStages = investor.StageFocus.Select(s => s.Stage.ToString()).ToList(),
            TicketSizeMin = investor.Preferences?.MinInvestmentSize,
            TicketSizeMax = investor.Preferences?.MaxInvestmentSize,
            PortfolioCount = investor.PortfolioCompanies.Count,
            UpdatedAt = investor.UpdatedAt,
            DiscoverableForStartups = discoverable,
            CanRequestConnection = discoverable,
            ProfileAvailabilityReason = discoverable ? "OPEN" : "INVESTOR_PAUSED_DISCOVERY"
        });
    }
}
