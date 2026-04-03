using AISEP.Application.DTOs.Payment;
using AISEP.Application.Interfaces;
using Microsoft.AspNetCore.Http;
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
        [ProducesResponseType(typeof(PaymentInfoDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PaymentInfoDto>> CreatePaymentLink(PaymentRequestDto paymentRequest)
        {
            try
            {
                var paymentInfo = await _paymentService.CreatePaymentLink(paymentRequest);
                return Ok(paymentInfo);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error creating payment link", error = ex.Message });
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
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<string>> CallBack()
        {
            try
            {
                var result = await _paymentService.CallBack(Request);
                return Ok(result);
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(new { message = "Invalid webhook payload", error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error processing webhook", error = ex.Message });
            }
        }
    }
}
