using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Staff;
using AISEP.Application.Interfaces;
using AISEP.Domain.Enums;
using AISEP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AISEP.Infrastructure.Services;

public class StaffDashboardService : IStaffDashboardService
{
    private readonly ApplicationDbContext _db;
    private readonly PythonAiClient _pythonClient;
    private readonly ILogger<StaffDashboardService> _logger;

    public StaffDashboardService(
        ApplicationDbContext db,
        PythonAiClient pythonClient,
        ILogger<StaffDashboardService> logger)
    {
        _db = db;
        _pythonClient = pythonClient;
        _logger = logger;
    }

    public async Task<ApiResponse<StaffDashboardStatsDto>> GetDashboardStatsAsync()
    {
        var totalUsers = await _db.Users.CountAsync();
        var lockedAccounts = await _db.Users.CountAsync(u => !u.IsActive);

        var pendingStartupKyc = await _db.StartupKycSubmissions.CountAsync(s =>
            s.IsActive && s.WorkflowStatus == StartupKycWorkflowStatus.UnderReview);
        var pendingInvestorKyc = await _db.InvestorKycSubmissions.CountAsync(s =>
            s.IsActive && s.WorkflowStatus == InvestorKycWorkflowStatus.UnderReview);
        var escalatedComplaints = await _db.IssueReports.CountAsync(r =>
            r.Status == IssueReportStatus.Escalated);

        var aiOnline = false;
        try { aiOnline = await _pythonClient.IsHealthyAsync(); }
        catch (Exception ex) { _logger.LogWarning(ex, "AI health check failed in staff dashboard"); }

        return ApiResponse<StaffDashboardStatsDto>.SuccessResponse(new StaffDashboardStatsDto
        {
            TotalUsers = totalUsers,
            LockedAccounts = lockedAccounts,
            PendingKycCount = pendingStartupKyc + pendingInvestorKyc,
            EscalatedComplaintsCount = escalatedComplaints,
            AiServiceOnline = aiOnline,
            CheckedAt = DateTime.UtcNow
        });
    }

    public async Task<ApiResponse<KycTrendDto>> GetKycTrendAsync(string period)
    {
        var days = period?.ToUpperInvariant() == "30D" ? 30 : 7;
        var from = DateTime.UtcNow.Date.AddDays(-days + 1);

        // Startup KYC events
        var startupSubs = await _db.StartupKycSubmissions
            .Where(s => s.UpdatedAt >= from)
            .Select(s => new { s.UpdatedAt, s.WorkflowStatus })
            .ToListAsync();

        // Investor KYC events
        var investorSubs = await _db.InvestorKycSubmissions
            .Where(s => s.UpdatedAt >= from)
            .Select(s => new { s.UpdatedAt, s.WorkflowStatus })
            .ToListAsync();

        var points = Enumerable.Range(0, days).Select(i =>
        {
            var date = from.AddDays(i);

            var startupOnDay = startupSubs.Where(s => s.UpdatedAt.Date == date).ToList();
            var investorOnDay = investorSubs.Where(s => s.UpdatedAt.Date == date).ToList();

            return new KycTrendPointDto
            {
                Date = date.ToString("yyyy-MM-dd"),
                Submitted = startupOnDay.Count(s => s.WorkflowStatus == StartupKycWorkflowStatus.UnderReview)
                          + investorOnDay.Count(s => s.WorkflowStatus == InvestorKycWorkflowStatus.UnderReview),
                Approved  = startupOnDay.Count(s => s.WorkflowStatus == StartupKycWorkflowStatus.Approved)
                          + investorOnDay.Count(s => s.WorkflowStatus == InvestorKycWorkflowStatus.Approved),
                Rejected  = startupOnDay.Count(s => s.WorkflowStatus == StartupKycWorkflowStatus.Rejected)
                          + investorOnDay.Count(s => s.WorkflowStatus == InvestorKycWorkflowStatus.Rejected),
            };
        }).ToList();

        return ApiResponse<KycTrendDto>.SuccessResponse(new KycTrendDto
        {
            Period = period?.ToUpperInvariant() ?? "7D",
            Points = points
        });
    }

    public async Task<ApiResponse<List<ActivityFeedItemDto>>> GetActivityFeedAsync(int limit)
    {
        var safeLimit = Math.Clamp(limit, 1, 50);

        var logs = await _db.AuditLogs
            .AsNoTracking()
            .OrderByDescending(l => l.CreatedAt)
            .Take(safeLimit)
            .Select(l => new ActivityFeedItemDto
            {
                LogId         = l.LogID,
                ActionType    = l.ActionType,
                EntityType    = l.EntityType,
                EntityId      = l.EntityID,
                ActionDetails = l.ActionDetails,
                UserId        = l.UserID,
                UserEmail     = l.User != null ? l.User.Email : null,
                CreatedAt     = l.CreatedAt
            })
            .ToListAsync();

        return ApiResponse<List<ActivityFeedItemDto>>.SuccessResponse(logs);
    }
}
