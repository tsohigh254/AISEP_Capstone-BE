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
            if (advisorReport != null && advisorReport.SubmittedAt.HasValue)
            {
                var deadline = advisorReport.SubmittedAt.Value.AddHours(24);
                if (now > deadline)
                    return ApiResponse<IssueReportSummaryDto>.ErrorResponse(
                        "ISSUE_REPORT_WINDOW_EXPIRED",
                        $"The 24-hour window to report this advisor report has expired (deadline: {deadline:O}).");
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

        // Notify reporter — use role-prefixed route matching FE layout
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
