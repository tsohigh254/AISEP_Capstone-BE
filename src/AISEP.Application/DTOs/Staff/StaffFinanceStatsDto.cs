using System;
using System.Collections.Generic;

namespace AISEP.Application.DTOs.Staff;

public class StaffFinanceStatsDto
{
    public decimal TotalRevenue { get; set; }           // Tổng tiền hệ thống nhận được (Mentorship + Subscriptions)
    public decimal TotalCommission { get; set; }        // Tổng hoa hồng (PlatformFeeAmount)
    public decimal TotalPayouts { get; set; }           // Tổng tiền đã thực chi (đã giải ngân + đã hoàn tiền)
    public decimal PendingAdvisorPayouts { get; set; }  // Nợ Advisor (Sessions Completed nhưng chưa release tiền)
    public decimal PendingStartupRefunds { get; set; }   // Nợ Startup (Sessions Cancelled nhưng chưa hoàn tiền)
    public decimal CurrentSystemBalance { get; set; }    // Tiền mặt thực tế đang có

    public List<FinanceSourceDto> IncomeSources { get; set; } = new();
    public List<FinanceSourceDto> ExpenseSources { get; set; } = new();
    public List<FinanceTransactionDto> RecentTransactions { get; set; } = new();
    
    public int TotalTransactions { get; set; }
    public DateTime CheckedAt { get; set; }
}

public class FinanceTransactionDto
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty; // "IN" or "OUT"
    public string Source { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

public class FinanceSourceDto
{
    public string SourceName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public double Percentage { get; set; }
}

