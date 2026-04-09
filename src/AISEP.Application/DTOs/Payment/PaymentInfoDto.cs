using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AISEP.Application.DTOs.Payment
{
    public class PaymentInfoDto
    {
        public string CheckoutUrl { get; set; } = string.Empty;
        public int OrderCode { get; set; }
    }
}
