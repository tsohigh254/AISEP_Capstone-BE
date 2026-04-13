using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AISEP.Application.DTOs.Payment
{
    public class CashoutRequestDto
    {
        public string AccountNumber { get; set; } = null!;
        public string Bin { get; set; } = null!;
        public int TransactionId { get; set; }
    }
}
