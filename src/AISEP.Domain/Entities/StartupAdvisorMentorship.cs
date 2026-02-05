namespace AISEP.Domain.Entities;

public class StartupAdvisorMentorship
{
    public int MentorshipID { get; set; }
    public int StartupID { get; set; }
    public int AdvisorID { get; set; }
    public string MentorshipStatus { get; set; } = "Requested"; // Requested, Rejected, Accepted, InProgress, Completed, InDispute, Resolved
    public string? ChallengeDescription { get; set; }
    public string? SpecificQuestions { get; set; }
    public string? ExpectedScope { get; set; }
    public string? ExpectedDuration { get; set; }
    public string? PreferredFormat { get; set; }
    public DateTime? RequestedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectedReason { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? LastUpdatedByRole { get; set; }
    public string? ObligationSummary { get; set; }
    public bool CompletionConfirmedByStartup { get; set; }
    public bool CompletionConfirmedByAdvisor { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Startup Startup { get; set; } = null!;
    public Advisor Advisor { get; set; } = null!;
    public ICollection<MentorshipSession> Sessions { get; set; } = new List<MentorshipSession>();
    public ICollection<MentorshipReport> Reports { get; set; } = new List<MentorshipReport>();
    public ICollection<MentorshipFeedback> Feedbacks { get; set; } = new List<MentorshipFeedback>();
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    public ICollection<AdvisorTestimonial> Testimonials { get; set; } = new List<AdvisorTestimonial>();
}
