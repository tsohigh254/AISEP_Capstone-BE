using AISEP.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace AISEP.Application.DTOs.IssueReport;

// ── Request DTOs ──────────────────────────────────────────────

public class CreateIssueReportRequest
{
    public IssueCategory IssueCategory { get; set; }
    public string Description { get; set; } = null!;
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityID { get; set; }
    /// <summary>Up to 5 files, max 10MB each.</summary>
    public List<IFormFile>? Attachments { get; set; }
}

/// <summary>
/// Danh sách giá trị hợp lệ cho RelatedEntityType khi tạo báo cáo.
/// </summary>
public static class IssueRelatedEntityType
{
    public const string Mentorship    = "Mentorship";
    public const string Session       = "Session";
    public const string Payment       = "Payment";
    public const string AdvisorReport = "AdvisorReport";
    public const string Connection    = "Connection";
    public const string User          = "User";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Mentorship, Session, Payment, AdvisorReport, Connection, User
    };
}

public class UpdateIssueReportStatusRequest
{
    public IssueReportStatus Status { get; set; }
    public string? StaffNote { get; set; }
}

// ── Response DTOs ─────────────────────────────────────────────

public class IssueReportSummaryDto
{
    public int IssueReportID { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityID { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class IssueReportDetailDto : IssueReportSummaryDto
{
    public int ReporterUserID { get; set; }
    public string? ReporterEmail { get; set; }
    public string? ReporterUserType { get; set; }
    public string? ReporterAvatarUrl { get; set; }
    public string? StaffNote { get; set; }
    public int? AssignedToStaffID { get; set; }
    public List<IssueAttachmentDto> Attachments { get; set; } = new();
}

public class IssueAttachmentDto
{
    public int AttachmentID { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public string? MimeType { get; set; }
}
