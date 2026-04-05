using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AISEP.Application.DTOs.Payment
{
    public class PaymentRequestDto
    {
        public int Amount { get; set; }
        public int OrderCode { get; set; }
        public string? Description { get; set; }
        public string? ReturnUrl { get; set; }
        public string? CancelUrl { get; set; }
    }
}
