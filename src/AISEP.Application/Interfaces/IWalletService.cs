using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Wallet;
using AISEP.Application.QueryParams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AISEP.Application.Interfaces
{
    public interface IWalletService
    {
        public Task<ApiResponse<WalletDto>> GetWalletByAdvisorAsync(int userId);
        public Task<ApiResponse<PagedResponse<TransactionDto>>> GetTransactionsAsync(int walletId, WalletTransactionQueryParams transactionQueryParams);
        public Task<ApiResponse<WalletDto>> UpdateBankInfoAsync(int userId, UpdateBankInfoDto request);
        public Task<ApiResponse<WalletDto>> CreateWalletAsync(int userId, CreateWalletDto request);
    }
}
