using System.ComponentModel.DataAnnotations;

namespace AISEP.Application.DTOs.Wallet
{
    public class CreateWalletDto
    {
        [Required]
        public string BankAccountNumber { get; set; } = null!;

        [Required]
        public string BankBin { get; set; } = null!;

        [Required]
        public string BankName { get; set; } = null!;
    }
}
