using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AISEP.Application.DTOs.Wallet
{
    public class UpdateBankInfoDto
    {
        [Required]
        public string BankAccountNumber { get; set; } = null!;
        [Required]
        public string BankBin { get; set; } = null!;
        [Required]
        public string BankName { get; set; } = null!;
    }
}
