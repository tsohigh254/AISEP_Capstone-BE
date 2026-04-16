using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AISEP.Domain.Entities
{
    public class AdvisorWallet
    {
        public int WalletId { get; set; }
        public int AdvisorId { get; set; }
        public decimal Balance { get; set; } = 0M;
        public decimal TotalEarned { get; set; } = 0M;
        public decimal TotalWithdrawn { get; set; } = 0M;
        public string? BankAccountNumber { get; set; }
        public string? BankBin { get; set; }
        public string? BankName { get; set; }
        public DateTime CreatedAt { get; set; }
        public Advisor Advisor { get; set; } = null!;
        public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
    }
}
