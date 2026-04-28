using AISEP.Application.Const;
using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.IssueReport;
using AISEP.Application.DTOs.Notification;
using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Domain.Enums;
using AISEP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AISEP.Infrastructure.Services;

public class IssueReportService : IIssueReportService
{
    private readonly ApplicationDbContext _db;
    private readonly ICloudinaryService _cloudinary;
    private readonly INotificationDeliveryService _notifications;
    private readonly IAuditService _audit;
    private readonly ILogger<IssueReportService> _logger;

    private const int MaxAttachments = 5;

    public IssueReportService(
        ApplicationDbContext db,
        ICloudinaryService cloudinary,
        INotificationDeliveryService notifications,
        IAuditService audit,
        ILogger<IssueReportService> logger)
    {
        _db = db;
        _cloudinary = cloudinary;
        _notifications = notifications;
        _audit = audit;
        _logger = logger;
    }

    // ================================================================
    // CREATE — any authenticated user
    // ================================================================

    public async Task<ApiResponse<IssueReportSummaryDto>> CreateAsync(int userId, CreateIssueReportRequest request)
    {
        var now = DateTime.UtcNow;

        // Validate relatedEntityType
        if (request.RelatedEntityType != null &&
            !IssueRelatedEntityType.All.Contains(request.RelatedEntityType))
        {
            return ApiResponse<IssueReportSummaryDto>.ErrorResponse(
                "INVALID_RELATED_ENTITY_TYPE",
                $"Invalid relatedEntityType. Accepted values: {string.Join(", ", IssueRelatedEntityType.All)}");
        }

        // Enforce 24h window for AdvisorReport issue reports
        if (request.RelatedEntityType == IssueRelatedEntityType.AdvisorReport && request.RelatedEntityID.HasValue)
        {
            var advisorReport = await _db.MentorshipReports
                .FirstOrDefaultAsync(r => r.ReportID == request.RelatedEntityID.Value);
            if (advisorReport != null)
            {
                // Enforce 24h window
                if (advisorReport.SubmittedAt.HasValue)
                {
                    var deadline = advisorReport.SubmittedAt.Value.AddHours(24);
                    if (now > deadline)
                        return ApiResponse<IssueReportSummaryDto>.ErrorResponse(
                            "ISSUE_REPORT_WINDOW_EXPIRED",
                            $"The 24-hour window to report this advisor report has expired (deadline: {deadline:O}).");
                }

                // If acknowledged, cannot report issue
                if (advisorReport.StartupAcknowledgedAt.HasValue)
                {
                    return ApiResponse<IssueReportSummaryDto>.ErrorResponse(
                        "ALREADY_ACKNOWLEDGED",
                        "Cannot report an issue for an advisor report that you have already acknowledged.");
                }
            }
        }

        var report = new IssueReport
        {
            ReporterUserID = userId,
            Category = request.IssueCategory,
            Description = request.Description,
            RelatedEntityType = request.RelatedEntityType,
            RelatedEntityID = request.RelatedEntityID,
            Status = IssueReportStatus.New,
            CreatedAt = now
        };

        _db.IssueReports.Add(report);
        await _db.SaveChangesAsync(); // get ID first

        // Upload attachments
        if (request.Attachments != null && request.Attachments.Count > 0)
        {
            var files = request.Attachments.Take(MaxAttachments).ToList();
            foreach (var file in files)
            {
                try
                {
                    var meta = await _cloudinary.UploadDocumentWithMetadata(file, CloudinaryFolderSaving.IssueReport);
                    _db.IssueReportAttachments.Add(new IssueReportAttachment
                    {
                        IssueReportID = report.IssueReportID,
                        FileUrl = meta.Url,
                        FileName = file.FileName,
                        FileSize = file.Length,
                        MimeType = file.ContentType,
                        UploadedAt = now
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[IssueReport] Failed to upload attachment for IssueReport {Id}", report.IssueReportID);
                }
            }
            await _db.SaveChangesAsync();
        }

        // Notify all Staff users
        try
        {
            var staffUserIds = await _db.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.Role.RoleName == "Staff" || ur.Role.RoleName == "Admin")
                .Select(ur => ur.UserID)
                .Distinct()
                .ToListAsync();

            foreach (var staffId in staffUserIds)
            {
                await _notifications.CreateAndPushAsync(new CreateNotificationRequest
                {
                    UserId = staffId,
                    NotificationType = "SYSTEM",
                    Title = "Báo cáo sự cố mới",
                    Message = $"Có báo cáo sự cố mới (#{report.IssueReportID}) thuộc danh mục {report.Category}. Vui lòng xem xét.",
                    RelatedEntityType = "IssueReport",
                    RelatedEntityId = report.IssueReportID,
                    ActionUrl = $"/staff/issue-reports/{report.IssueReportID}"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[IssueReport] Failed to notify staff for IssueReport {Id}", report.IssueReportID);
        }

        await _audit.LogAsync("CREATE_ISSUE_REPORT", "IssueReport", report.IssueReportID, $"Category={report.Category}");

        return ApiResponse<IssueReportSummaryDto>.SuccessResponse(MapSummary(report),
            "Issue report submitted successfully.");
    }

    // ================================================================
    // GET MY REPORTS (Reporter)
    // ================================================================

    public async Task<ApiResponse<PagedResponse<IssueReportSummaryDto>>> GetMyReportsAsync(
        int userId, int page, int pageSize,
        IssueReportStatus? status,
        IssueCategory? category)
    {
        pageSize = Math.Min(pageSize, 100);

        var query = _db.IssueReports
            .Where(r => r.ReporterUserID == userId)
            .AsQueryable();

        if (status.HasValue) query = query.Where(r => r.Status == status.Value);
        if (category.HasValue) query = query.Where(r => r.Category == category.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return ApiResponse<PagedResponse<IssueReportSummaryDto>>.SuccessResponse(new PagedResponse<IssueReportSummaryDto>
        {
            Items = items.Select(MapSummary).ToList(),
            Paging = new PagingInfo { Page = page, PageSize = pageSize, TotalItems = total }
        });
    }

    // ================================================================
    // GET BY ID
    // ================================================================

    public async Task<ApiResponse<IssueReportDetailDto>> GetByIdAsync(int userId, string userType, int issueReportId)
    {
        var report = await _db.IssueReports
            .Include(r => r.Reporter).ThenInclude(u => u.Startup)
            .Include(r => r.Reporter).ThenInclude(u => u.Advisor)
            .Include(r => r.Reporter).ThenInclude(u => u.Investor)
            .Include(r => r.Attachments)
            .FirstOrDefaultAsync(r => r.IssueReportID == issueReportId);

        if (report == null)
            return ApiResponse<IssueReportDetailDto>.ErrorResponse("ISSUE_REPORT_NOT_FOUND", "Issue report not found.");

        var isStaffOrAdmin = userType is "Staff" or "Admin";
        if (!isStaffOrAdmin && report.ReporterUserID != userId)
            return ApiResponse<IssueReportDetailDto>.ErrorResponse("FORBIDDEN", "You do not have access to this report.");

        return ApiResponse<IssueReportDetailDto>.SuccessResponse(MapDetail(report));
    }

    // ================================================================
    // GET LIST (Staff/Admin)
    // ================================================================

    public async Task<ApiResponse<PagedResponse<IssueReportDetailDto>>> GetListAsync(
        int page, int pageSize,
        IssueReportStatus? status,
        IssueCategory? category,
        int? reporterUserId)
    {
        pageSize = Math.Min(pageSize, 100);

        var query = _db.IssueReports
            .Include(r => r.Reporter).ThenInclude(u => u.Startup)
            .Include(r => r.Reporter).ThenInclude(u => u.Advisor)
            .Include(r => r.Reporter).ThenInclude(u => u.Investor)
            .Include(r => r.Attachments)
            .AsQueryable();

        if (status.HasValue) query = query.Where(r => r.Status == status.Value);
        if (category.HasValue) query = query.Where(r => r.Category == category.Value);
        if (reporterUserId.HasValue) query = query.Where(r => r.ReporterUserID == reporterUserId.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return ApiResponse<PagedResponse<IssueReportDetailDto>>.SuccessResponse(new PagedResponse<IssueReportDetailDto>
        {
            Items = items.Select(MapDetail).ToList(),
            Paging = new PagingInfo { Page = page, PageSize = pageSize, TotalItems = total }
        });
    }

    // ================================================================
    // UPDATE STATUS (Staff/Admin)
    // ================================================================
    public async Task<ApiResponse<IssueReportDetailDto>> UpdateStatusAsync(int staffUserId, int issueReportId, UpdateIssueReportStatusRequest request)

    {
        var report = await _db.IssueReports
            .Include(r => r.Reporter)
            .Include(r => r.Attachments)
            .FirstOrDefaultAsync(r => r.IssueReportID == issueReportId);

        if (report == null)
            return ApiResponse<IssueReportDetailDto>.ErrorResponse("ISSUE_REPORT_NOT_FOUND", "Issue report not found.");

        var now = DateTime.UtcNow;
        report.Status = request.Status;
        report.StaffNote = request.StaffNote ?? report.StaffNote;
        report.AssignedToStaffID = staffUserId;
        report.UpdatedAt = now;

        await _db.SaveChangesAsync();

        // --- NEW: Sync with related entities (Mentorship/Session) ---
        if (request.Status == IssueReportStatus.Resolved || request.Status == IssueReportStatus.Dismissed)
        {
            try
            {
                if (report.RelatedEntityType == "Session" && report.RelatedEntityID.HasValue)
                {
                    var session = await _db.MentorshipSessions
                        .Include(s => s.Mentorship)
                        .FirstOrDefaultAsync(s => s.SessionID == report.RelatedEntityID.Value);
                        
                    if (session != null && session.SessionStatus == SessionStatusValues.InDispute)
                    {
                        session.SessionStatus = (request.Status == IssueReportStatus.Resolved) 
                            ? SessionStatusValues.Completed // Refund cases often mark session as "de-facto" completed or just closed
                            : SessionStatusValues.Completed; // Dismissed means we trust the advisor's completion
                        
                        if (session.Mentorship != null && session.Mentorship.MentorshipStatus == MentorshipStatus.InDispute)
                        {
                            session.Mentorship.MentorshipStatus = MentorshipStatus.Resolved;
                            session.Mentorship.UpdatedAt = now;
                        }
                        await _db.SaveChangesAsync();
                    }
                }
                else if ((report.RelatedEntityType == "Mentorship" || report.RelatedEntityType == "Payment") && report.RelatedEntityID.HasValue)
                {
                    var mentorship = await _db.StartupAdvisorMentorships
                        .FirstOrDefaultAsync(m => m.MentorshipID == report.RelatedEntityID.Value);
                        
                    if (mentorship != null && mentorship.MentorshipStatus == MentorshipStatus.InDispute)
                    {
                        mentorship.MentorshipStatus = MentorshipStatus.Resolved;
                        mentorship.UpdatedAt = now;
                        await _db.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync Mentorship/Session status for IssueReport {Id}", report.IssueReportID);
            }
        }
        // --- END Sync ---

        // Notify reporter
        try
        {
            var rolePrefix = (report.Reporter?.UserType?.ToLower()) switch
            {
                "startup"  => "startup",
                "advisor"  => "advisor",
                "investor" => "investor",
                _          => "startup"
            };
            await _notifications.CreateAndPushAsync(new CreateNotificationRequest
            {
                UserId = report.ReporterUserID,
                NotificationType = "SYSTEM",
                Title = "Báo cáo sự cố của bạn đã được cập nhật",
                Message = $"Báo cáo #{report.IssueReportID} của bạn đã chuyển sang trạng thái: {report.Status}.",
                RelatedEntityType = "IssueReport",
                RelatedEntityId = report.IssueReportID,
                ActionUrl = $"/{rolePrefix}/issue-reports/{report.IssueReportID}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[IssueReport] Failed to notify reporter for IssueReport {Id}", report.IssueReportID);
        }

        await _audit.LogAsync("UPDATE_ISSUE_REPORT_STATUS", "IssueReport", report.IssueReportID,
            $"Status={report.Status}, StaffId={staffUserId}");

        return ApiResponse<IssueReportDetailDto>.SuccessResponse(MapDetail(report), "Status updated.");
    }

    public async Task<ApiResponse<IssueReportDetailDto>> EscalateToDisputeAsync(int staffUserId, int issueReportId)
    {
        var report = await _db.IssueReports
            .Include(r => r.Reporter)
            .Include(r => r.Attachments)
            .FirstOrDefaultAsync(r => r.IssueReportID == issueReportId);

        if (report == null)
            return ApiResponse<IssueReportDetailDto>.ErrorResponse("ISSUE_REPORT_NOT_FOUND", "Issue report not found.");

        if (report.Status == IssueReportStatus.Resolved || report.Status == IssueReportStatus.Dismissed)
            return ApiResponse<IssueReportDetailDto>.ErrorResponse("INVALID_STATUS", "Resolved or Dismissed reports cannot be escalated.");

        // Check if it's related to a Session, Mentorship, or AdvisorReport
        if (report.RelatedEntityType != IssueRelatedEntityType.Session && 
            report.RelatedEntityType != IssueRelatedEntityType.Mentorship &&
            report.RelatedEntityType != IssueRelatedEntityType.AdvisorReport)
        {
             return ApiResponse<IssueReportDetailDto>.ErrorResponse("INVALID_RELATED_ENTITY", 
                 "Only session, mentorship, or advisor report issues can be escalated to dispute.");
        }

        if (!report.RelatedEntityID.HasValue)
             return ApiResponse<IssueReportDetailDto>.ErrorResponse("MISSING_ENTITY_ID", "Related entity ID is missing.");

        // 1. Mark session/mentorship as InDispute
        int? startupUserId = null;
        int? advisorUserId = null;
        int? mentorshipId = null;
        string entityLabel = "";

        if (report.RelatedEntityType == IssueRelatedEntityType.Session)
        {
            var session = await _db.MentorshipSessions
                .Include(s => s.Mentorship).ThenInclude(m => m.Startup)
                .Include(s => s.Mentorship).ThenInclude(m => m.Advisor)
                .FirstOrDefaultAsync(s => s.SessionID == report.RelatedEntityID.Value);

            if (session == null)
                 return ApiResponse<IssueReportDetailDto>.ErrorResponse("SESSION_NOT_FOUND", "Related session not found.");

            session.SessionStatus = SessionStatusValues.InDispute;
            session.DisputeReason = report.Description;
            session.MarkedByStaffID = staffUserId;
            session.MarkedAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;

            // Update mentorship status to InDispute if it wasn't already
            if (session.Mentorship != null && session.Mentorship.MentorshipStatus != MentorshipStatus.InDispute)
            {
                session.Mentorship.MentorshipStatus = MentorshipStatus.InDispute;
                session.Mentorship.UpdatedAt = DateTime.UtcNow;
            }

            startupUserId = session.Mentorship?.Startup?.UserID;
            advisorUserId = session.Mentorship?.Advisor?.UserID;
            mentorshipId = session.MentorshipID;
            entityLabel = $"phiên tư vấn #{session.SessionID}";
        }
        else if (report.RelatedEntityType == IssueRelatedEntityType.AdvisorReport)
        {
            var reportEntity = await _db.MentorshipReports
                .Include(r => r.Mentorship).ThenInclude(m => m.Startup)
                .Include(r => r.Mentorship).ThenInclude(m => m.Advisor)
                .Include(r => r.Session)
                .FirstOrDefaultAsync(r => r.ReportID == report.RelatedEntityID.Value);

            if (reportEntity == null)
                 return ApiResponse<IssueReportDetailDto>.ErrorResponse("REPORT_NOT_FOUND", "Related advisor report not found.");

            if (reportEntity.Session != null)
            {
                reportEntity.Session.SessionStatus = SessionStatusValues.InDispute;
                reportEntity.Session.DisputeReason = report.Description;
                reportEntity.Session.MarkedByStaffID = staffUserId;
                reportEntity.Session.MarkedAt = DateTime.UtcNow;
                reportEntity.Session.UpdatedAt = DateTime.UtcNow;
                entityLabel = $"phiên tư vấn #{reportEntity.Session.SessionID} (thông qua báo cáo #{reportEntity.ReportID})";
            }
            else
            {
                entityLabel = $"báo cáo tư vấn #{reportEntity.ReportID}";
            }

            if (reportEntity.Mentorship.MentorshipStatus != MentorshipStatus.InDispute)
            {
                reportEntity.Mentorship.MentorshipStatus = MentorshipStatus.InDispute;
                reportEntity.Mentorship.UpdatedAt = DateTime.UtcNow;
            }

            startupUserId = reportEntity.Mentorship.Startup?.UserID;
            advisorUserId = reportEntity.Mentorship.Advisor?.UserID;
            mentorshipId = reportEntity.MentorshipID;
        }
        else // Mentorship
        {
            var mentorship = await _db.StartupAdvisorMentorships
                .Include(m => m.Startup)
                .Include(m => m.Advisor)
                .FirstOrDefaultAsync(m => m.MentorshipID == report.RelatedEntityID.Value);

            if (mentorship == null)
                 return ApiResponse<IssueReportDetailDto>.ErrorResponse("MENTORSHIP_NOT_FOUND", "Related mentorship not found.");

            mentorship.MentorshipStatus = MentorshipStatus.InDispute;
            mentorship.UpdatedAt = DateTime.UtcNow;

            startupUserId = mentorship.Startup?.UserID;
            advisorUserId = mentorship.Advisor?.UserID;
            mentorshipId = mentorship.MentorshipID;
            entityLabel = $"yêu cầu tư vấn #{mentorship.MentorshipID}";
        }

        // 2. Update IssueReport status to Escalated
        report.Status = IssueReportStatus.Escalated;
        report.StaffNote = (report.StaffNote ?? "") + $"\n[SYSTEM] {DateTime.UtcNow:O}: Escalated to formal dispute by staff.";
        report.AssignedToStaffID = staffUserId;
        report.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // 3. Send Notifications
        if (startupUserId.HasValue)
        {
            await _notifications.CreateAndPushAsync(new CreateNotificationRequest
            {
                UserId = startupUserId.Value,
                NotificationType = "CONSULTING",
                Title = "Tranh chấp đã được mở",
                Message = $"Báo cáo sự cố liên quan đến {entityLabel} đã được chuyển thành tranh chấp chính thức và đang được xem xét.",
                RelatedEntityType = "IssueReport",
                RelatedEntityId = report.IssueReportID,
                ActionUrl = $"/startup/mentorship-requests/{mentorshipId}"
            });
        }

        if (advisorUserId.HasValue)
        {
            await _notifications.CreateAndPushAsync(new CreateNotificationRequest
            {
                UserId = advisorUserId.Value,
                NotificationType = "CONSULTING",
                Title = "Tranh chấp đã được mở",
                Message = $"Có tranh chấp chính thức liên quan đến {entityLabel} và đang được nhân viên hệ thống xem xét.",
                RelatedEntityType = "IssueReport",
                RelatedEntityId = report.IssueReportID,
                ActionUrl = $"/advisor/requests/{mentorshipId}"
            });
        }

        await _audit.LogAsync("ESCALATE_TO_DISPUTE", "IssueReport", issueReportId, $"StaffId={staffUserId}");

        return ApiResponse<IssueReportDetailDto>.SuccessResponse(MapDetail(report), "Issue escalated to formal dispute.");
    }

    // ================================================================
    // Mappers
    // ================================================================

    private static IssueReportSummaryDto MapSummary(IssueReport r) => new()
    {
        IssueReportID = r.IssueReportID,
        Category = r.Category.ToString(),
        Status = r.Status.ToString(),
        Description = r.Description,
        RelatedEntityType = r.RelatedEntityType,
        RelatedEntityID = r.RelatedEntityID,
        SubmittedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt
    };

    private static IssueReportDetailDto MapDetail(IssueReport r) => new()
    {
        IssueReportID = r.IssueReportID,
        Category = r.Category.ToString(),
        Status = r.Status.ToString(),
        Description = r.Description,
        RelatedEntityType = r.RelatedEntityType,
        RelatedEntityID = r.RelatedEntityID,
        SubmittedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
        ReporterUserID = r.ReporterUserID,
        ReporterEmail = r.Reporter?.Email,
        ReporterUserType = r.Reporter?.UserType,
        ReporterAvatarUrl = r.Reporter?.UserType switch
        {
            "Startup"  => r.Reporter.Startup?.LogoURL,
            "Advisor"  => r.Reporter.Advisor?.ProfilePhotoURL,
            "Investor" => r.Reporter.Investor?.ProfilePhotoURL,
            _          => null
        },
        StaffNote = r.StaffNote,
        AssignedToStaffID = r.AssignedToStaffID,
        Attachments = r.Attachments.Select(a => new IssueAttachmentDto
        {
            AttachmentID = a.AttachmentID,
            FileUrl = a.FileUrl,
            FileName = a.FileName,
            FileSize = a.FileSize,
            MimeType = a.MimeType
        }).ToList()
    };
}
