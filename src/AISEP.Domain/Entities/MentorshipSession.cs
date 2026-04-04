using AISEP.Domain.Enums;

namespace AISEP.Domain.Entities;

public class MentorshipSession
{
    public int SessionID { get; set; }
    public int MentorshipID { get; set; }
    public DateTime ScheduledStartAt { get; set; }
    public int? DurationMinutes { get; set; }
    public string MeetingURL { get; set; }
    public SessionStatus SessionStatus { get; set; } = SessionStatus.Pending;
    public string? TopicsDiscussed { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public StartupAdvisorMentorship Mentorship { get; set; } = null!;
    public MentorshipFeedback MentorshipFeedback { get; set; } = null!;
    public ICollection<MentorshipReport> Reports { get; set; } = new List<MentorshipReport>();
}
