using AISEP.Application.DTOs.Advisor;
using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Investor;
using AISEP.Application.DTOs.Staff;
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
using AISEP.Application.DTOs.Notification;

namespace AISEP.Infrastructure.Services
{
    public class RegistrationApprovalService : IRegistrationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RegistrationApprovalService> _logger;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly INotificationDeliveryService _notifications;
        private readonly IAuditService _audit;

        public RegistrationApprovalService(
            ApplicationDbContext context,
            ILogger<RegistrationApprovalService> logger,
            ICloudinaryService cloudinaryService,
            INotificationDeliveryService notifications,
            IAuditService audit)
        {
            _context = context;
            _logger = logger;
            _cloudinaryService = cloudinaryService;
            _notifications = notifications;
            _audit = audit;
        }

        public async Task<ApiResponse<StartupKycSubmissionDto>> ApproveStartupRegistrationAsync(int staffId, ApproveStartupRegistrationRequest startupRegistrationRequest)
        {
            var startupSubmission = await _context.StartupKycSubmissions
                .Include(s => s.Startup)
                .Include(s => s.EvidenceFiles)
                .Include(s => s.RequestedAdditionalItems)
                .FirstOrDefaultAsync(s => s.StartupID == startupRegistrationRequest.StartupId && s.IsActive);

            if (startupSubmission == null)
            {
                return ApiResponse<StartupKycSubmissionDto>.ErrorResponse("STARTUP_KYC_SUBMISSION_NOT_FOUND",
                    "No active startup KYC submission was found for this startup.");
            }

            var reviewedStartup = startupSubmission.Startup;
            var reviewedAt = DateTime.UtcNow;

            startupSubmission.ReviewedAt = reviewedAt;
            startupSubmission.ReviewedBy = staffId;
            startupSubmission.UpdatedAt = reviewedAt;
            startupSubmission.Remarks = string.IsNullOrWhiteSpace(startupRegistrationRequest.Remarks)
                ? null
                : startupRegistrationRequest.Remarks.Trim();
            startupSubmission.RequiresNewEvidence = startupRegistrationRequest.RequiresNewEvidence;

            if (startupRegistrationRequest.Score >= 10)
            {
                startupSubmission.WorkflowStatus = StartupKycWorkflowStatus.Approved;
                startupSubmission.ResultLabel = StartupKycResultLabel.VerifiedCompany;
                startupSubmission.Explanation = "Startup KYC has been approved as a verified company.";
                startupSubmission.RequiresNewEvidence = false;
                reviewedStartup.ProfileStatus = ProfileStatus.Approved;
                reviewedStartup.StartupTag = StartupTag.VerifiedCompany;
                reviewedStartup.ApprovedAt = reviewedAt;
                reviewedStartup.ApprovedBy = staffId;
            }
            else if (startupRegistrationRequest.Score >= 6)
            {
                startupSubmission.WorkflowStatus = StartupKycWorkflowStatus.Approved;
                startupSubmission.ResultLabel = StartupKycResultLabel.BasicVerified;
                startupSubmission.Explanation = "Startup KYC has been approved as basic verified.";
                startupSubmission.RequiresNewEvidence = false;
                reviewedStartup.ProfileStatus = ProfileStatus.Approved;
                reviewedStartup.StartupTag = StartupTag.BasicVerified;
                reviewedStartup.ApprovedAt = reviewedAt;
                reviewedStartup.ApprovedBy = staffId;
            }
            else if (startupRegistrationRequest.Score >= 2)
            {
                startupSubmission.WorkflowStatus = StartupKycWorkflowStatus.PendingMoreInfo;
                startupSubmission.ResultLabel = StartupKycResultLabel.PendingMoreInfo;
                startupSubmission.Explanation = "Additional information has been requested for this KYC submission.";
                reviewedStartup.ProfileStatus = ProfileStatus.PendingKYC;
                reviewedStartup.StartupTag = StartupTag.PendingMoreInfo;
                reviewedStartup.UpdatedAt = reviewedAt;
            }
            else
            {
                startupSubmission.WorkflowStatus = StartupKycWorkflowStatus.Rejected;
                startupSubmission.ResultLabel = StartupKycResultLabel.VerificationFailed;
                startupSubmission.Explanation = "Startup KYC has been rejected.";
                reviewedStartup.ProfileStatus = ProfileStatus.Rejected;
                reviewedStartup.StartupTag = StartupTag.VerificationFailed;
                reviewedStartup.UpdatedAt = reviewedAt;
            }

            await using var tx = await _context.Database.BeginTransactionAsync();
            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            // Notify Startup
            await _notifications.CreateAndPushAsync(new CreateNotificationRequest
            {
                UserId = reviewedStartup.UserID,
                NotificationType = "VERIFICATION",
                Title = "Cập nhật trạng thái KYC",
                Message = startupSubmission.WorkflowStatus switch
                {
                    StartupKycWorkflowStatus.Approved => "Chúc mừng! Hồ sơ KYC của bạn đã được phê duyệt.",
                    StartupKycWorkflowStatus.PendingMoreInfo => "Hồ sơ KYC của bạn cần bổ sung thêm thông tin. Vui lòng kiểm tra lại.",
                    StartupKycWorkflowStatus.Rejected => $"Hồ sơ KYC của bạn đã bị từ chối. Lý do: {startupSubmission.Remarks ?? "Không có lý do cụ thể."}",
                    _ => "Trạng thái hồ sơ KYC của bạn đã thay đổi."
                },
                RelatedEntityType = "StartupKycSubmission",
                RelatedEntityId = startupSubmission.SubmissionID,
                ActionUrl = "/startup/verification"
            });

            return ApiResponse<StartupKycSubmissionDto>.SuccessResponse(
                MapToKycSubmissionDto(startupSubmission),
                "Startup reviewed successfully");
#if false
            var startup = await _context.Startups.FirstOrDefaultAsync(s => s.StartupID == startupRegistrationRequest.StartupId);

            if (startup == null)
            {
                return ApiResponse<Startup>.ErrorResponse("STARTUP_PROFILE_DOES_NOT_EXISTS",
                "Startup profile does not exist");
            }

            startup.ProfileStatus = ProfileStatus.Approved;
            startup.ApprovedAt = DateTime.UtcNow;
            startup.ApprovedBy = staffId;

            if (startupRegistrationRequest.Score >= 10)
            {
                startup.StartupTag = StartupTag.VerifiedCompany;
            }else if (startupRegistrationRequest.Score >= 6 && startupRegistrationRequest.Score <= 9)
            {
                startup.StartupTag = StartupTag.BasicVerified;
            }else if (startupRegistrationRequest.Score >= 2 && startupRegistrationRequest.Score <= 5)
            {
                startup.StartupTag = StartupTag.VerificationFailed;
                startup.ProfileStatus = ProfileStatus.Rejected; // Overwrite if failed
            }

            _context.Startups.Update(startup);
            await _context.SaveChangesAsync();

            return ApiResponse<Startup>.SuccessResponse(startup, "Startup reviewed successfully");
#endif
        }

