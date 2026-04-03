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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AISEP.Infrastructure.Services
{
    public class RegistrationApprovalService : IRegistrationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RegistrationApprovalService> _logger;

        public RegistrationApprovalService(
            ApplicationDbContext context,
            ILogger<RegistrationApprovalService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ApiResponse<Startup>> ApproveStartupRegistrationAsync(int staffId, ApproveStartupRegistrationRequest startupRegistrationRequest)
        {
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

        public async Task<ApiResponse<Startup>> RejectStartupRegistrationAsync(int staffId, RejectRegistrationRequest request)
        {
            var startup = await _context.Startups.FirstOrDefaultAsync(s => s.StartupID == request.Id);
            if (startup == null)
                return ApiResponse<Startup>.ErrorResponse("NOT_FOUND", "Profile not found");

            startup.ProfileStatus = ProfileStatus.Rejected;
            // Optionally store the reject reason somewhere (maybe a notification or comment field)
            _context.Startups.Update(startup);
            await _context.SaveChangesAsync();
            return ApiResponse<Startup>.SuccessResponse(startup, "Rejected successfully");
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
        }

        public Task<ApiResponse<RegistrationApprovalResponse>> RejectRegistrationAsync(Guid userId, string reason)
        {
            throw new NotImplementedException();
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
                MarketScope = startup.MarketScope,
                ProblemStatement = startup.ProblemStatement,
                SolutionSummary = startup.SolutionSummary,
                LinkedInURL = startup.LinkedInURL,
                TeamSize = startup.TeamMembers.Count(),
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
    }
}
