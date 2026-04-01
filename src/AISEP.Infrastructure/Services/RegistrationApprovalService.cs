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

        public async Task<ApiResponse<Startup>> ApproveStartupRegistrationAsync(int staffId, ApproveRegistrationRequest registrationRequest)
        {
            var startup = await _context.Startups.FirstOrDefaultAsync(s => s.StartupID == registrationRequest.Id);

            if (startup == null)
            {
                return ApiResponse<Startup>.ErrorResponse("STARTUP_PROFILE_DOES_NOT_EXISTS",
                "Startup profile does not exist");
            }

            startup.ApprovedAt = DateTime.UtcNow;
            startup.ApprovedBy = staffId;

            if (registrationRequest.Score >= 10)
            {   
                startup.StartupTag = StartupTag.VerifiedCompany;
                startup.ProfileStatus = ProfileStatus.Approved;
            }else if (registrationRequest.Score >= 6 && registrationRequest.Score <= 9)
            {
                startup.StartupTag = StartupTag.BasicVerified;
                startup.ProfileStatus = ProfileStatus.Approved;
            }else if (registrationRequest.Score >= 2 && registrationRequest.Score <= 5)
            {
                startup.StartupTag = StartupTag.VerificationFailed;
                startup.ProfileStatus = ProfileStatus.Rejected;
            }

            _context.Startups.Update(startup);
            await _context.SaveChangesAsync();

            return ApiResponse<Startup>.SuccessResponse(startup, "Startup approved successfully");
        }

        public async Task<ApiResponse<PagedResponse<AdvisorDto>>> GetAdvisorAsync(RegistrationQueryParams registrationQuery)
        {
            var registrations = _context.Advisors
                .Where(s => s.ProfileStatus != ProfileStatus.Draft)
                .AsNoTracking()
                .AsQueryable();

            var registrationsToDto = registrations.Select(r => new AdvisorDto
            {
                AdvisorID = r.AdvisorID,
                UserId = r.UserID,
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

        public async Task<ApiResponse<PagedResponse<InvestorDto>>> GetInvestorAsync(RegistrationQueryParams registrationQuery)
        {
            var registrations = _context.Investors
                .Where(s => s.ProfileStatus != ProfileStatus.Draft)
                .AsNoTracking()
                .AsQueryable();

            var registrationsToDto = registrations.Select(r => new InvestorDto
            {
                InvestorID = r.InvestorID,
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

        public async Task<ApiResponse<PagedResponse<StartupDto>>> GetStartupAsync(RegistrationQueryParams registrationQuery)
        {
            var registrations = _context.Startups
                .Where(s => s.ProfileStatus != ProfileStatus.Draft)
                .AsNoTracking()
                .AsQueryable();

            var registrationsToDto = registrations.Select(r => new StartupDto
            {
                StartupID = r.StartupID,
                UserID = r.UserID,
                CompanyName = r.CompanyName,
                OneLiner = r.OneLiner,
                Description = r.Description,
                IndustryID = r.IndustryID,
                IndustryName = r.Industry.IndustryName,
                Stage = r.Stage.ToString(),
                FoundedDate = r.FoundedDate,
                Website = r.Website,
                LogoURL = r.LogoURL,
                FundingAmountSought = r.FundingAmountSought,
                CurrentFundingRaised = r.CurrentFundingRaised,
                Valuation = r.Valuation,
                FullNameOfApplicant = r.FullNameOfApplicant,
                RoleOfApplicant = r.RoleOfApplicant,
                ContactEmail = r.ContactEmail,
                ContactPhone = r.ContactPhone,
                BussinessCode = r.BussinessCode,
                MarketScope = r.MarketScope,
                ProblemStatement = r.ProblemStatement,
                SolutionSummary = r.SolutionSummary,
                LinkedInURL = r.LinkedInURL,
                TeamSize = r.TeamMembers.Count(),
                FileCertificateBusiness = r.FileCertificateBusiness,
                ProfileStatus = r.ProfileStatus.ToString(),
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                TeamMembers = r.TeamMembers.Select(m => new TeamMemberPublicDto
                {
                    FullName = m.FullName,
                    Role = m.Role,
                    Title = m.Title,
                    LinkedInURL = m.LinkedInURL,
                    Bio = m.Bio,
                    PhotoURL = m.PhotoURL,
                    IsFounder = m.IsFounder,
                }).ToList()
            }).Paging(registrationQuery.Page, registrationQuery.PageSize);

            return ApiResponse<PagedResponse<StartupDto>>.SuccessResponse
                (
                     new PagedResponse<StartupDto>
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

        public async Task<ApiResponse<InvestorDto>> GetInvestorByIdAsync(int investorId)
        {
            var investor = await _context.Investors.FirstOrDefaultAsync(i => i.InvestorID == investorId);

            var investorToDto = new InvestorDto
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
                ProfileStatus = investor.ProfileStatus.ToString(),
                CreatedAt = investor.CreatedAt,
                UpdatedAt = investor.UpdatedAt,
            };

            return ApiResponse<InvestorDto>.SuccessResponse(investorToDto);
        }

        public async Task<ApiResponse<AdvisorDto>> GetAdvisorByIdAsync(int advisorId)
        {
            var advisor = await _context.Advisors
                .Include(a => a.IndustryFocus)
                .FirstOrDefaultAsync(i => i.AdvisorID == advisorId);

            var advisorToDto = new AdvisorDto
            {
                AdvisorID = advisorId,
                UserId = advisor.UserID,
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

        public async Task<ApiResponse<StartupDto>> GetStartupByIdAsync(int startupId)
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
                BussinessCode = startup.BussinessCode,
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

        public async Task<ApiResponse<Investor>> ApproveInvestorRegistrationAsync(int staffId, ApproveRegistrationRequest approveRegistration)
        {
            var investor = await _context.Investors.FirstOrDefaultAsync(s => s.InvestorID == approveRegistration.Id);

            //if (investor == null)
            //{
            //    return ApiResponse<Investor>.ErrorResponse("STARTUP_PROFILE_DOES_NOT_EXISTS",
            //    "Startup profile does not exist");
            //}

            //investor.ApprovedAt = DateTime.UtcNow;
            //investor.ApprovedBy = staffId;

            //if (approveRegistration.Score >= 10)
            //{
            //    investor.StartupTag = StartupTag.VerifiedCompany;
            //    investor.ProfileStatus = ProfileStatus.Approved;
            //}
            //else if (approveRegistration.Score >= 6 && approveRegistration.Score <= 9)
            //{
            //    investor.StartupTag = StartupTag.BasicVerified;
            //    investor.ProfileStatus = ProfileStatus.Approved;
            //}
            //else if (approveRegistration.Score >= 2 && approveRegistration.Score <= 5)
            //{
            //    investor.StartupTag = StartupTag.VerificationFailed;
            //    investor.ProfileStatus = ProfileStatus.Rejected;
            //}

            //_context.Investors.Update(investor);
            //await _context.SaveChangesAsync();

            return ApiResponse<Investor>.SuccessResponse(investor, "Investor approved successfully");
        }
    }
}