        public async Task<ApiResponse<Advisor>> ApproveAdvisorRegistrationAsync(int staffId, ApproveAdvisorRegistrationRequest request)
        {
            var advisor = await _context.Advisors.FirstOrDefaultAsync(a => a.AdvisorID == request.AdvisorId);
            if (advisor == null)
            {
                return ApiResponse<Advisor>.ErrorResponse("ADVISOR_PROFILE_DOES_NOT_EXISTS", "Advisor profile does not exist");
            }

            advisor.ProfileStatus = ProfileStatus.Approved;
            advisor.IsVerified = true;
            advisor.ApprovedAt = DateTime.UtcNow;
            advisor.ApprovedBy = staffId;
            advisor.RejectionRemarks = string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim();

            if (request.Score >= 11)
            {
                advisor.AdvisorTag = AdvisorTag.VerifiedAdvisor;
            }
            else if (request.Score >= 7)
            {
                advisor.AdvisorTag = AdvisorTag.BasicVerified;
            }
            else if (request.Score >= 3)
            {
                advisor.AdvisorTag = AdvisorTag.PendingMoreInfo;
                advisor.ProfileStatus = ProfileStatus.Pending; // Stay pending for more info
            }
            else
            {
                advisor.AdvisorTag = AdvisorTag.VerificationFailed;
                advisor.ProfileStatus = ProfileStatus.Rejected;
            }

            _context.Advisors.Update(advisor);
            await _context.SaveChangesAsync();

            // Audit log for KYC timeline reconstruction
            var reviewAuditAction = advisor.AdvisorTag switch
            {
                AdvisorTag.VerifiedAdvisor or AdvisorTag.BasicVerified => "APPROVE_ADVISOR_KYC",
                AdvisorTag.PendingMoreInfo => "REQUEST_MORE_INFO_ADVISOR_KYC",
                _ => "REJECT_ADVISOR_KYC"
            };
            var reviewResultLabel = advisor.AdvisorTag switch
            {
                AdvisorTag.VerifiedAdvisor    => "VERIFIED_ADVISOR",
                AdvisorTag.BasicVerified      => "BASIC_VERIFIED",
                AdvisorTag.PendingMoreInfo    => "PENDING_MORE_INFO",
                _ => "VERIFICATION_FAILED"
            };
            await _audit.LogAsync(staffId, reviewAuditAction, "Advisor", advisor.AdvisorID,
                JsonSerializer.Serialize(new { resultLabel = reviewResultLabel, remarks = advisor.RejectionRemarks, requiresNewEvidence = false }),
                "system", "system");

            // Notify Advisor
            await _notifications.CreateAndPushAsync(new CreateNotificationRequest
            {
                UserId = advisor.UserID,
                NotificationType = "VERIFICATION",
                Title = "Cập nhật trạng thái KYC",
                Message = advisor.ProfileStatus switch
                {
                    ProfileStatus.Approved => "Chúc mừng! Hồ sơ của bạn đã được phê duyệt.",
                    ProfileStatus.Pending => "Hồ sơ của bạn cần bổ sung thêm thông tin. Vui lòng kiểm tra lại.",
                    ProfileStatus.Rejected => $"Hồ sơ của bạn đã bị từ chối. Lý do: {advisor.RejectionRemarks ?? "Không có lý do cụ thể."}",
                    _ => "Trạng thái hồ sơ của bạn đã thay đổi."
                },
                RelatedEntityType = "Advisor",
                RelatedEntityId = advisor.AdvisorID,
                ActionUrl = "/advisor/kyc"
            });

            return ApiResponse<Advisor>.SuccessResponse(advisor, "Advisor reviewed successfully");
        }

        public async Task<ApiResponse<Investor>> ApproveInvestorRegistrationAsync(int staffId, ApproveInvestorRegistrationRequest request)
        {
            var submission = await _context.InvestorKycSubmissions
                .Include(s => s.Investor)
                .Include(s => s.EvidenceFiles)
                .FirstOrDefaultAsync(s => s.InvestorID == request.InvestorId && s.IsActive);

            if (submission == null)
            {
                // Fallback: find the investor even without an active submission
                var inv = await _context.Investors.FirstOrDefaultAsync(i => i.InvestorID == request.InvestorId);
                if (inv == null)
                    return ApiResponse<Investor>.ErrorResponse("INVESTOR_PROFILE_DOES_NOT_EXISTS", "Investor profile does not exist");
                return ApiResponse<Investor>.ErrorResponse("INVESTOR_KYC_SUBMISSION_NOT_FOUND", "No active KYC submission found for this investor.");
            }

            var investor = submission.Investor;
            var reviewedAt = DateTime.UtcNow;

            InvestorTag awardedTag;
            InvestorKycResultLabel resultLabel;
            InvestorKycWorkflowStatus workflowStatus;
            ProfileStatus profileStatus;

            if (request.IsInstitutional)
            {
                if (request.Score >= 10)      { awardedTag = InvestorTag.VerifiedInvestorEntity; resultLabel = InvestorKycResultLabel.VerifiedInvestorEntity; workflowStatus = InvestorKycWorkflowStatus.Approved; profileStatus = ProfileStatus.Approved; }
                else if (request.Score >= 6)  { awardedTag = InvestorTag.BasicVerified;          resultLabel = InvestorKycResultLabel.BasicVerified;          workflowStatus = InvestorKycWorkflowStatus.Approved; profileStatus = ProfileStatus.Approved; }
                else if (request.Score >= 2)  { awardedTag = InvestorTag.None;                   resultLabel = InvestorKycResultLabel.PendingMoreInfo;         workflowStatus = InvestorKycWorkflowStatus.PendingMoreInfo; profileStatus = ProfileStatus.PendingKYC; }
                else                          { awardedTag = InvestorTag.None;                   resultLabel = InvestorKycResultLabel.VerificationFailed;     workflowStatus = InvestorKycWorkflowStatus.Rejected; profileStatus = ProfileStatus.Rejected; }
            }
            else
            {
                if (request.Score >= 8)      { awardedTag = InvestorTag.VerifiedAngelInvestor; resultLabel = InvestorKycResultLabel.VerifiedAngelInvestor; workflowStatus = InvestorKycWorkflowStatus.Approved; profileStatus = ProfileStatus.Approved; }
                else if (request.Score >= 5) { awardedTag = InvestorTag.BasicVerified;         resultLabel = InvestorKycResultLabel.BasicVerified;         workflowStatus = InvestorKycWorkflowStatus.Approved; profileStatus = ProfileStatus.Approved; }
                else if (request.Score >= 2) { awardedTag = InvestorTag.None;                  resultLabel = InvestorKycResultLabel.PendingMoreInfo;       workflowStatus = InvestorKycWorkflowStatus.PendingMoreInfo; profileStatus = ProfileStatus.PendingKYC; }
                else                         { awardedTag = InvestorTag.None;                  resultLabel = InvestorKycResultLabel.VerificationFailed;    workflowStatus = InvestorKycWorkflowStatus.Rejected; profileStatus = ProfileStatus.Rejected; }
            }

            submission.WorkflowStatus = workflowStatus;
            submission.ResultLabel = resultLabel;
            submission.ReviewedAt = reviewedAt;
            submission.ReviewedBy = staffId;
            submission.UpdatedAt = reviewedAt;
            submission.Explanation = workflowStatus == InvestorKycWorkflowStatus.Approved
                ? "KYC has been approved."
                : workflowStatus == InvestorKycWorkflowStatus.PendingMoreInfo
                    ? "Additional information is required."
                    : "KYC has been rejected.";
            submission.Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? null : request.Remarks.Trim();

            investor.InvestorTag = awardedTag;
            investor.ProfileStatus = profileStatus;
            investor.ApprovedAt = workflowStatus == InvestorKycWorkflowStatus.Approved ? reviewedAt : null;
            investor.ApprovedBy = workflowStatus == InvestorKycWorkflowStatus.Approved ? staffId : null;
            investor.UpdatedAt = reviewedAt;

            // Auto-disable AcceptingConnections when KYC is not approved
            if (workflowStatus != InvestorKycWorkflowStatus.Approved && investor.AcceptingConnections)
            {
                investor.AcceptingConnections = false;
                _logger.LogInformation("Auto-disabled AcceptingConnections for investor {InvestorId} due to KYC status: {Status}.",
                    investor.InvestorID, workflowStatus);
            }

            await _context.SaveChangesAsync();

            // Notify Investor
            await _notifications.CreateAndPushAsync(new CreateNotificationRequest
            {
                UserId = investor.UserID,
                NotificationType = "VERIFICATION",
                Title = "Cập nhật trạng thái KYC",
                Message = submission.WorkflowStatus switch
                {
                    InvestorKycWorkflowStatus.Approved => "Chúc mừng! Hồ sơ KYC của bạn đã được phê duyệt.",
                    InvestorKycWorkflowStatus.PendingMoreInfo => "Hồ sơ KYC của bạn cần bổ sung thêm thông tin. Vui lòng kiểm tra lại.",
                    InvestorKycWorkflowStatus.Rejected => $"Hồ sơ KYC của bạn đã bị từ chối. Lý do: {submission.Remarks ?? "Không có lý do cụ thể."}",
                    _ => "Trạng thái hồ sơ KYC của bạn đã thay đổi."
                },
                RelatedEntityType = "InvestorKycSubmission",
                RelatedEntityId = submission.SubmissionID,
                ActionUrl = "/investor/verification"
            });

            return ApiResponse<Investor>.SuccessResponse(investor, "Investor reviewed successfully");
        }

