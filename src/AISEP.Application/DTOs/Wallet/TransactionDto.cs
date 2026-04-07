using AISEP.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AISEP.Application.DTOs.Wallet
{
    public class TransactionDto
    {
        public int TransactionID { get; set; }
        public int WalletId { get; set; }

        public decimal Amount { get; set; }                  // Số tiền giao dịch
        public TransactionType Type { get; set; }            // Deposit (từ mentorship) hoặc Withdrawal (rút tiền)
        public TransactionStatus Status { get; set; }        // Pending, Completed, Failed

        public DateTime CreatedAt { get; set; }
    }
}
