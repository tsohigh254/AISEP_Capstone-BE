using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Wallet;
using AISEP.Application.Extensions;
using AISEP.Application.Interfaces;
using AISEP.Application.QueryParams;
using AISEP.Domain.Entities;
using AISEP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AISEP.Infrastructure.Services
{
    public class WalletService : IWalletService
    {
        public readonly ApplicationDbContext _context;

        public WalletService(ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task<ApiResponse<PagedResponse<TransactionDto>>> GetTransactionsAsync(int walletId, WalletTransactionQueryParams transactionQueryParams)
        {
            var transactions = _context.WalletTransactions
                .OrderByDescending(t => t.CreatedAt)
                .Where(t => t.WalletId == walletId)
                .AsQueryable();

            if (transactionQueryParams.TransactionType.HasValue)
                transactions = transactions.Where(t => t.Type == transactionQueryParams.TransactionType.Value);

            if (transactionQueryParams.TransactionStatus.HasValue)
                transactions = transactions.Where(t => t.Status == transactionQueryParams.TransactionStatus.Value);

            var transactionsToDto = transactions.Select(t => new TransactionDto
            {
                TransactionID = t.TransactionID,
                WalletId = t.WalletId,
                Amount = t.Amount,
                Type = t.Type,
                Status = t.Status,
                CreatedAt = t.CreatedAt,
            }).Paging(transactionQueryParams.Page, transactionQueryParams.PageSize);

            return ApiResponse<PagedResponse<TransactionDto>>.SuccessResponse
                (new PagedResponse<TransactionDto>
                {
                    Items = await transactionsToDto.ToListAsync(),
                    Paging = new PagingInfo
                    {
                        Page = transactionQueryParams.Page,
                        PageSize = transactionQueryParams.PageSize,
                        TotalItems = await transactions.CountAsync()
                    }
                });
        }

        public async Task<ApiResponse<WalletDto>> GetWalletByAdvisorAsync(int userId)
        {
            var wallet = await _context.AdvisorWallets
                .Include(a => a.Advisor)
                .FirstOrDefaultAsync(w => w.Advisor.UserID == userId);

            if (wallet == null)
                return ApiResponse<WalletDto>.ErrorResponse("WALLET_DOES_NOT_EXIST", "Ví không tồn tại");

            var walletToDto = new WalletDto
            {
                WalletId = wallet.WalletId,
                AdvisorId = wallet.AdvisorId,
                Balance = wallet.Balance,
                TotalEarned = wallet.TotalEarned,
                TotalWithdrawn = wallet.TotalWithdrawn,
                CreatedAt  = wallet.CreatedAt,
            };

            return ApiResponse<WalletDto>.SuccessResponse(walletToDto, "Lấy ví thành công");
        }
    }
}
