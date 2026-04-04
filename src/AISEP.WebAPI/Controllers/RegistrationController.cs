using AISEP.Application.DTOs.Advisor;
using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Investor;
using AISEP.Application.DTOs.Staff;
using AISEP.Application.DTOs.Startup;
using AISEP.Application.Interfaces;
using AISEP.Application.QueryParams;
using AISEP.Domain.Entities;
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
            var response = await _registrationService.GetStartupAsync(query);
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
            var response = await _registrationService.GetAdvisorAsync(query);
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
            var response = await _registrationService.GetInvestorAsync(query);
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
            var response = await _registrationService.GetStartupByIdAsync(startupId);
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
            var response = await _registrationService.GetInvestorByIdAsync(investorId);
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
            var response = await _registrationService.GetAdvisorByIdAsync(advisorId);
            return Ok(response);
        }

        /// <summary>
        /// Approve startup registration
        /// </summary>
        [HttpPost("approve/startups")]
        [Authorize(Policy = "StaffOrAdmin")]
        [ProducesResponseType(typeof(ApiResponse<Startup>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ApproveStartupRegistration([FromBody]ApproveRegistrationRequest startupRegistrationRequest)
        {
            var staffId = GetCurrentUserId();
            var response = await _registrationService.ApproveStartupRegistrationAsync(staffId, startupRegistrationRequest);
            return Ok(response);
        }

        [HttpPost("approve/investors")]
        [Authorize(Policy = "StaffOrAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ApproveInvestorRegistration([FromBody]ApproveRegistrationRequest request)
        {
            var staffId = GetCurrentUserId();
            var response = await _registrationService.ApproveInvestorRegistrationAsync(staffId, request);
            return Ok(response);
        }

        #region helper method
        private int GetCurrentUserId()
        {
            var claim = User.FindFirst("sub")?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }
        #endregion
    }
}
