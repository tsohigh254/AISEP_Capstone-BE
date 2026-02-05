namespace AISEP.Domain.Entities;

public class AdvisorTestimonial
{
    public int TestimonialID { get; set; }
    public int AdvisorID { get; set; }
    public int? StartupID { get; set; }
    public int? MentorshipID { get; set; }
    public int Rating { get; set; }
    public string? TestimonialText { get; set; }
    public bool IsApprovedByFounder { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }

    // Navigation properties
    public Advisor Advisor { get; set; } = null!;
    public Startup? Startup { get; set; }
    public StartupAdvisorMentorship? Mentorship { get; set; }
}
