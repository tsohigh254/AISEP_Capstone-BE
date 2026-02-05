namespace AISEP.Domain.Entities;

public class MentorshipFeedback
{
    public int FeedbackID { get; set; }
    public int MentorshipID { get; set; }
    public int? SessionID { get; set; }
    public string FromRole { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public bool IsPublic { get; set; }

    // Navigation properties
    public StartupAdvisorMentorship Mentorship { get; set; } = null!;
    public MentorshipSession? Session { get; set; }
}
