using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AISEP.Domain.Entities
{
    public class StartupWallet
    {
        public int WalletId { get; set; }

        public int StartupId { get; set; }

        public decimal Balance { get; set; } = 0M;

        public decimal TotalRefunded { get; set; } = 0M;

        public decimal TotalWithdrawn { get; set; } = 0M;

        public string? BankAccountNumber { get; set; }

        public string? BankBin { get; set; }

        public string? BankName { get; set; }

        public DateTime CreatedAt { get; set; }

        public Startup Startup { get; set; } = null!;
        public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
    }
}
