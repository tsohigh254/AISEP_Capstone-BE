using AISEP.Domain.Enums;

namespace AISEP.Domain.Entities;

public class Advisor
{
    public int AdvisorID { get; set; }
    public int UserID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Company { get; set; }
    public string? Bio { get; set; }
    public string? ProfilePhotoURL { get; set; }
    public string? MentorshipPhilosophy { get; set; }
    public string? LinkedInURL { get; set; }
    public string? Website { get; set; }
    public ProfileStatus ProfileStatus { get; set; } = ProfileStatus.Draft;
    public int? ProfileCompleteness { get; set; }
    public int TotalMentees { get; set; }
    public float TotalSessionHours { get; set; }
    public float? AverageRating { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public AdvisorAvailability? Availability { get; set; }
    public ICollection<AdvisorExpertise> Expertise { get; set; } = new List<AdvisorExpertise>();
    public ICollection<AdvisorIndustryFocus> IndustryFocus { get; set; } = new List<AdvisorIndustryFocus>();
    public ICollection<AdvisorAchievement> Achievements { get; set; } = new List<AdvisorAchievement>();
    public ICollection<AdvisorTestimonial> Testimonials { get; set; } = new List<AdvisorTestimonial>();
    public ICollection<StartupAdvisorMentorship> Mentorships { get; set; } = new List<StartupAdvisorMentorship>();
    public ICollection<ProfileView> ProfileViews { get; set; } = new List<ProfileView>();
}
