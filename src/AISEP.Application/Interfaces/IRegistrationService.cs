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
        Task<ApiResponse<PagedResponse<StartupDto>>> GetStartupAsync(RegistrationQueryParams registrationQuery);
        Task<ApiResponse<PagedResponse<AdvisorDto>>> GetAdvisorAsync(RegistrationQueryParams registrationQuery);
        Task<ApiResponse<PagedResponse<InvestorDto>>> GetInvestorAsync(RegistrationQueryParams registrationQuery);
        Task<ApiResponse<StartupDto>> GetStartupByIdAsync(int startupId);
        Task<ApiResponse<InvestorDto>> GetInvestorByIdAsync(int investorId);
        Task<ApiResponse<AdvisorDto>> GetAdvisorByIdAsync(int advisorId);
        Task<ApiResponse<Startup>> ApproveStartupRegistrationAsync(int staffId, ApproveRegistrationRequest registrationRequest);
        Task<ApiResponse<Investor>> ApproveInvestorRegistrationAsync(int staffId, ApproveRegistrationRequest approveRegistration);
    }
}
