namespace AISEP.Domain.Entities;

public class MentorshipSession
{
    public int SessionID { get; set; }
    public int MentorshipID { get; set; }
    public DateTime? ScheduledStartAt { get; set; }
    public int? DurationMinutes { get; set; }
    public string? SessionFormat { get; set; }
    public string? MeetingURL { get; set; }
    public string? SessionStatus { get; set; }
    public DateTime? AdvisorConfirmedConductedAt { get; set; }
    public DateTime? StartupConfirmedConductedAt { get; set; }
    public DateTime? ConductedConfirmedAt { get; set; }
    public string? TopicsDiscussed { get; set; }
    public string? KeyInsights { get; set; }
    public string? ActionItems { get; set; }
    public string? NextSteps { get; set; }
    public string? Note { get; set; }
    public string? RecommendedResources { get; set; }
    public string? AdvisorInternalNotes { get; set; }
    public string? StartupNotes { get; set; }
    public string? DisputeReason { get; set; }
    public string? ResolutionNote { get; set; }
    public int? MarkedByStaffID { get; set; }
    public DateTime? MarkedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public StartupAdvisorMentorship Mentorship { get; set; } = null!;
    public ICollection<MentorshipReport> Reports { get; set; } = new List<MentorshipReport>();
    public ICollection<MentorshipFeedback> Feedbacks { get; set; } = new List<MentorshipFeedback>();
}
