using AISEP.Domain.Enums;

namespace AISEP.Domain.Entities;

public class MentorshipReport
{
    public int ReportID { get; set; }
    public int MentorshipID { get; set; }
    public int? SessionID { get; set; }
    public int? CreatedByAdvisorID { get; set; }
    public string? ReportSummary { get; set; }
    public string? DetailedFindings { get; set; }
    public string? Recommendations { get; set; }
    public string? AttachmentsURL { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public bool IsMandatory { get; set; }
    public ReportReviewStatus ReportReviewStatus { get; set; } = ReportReviewStatus.PendingReview;
    public int? ReviewedByStaffID { get; set; }
    public string? StaffReviewNote { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int? SupersededByReportID { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public StartupAdvisorMentorship Mentorship { get; set; } = null!;
    public MentorshipSession? Session { get; set; }
}
