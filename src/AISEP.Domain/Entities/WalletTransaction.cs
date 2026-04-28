using AISEP.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace AISEP.Domain.Entities
{
    public class WalletTransaction
    {
        public int TransactionID { get; set; }
        public int? WalletId { get; set; }                   // Link to AdvisorWallet
        public int? StartupWalletId { get; set; }            // Link to StartupWallet
        public int MentorshipID { get; set; }

        public decimal Amount { get; set; }                  // Số tiền giao dịch
        public TransactionType Type { get; set; }            // Deposit, Withdrawal, Refund
        public TransactionStatus Status { get; set; }        // Pending, Completed, Failed

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public AdvisorWallet? Wallet { get; set; }
        public StartupWallet? StartupWallet { get; set; }
        public StartupAdvisorMentorship Mentorship { get; set; } = null!;
    }
}
