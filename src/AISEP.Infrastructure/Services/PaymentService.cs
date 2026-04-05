using AISEP.Application.DTOs.Payment;
using AISEP.Application.Interfaces;
using AISEP.Infrastructure.Data;
using DotNetEnv;
using Microsoft.AspNetCore.Http;
using Nethereum.Model;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AISEP.Infrastructure.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly PayOSClient _payOS;

        public PaymentService(ApplicationDbContext context, PayOSClient payOS)
        {
            _context = context;
            _payOS = payOS;
        }
        public async Task<string> CallBack(HttpRequest request)
        {
            using var reader = new StreamReader(request.Body);
            var body = await reader.ReadToEndAsync();

            var webhook = JsonSerializer.Deserialize<Webhook>(
             body,
             new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (webhook?.Data == null)
                throw new ArgumentNullException(nameof(webhook), "Invalid payload");

            // Verify signature + parse data (SDK làm)
            var result = await _payOS.Webhooks.VerifyAsync(webhook);

            // Write logic if user paid sucessfully
            return "Webhook processed successfully";
        }

        public async Task<string> ConfirmWebHook(string webhookUrl)
        {
            var result = await _payOS.Webhooks.ConfirmAsync(webhookUrl);
            return result.WebhookUrl;
        }

        public async Task<PaymentInfoDto> CreatePaymentLink(PaymentRequestDto paymentRequest)
        {
            Env.Load();
            var url = Env.GetString("Frontend__URI");

            var paymentLinkRequest = new CreatePaymentLinkRequest
            {
                OrderCode = paymentRequest.OrderCode,
                Amount = paymentRequest.Amount,
                Description = string.IsNullOrEmpty(paymentRequest.Description) ? "Nâng cấp tài khoản" : paymentRequest.Description,
                ExpiredAt = (int)DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds(),
                ReturnUrl = string.IsNullOrEmpty(paymentRequest.ReturnUrl) ? $"{url}/checkout/success?orderCode={paymentRequest.OrderCode}" : paymentRequest.ReturnUrl,
                CancelUrl = string.IsNullOrEmpty(paymentRequest.CancelUrl) ? url : paymentRequest.CancelUrl
            };


            var paymentInfo = await _payOS.PaymentRequests.CreateAsync(paymentLinkRequest);

            var response = new PaymentInfoDto
            {
                CheckoutUrl = paymentInfo.CheckoutUrl,
                OrderCode = (int)paymentInfo.OrderCode
            };

            return response;
        }

        //public Task<string> Payout(int totalAmount, string accountNumber, string bin)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
