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
                .Include(w => w.Advisor)
                .FirstOrDefaultAsync(a => a.Advisor.UserID == userId);

            if (wallet == null)
                return ApiResponse<WalletDto>.ErrorResponse("WALLET_DOES_NOT_EXIST", "Ví không tồn tại");

            var walletToDto = new WalletDto
            {
                WalletId = wallet.WalletId,
                AdvisorId = wallet.AdvisorId,
                Balance = wallet.Balance,
                TotalEarned = wallet.TotalEarned,
                TotalWithdrawn = wallet.TotalWithdrawn,
                BankAccountNumber = wallet.BankAccountNumber,
                BankBin = wallet.BankBin,
                BankName = wallet.BankName,
                CreatedAt  = wallet.CreatedAt,
            };

            return ApiResponse<WalletDto>.SuccessResponse(walletToDto, "Lấy ví thành công");
        }

        public async Task<ApiResponse<WalletDto>> UpdateBankInfoAsync(int userId, UpdateBankInfoDto request)
        {
            var wallet = await _context.AdvisorWallets
                .Include(w => w.Advisor)
                .FirstOrDefaultAsync(a => a.Advisor.UserID == userId);

            if (wallet == null)
                return ApiResponse<WalletDto>.ErrorResponse("WALLET_DOES_NOT_EXIST", "Ví không tồn tại");

            wallet.BankAccountNumber = request.BankAccountNumber;
            wallet.BankBin = request.BankBin;
            wallet.BankName = request.BankName;

            _context.AdvisorWallets.Update(wallet);
            await _context.SaveChangesAsync();

            var walletToDto = new WalletDto
            {
                WalletId = wallet.WalletId,
                AdvisorId = wallet.AdvisorId,
                Balance = wallet.Balance,
                TotalEarned = wallet.TotalEarned,
                TotalWithdrawn = wallet.TotalWithdrawn,
                BankAccountNumber = wallet.BankAccountNumber,
                BankBin = wallet.BankBin,
                BankName = wallet.BankName,
                CreatedAt  = wallet.CreatedAt,
            };

            return ApiResponse<WalletDto>.SuccessResponse(walletToDto, "Cập nhật thông tin ngân hàng thành công");
        }

        public async Task<ApiResponse<WalletDto>> CreateWalletAsync(int userId, CreateWalletDto request)
        {
            var advisor = await _context.Advisors.FirstOrDefaultAsync(a => a.UserID == userId);
            if (advisor == null)
                return ApiResponse<WalletDto>.ErrorResponse("ADVISOR_NOT_FOUND", "Tài khoản advisor không tồn tại.");

            var existingWallet = await _context.AdvisorWallets.FirstOrDefaultAsync(w => w.AdvisorId == advisor.AdvisorID);
            if (existingWallet != null)
                return ApiResponse<WalletDto>.ErrorResponse("WALLET_ALREADY_EXISTS", "Ví đã được tạo.");

            var wallet = new AdvisorWallet
            {
                AdvisorId = advisor.AdvisorID,
                BankAccountNumber = request.BankAccountNumber,
                BankBin = request.BankBin,
                BankName = request.BankName,
                CreatedAt = DateTime.UtcNow
            };

            await _context.AdvisorWallets.AddAsync(wallet);
            await _context.SaveChangesAsync();

            var walletToDto = new WalletDto
            {
                WalletId = wallet.WalletId,
                AdvisorId = wallet.AdvisorId,
                Balance = wallet.Balance,
                TotalEarned = wallet.TotalEarned,
                TotalWithdrawn = wallet.TotalWithdrawn,
                BankAccountNumber = wallet.BankAccountNumber,
                BankBin = wallet.BankBin,
                BankName = wallet.BankName,
                CreatedAt = wallet.CreatedAt,
            };

            return ApiResponse<WalletDto>.SuccessResponse(walletToDto, "Tạo ví thành công");
        }
    }
}
