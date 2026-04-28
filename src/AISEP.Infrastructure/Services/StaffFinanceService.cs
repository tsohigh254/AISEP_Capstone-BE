using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Staff;
using AISEP.Application.Interfaces;
using AISEP.Domain.Enums;
using AISEP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AISEP.Infrastructure.Services;

public class StaffFinanceService : IStaffFinanceService
{
    private readonly ApplicationDbContext _db;

    public StaffFinanceService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResponse<StaffFinanceStatsDto>> GetFinanceOverviewAsync(string period = "30D", int page = 1, int pageSize = 10)
    {
        var days = period?.ToUpperInvariant() == "30D" ? 30 : 7;
        var fromDate = DateTime.UtcNow.Date.AddDays(-days + 1);

        // 1. Get Mentorship Revenue & Commission
        var mentorships = await _db.StartupAdvisorMentorships
            .Where(m => m.PaymentStatus == PaymentStatus.Completed)
            .Select(m => new { 
                m.MentorshipID, 
                m.SessionAmount, 
                m.PlatformFeeAmount, 
                m.PaidAt, 
                m.MentorshipStatus, 
                StartupName = m.Startup.CompanyName,
                m.ActualAmount,
                m.PayoutReleasedAt,
                m.PaymentStatus,
                AdvisorName = m.Advisor.FullName
            })
            .ToListAsync();

        // 2. Get Subscription Revenue
        var subscriptions = await _db.StartupSubscriptionPayments
            .Where(s => s.PaymentStatus == PaymentStatus.Completed)
            .Select(s => new { s.Amount, s.PaidAt, s.TargetPlan, StartupName = s.Startup.CompanyName })
            .ToListAsync();

        // 3. Get Payouts (Withdrawals)
        var payouts = await _db.WalletTransactions
            .Where(t => t.Type == TransactionType.Withdrawal && t.Status == TransactionStatus.Completed)
            .Select(t => new { t.Amount, t.CreatedAt, AdvisorName = t.Wallet!.Advisor!.FullName })
            .ToListAsync();

        var filteredMentorships = mentorships.Where(m => m.PaidAt >= fromDate).ToList();
        var filteredSubscriptions = subscriptions.Where(s => s.PaidAt >= fromDate).ToList();
        var filteredPayouts = payouts.Where(p => p.CreatedAt >= fromDate).ToList();

        var totalMentorshipRevenue = filteredMentorships.Where(m => m.MentorshipStatus != MentorshipStatus.Cancelled).Sum(m => m.SessionAmount);
        var totalRefunds = filteredMentorships.Where(m => m.MentorshipStatus == MentorshipStatus.Cancelled).Sum(m => m.SessionAmount);
        var totalSubscriptionRevenue = filteredSubscriptions.Sum(s => s.Amount);
        var totalCommission = filteredMentorships.Where(m => m.MentorshipStatus != MentorshipStatus.Cancelled).Sum(m => m.PlatformFeeAmount) + totalSubscriptionRevenue;
        var totalPayouts = filteredPayouts.Sum(p => p.Amount);
        
        var totalRevenue = totalMentorshipRevenue + totalSubscriptionRevenue;

        // Balance always calculated from ALL completed transactions for accuracy
        var allTimeRevenue = mentorships.Where(m => m.MentorshipStatus != MentorshipStatus.Cancelled).Sum(m => m.SessionAmount) + subscriptions.Sum(s => s.Amount);
        var allTimeOutflow = payouts.Sum(p => p.Amount) + mentorships.Where(m => m.MentorshipStatus == MentorshipStatus.Cancelled).Sum(m => m.SessionAmount);
        var currentBalance = allTimeRevenue - allTimeOutflow;

        var incomeSources = new List<FinanceSourceDto>
        {
            new FinanceSourceDto { 
                SourceName = "Tiền Startup gửi (Tư vấn)", 
                Amount = totalMentorshipRevenue, 
                Percentage = totalRevenue > 0 ? (double)(totalMentorshipRevenue / totalRevenue * 100) : 0 
            },
            new FinanceSourceDto { 
                SourceName = "Tiền Startup mua gói (Sub)", 
                Amount = totalSubscriptionRevenue, 
                Percentage = totalRevenue > 0 ? (double)(totalSubscriptionRevenue / totalRevenue * 100) : 0 
            }
        };

        var expenseSources = new List<FinanceSourceDto>
        {
            new FinanceSourceDto { 
                SourceName = "Hệ thống trả Advisor (Payout)", 
                Amount = totalPayouts, 
                Percentage = (totalPayouts + totalRefunds) > 0 ? (double)(totalPayouts / (totalPayouts + totalRefunds) * 100) : 0 
            },
            new FinanceSourceDto { 
                SourceName = "Hoàn tiền cho Startup (Refund)", 
                Amount = totalRefunds, 
                Percentage = (totalPayouts + totalRefunds) > 0 ? (double)(totalRefunds / (totalPayouts + totalRefunds) * 100) : 0 
            }
        };

        // 4. Map all transactions in range (Actual realized transactions)
        var transMentorships = filteredMentorships
            .Where(m => m.PayoutReleasedAt != null || m.MentorshipStatus == MentorshipStatus.Cancelled) // Only realized ones for the list
            .Select(m => new FinanceTransactionDto
            {
                Description = m.MentorshipStatus == MentorshipStatus.Cancelled ? "Hoàn tiền cho Startup" : $"Hệ thống trả tiền tư vấn #{m.MentorshipID}",
                Amount = m.MentorshipStatus == MentorshipStatus.Cancelled ? m.SessionAmount : m.ActualAmount,
                Type = "OUT",
                Source = m.MentorshipStatus == MentorshipStatus.Cancelled ? m.StartupName : m.AdvisorName,
                Date = m.PayoutReleasedAt ?? m.PaidAt ?? DateTime.UtcNow
            });

        // Add Startup Income transactions
        var transIncome = filteredMentorships
            .Where(m => m.MentorshipStatus != MentorshipStatus.Cancelled)
            .Select(m => new FinanceTransactionDto
            {
                Description = $"Startup gửi tiền tư vấn #{m.MentorshipID}",
                Amount = m.SessionAmount,
                Type = "IN",
                Source = m.StartupName,
                Date = m.PaidAt ?? DateTime.UtcNow
            });

        var transSubscriptions = filteredSubscriptions
            .Select(s => new FinanceTransactionDto
            {
                Description = $"Startup mua gói {s.TargetPlan}",
                Amount = s.Amount,
                Type = "IN",
                Source = s.StartupName,
                Date = s.PaidAt ?? DateTime.UtcNow
            });

        var allTransactions = transIncome
            .Concat(transSubscriptions)
            .Concat(transMentorships)
            .OrderByDescending(t => t.Date)
            .ToList();

        var totalCount = allTransactions.Count;
        var pagedTransactions = allTransactions
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // 5. Calculate Pending Liabilities (Obligations)
        // Tiền nợ Mentor: Đã xong cuộc họp nhưng chưa bấm Release
        var pendingAdvisorPayouts = mentorships
            .Where(m => m.MentorshipStatus == MentorshipStatus.Completed && m.PayoutReleasedAt == null)
            .Sum(m => m.ActualAmount);

        // Tiền nợ Startup: Đã hủy nhưng chưa thực hiện hoàn tiền (Giả sử chưa refund nếu status vẫn là Completed payment)
        // Chú ý: Ở đây ta coi như các bản ghi Cancelled trong bảng mentorship mà Payment=Completed là đang "treo" tiền hoàn
        var pendingStartupRefunds = mentorships
            .Where(m => m.MentorshipStatus == MentorshipStatus.Cancelled && m.PaymentStatus == PaymentStatus.Completed)
            .Sum(m => m.SessionAmount);

        // Total realized payouts (money that actually left the system)
        var totalRealizedOut = payouts.Sum(p => p.Amount) + totalRefunds; 

        return ApiResponse<StaffFinanceStatsDto>.SuccessResponse(new StaffFinanceStatsDto
        {
            TotalRevenue = totalRevenue,
            TotalCommission = totalCommission,
            TotalPayouts = totalRealizedOut,
            PendingAdvisorPayouts = pendingAdvisorPayouts,
            PendingStartupRefunds = pendingStartupRefunds,
            CurrentSystemBalance = currentBalance,
            IncomeSources = incomeSources,
            ExpenseSources = expenseSources,
            RecentTransactions = pagedTransactions,
            TotalTransactions = totalCount,
            CheckedAt = DateTime.UtcNow
        });
    }
}
