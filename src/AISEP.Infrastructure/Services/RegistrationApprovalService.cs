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

namespace AISEP.Infrastructure.Services
{
    public class RegistrationApprovalService : IRegistrationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RegistrationApprovalService> _logger;
        private readonly ICloudinaryService _cloudinaryService;

        public RegistrationApprovalService(
            ApplicationDbContext context,
            ILogger<RegistrationApprovalService> logger,
            ICloudinaryService cloudinaryService)
        {
            _context = context;
            _logger = logger;
            _cloudinaryService = cloudinaryService;
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

            await _context.SaveChangesAsync();
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

            return ApiResponse<Advisor>.SuccessResponse(advisor, "Advisor reviewed successfully");
        }

        public async Task<ApiResponse<Investor>> ApproveInvestorRegistrationAsync(int staffId, ApproveInvestorRegistrationRequest request)
        {
            var investor = await _context.Investors.FirstOrDefaultAsync(i => i.InvestorID == request.InvestorId);
            if (investor == null)
            {
                return ApiResponse<Investor>.ErrorResponse("INVESTOR_PROFILE_DOES_NOT_EXISTS", "Investor profile does not exist");
            }

            investor.ProfileStatus = ProfileStatus.Approved;
            investor.ApprovedAt = DateTime.UtcNow;
            investor.ApprovedBy = staffId;

            if (request.IsInstitutional)
            {
                if (request.Score >= 10) investor.InvestorTag = InvestorTag.VerifiedInvestorEntity;
                else if (request.Score >= 6) investor.InvestorTag = InvestorTag.BasicVerified;
                else if (request.Score >= 2) 
                {
                    investor.InvestorTag = InvestorTag.PendingMoreInfo;
                    investor.ProfileStatus = ProfileStatus.Pending;
                }
                else
                {
                    investor.InvestorTag = InvestorTag.VerificationFailed;
                    investor.ProfileStatus = ProfileStatus.Rejected;
                }
            }
            else
            {
                if (request.Score >= 8) investor.InvestorTag = InvestorTag.VerifiedAngelInvestor;
                else if (request.Score >= 5) investor.InvestorTag = InvestorTag.BasicVerified;
                else if (request.Score >= 2)
                {
                    investor.InvestorTag = InvestorTag.PendingMoreInfo;
                    investor.ProfileStatus = ProfileStatus.Pending;
                }
                else
                {
                    investor.InvestorTag = InvestorTag.VerificationFailed;
                    investor.ProfileStatus = ProfileStatus.Rejected;
                }
            }

            _context.Investors.Update(investor);
            await _context.SaveChangesAsync();

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

            advisor.ProfileStatus = ProfileStatus.Rejected;
            _context.Advisors.Update(advisor);
            await _context.SaveChangesAsync();
            return ApiResponse<Advisor>.SuccessResponse(advisor, "Rejected successfully");
        }