        public async Task<ApiResponse<StartupKycSubmissionDto>> RejectStartupRegistrationAsync(int staffId, RejectRegistrationRequest request)
        {
            var startupSubmission = await _context.StartupKycSubmissions
                .Include(s => s.Startup)
                .Include(s => s.EvidenceFiles)
                .Include(s => s.RequestedAdditionalItems)
                .FirstOrDefaultAsync(s => s.StartupID == request.Id && s.IsActive);

            if (startupSubmission == null)
            {
                return ApiResponse<StartupKycSubmissionDto>.ErrorResponse("STARTUP_KYC_SUBMISSION_NOT_FOUND",
                    "No active startup KYC submission was found for this startup.");
            }

            var reviewedAt = DateTime.UtcNow;
            startupSubmission.WorkflowStatus = StartupKycWorkflowStatus.Rejected;
            startupSubmission.ResultLabel = StartupKycResultLabel.VerificationFailed;
            startupSubmission.Explanation = "Startup KYC has been rejected.";
            startupSubmission.Remarks = request.Reason;
            startupSubmission.RequiresNewEvidence = request.RequiresNewEvidence ?? false;
            startupSubmission.ReviewedAt = reviewedAt;
            startupSubmission.ReviewedBy = staffId;
            startupSubmission.UpdatedAt = reviewedAt;

            startupSubmission.Startup.ProfileStatus = ProfileStatus.Rejected;
            startupSubmission.Startup.StartupTag = StartupTag.VerificationFailed;
            startupSubmission.Startup.UpdatedAt = reviewedAt;

            await _context.SaveChangesAsync();

            await _notifications.CreateAndPushAsync(new CreateNotificationRequest
            {
                UserId = startupSubmission.Startup.UserID,
                NotificationType = "VERIFICATION",
                Title = "Cập nhật trạng thái KYC",
                Message = $"Hồ sơ KYC của bạn đã bị từ chối. Lý do: {startupSubmission.Remarks ?? "Không có lý do cụ thể."}",
                RelatedEntityType = "StartupKycSubmission",
                RelatedEntityId = startupSubmission.SubmissionID,
                ActionUrl = "/startup/verification"
            });

            return ApiResponse<StartupKycSubmissionDto>.SuccessResponse(
                MapToKycSubmissionDto(startupSubmission),
                "Rejected successfully");
#if false
            var startup = await _context.Startups.FirstOrDefaultAsync(s => s.StartupID == request.Id);
            if (startup == null)
                return ApiResponse<Startup>.ErrorResponse("NOT_FOUND", "Profile not found");

            startup.ProfileStatus = ProfileStatus.Rejected;
            // Optionally store the reject reason somewhere (maybe a notification or comment field)
            _context.Startups.Update(startup);
            await _context.SaveChangesAsync();
            return ApiResponse<Startup>.SuccessResponse(startup, "Rejected successfully");
#endif
        }

        public async Task<ApiResponse<Advisor>> RejectAdvisorRegistrationAsync(int staffId, RejectRegistrationRequest request)
        {
            var advisor = await _context.Advisors.FirstOrDefaultAsync(a => a.AdvisorID == request.Id);
            if (advisor == null)
                return ApiResponse<Advisor>.ErrorResponse("NOT_FOUND", "Profile not found");

            var rejectedAt = DateTime.UtcNow;
            advisor.ProfileStatus = ProfileStatus.Rejected;
            advisor.AdvisorTag = AdvisorTag.VerificationFailed;
            advisor.RequiresNewEvidence = request.RequiresNewEvidence ?? false;
            advisor.RejectionRemarks = request.Reason;
            advisor.ApprovedAt = rejectedAt;
            advisor.ApprovedBy = staffId;
            advisor.UpdatedAt = rejectedAt;

            _context.Advisors.Update(advisor);
            await _context.SaveChangesAsync();

            // Audit log for KYC timeline reconstruction
            await _audit.LogAsync(staffId, "REJECT_ADVISOR_KYC", "Advisor", advisor.AdvisorID,
                JsonSerializer.Serialize(new { resultLabel = "VERIFICATION_FAILED", remarks = advisor.RejectionRemarks, requiresNewEvidence = advisor.RequiresNewEvidence }),
                "system", "system");

            await _notifications.CreateAndPushAsync(new CreateNotificationRequest
            {
                UserId = advisor.UserID,
                NotificationType = "VERIFICATION",
                Title = "Cập nhật trạng thái KYC",
                Message = $"Hồ sơ của bạn đã bị từ chối. Lý do: {advisor.RejectionRemarks ?? "Không có lý do cụ thể."}",
                RelatedEntityType = "Advisor",
                RelatedEntityId = advisor.AdvisorID,
                ActionUrl = "/advisor/kyc"
            });

            return ApiResponse<Advisor>.SuccessResponse(advisor, "Rejected successfully");
        }

        public async Task<ApiResponse<Investor>> RejectInvestorRegistrationAsync(int staffId, RejectRegistrationRequest request)
        {
            var submission = await _context.InvestorKycSubmissions
                .Include(s => s.Investor)
                .FirstOrDefaultAsync(s => s.InvestorID == request.Id && s.IsActive);

            if (submission == null)
                return ApiResponse<Investor>.ErrorResponse("INVESTOR_KYC_SUBMISSION_NOT_FOUND", "No active KYC submission found for this investor.");

            var rejectedAt = DateTime.UtcNow;
            submission.WorkflowStatus = InvestorKycWorkflowStatus.Rejected;
            submission.ResultLabel = InvestorKycResultLabel.VerificationFailed;
            submission.Explanation = "Investor KYC has been rejected.";
            submission.Remarks = request.Reason;
            submission.RequiresNewEvidence = request.RequiresNewEvidence ?? false;
            submission.ReviewedAt = rejectedAt;
            submission.ReviewedBy = staffId;
            submission.UpdatedAt = rejectedAt;

            var investor = submission.Investor;
            investor.ProfileStatus = ProfileStatus.Rejected;
            investor.InvestorTag = InvestorTag.None;
            investor.UpdatedAt = rejectedAt;

            // Auto-disable AcceptingConnections on explicit rejection
            if (investor.AcceptingConnections)
            {
                investor.AcceptingConnections = false;
                _logger.LogInformation("Auto-disabled AcceptingConnections for investor {InvestorId} due to registration rejection.",
                    investor.InvestorID);
            }

            await _context.SaveChangesAsync();

            await _notifications.CreateAndPushAsync(new CreateNotificationRequest
            {
                UserId = investor.UserID,
                NotificationType = "VERIFICATION",
                Title = "Cập nhật trạng thái KYC",
                Message = $"Hồ sơ KYC của bạn đã bị từ chối. Lý do: {submission.Remarks ?? "Không có lý do cụ thể."}",
                RelatedEntityType = "InvestorKycSubmission",
                RelatedEntityId = submission.SubmissionID,
                ActionUrl = "/investor/verification"
            });

            return ApiResponse<Investor>.SuccessResponse(investor, "Rejected successfully");
        }

