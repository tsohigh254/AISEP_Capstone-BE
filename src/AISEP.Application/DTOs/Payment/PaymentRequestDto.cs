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
        public int MentorshipId { get; set; }
    }
}