        public async Task<ApiResponse<Investor>> RejectInvestorRegistrationAsync(int staffId, RejectRegistrationRequest request)
        {
            var investor = await _context.Investors.FirstOrDefaultAsync(i => i.InvestorID == request.Id);
            if (investor == null)
                return ApiResponse<Investor>.ErrorResponse("NOT_FOUND", "Profile not found");

            investor.ProfileStatus = ProfileStatus.Rejected;
            _context.Investors.Update(investor);
            await _context.SaveChangesAsync();
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
            var registrations = _context.Investors
                .Where(s => s.ProfileStatus == ProfileStatus.Pending || s.ProfileStatus == ProfileStatus.PendingKYC)
                .AsNoTracking()
                .AsQueryable();

            var registrationsToDto = registrations.Select(r => new InvestorDto
            {
                InvestorID = r.InvestorID,
                UserID = r.UserID,
                Email = r.User.Email,
                FullName = r.FullName,
                FirmName = r.FirmName,
                Title = r.Title,
                Bio = r.Bio,
                ProfilePhotoURL = r.ProfilePhotoURL,
                InvestmentThesis = r.InvestmentThesis,
                Location = r.Location,
                Country = r.Country,
                LinkedInURL = r.LinkedInURL,
                Website = r.Website,
                ProfileStatus = r.ProfileStatus.ToString(),
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
            }).Paging(registrationQuery.Page, registrationQuery.PageSize);

            return ApiResponse<PagedResponse<InvestorDto>>.SuccessResponse
                (
                     new PagedResponse<InvestorDto>
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

        public async Task<ApiResponse<PagedResponse<StartupListItemDto>>> GetPendingRegistrationsStartupAsync(RegistrationQueryParams registrationQuery)
        {
            var submissionQuery = _context.StartupKycSubmissions
                .AsNoTracking()
                .Include(s => s.Startup)
                .ThenInclude(s => s.Industry)
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
                    IndustryName = s.Startup.Industry != null ? s.Startup.Industry.IndustryName : null,
                    Stage = s.Startup.Stage != null ? s.Startup.Stage.ToString() : null,
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

                // KYC Information
                InvestorType = investor.InvestorType?.ToString(),
                ContactEmail = investor.ContactEmail,
                CurrentOrganization = investor.CurrentOrganization,
                CurrentRoleTitle = investor.CurrentRoleTitle,
                BusinessCode = investor.BusinessCode,
                SubmitterRole = investor.SubmitterRole,
                IDProofFileURL = investor.IDProofFileURL,
                InvestmentProofFileURL = investor.InvestmentProofFileURL,
                Remarks = investor.Remarks
            };

            return ApiResponse<InvestorDto>.SuccessResponse(investorToDto);
        }

        public async Task<ApiResponse<AdvisorDto>> GetPendingRegistrationAdvisorByIdAsync(int advisorId)
        {
            var advisor = await _context.Advisors
                .Include(a => a.IndustryFocus)
                .Include(a => a.User)
                .FirstOrDefaultAsync(i => i.AdvisorID == advisorId);

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
                CreatedAt = advisor.CreatedAt,
                UpdatedAt = advisor.UpdatedAt,
                IndustryFocus = advisor.IndustryFocus.Select(i => new AdvisorIndustryFocusDto
                {
                    IndustryId = i.IndustryID,
                    Industry = i.Industry.IndustryName
                }).ToList()
            };

            return ApiResponse<AdvisorDto>.SuccessResponse(advisorToDto);
        }

        public async Task<ApiResponse<StartupDto>> GetPendingRegistrationStartupByIdAsync(int startupId)
        {
            var startup = await _context.Startups
               .Include(s => s.TeamMembers)
               .Include(s => s.Industry)
               .FirstOrDefaultAsync(i => i.StartupID == startupId);

            var startupToDto = new StartupDto
            {
                StartupID = startupId,
                UserID = startup.UserID,
                CompanyName = startup.CompanyName,
                OneLiner = startup.OneLiner,
                Description = startup.Description,
                IndustryID = startup.IndustryID,
                IndustryName = startup.Industry.IndustryName,
                Stage = startup.Stage.ToString(),
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
                SubIndustry = startup.SubIndustry,
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
                .Include(s => s.EvidenceFiles)
                .Include(s => s.RequestedAdditionalItems)
                .FirstOrDefaultAsync(s => s.StartupID == startupId && s.IsActive);

            if (submission == null)
            {
                return ApiResponse<StartupKycSubmissionDto>.ErrorResponse("STARTUP_KYC_SUBMISSION_NOT_FOUND",
                    "No active startup KYC submission was found for this startup.");
            }

            return ApiResponse<StartupKycSubmissionDto>.SuccessResponse(MapToKycSubmissionDto(submission));
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
                            Url = _cloudinaryService.GenerateSignedDocumentUrl(f.StorageKey, f.FileUrl, f.FileName),
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
    }
}