        public async Task<ApiResponse<PagedResponse<AdvisorDto>>> GetPendingRegistrationsAdvisorAsync(RegistrationQueryParams registrationQuery)
        {
            var registrations = _context.Advisors
                .Where(s => s.ProfileStatus == ProfileStatus.Pending || s.ProfileStatus == ProfileStatus.PendingKYC)
                .AsNoTracking()
                .AsQueryable();

            var registrationsToDto = registrations.Select(r => new AdvisorDto
            {
                AdvisorID = r.AdvisorID,
                UserId = r.UserID,
                Email = r.User.Email,
                FullName = r.FullName,
                Title = r.Title,
                Bio = r.Bio,
                ProfilePhotoURL = r.ProfilePhotoURL,
                MentorshipPhilosophy = r.MentorshipPhilosophy,
                LinkedInURL = r.LinkedInURL,
                ProfileStatus = r.ProfileStatus.ToString(),
                TotalMentees = r.TotalMentees,
                TotalSessionHours = r.TotalSessionHours,
                AverageRating = r.AverageRating,
                Expertise = r.Expertise,
                YearsOfExperience = r.YearsOfExperience,
                ContactEmail = r.ContactEmail,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                IndustryFocus = r.IndustryFocus.Select(i => new AdvisorIndustryFocusDto
                {
                    IndustryId = i.IndustryID,
                    Industry = i.Industry.IndustryName
                }).ToList()
            }).Paging(registrationQuery.Page, registrationQuery.PageSize);

            return ApiResponse<PagedResponse<AdvisorDto>>.SuccessResponse
                (
                     new PagedResponse<AdvisorDto>
                     {
                         Items = await registrationsToDto.ToListAsync(),
                         Paging = new PagingInfo
                         {
                             Page = registrationQuery.Page,
                             PageSize = registrationQuery.PageSize,
                             TotalItems = await registrations.CountAsync()
                         }
                     }
                );
        }

        public async Task<ApiResponse<PagedResponse<InvestorDto>>> GetPendingRegistrationsInvestorAsync(RegistrationQueryParams registrationQuery)
        {
            var submissionQuery = _context.InvestorKycSubmissions
                .AsNoTracking()
                .Include(s => s.Investor)
                    .ThenInclude(i => i.User)
                .Where(s => s.IsActive
                    && (s.WorkflowStatus == InvestorKycWorkflowStatus.UnderReview
                        || s.WorkflowStatus == InvestorKycWorkflowStatus.PendingMoreInfo))
                .AsQueryable();

            var items = submissionQuery
                .OrderByDescending(s => s.UpdatedAt)
                .Select(s => new InvestorDto
                {
                    InvestorID = s.InvestorID,
                    UserID = s.Investor.UserID,
                    Email = s.Investor.User.Email,
                    FullName = s.Investor.FullName,
                    FirmName = s.Investor.FirmName,
                    Title = s.Investor.Title,
                    Bio = s.Investor.Bio,
                    ProfilePhotoURL = s.Investor.ProfilePhotoURL,
                    InvestmentThesis = s.Investor.InvestmentThesis,
                    Location = s.Investor.Location,
                    Country = s.Investor.Country,
                    LinkedInURL = s.Investor.LinkedInURL,
                    Website = s.Investor.Website,
                    ProfileStatus = s.Investor.ProfileStatus.ToString(),
                    CreatedAt = s.Investor.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                    InvestorType = s.InvestorCategory,
                    ContactEmail = s.ContactEmail,
                    CurrentOrganization = s.OrganizationName,
                    CurrentRoleTitle = s.CurrentRoleTitle
                }).Paging(registrationQuery.Page, registrationQuery.PageSize);

            return ApiResponse<PagedResponse<InvestorDto>>.SuccessResponse(
                new PagedResponse<InvestorDto>
                {
                    Items = await items.ToListAsync(),
                    Paging = new PagingInfo
                    {
                        Page = registrationQuery.Page,
                        PageSize = registrationQuery.PageSize,
                        TotalItems = await submissionQuery.CountAsync()
                    }
                });
        }

        public async Task<ApiResponse<PagedResponse<StartupListItemDto>>> GetPendingRegistrationsStartupAsync(RegistrationQueryParams registrationQuery)
        {
            var submissionQuery = _context.StartupKycSubmissions
                .AsNoTracking()
                .Include(s => s.Startup).ThenInclude(s => s.Industry!).ThenInclude(i => i.ParentIndustry)
                .Include(s => s.Startup).ThenInclude(s => s.StageRef)
                .Include(s => s.Startup).ThenInclude(s => s.SubIndustryRef)
                .Where(s => s.IsActive
                    && (s.WorkflowStatus == StartupKycWorkflowStatus.UnderReview
                        || s.WorkflowStatus == StartupKycWorkflowStatus.PendingMoreInfo))
                .AsQueryable();

            var startupItems = submissionQuery
                .OrderByDescending(s => s.UpdatedAt)
                .Select(s => new StartupListItemDto
                {
                    StartupID = s.StartupID,
                    CompanyName = s.Startup.CompanyName,
                    StageID = s.Startup.StageID,
                    StageName = s.Startup.StageRef != null ? s.Startup.StageRef.StageName : null,
                    IndustryName = s.Startup.Industry != null ? s.Startup.Industry.IndustryName : null,
                    ParentIndustryName = s.Startup.Industry != null && s.Startup.Industry.ParentIndustry != null ? s.Startup.Industry.ParentIndustry.IndustryName : null,
                    SubIndustryID = s.Startup.SubIndustryID,
                    SubIndustryName = s.Startup.SubIndustryRef != null ? s.Startup.SubIndustryRef.IndustryName : null,
                    LogoURL = s.Startup.LogoURL,
                    ProfileStatus = MapWorkflowStatus(s.WorkflowStatus),
                    UpdatedAt = s.UpdatedAt,
                    StartupVerificationType = MapVerificationType(s.StartupVerificationType)
                }).Paging(registrationQuery.Page, registrationQuery.PageSize);

            return ApiResponse<PagedResponse<StartupListItemDto>>.SuccessResponse(
                new PagedResponse<StartupListItemDto>
                {
                    Items = await startupItems.ToListAsync(),
                    Paging = new PagingInfo
                    {
                        Page = registrationQuery.Page,
                        PageSize = registrationQuery.PageSize,
                        TotalItems = await submissionQuery.CountAsync()
                    }
                });
#if false
            var registrations = _context.Startups
                .Where(s => s.ProfileStatus == ProfileStatus.Pending || s.ProfileStatus == ProfileStatus.PendingKYC)
                .AsNoTracking()
                .AsQueryable();

            var registrationsToDto = registrations.Select(r => new StartupListItemDto
            {
                StartupID = r.StartupID,
                CompanyName = r.CompanyName,
                IndustryName = r.Industry.IndustryName,
                Stage = r.Stage.ToString(),
                LogoURL = r.LogoURL,
                ProfileStatus = r.ProfileStatus.ToString(),
                UpdatedAt = r.UpdatedAt,
            }).Paging(registrationQuery.Page, registrationQuery.PageSize);

            return ApiResponse<PagedResponse<StartupListItemDto>>.SuccessResponse
                (
                     new PagedResponse<StartupListItemDto>
                     {
                         Items = await registrationsToDto.ToListAsync(),
                         Paging = new PagingInfo
                         {
                             Page = registrationQuery.Page,
                             PageSize = registrationQuery.PageSize,
                             TotalItems = await registrations.CountAsync()
                         }
                     }
                );
#endif
        }

