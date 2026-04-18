using AISEP.Application.Const;
using AISEP.Application.DTOs.Notification;
using AISEP.Application.Interfaces;
using AISEP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AISEP.Infrastructure.Jobs;

/// <summary>
/// Recurring job — chạy mỗi 15 phút.
///
/// Phase 1 — REMINDER: Session đã kết thúc nhưng chưa đủ 24h, Startup chưa xác nhận
///   → Gửi notification ngay sau khi session kết thúc, sau đó lặp lại mỗi 4 tiếng nếu chưa xác nhận.
///   → Chỉ gửi nếu chưa có reminder nào trong 4 tiếng gần nhất (kiểm tra Notifications table).
///
/// Phase 2 — AUTO-CONFIRM: Session đã kết thúc hơn 24h, Startup vẫn chưa xác nhận
///   → Tự động set StartupConfirmedConductedAt, chuyển SessionStatus = Conducted.
///   → Gửi notification thông báo đã tự động xác nhận.
/// </summary>
public class SessionAutoConfirmJob
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<SessionAutoConfirmJob> _logger;
    private readonly INotificationDeliveryService _notifications;

    private const int AutoConfirmAfterHours = 24;
    private const int ReminderIntervalHours = 4;
    private const string ReminderNotificationType = "CONFIRM_CONDUCTED_REMINDER";

    public SessionAutoConfirmJob(
        ApplicationDbContext db,
        ILogger<SessionAutoConfirmJob> logger,
        INotificationDeliveryService notifications)
    {
        _db = db;
        _logger = logger;
        _notifications = notifications;
    }

    public async Task RunAsync()
    {
        var now = DateTime.UtcNow;
        var autoConfirmCutoff = now.AddHours(-AutoConfirmAfterHours);

        // Lấy tất cả session Scheduled hoặc InProgress đã kết thúc (qua giờ + duration), Startup chưa xác nhận
        var candidates = await _db.MentorshipSessions
            .Include(s => s.Mentorship).ThenInclude(m => m.Startup)
            .Where(s =>
                (s.SessionStatus == SessionStatusValues.Scheduled
                    || s.SessionStatus == SessionStatusValues.InProgress)
                && s.StartupConfirmedConductedAt == null
                && s.ScheduledStartAt != null)
            .ToListAsync();

        // Lọc: chỉ những session đã kết thúc (giờ kết thúc <= now)
        var finished = candidates
            .Where(s => s.ScheduledStartAt!.Value.AddMinutes(s.DurationMinutes ?? 60) <= now)
            .ToList();

        if (!finished.Any())
        {
            _logger.LogInformation("[SessionAutoConfirm] No finished sessions pending confirmation.");
            return;
        }

        // Lấy danh sách SessionID đã gửi reminder trong ReminderIntervalHours gần nhất
        var finishedIds = finished.Select(s => s.SessionID).ToList();
        var recentReminderCutoff = now.AddHours(-ReminderIntervalHours);
        var recentlyRemindedIds = await _db.Notifications
            .Where(n => n.NotificationType == ReminderNotificationType
                && n.RelatedEntityType == "MentorshipSession"
                && n.RelatedEntityID != null
                && finishedIds.Contains(n.RelatedEntityID.Value)
                && n.CreatedAt >= recentReminderCutoff)
            .Select(n => n.RelatedEntityID!.Value)
            .Distinct()
            .ToListAsync();

        int remindedCount = 0, autoConfirmedCount = 0;

        foreach (var session in finished)
        {
            var endTime = session.ScheduledStartAt!.Value.AddMinutes(session.DurationMinutes ?? 60);
            var startup = session.Mentorship?.Startup;
            if (startup == null) continue;

            if (endTime <= autoConfirmCutoff)
            {
                // ── PHASE 2: AUTO-CONFIRM ──
                session.StartupConfirmedConductedAt = now;
                session.SessionStatus = SessionStatusValues.Conducted;
                session.UpdatedAt = now;
                autoConfirmedCount++;

                _logger.LogInformation(
                    "[SessionAutoConfirm] Session {SessionId} (Mentorship {MentorshipId}) auto-confirmed.",
                    session.SessionID, session.MentorshipID);

                // Nếu đã có Passed report → auto-complete session luôn
                var hasPassedReport = await _db.MentorshipReports
                    .AnyAsync(r => r.SessionID == session.SessionID
                               && r.MentorshipID == session.MentorshipID
                               && r.ReportReviewStatus == AISEP.Domain.Enums.ReportReviewStatus.Passed
                               && r.SupersededByReportID == null);
                if (hasPassedReport)
                {
                    session.SessionStatus = SessionStatusValues.Completed;
                    _logger.LogInformation(
                        "[SessionAutoConfirm] Session {SessionId} auto-completed (Passed report exists).",
                        session.SessionID);
                }

                try
                {
                    await _notifications.CreateAndPushAsync(new CreateNotificationRequest
                    {
                        UserId = startup.UserID,
                        NotificationType = "CONSULTING",
                        Title = "Buổi tư vấn đã được xác nhận tự động",
                        Message = "Bạn chưa xác nhận buổi tư vấn trong 24h. Hệ thống đã tự động xác nhận. Nếu có vấn đề, vui lòng liên hệ bộ phận hỗ trợ.",
                        RelatedEntityType = "MentorshipSession",
                        RelatedEntityId = session.SessionID,
                        ActionUrl = $"/startup/mentorship-requests/{session.MentorshipID}"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "[SessionAutoConfirm] Failed to send auto-confirm notification for session {SessionId}.",
                        session.SessionID);
                }
            }
            else if (!recentlyRemindedIds.Contains(session.SessionID))
            {
                // ── PHASE 1: REMINDER (lặp lại mỗi 4 tiếng) ──
                remindedCount++;

                _logger.LogInformation(
                    "[SessionAutoConfirm] Sending reminder for session {SessionId} (Mentorship {MentorshipId}).",
                    session.SessionID, session.MentorshipID);

                try
                {
                    await _notifications.CreateAndPushAsync(new CreateNotificationRequest
                    {
                        UserId = startup.UserID,
                        NotificationType = ReminderNotificationType,
                        Title = "Vui lòng xác nhận buổi tư vấn",
                        Message = "Buổi tư vấn của bạn đã kết thúc. Vui lòng xác nhận để tiếp tục quy trình. Nếu không xác nhận trong 24h, hệ thống sẽ tự động xác nhận.",
                        RelatedEntityType = "MentorshipSession",
                        RelatedEntityId = session.SessionID,
                        ActionUrl = $"/startup/mentorship-requests/{session.MentorshipID}"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "[SessionAutoConfirm] Failed to send reminder for session {SessionId}.",
                        session.SessionID);
                }
            }
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation(
            "[SessionAutoConfirm] Done. Reminders sent: {Reminded}, Auto-confirmed: {AutoConfirmed}.",
            remindedCount, autoConfirmedCount);

        // Recalculate payout eligibility cho các mentorship có session bị auto-confirmed/completed
        var autoConfirmedMentorships = finished
            .Where(s => s.StartupConfirmedConductedAt != null)
            .Select(s => s.MentorshipID)
            .Distinct();
        foreach (var mentorshipId in autoConfirmedMentorships)
        {
            var m = await _db.StartupAdvisorMentorships
                .Include(x => x.Sessions)
                .Include(x => x.Reports)
                .FirstOrDefaultAsync(x => x.MentorshipID == mentorshipId);
            if (m != null)
            {
                RecalculateMentorshipStatus(m);
                RecalculatePayoutEligibility(m);
            }
        }
        await _db.SaveChangesAsync();

        await RunAutoAcknowledgeReportsAsync(now);
    }

    // ----------------------------------------------------------------
    // AUTO-ACKNOWLEDGE REPORTS: Passed reports không được Startup xác nhận trong 24h
    // ----------------------------------------------------------------
    private async Task RunAutoAcknowledgeReportsAsync(DateTime now)
    {
        var cutoff = now.AddHours(-AutoConfirmAfterHours);

        var unacknowledged = await _db.MentorshipReports
            .Include(r => r.Mentorship)
                .ThenInclude(m => m.Sessions)
            .Include(r => r.Mentorship)
                .ThenInclude(m => m.Reports)
            .Include(r => r.Mentorship)
                .ThenInclude(m => m.Startup)
            .Where(r =>
                r.ReportReviewStatus == AISEP.Domain.Enums.ReportReviewStatus.Passed
                && r.SupersededByReportID == null
                && r.StartupAcknowledgedAt == null
                && r.SubmittedAt != null
                && r.SubmittedAt <= cutoff)
            .ToListAsync();

        if (!unacknowledged.Any())
        {
            _logger.LogInformation("[ReportAutoAcknowledge] No reports to auto-acknowledge.");
            return;
        }

        int count = 0;
        var affectedMentorships = new HashSet<int>();

        foreach (var report in unacknowledged)
        {
            report.StartupAcknowledgedAt = now;
            report.UpdatedAt = now;
            affectedMentorships.Add(report.MentorshipID);
            count++;

            try
            {
                var startupUserId = report.Mentorship?.Startup?.UserID;
                if (startupUserId != null)
                {
                    await _notifications.CreateAndPushAsync(new CreateNotificationRequest
                    {
                        UserId = startupUserId.Value,
                        NotificationType = "REPORT_AUTO_ACKNOWLEDGED",
                        Title = "Báo cáo tư vấn đã được tự động xác nhận",
                        Message = $"Báo cáo #{report.ReportID} đã quá 24 giờ kể từ khi được phê duyệt mà chưa được Startup xác nhận. Hệ thống đã tự động xác nhận.",
                        RelatedEntityType = "MentorshipReport",
                        RelatedEntityId = report.ReportID,
                        ActionUrl = $"/startup/mentorship-requests/{report.MentorshipID}"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[ReportAutoAcknowledge] Failed to send notification for report {ReportId}.", report.ReportID);
            }
        }

        await _db.SaveChangesAsync();

        // Recalculate payout eligibility for affected mentorships
        foreach (var mentorshipId in affectedMentorships)
        {
            var m = await _db.StartupAdvisorMentorships
                .Include(x => x.Sessions)
                .Include(x => x.Reports)
                .FirstOrDefaultAsync(x => x.MentorshipID == mentorshipId);
            if (m != null)
            {
                RecalculateMentorshipStatus(m);
                RecalculatePayoutEligibility(m);
            }
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "[ReportAutoAcknowledge] Auto-acknowledged {Count} reports across {Mentorships} mentorships.",
            count, affectedMentorships.Count);
    }

    // ----------------------------------------------------------------
    // Recalculate helpers (mirrors MentorshipService logic)
    // ----------------------------------------------------------------
    private static void RecalculateMentorshipStatus(AISEP.Domain.Entities.StartupAdvisorMentorship mentorship)
    {
        var active = mentorship.Sessions
            .Where(s => s.SessionStatus != SessionStatusValues.Cancelled
                     && s.SessionStatus != SessionStatusValues.ProposedByStartup
                     && s.SessionStatus != SessionStatusValues.ProposedByAdvisor)
            .ToList();

        if (!active.Any() || active.All(s => s.SessionStatus == SessionStatusValues.Completed))
        {
            var allPassedReports = mentorship.Reports
                .Where(r => r.SupersededByReportID == null)
                .All(r => r.ReportReviewStatus == AISEP.Domain.Enums.ReportReviewStatus.Passed);
            if (allPassedReports)
                mentorship.MentorshipStatus = AISEP.Domain.Enums.MentorshipStatus.Completed;
        }
        else if (active.Any(s => s.SessionStatus == SessionStatusValues.InDispute))
        {
            mentorship.MentorshipStatus = AISEP.Domain.Enums.MentorshipStatus.InDispute;
        }
    }

    private static void RecalculatePayoutEligibility(AISEP.Domain.Entities.StartupAdvisorMentorship mentorship)
    {
        var activeSessions = mentorship.Sessions
            .Where(s => s.SessionStatus != SessionStatusValues.Cancelled
                     && s.SessionStatus != SessionStatusValues.ProposedByStartup
                     && s.SessionStatus != SessionStatusValues.ProposedByAdvisor)
            .ToList();
        var allSessionsCompleted = activeSessions.Any()
            && activeSessions.All(s => s.SessionStatus == SessionStatusValues.Completed);
        var allStartupConfirmed = activeSessions.All(s => s.StartupConfirmedConductedAt != null);
        var currentReports = mentorship.Reports.Where(r => r.SupersededByReportID == null);
        var allReportsPassed = currentReports.Any()
            && currentReports.All(r => r.ReportReviewStatus == AISEP.Domain.Enums.ReportReviewStatus.Passed);
        var allReportsAcknowledged = currentReports.Any()
            && currentReports
                .Where(r => r.ReportReviewStatus == AISEP.Domain.Enums.ReportReviewStatus.Passed)
                .All(r => r.StartupAcknowledgedAt != null);
        var noDispute = !activeSessions.Any(s => s.SessionStatus == SessionStatusValues.InDispute);

        mentorship.IsPayoutEligible =
            allSessionsCompleted && allStartupConfirmed && allReportsPassed && allReportsAcknowledged && noDispute;
    }
}
