using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AISEP.Application.DTOs.Wallet
{
    public class WalletDto
    {
        public int WalletId { get; set; }
        public int AdvisorId { get; set; }
        public decimal Balance { get; set; } 
        public decimal TotalEarned { get; set; } 
        public decimal TotalWithdrawn { get; set; } 
        public DateTime CreatedAt { get; set; }
    }
}
