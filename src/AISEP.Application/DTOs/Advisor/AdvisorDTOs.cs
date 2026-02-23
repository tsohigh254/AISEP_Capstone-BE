namespace AISEP.Application.DTOs.Advisor;

// ========== REQUEST DTOs ==========

/// <summary>
/// Create a new advisor profile.
/// Example: { "fullName": "Dr. Jane Smith", "title": "Startup Mentor", "company": "TechAdvisors Inc.",
///            "bio": "20+ years in SaaS...", "website": "https://janesmith.com",
///            "linkedInURL": "https://linkedin.com/in/janesmith", "yearsOfExperience": 20 }
/// </summary>
public class CreateAdvisorRequest
{
    public string FullName { get; set; } = null!;
    public string? Title { get; set; }
    public string? Company { get; set; }
    public string? Bio { get; set; }
    public string? Website { get; set; }
    public string? LinkedInURL { get; set; }
    public int? YearsOfExperience { get; set; }
    public string? MentorshipPhilosophy { get; set; }
}

/// <summary>
/// Update advisor profile. Only non-null fields are updated.
/// </summary>
public class UpdateAdvisorRequest
{
    public string? FullName { get; set; }
    public string? Title { get; set; }
    public string? Company { get; set; }
    public string? Bio { get; set; }
    public string? Website { get; set; }
    public string? LinkedInURL { get; set; }
    public int? YearsOfExperience { get; set; }
    public string? MentorshipPhilosophy { get; set; }
}

/// <summary>
/// Replace-all expertise items.
/// Example: { "items": [{ "category": "Strategy", "subTopic": "Go-to-market", "proficiencyLevel": "Expert" }] }
/// </summary>
public class UpdateExpertiseRequest
{
    public List<ExpertiseItemDto> Items { get; set; } = new();
}

/// <summary>
/// Update advisor availability settings (upsert one-to-one record).
/// Example: { "sessionFormats": "Video,In-Person", "typicalSessionDuration": 60,
///            "weeklyAvailableHours": 10, "maxConcurrentMentees": 5,
///            "responseTimeCommitment": "Within 24 hours", "isAcceptingNewMentees": true }
/// </summary>
public class UpdateAvailabilityRequest
{
    public string? SessionFormats { get; set; }
    public int? TypicalSessionDuration { get; set; }
    public int? WeeklyAvailableHours { get; set; }
    public int? MaxConcurrentMentees { get; set; }
    public string? ResponseTimeCommitment { get; set; }
    public bool? IsAcceptingNewMentees { get; set; }
}

// ========== RESPONSE DTOs ==========

/// <summary>
/// Full advisor profile for the owner (GET /me, POST, PUT responses).
/// </summary>
public class AdvisorMeDto
{
    public int AdvisorID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Company { get; set; }
    public string? Bio { get; set; }
    public string? ProfilePhotoURL { get; set; }
    public int? YearsOfExperience { get; set; }
    public string? MentorshipPhilosophy { get; set; }
    public string? LinkedInURL { get; set; }
    public string? Website { get; set; }
    public string? ProfileStatus { get; set; }
    public int? ProfileCompleteness { get; set; }
    public int TotalMentees { get; set; }
    public float TotalSessionHours { get; set; }
    public float? AverageRating { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<ExpertiseItemDto> Expertise { get; set; } = new();
    public AvailabilityDto? Availability { get; set; }
    public List<string> IndustryFocus { get; set; } = new();
}

/// <summary>Expertise item used in both request and response.</summary>
public class ExpertiseItemDto
{
    public string Category { get; set; } = null!;
    public string? SubTopic { get; set; }
    public string? ProficiencyLevel { get; set; }
}

/// <summary>Advisor availability configuration.</summary>
public class AvailabilityDto
{
    public string? SessionFormats { get; set; }
    public int? TypicalSessionDuration { get; set; }
    public int? WeeklyAvailableHours { get; set; }
    public int? MaxConcurrentMentees { get; set; }
    public string? ResponseTimeCommitment { get; set; }
    public bool CalendarConnected { get; set; }
    public bool IsAcceptingNewMentees { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Public-safe advisor search result. No email/phone/userId.
/// Response: { "items": [...], "paging": { "page":1, "pageSize":20, "totalItems":5, "totalPages":1 } }
/// </summary>
public class AdvisorSearchItemDto
{
    public int AdvisorID { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Company { get; set; }
    public string? BioShort { get; set; }
    public string? Website { get; set; }
    public int? YearsOfExperience { get; set; }
    public float? AverageRating { get; set; }
    public bool IsAcceptingNewMentees { get; set; }
    public List<string> Industries { get; set; } = new();
    public List<string> Expertise { get; set; } = new();
}
