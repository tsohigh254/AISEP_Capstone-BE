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
        public int? AdvisorId { get; set; }
        public int? StartupId { get; set; }
        public decimal Balance { get; set; } 
        public decimal TotalEarned { get; set; } // For Advisor
        public decimal TotalRefunded { get; set; } // For Startup
        public decimal TotalWithdrawn { get; set; } 
        public string? BankAccountNumber { get; set; }
        public string? BankBin { get; set; }
        public string? BankName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
