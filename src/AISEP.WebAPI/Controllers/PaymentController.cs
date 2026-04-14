using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Payment;
using AISEP.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayOS;
using PayOS.Models.V1.PayoutsAccount;

namespace AISEP.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        /// <summary>
        /// Creates a payment link for upgrading account
        /// </summary>
        /// <param name="amount">Payment amount in Vietnamese Dong (VND)</param>
        /// <param name="orderCode">Unique order code</param>
        /// <returns>Payment link information including checkout URL</returns>
        [HttpPost("create-payment-link")]
        [ProducesResponseType(typeof(ApiResponse<PaymentInfoDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<PaymentInfoDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<PaymentInfoDto>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PaymentInfoDto>> CreatePaymentLink([FromBody] PaymentRequestDto paymentRequest)
        {
            if (paymentRequest == null || paymentRequest.MentorshipId <= 0 || paymentRequest.Amount <= 0)
                return BadRequest(ApiResponse<PaymentInfoDto>.ErrorResponse(
                    "INVALID_PAYMENT_REQUEST",
                    "MentorshipId and amount must be greater than 0."));

            var paymentInfo = await _paymentService.CreatePaymentLinkForMentorship(paymentRequest);
            return ToActionResult(paymentInfo);
        }

        /// <summary>
        /// Creates a payment link for Startup Subscription upgrades (Pro, Fundraising)
        /// </summary>
        [HttpPost("subscription/create-payment-link")]
        [ProducesResponseType(typeof(ApiResponse<PaymentInfoDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<PaymentInfoDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<PaymentInfoDto>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PaymentInfoDto>> CreatePaymentLinkForSubscription([FromBody] SubscriptionPaymentRequestDto paymentRequest)
        {
            if (paymentRequest == null || paymentRequest.Amount <= 0)
                return BadRequest(ApiResponse<PaymentInfoDto>.ErrorResponse(
                    "INVALID_PAYMENT_REQUEST",
                    "Amount must be greater than 0."));

            var userId = GetCurrentUserId();
            if (userId <= 0)
                return Unauthorized(ApiResponse<PaymentInfoDto>.ErrorResponse(
                    "UNAUTHORIZED",
                    "User identity is missing or invalid."));

            var paymentInfo = await _paymentService.CreatePaymentLinkForSubscription(userId, paymentRequest);
            return ToActionResult(paymentInfo);
        }

        /// <summary>
        /// Confirms webhook URL for PayOS
        /// </summary>
        /// <param name="webhookUrl">The webhook URL to confirm</param>
        /// <returns>Confirmed webhook URL</returns>
        [HttpPost("confirm-webhook")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<string>> ConfirmWebHook([FromQuery] string webhookUrl)
        {
            if (string.IsNullOrWhiteSpace(webhookUrl))
                return BadRequest("Webhook URL cannot be empty");

            try
            {
                var result = await _paymentService.ConfirmWebHook(webhookUrl);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error confirming webhook", error = ex.Message });
            }
        }

        /// <summary>
        /// Handles PayOS webhook callback for payment notifications
        /// </summary>
        /// <returns>Webhook processing result</returns>
        [HttpPost("callback")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CallBack()
        {
            var result = await _paymentService.CallBack(Request);
            return Ok(result);
            //return Ok("Ok");
            
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

        [HttpGet("balance")]
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
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error processing cashout", error = ex.Message });
            }
        }

        #region helper method

        private ActionResult<TResponse> ToActionResult<TResponse>(ApiResponse<TResponse> response)
        {
            if (response.Success)
                return Ok(response);

            return BadRequest(response);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst("sub")?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }
        #endregion
    }
}
