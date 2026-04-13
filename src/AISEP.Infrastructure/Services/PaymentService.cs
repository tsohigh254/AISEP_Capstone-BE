using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Payment;
using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Domain.Enums;
using AISEP.Infrastructure.Data;
using DotNetEnv;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PayOS;
using PayOS.Models.V1.Payouts;
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
        private readonly IBackgroundJobClient _backgroundJobClient;
        private const decimal PLATFORM_FEE_PERCENTAGE = 15M;
        private readonly string url;

        public PaymentService(
            ApplicationDbContext context,
            PayOSClient payOS,
            ILogger<PaymentService> logger,
            IBackgroundJobClient backgroundJobClient)
        {
            Env.Load();
            _context = context;
            _payOS = payOS;
            _logger = logger;
            _backgroundJobClient = backgroundJobClient;
            url = Env.GetString("Frontend__URI");
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

            if (mentorship != null)
            {
                _logger.LogInformation("Mentorship {id}", mentorship.MentorshipID);     

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

                if (wallet == null)
                    return ApiResponse<string>.ErrorResponse("WALLET_NOT_FOUND", "Wallet not found");

                _logger.LogInformation("Wallet {id}", wallet.WalletId);

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

                return ApiResponse<string>.SuccessResponse("Webhook processed successfully for mentorship");
            }

            var subPayment = await _context.StartupSubscriptionPayments
                .Include(p => p.Startup)
                .FirstOrDefaultAsync(p => p.TransactionCode == result.OrderCode);

            if (subPayment != null)
            {
                if (subPayment.PaymentStatus == PaymentStatus.Completed)
                    return ApiResponse<string>.SuccessResponse("Webhook already processed");

                if (!string.Equals(result.Code, "00", StringComparison.OrdinalIgnoreCase))
                {
                    subPayment.PaymentStatus = PaymentStatus.Failed;
                    await _context.SaveChangesAsync();
                    return ApiResponse<string>.ErrorResponse("PAYMENT_FAILED", $"Payment failed with code '{result.Code}'");
                }

                subPayment.PaymentStatus = PaymentStatus.Completed;
                subPayment.PaidAt = DateTime.UtcNow;

                var startup = subPayment.Startup;
                startup.SubscriptionPlan = subPayment.TargetPlan;
                startup.SubscriptionEndDate = DateTime.UtcNow.AddDays(30);

                await _context.SaveChangesAsync();
                return ApiResponse<string>.SuccessResponse("Subscription upgrade processed successfully");
            }

            return ApiResponse<string>.ErrorResponse("TRANSACTION_NOT_FOUND", "Transaction not found");
        }

        public async Task<string> ConfirmWebHook(string webhookUrl)
        {
            var result = await _payOS.Webhooks.ConfirmAsync(webhookUrl);
            return result.WebhookUrl;
        }

        public async Task<ApiResponse<PaymentInfoDto>> CreatePaymentLinkForMentorship(PaymentRequestDto paymentRequest)
        {
            if (paymentRequest.Amount <= 0)
                return ApiResponse<PaymentInfoDto>.ErrorResponse("INVALID_AMOUNT", "Amount must be greater than zero.");

            var mentorship = await _context.StartupAdvisorMentorships
                .FirstOrDefaultAsync(m => m.MentorshipID == paymentRequest.MentorshipId);

            if (mentorship == null)
               return ApiResponse<PaymentInfoDto>.ErrorResponse("MENTORSHIP_NOT_FOUND", "Mentorship not found.");

            if (mentorship.PaymentStatus == PaymentStatus.Completed)
                return ApiResponse<PaymentInfoDto>.ErrorResponse("ALREADY_PAID", "This mentorship has already been paid.");

            var response = await PaymentLinkForMentorship(paymentRequest);

            mentorship.TransactionCode = response.OrderCode;
            mentorship.PaymentStatus = PaymentStatus.Pending;

            _context.StartupAdvisorMentorships.Update(mentorship);
            await _context.SaveChangesAsync();

            return ApiResponse<PaymentInfoDto>.SuccessResponse(response, "Create payment link successfully");
            
        }

        public async Task<ApiResponse<PaymentInfoDto>> CreatePaymentLinkForSubscription(int userId, SubscriptionPaymentRequestDto paymentRequest)
        {
            if (paymentRequest.Amount <= 0)
                return ApiResponse<PaymentInfoDto>.ErrorResponse("INVALID_AMOUNT", "Amount must be greater than zero.");

            var startup = await _context.Startups.FirstOrDefaultAsync(s => s.UserID == userId);
            if (startup == null)
                return ApiResponse<PaymentInfoDto>.ErrorResponse("STARTUP_NOT_FOUND", "Startup not found.");

            if (startup.SubscriptionPlan == paymentRequest.TargetPlan && startup.SubscriptionEndDate > DateTime.UtcNow)
                return ApiResponse<PaymentInfoDto>.ErrorResponse("ALREADY_SUBSCRIBED", "Startup is already subscribed to this plan and it is still active.");

            var response = await PaymentLinkForSubscription(paymentRequest); 

            var subPayment = new StartupSubscriptionPayment
            {
                StartupID = startup.StartupID,
                TargetPlan = paymentRequest.TargetPlan,
                Amount = paymentRequest.Amount,
                TransactionCode = response.OrderCode,
                PaymentStatus = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _context.StartupSubscriptionPayments.AddAsync(subPayment);
            await _context.SaveChangesAsync();

            _backgroundJobClient.Schedule<AISEP.Infrastructure.Jobs.PaymentExpirationJob>(
                job => job.ExpireSubscriptionPayment(subPayment.PaymentID),
                TimeSpan.FromMinutes(10));

            return ApiResponse<PaymentInfoDto>.SuccessResponse(response, "Create payment link successfully");
        }

        public async Task<ApiResponse<string>> Cashout(int userId, CashoutRequestDto cashoutRequestDto)
        {
            var transaction = await _context.WalletTransactions
                .FirstOrDefaultAsync(t => t.TransactionID == cashoutRequestDto.TransactionId);

            if (transaction == null)
                return ApiResponse<string>.ErrorResponse("TRANSACTION_NOT_FOUND", "Transaction not found");

            if (transaction.Type != TransactionType.Deposit || transaction.Status != TransactionStatus.Completed)
                return ApiResponse<string>.ErrorResponse(
                    "INVALID_TRANSACTION",
                    "Only completed deposit transactions can be withdrawn.");

            var wallet = await _context.AdvisorWallets
                .Include(w => w.Advisor)
                .FirstOrDefaultAsync(w => w.WalletId == transaction.WalletId);

            if (wallet == null)
                return ApiResponse<string>.ErrorResponse("WALLET_NOT_FOUND", "Wallet not found");

            if (wallet.Advisor.UserID != userId)
                return ApiResponse<string>.ErrorResponse(
                    "FORBIDDEN",
                    "You cannot withdraw from another advisor's wallet.");

            if (wallet.Balance < transaction.Amount)
                return ApiResponse<string>.ErrorResponse("INSUFFICIENT_BALANCE", "Wallet balance is insufficient");

            transaction.Type = TransactionType.Withdrawal;
            transaction.CreatedAt = DateTime.UtcNow;

            var payoutRequest = new PayoutRequest
            {
                ReferenceId = "payout",
                Amount = (int)transaction.Amount,
                Description = "Rút tiền",
                ToAccountNumber = cashoutRequestDto.AccountNumber,
                ToBin = cashoutRequestDto.Bin
            };

            var response = await _payOS.Payouts.CreateAsync(payoutRequest);

            _logger.LogInformation("Payout request : {0}", payoutRequest);

            wallet.Balance -= transaction.Amount;
            wallet.TotalWithdrawn += transaction.Amount;

            _context.AdvisorWallets.Update(wallet);
            _context.WalletTransactions.Update(transaction);
            await _context.SaveChangesAsync();

            return ApiResponse<string>.SuccessResponse("CASH_OUT_SUCCESSFULLY", "Cash out successfully");
        }

        #region helper method


        private async Task<PaymentInfoDto> PaymentLinkForMentorship(PaymentRequestDto paymentRequest)
        {
            var orderCode = (int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 2_000_000_000);

            var paymentLinkRequest = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = paymentRequest.Amount,
                Description = "Thanh toan mentorship",
                ExpiredAt = (int)DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds(),
                ReturnUrl = $"{url}/startup/mentorship-requests/{paymentRequest.MentorshipId}/checkout/result?status=success",
                CancelUrl = $"{url}/startup/mentorship-requests/{paymentRequest.MentorshipId}/checkout"
            };

            var paymentInfo = await _payOS.PaymentRequests.CreateAsync(paymentLinkRequest);

            return new PaymentInfoDto
            {
                CheckoutUrl = paymentInfo.CheckoutUrl,
                OrderCode = (int)paymentInfo.OrderCode
            };
        }

        private async Task<PaymentInfoDto> PaymentLinkForSubscription(SubscriptionPaymentRequestDto paymentRequest)
        {
            var orderCode = (int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 2_000_000_000);

            var paymentLinkRequest = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = paymentRequest.Amount,
                Description = "Nang cap tai khoan",
                ExpiredAt = (int)DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds(),
                ReturnUrl = $"{url}/startup/subscription/checkout/result?status=success",
                CancelUrl = $"{url}/startup/subscription"
            };

            var paymentInfo = await _payOS.PaymentRequests.CreateAsync(paymentLinkRequest);

            return new PaymentInfoDto
            {
                CheckoutUrl = paymentInfo.CheckoutUrl,
                OrderCode = orderCode
            };
        }

       
        #endregion
    }
}
