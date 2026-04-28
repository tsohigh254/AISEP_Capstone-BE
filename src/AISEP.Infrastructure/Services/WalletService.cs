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
        public async Task<ApiResponse<PagedResponse<TransactionDto>>> GetTransactionsAsync(int walletId, string userType, WalletTransactionQueryParams transactionQueryParams)
        {
            var query = _context.WalletTransactions
                .OrderByDescending(t => t.CreatedAt)
                .AsQueryable();

            if (userType == "Advisor")
            {
                query = query.Where(t => t.WalletId == walletId);
            }
            else // Startup
            {
                query = query.Where(t => t.StartupWalletId == walletId);
            }

            if (transactionQueryParams.TransactionType.HasValue)
                query = query.Where(t => t.Type == transactionQueryParams.TransactionType.Value);

            if (transactionQueryParams.TransactionStatus.HasValue)
                query = query.Where(t => t.Status == transactionQueryParams.TransactionStatus.Value);

            var total = await query.CountAsync();
            var items = await query
                .Skip((transactionQueryParams.Page - 1) * transactionQueryParams.PageSize)
                .Take(transactionQueryParams.PageSize)
                .Select(t => new TransactionDto
                {
                    TransactionID = t.TransactionID,
                    WalletId = t.WalletId ?? t.StartupWalletId ?? 0,
                    Amount = t.Amount,
                    Type = t.Type,
                    Status = t.Status,
                    CreatedAt = t.CreatedAt,
                })
                .ToListAsync();

            return ApiResponse<PagedResponse<TransactionDto>>.SuccessResponse(new PagedResponse<TransactionDto>
            {
                Items = items,
                Paging = new PagingInfo { Page = transactionQueryParams.Page, PageSize = transactionQueryParams.PageSize, TotalItems = total }
            });
        }

        public async Task<ApiResponse<WalletDto>> GetWalletByAdvisorAsync(int userId)
        {
            var wallet = await _context.AdvisorWallets
                .Include(w => w.Advisor)
                .FirstOrDefaultAsync(a => a.Advisor.UserID == userId);

            if (wallet == null)
                return ApiResponse<WalletDto>.ErrorResponse("WALLET_DOES_NOT_EXIST", "Ví không tồn tại");

            return ApiResponse<WalletDto>.SuccessResponse(MapWalletToDto(wallet));
        }

        public async Task<ApiResponse<WalletDto>> GetWalletByStartupAsync(int userId)
        {
            var wallet = await _context.StartupWallets
                .Include(w => w.Startup)
                .FirstOrDefaultAsync(s => s.Startup.UserID == userId);

            if (wallet == null)
                return ApiResponse<WalletDto>.ErrorResponse("WALLET_DOES_NOT_EXIST", "Ví không tồn tại");

            return ApiResponse<WalletDto>.SuccessResponse(MapWalletToDto(wallet));
        }

        public async Task<ApiResponse<WalletDto>> UpdateBankInfoAsync(int userId, string userType, UpdateBankInfoDto request)
        {
            if (userType == "Advisor")
            {
                var wallet = await _context.AdvisorWallets
                    .Include(w => w.Advisor)
                    .FirstOrDefaultAsync(a => a.Advisor.UserID == userId);

                if (wallet == null) return ApiResponse<WalletDto>.ErrorResponse("WALLET_DOES_NOT_EXIST", "Ví không tồn tại");

                wallet.BankAccountNumber = request.BankAccountNumber;
                wallet.BankBin = request.BankBin;
                wallet.BankName = request.BankName;
                await _context.SaveChangesAsync();
                return ApiResponse<WalletDto>.SuccessResponse(MapWalletToDto(wallet));
            }
            else
            {
                var wallet = await _context.StartupWallets
                    .Include(w => w.Startup)
                    .FirstOrDefaultAsync(s => s.Startup.UserID == userId);

                if (wallet == null) return ApiResponse<WalletDto>.ErrorResponse("WALLET_DOES_NOT_EXIST", "Ví không tồn tại");

                wallet.BankAccountNumber = request.BankAccountNumber;
                wallet.BankBin = request.BankBin;
                wallet.BankName = request.BankName;
                await _context.SaveChangesAsync();
                return ApiResponse<WalletDto>.SuccessResponse(MapWalletToDto(wallet));
            }
        }

        public async Task<ApiResponse<WalletDto>> CreateWalletAsync(int userId, string userType, CreateWalletDto request)
        {
            if (userType == "Advisor")
            {
                var advisor = await _context.Advisors.FirstOrDefaultAsync(a => a.UserID == userId);
                if (advisor == null) return ApiResponse<WalletDto>.ErrorResponse("ADVISOR_NOT_FOUND", "Tài khoản advisor không tồn tại.");

                var existingWallet = await _context.AdvisorWallets.FirstOrDefaultAsync(w => w.AdvisorId == advisor.AdvisorID);
                if (existingWallet != null) return ApiResponse<WalletDto>.ErrorResponse("WALLET_ALREADY_EXISTS", "Ví đã được tạo.");

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
                return ApiResponse<WalletDto>.SuccessResponse(MapWalletToDto(wallet));
            }
            else
            {
                var startup = await _context.Startups.FirstOrDefaultAsync(s => s.UserID == userId);
                if (startup == null) return ApiResponse<WalletDto>.ErrorResponse("STARTUP_NOT_FOUND", "Tài khoản startup không tồn tại.");

                var existingWallet = await _context.StartupWallets.FirstOrDefaultAsync(w => w.StartupId == startup.StartupID);
                if (existingWallet != null) return ApiResponse<WalletDto>.ErrorResponse("WALLET_ALREADY_EXISTS", "Ví đã được tạo.");

                var wallet = new StartupWallet
                {
                    StartupId = startup.StartupID,
                    BankAccountNumber = request.BankAccountNumber,
                    BankBin = request.BankBin,
                    BankName = request.BankName,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.StartupWallets.AddAsync(wallet);
                await _context.SaveChangesAsync();
                return ApiResponse<WalletDto>.SuccessResponse(MapWalletToDto(wallet));
            }
        }

        private WalletDto MapWalletToDto(AdvisorWallet w) => new WalletDto
        {
            WalletId = w.WalletId,
            AdvisorId = w.AdvisorId,
            Balance = w.Balance,
            TotalEarned = w.TotalEarned,
            TotalWithdrawn = w.TotalWithdrawn,
            BankAccountNumber = w.BankAccountNumber,
            BankBin = w.BankBin,
            BankName = w.BankName,
            CreatedAt = w.CreatedAt
        };

        private WalletDto MapWalletToDto(StartupWallet w) => new WalletDto
        {
            WalletId = w.WalletId,
            StartupId = w.StartupId,
            Balance = w.Balance,
            TotalRefunded = w.TotalRefunded,
            TotalWithdrawn = w.TotalWithdrawn,
            BankAccountNumber = w.BankAccountNumber,
            BankBin = w.BankBin,
            BankName = w.BankName,
            CreatedAt = w.CreatedAt
        };
    }
}
