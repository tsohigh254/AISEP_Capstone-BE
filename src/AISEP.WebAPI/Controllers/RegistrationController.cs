using AISEP.Application.DTOs.Advisor;
using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Investor;
using AISEP.Application.DTOs.Staff;
using AISEP.Application.DTOs.Startup;
using AISEP.Application.Interfaces;
using AISEP.Application.QueryParams;
using AISEP.Domain.Entities;
using AISEP.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AISEP.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RegistrationController : ControllerBase
    {
        private readonly IRegistrationService _registrationService;

        public RegistrationController(IRegistrationService registrationService)
        {
            _registrationService = registrationService;
        }

        /// <summary>
        /// Get pending startup registrations with pagination
        /// </summary>
        [HttpGet("pending/startups")]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<StartupListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetPendingStartupRegistrations([FromQuery] RegistrationQueryParams query)
        {
            var response = await _registrationService.GetPendingRegistrationsStartupAsync(query);
            return Ok(response);
        }

        /// <summary>
        /// Get pending advisor registrations with pagination
        /// </summary>
        [HttpGet("pending/advisors")]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<AdvisorDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetPendingAdvisorRegistrations([FromQuery] RegistrationQueryParams query)
        {
            var response = await _registrationService.GetPendingRegistrationsAdvisorAsync(query);
            return Ok(response);
        }

        /// <summary>
        /// Get pending investor registrations with pagination
        /// </summary>
        [HttpGet("pending/investors")]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<InvestorDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetPendingInvestorRegistrations([FromQuery] RegistrationQueryParams query)
        {
            var response = await _registrationService.GetPendingRegistrationsInvestorAsync(query);
            return Ok(response);
        }

        /// <summary>
        /// Get pending startup registration by ID
        /// </summary>
        [HttpGet("pending/startups/{startupId}")]
        [ProducesResponseType(typeof(ApiResponse<StartupDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetPendingStartupRegistrationById(int startupId)
        {
            var response = await _registrationService.GetPendingRegistrationStartupByIdAsync(startupId);
            return Ok(response);
        }

        /// <summary>
        /// Get active startup KYC submission for staff review
        /// </summary>
        [HttpGet("pending/startups/{startupId}/kyc")]
        [ProducesResponseType(typeof(ApiResponse<StartupKycSubmissionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetPendingStartupKycById(int startupId)
        {
            var response = await _registrationService.GetPendingRegistrationStartupKycByIdAsync(startupId);
            return Ok(response);
        }

        /// <summary>
        /// Get pending investor registration by ID
        /// </summary>
        [HttpGet("pending/investors/{investorId}")]
        [ProducesResponseType(typeof(ApiResponse<InvestorDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetPendingInvestorRegistrationById(int investorId)
        {
            var response = await _registrationService.GetPendingRegistrationInvestorByIdAsync(investorId);
            return Ok(response);
        }

        /// <summary>
        /// Get pending advisor registration by ID
        /// </summary>
        [HttpGet("pending/advisors/{advisorId}")]
        [ProducesResponseType(typeof(ApiResponse<AdvisorDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetPendingAdvisorRegistrationById(int advisorId)
        {
            var response = await _registrationService.GetPendingRegistrationAdvisorByIdAsync(advisorId);
            return Ok(response);
        }

        /// <summary>
        /// Approve startup registration
        /// </summary>
        [HttpPost("approve/startups/{staffId}")]
        [Authorize(Policy = "StaffOrAdmin")]
        [ProducesResponseType(typeof(ApiEnvelope<StartupKycSubmissionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ApproveStartupRegistration(int staffId, [FromBody]ApproveStartupRegistrationRequest startupRegistrationRequest)
        {
            var response = await _registrationService.ApproveStartupRegistrationAsync(staffId, startupRegistrationRequest);
            return response.ToEnvelope();
        }

        [HttpPost("approve/advisors/{staffId}")]
        [Authorize(Policy = "StaffOrAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ApproveAdvisorRegistration(int staffId, [FromBody]ApproveAdvisorRegistrationRequest request)
        {
            var response = await _registrationService.ApproveAdvisorRegistrationAsync(staffId, request);
            return Ok(response);
        }

        [HttpPost("approve/investors/{staffId}")]
        [Authorize(Policy = "StaffOrAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ApproveInvestorRegistration(int staffId, [FromBody]ApproveInvestorRegistrationRequest request)
        {
            var response = await _registrationService.ApproveInvestorRegistrationAsync(staffId, request);
            return Ok(response);
        }

        [HttpPost("reject/startups/{staffId}")]
        [Authorize(Policy = "StaffOrAdmin")]
        [ProducesResponseType(typeof(ApiEnvelope<StartupKycSubmissionDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RejectStartupRegistration(int staffId, [FromBody]RejectRegistrationRequest request)
        {
            var response = await _registrationService.RejectStartupRegistrationAsync(staffId, request);
            return response.ToEnvelope();
        }

        [HttpPost("reject/advisors/{staffId}")]
        [Authorize(Policy = "StaffOrAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> RejectAdvisorRegistration(int staffId, [FromBody]RejectRegistrationRequest request)
        {
            var response = await _registrationService.RejectAdvisorRegistrationAsync(staffId, request);
            return Ok(response);
        }

        [HttpPost("reject/investors/{staffId}")]
        [Authorize(Policy = "StaffOrAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> RejectInvestorRegistration(int staffId, [FromBody]RejectRegistrationRequest request)
        {
            var response = await _registrationService.RejectInvestorRegistrationAsync(staffId, request);
            return Ok(response);
        }
    }
}
