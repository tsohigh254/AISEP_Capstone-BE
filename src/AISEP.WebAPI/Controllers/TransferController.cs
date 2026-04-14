using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Payment;
using AISEP.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PayOS;
using PayOS.Models.V1.PayoutsAccount;

namespace AISEP.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransferController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public TransferController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        /// <summary>
        /// Process money withdrawal for completed Mentorship sessions
        /// </summary>
        [HttpPost("cashout")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<string>>> Cashout([FromBody] CashoutRequestDto cashoutRequestDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId <= 0)
                    return Unauthorized(ApiResponse<string>.ErrorResponse(
                        "UNAUTHORIZED",
                        "User identity is missing or invalid."));

                var result = await _paymentService.Cashout(userId, cashoutRequestDto);
                if (result.Success)
                    return Ok(result);
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error processing cashout", error = ex.Message });
            }
        }

        [HttpGet("check-balance")]
        [ProducesResponseType(typeof(PayoutAccountInfo), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(PayoutAccountInfo), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(PayoutAccountInfo), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PayoutAccountInfo>> CheckBalance()
        {
            try
            {
                var result = await _paymentService.GetAccountBalance();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Failed to get account balance", error = ex.Message });
            }
        }


        private int GetCurrentUserId()
        {
            var claim = User.FindFirst("sub")?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }
}
