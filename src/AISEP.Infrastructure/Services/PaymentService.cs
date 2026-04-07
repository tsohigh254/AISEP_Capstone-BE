using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Payment;
using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Domain.Enums;
using AISEP.Infrastructure.Data;
using DotNetEnv;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using System.Text.Json;

namespace AISEP.Infrastructure.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly PayOSClient _payOS;
        private readonly ILogger<PaymentService> _logger;
        private const decimal PLATFORM_FEE_PERCENTAGE = 15M;

        public PaymentService(
            ApplicationDbContext context,
            PayOSClient payOS,
            IConfiguration configuration,
            ILogger<PaymentService> logger)
        {
            _context = context;
            _payOS = payOS;
            _logger = logger;
        }

        public async Task<ApiResponse<string>> CallBack(HttpRequest request)
        {
            using var reader = new StreamReader(request.Body);
            var body = await reader.ReadToEndAsync();

            var webhook = JsonSerializer.Deserialize<Webhook>(
                body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (webhook?.Data == null)
                throw new ArgumentNullException(nameof(webhook), "Invalid payload");

            var result = await _payOS.Webhooks.VerifyAsync(webhook);

            _logger.LogInformation("Result of callback : {result}", result);

            if (result.Code == null)
                return ApiResponse<string>.ErrorResponse("TRANSACTION_CODE_INVALID", "Transaction code is invalid");

            var mentorship = await _context.StartupAdvisorMentorships
                .FirstOrDefaultAsync(m => m.TransactionCode == result.OrderCode);

            _logger.LogInformation("Mentorship {id}", mentorship.MentorshipID);

            if (mentorship == null)
                return ApiResponse<string>.ErrorResponse("MENTORSHIP_DOES_NOT_EXIST", "Mentorship does not exist");     

            if (mentorship.PaymentStatus == PaymentStatus.Completed)
                return ApiResponse<string>.SuccessResponse("Webhook already processed");

            if (!string.Equals(result.Code, "00", StringComparison.OrdinalIgnoreCase))
            {
                mentorship.PaymentStatus = PaymentStatus.Failed;
                mentorship.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogError($"Payment failed with code '{result.Code}'");
                return ApiResponse<string>.ErrorResponse("PAYMENT_FAILED", $"Payment failed with code '{result.Code}'");
            }

            var sessionAmount = (decimal)result.Amount;
            var platformFeeAmount = Math.Round(sessionAmount * PLATFORM_FEE_PERCENTAGE / 100, 2);
            var actualAmount = sessionAmount - platformFeeAmount;

            mentorship.SessionAmount = sessionAmount;
            mentorship.PlatformFeeAmount = platformFeeAmount;
            mentorship.ActualAmount = actualAmount;
            mentorship.PaymentStatus = PaymentStatus.Completed;
            mentorship.PaidAt = DateTime.UtcNow;
            mentorship.UpdatedAt = DateTime.UtcNow;

            var wallet = await _context.AdvisorWallets.FirstOrDefaultAsync(w => w.AdvisorId == mentorship.AdvisorID);

            _logger.LogInformation("Wallet {id}", wallet.WalletId);
            if (wallet == null)
                return ApiResponse<string>.ErrorResponse("WALLET_DOES_NOT_EXIST", "Wallet does not exist");

            wallet.Balance += actualAmount;
            wallet.TotalEarned += actualAmount;

            var walletTransaction = new WalletTransaction
            {
                WalletId = wallet.WalletId,
                MentorshipID = mentorship.MentorshipID,
                Amount = actualAmount,
                Type = TransactionType.Deposit,
                Status = TransactionStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            _context.StartupAdvisorMentorships.Update(mentorship);
            _context.AdvisorWallets.Update(wallet);
            await _context.WalletTransactions.AddAsync(walletTransaction);
            await _context.SaveChangesAsync();

            return ApiResponse<string>.SuccessResponse("Webhook processed successfully");
        }

        public async Task<string> ConfirmWebHook(string webhookUrl)
        {
            var result = await _payOS.Webhooks.ConfirmAsync(webhookUrl);
            return result.WebhookUrl;
        }

        public async Task<ApiResponse<PaymentInfoDto>> CreatePaymentLink(PaymentRequestDto paymentRequest)
        {
            if (paymentRequest.Amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.", nameof(paymentRequest.Amount));

            var mentorship = await _context.StartupAdvisorMentorships
                .FirstOrDefaultAsync(m => m.MentorshipID == paymentRequest.MentorshipId);

            if (mentorship == null)
                throw new InvalidOperationException($"Mentorship {paymentRequest.MentorshipId} not found.");

            if (mentorship.PaymentStatus == PaymentStatus.Completed)
                throw new InvalidOperationException("This mentorship has already been paid.");

            var response = await PaymentLink(paymentRequest);

            mentorship.TransactionCode = response.OrderCode;
            mentorship.PaymentStatus = PaymentStatus.Pending;

            _context.StartupAdvisorMentorships.Update(mentorship);
            await _context.SaveChangesAsync();

            return ApiResponse<PaymentInfoDto>.SuccessResponse(response, "Create payment link successfully");
            
        }

        private async Task<PaymentInfoDto> PaymentLink(PaymentRequestDto paymentRequest)
        {
            Env.Load();
            var orderCode = int.Parse(DateTimeOffset.Now.ToString("ffffff"));
            var url = Env.GetString("Frontend__URI");

            var paymentLinkRequest = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = paymentRequest.Amount,
                Description = "Thanh toán ??n hàng",
                ExpiredAt = (int)DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds(),
                ReturnUrl = $"{url}/startup/mentorship-requests/{paymentRequest.MentorshipId}/checkout/result?status=success",
                CancelUrl = $"url/startup/mentorship-requests/{paymentRequest.MentorshipId}/checkout"
            };

            var paymentInfo = await _payOS.PaymentRequests.CreateAsync(paymentLinkRequest);

            return new PaymentInfoDto
            {
                CheckoutUrl = paymentInfo.CheckoutUrl,
                OrderCode = (int)paymentInfo.OrderCode
            };
        }
    }
}
