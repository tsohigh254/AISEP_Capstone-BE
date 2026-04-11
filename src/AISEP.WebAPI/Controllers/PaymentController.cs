using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Payment;
using AISEP.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
            try
            {
                var paymentInfo = await _paymentService.CreatePaymentLinkForMentorship(paymentRequest);
                return Ok(paymentInfo);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
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
            try
            {
                var userIdStr = GetCurrentUserId();

                var paymentInfo = await _paymentService.CreatePaymentLinkForSubscription(userIdStr, paymentRequest);
                return Ok(paymentInfo);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
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

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst("sub")?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }
}