        public async Task<ApiResponse<InvestorDto>> GetPendingRegistrationInvestorByIdAsync(int investorId)
        {
            var investor = await _context.Investors
                .Include(i => i.User)
                .FirstOrDefaultAsync(i => i.InvestorID == investorId);

            if (investor == null)
                return ApiResponse<InvestorDto>.ErrorResponse("INVESTOR_NOT_FOUND", "Investor not found");

            var activeSubmission = await _context.InvestorKycSubmissions
                .Include(s => s.EvidenceFiles)
                .FirstOrDefaultAsync(s => s.InvestorID == investorId && s.IsActive);

            var investorToDto = new InvestorDto
            {
                InvestorID = investor.InvestorID,
                UserID = investor.UserID,
                Email = investor.User.Email,
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
                ProfileStatus = investor.ProfileStatus.ToString(),
                CreatedAt = investor.CreatedAt,
                UpdatedAt = investor.UpdatedAt,

                // KYC Information from active submission
                InvestorType = activeSubmission?.InvestorCategory,
                ContactEmail = activeSubmission?.ContactEmail,
                CurrentOrganization = activeSubmission?.OrganizationName,
                CurrentRoleTitle = activeSubmission?.CurrentRoleTitle ?? investor.Title,
                BusinessCode = activeSubmission?.TaxIdOrBusinessCode,
                SubmitterRole = activeSubmission?.SubmitterRole,
                IDProofFileURL = activeSubmission?.EvidenceFiles
                    .Where(f => f.Kind == InvestorKycEvidenceKind.IDProof)
                    .Select(f => _cloudinaryService.ToInlineUrl(f.FileUrl))
                    .FirstOrDefault(),
                InvestmentProofFileURL = activeSubmission?.EvidenceFiles
                    .Where(f => f.Kind == InvestorKycEvidenceKind.InvestmentProof)
                    .Select(f => _cloudinaryService.ToInlineUrl(f.FileUrl))
                    .FirstOrDefault(),
                Remarks = activeSubmission?.Remarks
            };

            return ApiResponse<InvestorDto>.SuccessResponse(investorToDto);
        }

        public async Task<ApiResponse<InvestorKycSubmissionDto>> GetPendingRegistrationInvestorKycByIdAsync(int investorId)
        {
            var investor = await _context.Investors
                .Include(i => i.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.InvestorID == investorId);

            if (investor == null)
                return ApiResponse<InvestorKycSubmissionDto>.ErrorResponse("INVESTOR_NOT_FOUND", "Investor not found");

            var submission = await _context.InvestorKycSubmissions
                .Include(s => s.EvidenceFiles)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.InvestorID == investorId && s.IsActive);

            if (submission == null)
                return ApiResponse<InvestorKycSubmissionDto>.ErrorResponse("INVESTOR_KYC_SUBMISSION_NOT_FOUND",
                    "No active investor KYC submission was found for this investor.");

