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
    public bool ReviewedByStaff { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public StartupAdvisorMentorship Mentorship { get; set; } = null!;
    public MentorshipSession? Session { get; set; }
}
