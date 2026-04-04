using AISEP.Application.DTOs.Payment;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AISEP.Application.Interfaces
{
    public interface IPaymentService
    {
        public Task<PaymentInfoDto> CreatePaymentLink(PaymentRequestDto paymentRequest);
        public Task<string> ConfirmWebHook(string webhookUrl);
        //public Task<string> Payout(int totalAmount, string accountNumber, string bin);
        public Task<string> CallBack(HttpRequest request);
    }
}