            return ApiResponse<InvestorKycSubmissionDto>.SuccessResponse(new InvestorKycSubmissionDto
            {
                Id = submission.SubmissionID,
                InvestorId = investorId,
                Version = submission.Version,
                IsActive = submission.IsActive,
                WorkflowStatus = MapInvestorWorkflowStatus(submission.WorkflowStatus),
                ResultLabel = MapInvestorResultLabel(submission.ResultLabel),
                SubmittedAt = submission.SubmittedAt,
                UpdatedAt = submission.UpdatedAt,
                ReviewedAt = submission.ReviewedAt,
                ReviewedBy = submission.ReviewedBy,
                Remarks = submission.Remarks,
                RequiresNewEvidence = submission.RequiresNewEvidence,

                // Investor profile context
                FullName = investor.FullName,
                Email = investor.User.Email,
                ProfileStatus = investor.ProfileStatus.ToString(),
                ProfilePhotoURL = investor.ProfilePhotoURL,

                SubmissionSummary = new InvestorKYCSubmissionSummaryDto
                {
                    FullName = submission.FullName,
                    InvestorCategory = submission.InvestorCategory,
                    ContactEmail = submission.ContactEmail,
                    OrganizationName = submission.OrganizationName,
                    CurrentRoleTitle = submission.CurrentRoleTitle,
                    Location = submission.Location,
                    Website = submission.Website,
                    LinkedInURL = submission.LinkedInURL,
                    SubmitterRole = submission.SubmitterRole,
                    TaxIdOrBusinessCode = submission.TaxIdOrBusinessCode,
                    SubmittedAt = submission.SubmittedAt,
                    Version = submission.Version,
                    EvidenceFiles = submission.EvidenceFiles
                        .OrderBy(f => f.UploadedAt)
                        .Select(f => new InvestorKYCEvidenceFileDto
                        {
                            Id = f.EvidenceFileID,
                            FileName = f.FileName,
                            FileType = f.ContentType,
                            FileSize = f.FileSize,
                            UploadedAt = f.UploadedAt,
                            Kind = MapInvestorEvidenceKind(f.Kind),
                            Url = _cloudinaryService.ToInlineUrl(f.FileUrl),
                            StorageKey = !string.IsNullOrWhiteSpace(f.StorageKey)
                                ? f.StorageKey
                                : _cloudinaryService.ExtractDocumentStorageKeyFromUrl(f.FileUrl)
                        })
                        .ToList()
                }
            });
        }

        public async Task<ApiResponse<AdvisorDto>> GetPendingRegistrationAdvisorByIdAsync(int advisorId)
        {
            var advisor = await _context.Advisors
                .Include(a => a.IndustryFocus)
                    .ThenInclude(i => i.Industry)
                .Include(a => a.User)
                .FirstOrDefaultAsync(i => i.AdvisorID == advisorId);

            if (advisor == null)
                return ApiResponse<AdvisorDto>.ErrorResponse("ADVISOR_NOT_FOUND", "Advisor not found");

            var advisorToDto = new AdvisorDto
            {
                AdvisorID = advisorId,
                UserId = advisor.UserID,
                Email = advisor.User.Email,
                FullName = advisor.FullName,
                Title = advisor.Title,
                Bio = advisor.Bio,
                ProfilePhotoURL = advisor.ProfilePhotoURL,
                MentorshipPhilosophy = advisor.MentorshipPhilosophy,
                LinkedInURL = advisor.LinkedInURL,
                ProfileStatus = advisor.ProfileStatus.ToString(),
                TotalMentees = advisor.TotalMentees,
                TotalSessionHours = advisor.TotalSessionHours,
                AverageRating = advisor.AverageRating,
                Expertise = advisor.Expertise,
                YearsOfExperience = advisor.YearsOfExperience,
                CurrentOrganization = advisor.CurrentOrganization,
                BasicExpertiseProofFileURL = advisor.BasicExpertiseProofFileURL,
                ContactEmail = advisor.ContactEmail,
                CreatedAt = advisor.CreatedAt,
                UpdatedAt = advisor.UpdatedAt,
                IndustryFocus = advisor.IndustryFocus.Select(i => new AdvisorIndustryFocusDto
                {
                    IndustryId = i.IndustryID,
                    Industry = i.Industry.IndustryName
                }).ToList(),
                SubmissionSummary = advisor.BasicExpertiseProofFileURL != null
                    ? new AdvisorDocumentSummaryDto
                    {
                        EvidenceFiles = new List<AdvisorEvidenceFileDto>
                        {
                            new AdvisorEvidenceFileDto
                            {
                                Id = 1,
                                Url = _cloudinaryService.ToInlineUrl(advisor.BasicExpertiseProofFileURL),
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
                    }
                    : null
            };

            return ApiResponse<AdvisorDto>.SuccessResponse(advisorToDto);
        }

        public async Task<ApiResponse<StartupDto>> GetPendingRegistrationStartupByIdAsync(int startupId)
        {
            var startup = await _context.Startups
               .Include(s => s.TeamMembers)
               .Include(s => s.Industry)
               .Include(s => s.StageRef)
               .Include(s => s.SubIndustryRef)
               .FirstOrDefaultAsync(i => i.StartupID == startupId);

            if (startup == null)
                return ApiResponse<StartupDto>.ErrorResponse("STARTUP_NOT_FOUND", "Startup not found");

            var startupToDto = new StartupDto
            {
                StartupID = startupId,
                UserID = startup.UserID,
                CompanyName = startup.CompanyName,
                OneLiner = startup.OneLiner,
                Description = startup.Description,
                IndustryID = startup.IndustryID,
                IndustryName = startup.Industry?.IndustryName ?? string.Empty,
                StageID = startup.StageID,
                StageName = startup.StageRef?.StageName,
                FoundedDate = startup.FoundedDate,
                Website = startup.Website,
                LogoURL = startup.LogoURL,
                FundingAmountSought = startup.FundingAmountSought,
                CurrentFundingRaised = startup.CurrentFundingRaised,
                Valuation = startup.Valuation,
                FullNameOfApplicant = startup.FullNameOfApplicant,
                RoleOfApplicant = startup.RoleOfApplicant,
                ContactEmail = startup.ContactEmail,
                ContactPhone = startup.ContactPhone,
                BusinessCode = startup.BusinessCode,
                SubIndustryID = startup.SubIndustryID,
                SubIndustryName = startup.SubIndustryRef?.IndustryName,
                MarketScope = startup.MarketScope,
                ProductStatus = startup.ProductStatus,
                Location = startup.Location,
                Country = startup.Country,
                ProblemStatement = startup.ProblemStatement,
                SolutionSummary = startup.SolutionSummary,
                CurrentNeeds = DeserializeCurrentNeeds(startup.CurrentNeeds),
                MetricSummary = startup.MetricSummary,
                PitchDeckUrl = startup.PitchDeckUrl,
                LinkedInURL = startup.LinkedInURL,
                TeamSize = startup.TeamSize,
                ProfileStatus = startup.ProfileStatus.ToString(),
                CreatedAt = startup.CreatedAt,
                UpdatedAt = startup.UpdatedAt,
                TeamMembers = startup.TeamMembers.Select(m => new TeamMemberPublicDto
                {
                    FullName = m.FullName,
                    Role = m.Role,
                    Title = m.Title,
                    LinkedInURL = m.LinkedInURL,
                    Bio = m.Bio,
                    PhotoURL = m.PhotoURL,
                    IsFounder = m.IsFounder,
                }).ToList()
            };

            return ApiResponse<StartupDto>.SuccessResponse(startupToDto);
        }

        public async Task<ApiResponse<StartupKycSubmissionDto>> GetPendingRegistrationStartupKycByIdAsync(int startupId)
        {
            var submission = await _context.StartupKycSubmissions
                .AsNoTracking()
                .Include(s => s.Startup)
                .Include(s => s.EvidenceFiles)
                .Include(s => s.RequestedAdditionalItems)
                .FirstOrDefaultAsync(s => s.StartupID == startupId && s.IsActive);

            if (submission == null)
            {
                return ApiResponse<StartupKycSubmissionDto>.ErrorResponse("STARTUP_KYC_SUBMISSION_NOT_FOUND",
                    "No active startup KYC submission was found for this startup.");
            }

            var dto = MapToKycSubmissionDto(submission);
            dto.LogoURL = submission.Startup?.LogoURL;
            dto.CompanyName = submission.Startup?.CompanyName;
            return ApiResponse<StartupKycSubmissionDto>.SuccessResponse(dto);
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
                SubmissionSummary = new StartupKYCSubmissionSummaryDto
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
                            Url = _cloudinaryService.ToInlineUrl(f.FileUrl),
                            StorageKey = !string.IsNullOrWhiteSpace(f.StorageKey)
                                ? f.StorageKey
                                : _cloudinaryService.ExtractDocumentStorageKeyFromUrl(f.FileUrl)
                        })
                        .ToList()
                },
                RequestedAdditionalItems = submission.RequestedAdditionalItems
                    .OrderBy(i => i.CreatedAt)
                    .Select(i => new StartupKycRequestedItemDto
                    {
                        Id = i.RequestedItemID,
                        FieldKey = i.FieldKey,
                        Label = i.Label,
                        Reason = i.Reason,
                        CreatedAt = i.CreatedAt,
                        ResolvedAt = i.ResolvedAt
                    })
                    .ToList(),
                Explanation = submission.Explanation,
                Remarks = submission.Remarks,
                RequiresNewEvidence = submission.RequiresNewEvidence
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

        private static string MapInvestorWorkflowStatus(InvestorKycWorkflowStatus status) => status switch
        {
            InvestorKycWorkflowStatus.NotSubmitted    => "NOT_STARTED",
            InvestorKycWorkflowStatus.Draft           => "DRAFT",
            InvestorKycWorkflowStatus.UnderReview     => "PENDING_REVIEW",
            InvestorKycWorkflowStatus.PendingMoreInfo => "PENDING_MORE_INFO",
            InvestorKycWorkflowStatus.Approved        => "VERIFIED",
            InvestorKycWorkflowStatus.Rejected        => "VERIFICATION_FAILED",
            _ => "UNKNOWN"
        };

        private static string MapInvestorResultLabel(InvestorKycResultLabel label) => label switch
        {
            InvestorKycResultLabel.VerifiedInvestorEntity => "VERIFIED_INVESTOR_ENTITY",
            InvestorKycResultLabel.VerifiedAngelInvestor  => "VERIFIED_ANGEL_INVESTOR",
            InvestorKycResultLabel.BasicVerified          => "BASIC_VERIFIED",
            InvestorKycResultLabel.PendingMoreInfo        => "PENDING_MORE_INFO",
            InvestorKycResultLabel.VerificationFailed     => "VERIFICATION_FAILED",
            _ => "NONE"
        };

        private static string MapInvestorEvidenceKind(InvestorKycEvidenceKind kind) => kind switch
        {
            InvestorKycEvidenceKind.IDProof         => "ID_PROOF",
            InvestorKycEvidenceKind.InvestmentProof => "INVESTMENT_PROOF",
            _ => "OTHER"
        };

        // ═══════════════════════════════════════════════════════════════
        //  Registration History (unified: Startup + Investor + Advisor)
        // ═══════════════════════════════════════════════════════════════

        public async Task<ApiResponse<PagedResponse<RegistrationHistoryItemDto>>> GetRegistrationHistoryAsync(RegistrationHistoryQueryParams query)
        {
            var roleFilter  = query.RoleType?.ToUpperInvariant();
            var resultFilter = query.Result?.ToUpperInvariant();
            var from = query.From?.ToUniversalTime();
            var to   = query.To?.ToUniversalTime();

            var items = new List<RegistrationHistoryItemDto>();

            // ── Startup KYC submissions ──────────────────────────────
            if (roleFilter == null || roleFilter == "STARTUP")
            {
                var startupQuery = _context.StartupKycSubmissions
                    .AsNoTracking()
                    .Include(s => s.Startup).ThenInclude(s => s.User)
                    .Include(s => s.ReviewedByUser)
                    .Where(s => s.ReviewedAt != null
                             && (s.WorkflowStatus == StartupKycWorkflowStatus.Approved
                              || s.WorkflowStatus == StartupKycWorkflowStatus.Rejected
                              || s.WorkflowStatus == StartupKycWorkflowStatus.PendingMoreInfo));

                if (from.HasValue) startupQuery = startupQuery.Where(s => s.ReviewedAt >= from.Value);
                if (to.HasValue)   startupQuery = startupQuery.Where(s => s.ReviewedAt <= to.Value);

                var startupRows = await startupQuery
                    .OrderByDescending(s => s.ReviewedAt)
                    .Select(s => new RegistrationHistoryItemDto
                    {
                        ApplicantId   = s.Startup.StartupID,
                        ApplicantName = s.Startup.CompanyName,
                        RoleType      = "STARTUP",
                        Result        = s.WorkflowStatus == StartupKycWorkflowStatus.Approved      ? "APPROVED"
                                      : s.WorkflowStatus == StartupKycWorkflowStatus.Rejected      ? "REJECTED"
                                      : "PENDING_MORE_INFO",
                        ProcessedAt   = s.ReviewedAt,
                        ReviewedBy    = s.ReviewedByUser != null ? s.ReviewedByUser.Email : null,
                        Remarks       = s.Remarks,
                        AvatarUrl     = s.Startup.LogoURL
                    })
                    .ToListAsync();

                items.AddRange(startupRows);
            }

            // ── Investor KYC submissions ─────────────────────────────
            if (roleFilter == null || roleFilter == "INVESTOR")
            {
                var investorQuery = _context.InvestorKycSubmissions
                    .AsNoTracking()
                    .Include(s => s.Investor).ThenInclude(i => i.User)
                    .Include(s => s.ReviewedByUser)
                    .Where(s => s.ReviewedAt != null
                             && (s.WorkflowStatus == InvestorKycWorkflowStatus.Approved
                              || s.WorkflowStatus == InvestorKycWorkflowStatus.Rejected
                              || s.WorkflowStatus == InvestorKycWorkflowStatus.PendingMoreInfo));

                if (from.HasValue) investorQuery = investorQuery.Where(s => s.ReviewedAt >= from.Value);
                if (to.HasValue)   investorQuery = investorQuery.Where(s => s.ReviewedAt <= to.Value);

                var investorRows = await investorQuery
                    .OrderByDescending(s => s.ReviewedAt)
                    .Select(s => new RegistrationHistoryItemDto
                    {
                        ApplicantId   = s.Investor.InvestorID,
                        ApplicantName = s.Investor.FullName ?? s.Investor.User.Email,
                        RoleType      = "INVESTOR",
                        Result        = s.WorkflowStatus == InvestorKycWorkflowStatus.Approved      ? "APPROVED"
                                      : s.WorkflowStatus == InvestorKycWorkflowStatus.Rejected      ? "REJECTED"
                                      : "PENDING_MORE_INFO",
                        ProcessedAt   = s.ReviewedAt,
                        ReviewedBy    = s.ReviewedByUser != null ? s.ReviewedByUser.Email : null,
                        Remarks       = s.Remarks,
                        AvatarUrl     = s.Investor.ProfilePhotoURL
                    })
                    .ToListAsync();

                items.AddRange(investorRows);
            }

            // ── Advisor KYC (stored on Advisor entity, no submission table) ─
            if (roleFilter == null || roleFilter == "ADVISOR")
            {
                var advisorQuery = _context.Advisors
                    .AsNoTracking()
                    .Include(a => a.ApprovedByUser)
                    .Where(a => a.ApprovedAt != null
                             && (a.AdvisorTag == AdvisorTag.VerifiedAdvisor
                              || a.AdvisorTag == AdvisorTag.BasicVerified
                              || a.AdvisorTag == AdvisorTag.VerificationFailed
                              || a.AdvisorTag == AdvisorTag.PendingMoreInfo));

                if (from.HasValue) advisorQuery = advisorQuery.Where(a => a.ApprovedAt >= from.Value);
                if (to.HasValue)   advisorQuery = advisorQuery.Where(a => a.ApprovedAt <= to.Value);

                var advisorRows = await advisorQuery
                    .OrderByDescending(a => a.ApprovedAt)
                    .Select(a => new RegistrationHistoryItemDto
                    {
                        ApplicantId   = a.AdvisorID,
                        ApplicantName = a.FullName,
                        RoleType      = "ADVISOR",
                        Result        = (a.AdvisorTag == AdvisorTag.VerifiedAdvisor || a.AdvisorTag == AdvisorTag.BasicVerified) ? "APPROVED"
                                      : a.AdvisorTag == AdvisorTag.VerificationFailed ? "REJECTED"
                                      : "PENDING_MORE_INFO",
                        ProcessedAt   = a.ApprovedAt,
                        ReviewedBy    = a.ApprovedByUser != null ? a.ApprovedByUser.Email : null,
                        Remarks       = a.RejectionRemarks,
                        AvatarUrl     = a.ProfilePhotoURL
                    })
                    .ToListAsync();

                items.AddRange(advisorRows);
            }

            // ── Apply result filter + sort + paginate in memory ──────
            if (resultFilter != null)
                items = items.Where(i => i.Result == resultFilter).ToList();

            items = items.OrderByDescending(i => i.ProcessedAt).ToList();

            var page     = query.Page     > 0 ? query.Page     : 1;
            var pageSize = query.PageSize > 0 ? query.PageSize : 20;
            var total    = items.Count;
            var paged    = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return ApiResponse<PagedResponse<RegistrationHistoryItemDto>>.SuccessResponse(new PagedResponse<RegistrationHistoryItemDto>
            {
                Items = paged,
                Paging = new PagingInfo { Page = page, PageSize = pageSize, TotalItems = total }
            });
        }

        // ================================================================
        // GET KYC CASE HISTORY — per-entity review timeline
        // ================================================================

        public async Task<ApiResponse<List<KycCaseHistoryEntryDto>>> GetKycCaseHistoryAsync(int entityId, string entityType)
        {
            var type = entityType.ToUpperInvariant();

            if (type == "STARTUP")
            {
                var startup = await _context.Startups
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.StartupID == entityId);

                if (startup == null)
                    return ApiResponse<List<KycCaseHistoryEntryDto>>.ErrorResponse(
                        "STARTUP_NOT_FOUND", "Startup not found.");

                var submissions = await _context.StartupKycSubmissions
                    .AsNoTracking()
                    .Include(s => s.ReviewedByUser)
                    .Where(s => s.StartupID == entityId
                             && s.WorkflowStatus != StartupKycWorkflowStatus.Draft)
                    .OrderBy(s => s.Version)
                    .ToListAsync();

                var entries = submissions.Select(s => new KycCaseHistoryEntryDto
                {
                    Version              = s.Version,
                    SubmittedAt          = s.SubmittedAt,
                    ReviewedAt           = s.ReviewedAt,
                    ReviewedByEmail      = s.ReviewedByUser?.Email,
                    Action               = MapStartupAction(s.WorkflowStatus),
                    ResultLabel          = MapStartupResultLabel(s.ResultLabel),
                    Remarks              = s.Remarks,
                    RequiresNewEvidence  = s.RequiresNewEvidence,
                }).ToList();

                return ApiResponse<List<KycCaseHistoryEntryDto>>.SuccessResponse(entries);
            }

            if (type == "INVESTOR")
            {
                var investor = await _context.Investors
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.InvestorID == entityId);

                if (investor == null)
                    return ApiResponse<List<KycCaseHistoryEntryDto>>.ErrorResponse(
                        "INVESTOR_NOT_FOUND", "Investor not found.");

                var submissions = await _context.InvestorKycSubmissions
                    .AsNoTracking()
                    .Include(s => s.ReviewedByUser)
                    .Where(s => s.InvestorID == entityId
                             && s.WorkflowStatus != InvestorKycWorkflowStatus.Draft)
                    .OrderBy(s => s.Version)
                    .ToListAsync();

                var entries = submissions.Select(s => new KycCaseHistoryEntryDto
                {
                    Version              = s.Version,
                    SubmittedAt          = s.SubmittedAt,
                    ReviewedAt           = s.ReviewedAt,
                    ReviewedByEmail      = s.ReviewedByUser?.Email,
                    Action               = MapInvestorAction(s.WorkflowStatus),
                    ResultLabel          = MapInvestorResultLabel(s.ResultLabel),
                    Remarks              = s.Remarks,
                    RequiresNewEvidence  = s.RequiresNewEvidence,
                }).ToList();

                return ApiResponse<List<KycCaseHistoryEntryDto>>.SuccessResponse(entries);
            }

            if (type == "ADVISOR")
            {
                var advisor = await _context.Advisors
                    .AsNoTracking()
                    .Include(a => a.ApprovedByUser)
                    .FirstOrDefaultAsync(a => a.AdvisorID == entityId);

                if (advisor == null)
                    return ApiResponse<List<KycCaseHistoryEntryDto>>.ErrorResponse(
                        "ADVISOR_NOT_FOUND", "Advisor not found.");

                // Build timeline from AuditLog: each SUBMIT / APPROVE / REJECT / MORE_INFO event
                // is recorded as an individual row, enabling a real multi-version timeline.
                var advisorAuditTypes = new[]
                {
                    "SUBMIT_ADVISOR_KYC",
                    "APPROVE_ADVISOR_KYC",
                    "REJECT_ADVISOR_KYC",
                    "REQUEST_MORE_INFO_ADVISOR_KYC"
                };

                var auditEntries = await _context.AuditLogs
                    .AsNoTracking()
                    .Include(a => a.User)
                    .Where(a => a.EntityType == "Advisor"
                             && a.EntityID == entityId
                             && advisorAuditTypes.Contains(a.ActionType))
                    .OrderBy(a => a.CreatedAt)
                    .ToListAsync();

                // Fallback for pre-existing advisors who have no audit entries yet:
                // reconstruct a single entry from current entity state.
                if (auditEntries.Count == 0)
                {
                    var fallbackAction = advisor.AdvisorTag switch
                    {
                        AdvisorTag.VerifiedAdvisor or AdvisorTag.BasicVerified => "APPROVED",
                        AdvisorTag.VerificationFailed => "REJECTED",
                        AdvisorTag.PendingMoreInfo    => "REQUESTED_MORE_INFO",
                        _ => "UNDER_REVIEW"
                    };
                    var fallbackResultLabel = advisor.AdvisorTag switch
                    {
                        AdvisorTag.VerifiedAdvisor    => "VERIFIED_ADVISOR",
                        AdvisorTag.BasicVerified      => "BASIC_VERIFIED",
                        AdvisorTag.VerificationFailed => "VERIFICATION_FAILED",
                        AdvisorTag.PendingMoreInfo    => "PENDING_MORE_INFO",
                        _ => "NONE"
                    };
                    return ApiResponse<List<KycCaseHistoryEntryDto>>.SuccessResponse(new List<KycCaseHistoryEntryDto>
                    {
                        new()
                        {
                            Version             = 1,
                            SubmittedAt         = advisor.UpdatedAt ?? advisor.CreatedAt,
                            ReviewedAt          = advisor.ApprovedAt,
                            ReviewedByEmail     = advisor.ApprovedByUser?.Email,
                            Action              = fallbackAction,
                            ResultLabel         = fallbackResultLabel,
                            Remarks             = advisor.RejectionRemarks,
                            RequiresNewEvidence = advisor.RequiresNewEvidence,
                        }
                    });
                }

                // Map audit entries → history items.
                // Each SUBMIT increments the version counter; review entries share the same version.
                var historyEntries = new List<KycCaseHistoryEntryDto>();
                int currentVersion = 0;
                foreach (var entry in auditEntries)
                {
                    bool isSubmit = entry.ActionType == "SUBMIT_ADVISOR_KYC";
                    if (isSubmit) currentVersion++;

                    string entryAction = entry.ActionType switch
                    {
                        "SUBMIT_ADVISOR_KYC"            => "UNDER_REVIEW",
                        "APPROVE_ADVISOR_KYC"           => "APPROVED",
                        "REJECT_ADVISOR_KYC"            => "REJECTED",
                        "REQUEST_MORE_INFO_ADVISOR_KYC" => "REQUESTED_MORE_INFO",
                        _                               => "UNDER_REVIEW"
                    };

                    string? entryResultLabel = null;
                    string? entryRemarks     = null;
                    bool entryRequiresNew    = false;

                    if (!isSubmit && !string.IsNullOrEmpty(entry.ActionDetails))
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(entry.ActionDetails);
                            var root = doc.RootElement;
                            if (root.TryGetProperty("resultLabel", out var rl))    entryResultLabel = rl.GetString();
                            if (root.TryGetProperty("remarks", out var rm))        entryRemarks     = rm.ValueKind != JsonValueKind.Null ? rm.GetString() : null;
                            if (root.TryGetProperty("requiresNewEvidence", out var rne)) entryRequiresNew = rne.GetBoolean();
                        }
                        catch { /* ignore malformed JSON */ }
                    }

                    historyEntries.Add(new KycCaseHistoryEntryDto
                    {
                        Version             = Math.Max(1, currentVersion),
                        SubmittedAt         = isSubmit ? entry.CreatedAt : null,
                        ReviewedAt          = isSubmit ? null : entry.CreatedAt,
                        ReviewedByEmail     = isSubmit ? null : entry.User?.Email,
                        Action              = entryAction,
                        ResultLabel         = entryResultLabel ?? (isSubmit ? "NONE" : "NONE"),
                        Remarks             = entryRemarks,
                        RequiresNewEvidence = entryRequiresNew,
                    });
                }

                return ApiResponse<List<KycCaseHistoryEntryDto>>.SuccessResponse(historyEntries);
            }

            return ApiResponse<List<KycCaseHistoryEntryDto>>.ErrorResponse(
                "INVALID_ENTITY_TYPE", "entityType must be STARTUP, ADVISOR, or INVESTOR.");
        }

        private static string MapStartupAction(StartupKycWorkflowStatus status) => status switch
        {
            StartupKycWorkflowStatus.UnderReview    => "UNDER_REVIEW",
            StartupKycWorkflowStatus.Approved       => "APPROVED",
            StartupKycWorkflowStatus.Rejected       => "REJECTED",
            StartupKycWorkflowStatus.PendingMoreInfo => "REQUESTED_MORE_INFO",
            StartupKycWorkflowStatus.Superseded     => "SUPERSEDED",
            _                                       => "UNDER_REVIEW",
        };

        private static string MapStartupResultLabel(StartupKycResultLabel label) => label switch
        {
            StartupKycResultLabel.VerifiedCompany   => "VERIFIED_COMPANY",
            StartupKycResultLabel.BasicVerified     => "BASIC_VERIFIED",
            StartupKycResultLabel.PendingMoreInfo   => "PENDING_MORE_INFO",
            StartupKycResultLabel.VerificationFailed => "VERIFICATION_FAILED",
            _                                       => "NONE",
        };

        private static string MapInvestorAction(InvestorKycWorkflowStatus status) => status switch
        {
            InvestorKycWorkflowStatus.UnderReview    => "UNDER_REVIEW",
            InvestorKycWorkflowStatus.Approved       => "APPROVED",
            InvestorKycWorkflowStatus.Rejected       => "REJECTED",
            InvestorKycWorkflowStatus.PendingMoreInfo => "REQUESTED_MORE_INFO",
            InvestorKycWorkflowStatus.Superseded     => "SUPERSEDED",
            _                                        => "UNDER_REVIEW",
        };
    }
}
