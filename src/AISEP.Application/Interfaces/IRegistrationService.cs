using AISEP.Application.DTOs.Advisor;
using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Investor;
using AISEP.Application.DTOs.Staff;
using AISEP.Application.DTOs.Startup;
using AISEP.Application.QueryParams;
using AISEP.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AISEP.Application.Interfaces
{
    public interface IRegistrationService
    {
        Task<ApiResponse<PagedResponse<StartupListItemDto>>> GetPendingRegistrationsStartupAsync(RegistrationQueryParams registrationQuery);
        Task<ApiResponse<PagedResponse<AdvisorDto>>> GetPendingRegistrationsAdvisorAsync(RegistrationQueryParams registrationQuery);
        Task<ApiResponse<PagedResponse<InvestorDto>>> GetPendingRegistrationsInvestorAsync(RegistrationQueryParams registrationQuery);
        Task<ApiResponse<StartupDto>> GetPendingRegistrationStartupByIdAsync(int startupId);
        Task<ApiResponse<StartupKycSubmissionDto>> GetPendingRegistrationStartupKycByIdAsync(int startupId);
        Task<ApiResponse<InvestorDto>> GetPendingRegistrationInvestorByIdAsync(int investorId);
        Task<ApiResponse<InvestorKycSubmissionDto>> GetPendingRegistrationInvestorKycByIdAsync(int investorId);
        Task<ApiResponse<AdvisorDto>> GetPendingRegistrationAdvisorByIdAsync(int advisorId);
        Task<ApiResponse<StartupKycSubmissionDto>> ApproveStartupRegistrationAsync(int staffId, ApproveStartupRegistrationRequest startupRegistrationRequest);
        Task<ApiResponse<Advisor>> ApproveAdvisorRegistrationAsync(int staffId, ApproveAdvisorRegistrationRequest advisorRegistrationRequest);
        Task<ApiResponse<Investor>> ApproveInvestorRegistrationAsync(int staffId, ApproveInvestorRegistrationRequest investorRegistrationRequest);
        
        Task<ApiResponse<StartupKycSubmissionDto>> RejectStartupRegistrationAsync(int staffId, RejectRegistrationRequest request);
        Task<ApiResponse<Advisor>> RejectAdvisorRegistrationAsync(int staffId, RejectRegistrationRequest request);
        Task<ApiResponse<Investor>> RejectInvestorRegistrationAsync(int staffId, RejectRegistrationRequest request);
    }
}
