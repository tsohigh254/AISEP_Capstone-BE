using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Payment;
using Microsoft.AspNetCore.Http;
using PayOS.Models.V1.PayoutsAccount;
using PayOS.Models.V2.PaymentRequests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AISEP.Application.Interfaces
{
    public interface IPaymentService
    {
        public Task<ApiResponse<PaymentInfoDto>> CreatePaymentLinkForMentorship(PaymentRequestDto paymentRequest);
        public Task<ApiResponse<PaymentInfoDto>> CreatePaymentLinkForSubscription(int userId, SubscriptionPaymentRequestDto paymentRequest);
        public Task<string> ConfirmWebHook(string webhookUrl);
        public Task<ApiResponse<string>> Cashout(int userId, CashoutRequestDto cashoutRequestDto);
        public Task<ApiResponse<string>> CallBack(HttpRequest request);
        public Task<ApiResponse<string>> SyncPaymentStatusAsync(long orderCode, int? mentorshipId = null);
        public Task<PayoutAccountInfo> GetAccountBalance();
    }
}
