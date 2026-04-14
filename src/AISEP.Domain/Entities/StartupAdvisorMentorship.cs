using AISEP.Domain.Enums;

namespace AISEP.Domain.Entities;

public class StartupAdvisorMentorship
{
    public int MentorshipID { get; set; }
    public int StartupID { get; set; }
    public int AdvisorID { get; set; }
    public MentorshipStatus MentorshipStatus { get; set; } = MentorshipStatus.Requested;
    public string? ChallengeDescription { get; set; }
    public string? SpecificQuestions { get; set; }
    public string? ExpectedScope { get; set; }
    public string? ExpectedDuration { get; set; }
    public string? PreferredFormat { get; set; }
    public DateTime? RequestedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? InProgressAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectedReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelledBy { get; set; }    // "Startup" | "Advisor" | "System"
    public string? CancellationReason { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? LastUpdatedByRole { get; set; }
    public string? ObligationSummary { get; set; }
    public bool CompletionConfirmedByStartup { get; set; }
    public bool CompletionConfirmedByAdvisor { get; set; }

    // ===== PAYMENT FIELDS =====
    public decimal SessionAmount { get; set; }           // Giá gốc của mentorship
    public decimal PlatformFeeAmount { get; set; }       // 15% phí nền tảng
    public decimal ActualAmount { get; set; }            // Số tiền advisor nh?n
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public DateTime? PaidAt { get; set; }                
    public int? TransactionCode { get; set; }          

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
