using AISEP.Domain.Enums;

namespace AISEP.Domain.Entities;

public class Advisor
{
    public int AdvisorID { get; set; }
    public int UserID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Bio { get; set; }
    public string? ProfilePhotoURL { get; set; }
    public string? MentorshipPhilosophy { get; set; }
    public string? LinkedInURL { get; set; }
    public ProfileStatus ProfileStatus { get; set; } = ProfileStatus.Draft;
    public int TotalMentees { get; set; }
    public float TotalSessionHours { get; set; }
    public float? AverageRating { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // UI Extra requirements
    public int? YearsOfExperience { get; set; }
    public bool IsVerified { get; set; } = false;
    public decimal? HourlyRate { get; set; }
    public string? Expertise { get; set; }          // comma-separated
    public string? DomainTags { get; set; }         // comma-separated
    public string? SuitableFor { get; set; }        // comma-separated
    public string? SupportedDurations { get; set; } // comma-separated
    public int ReviewCount { get; set; } = 0;
    public int CompletedSessions { get; set; } = 0;
    
    public string? ExperiencesJson { get; set; }    // JSON array of experience objects
    public string? Skills { get; set; }             // comma-separated

    // Navigation properties
    public User User { get; set; } = null!;
    public AdvisorAvailability? Availability { get; set; }
    public ICollection<AdvisorIndustryFocus> IndustryFocus { get; set; } = new List<AdvisorIndustryFocus>();
    public ICollection<AdvisorTestimonial> Testimonials { get; set; } = new List<AdvisorTestimonial>();
    public ICollection<StartupAdvisorMentorship> Mentorships { get; set; } = new List<StartupAdvisorMentorship>();
}
