using AISEP.Domain.Enums;

namespace AISEP.Domain.Entities;

public class IssueReport
{
    public int IssueReportID { get; set; }
    public int ReporterUserID { get; set; }
    public IssueCategory Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityID { get; set; }
    public IssueReportStatus Status { get; set; } = IssueReportStatus.New;
    public string? StaffNote { get; set; }
    public int? AssignedToStaffID { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public User Reporter { get; set; } = null!;
    public User? AssignedStaff { get; set; }
    public ICollection<IssueReportAttachment> Attachments { get; set; } = new List<IssueReportAttachment>();
}

public class IssueReportAttachment
{
    public int AttachmentID { get; set; }
    public int IssueReportID { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public long? FileSize { get; set; }
    public string? MimeType { get; set; }
    public DateTime UploadedAt { get; set; }

    // Navigation
    public IssueReport IssueReport { get; set; } = null!;
}
