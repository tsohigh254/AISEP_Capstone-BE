using AISEP.Domain.Enums;
using Microsoft.AspNetCore.Http;

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
    public string? Bio { get; set; }
    public IFormFile? ProfilePhotoURL { get; set; }
    public string? LinkedInURL { get; set; }
    public string? MentorshipPhilosophy { get; set; }
    public List<AdvisorIndustryFocusRequest> AdvisorIndustryFocus { get; set; } = new List<AdvisorIndustryFocusRequest>();
}

/// <summary>
/// Update advisor profile. Only non-null fields are updated.
/// </summary>
public class UpdateAdvisorRequest
{
    public string? FullName { get; set; }
    public string? Title { get; set; }
    public string? Bio { get; set; }
    public IFormFile? ProfilePhotoURL { get; set; }
    public string? LinkedInURL { get; set; }
    public string? MentorshipPhilosophy { get; set; }
    public List<AdvisorIndustryFocusRequest> AdvisorIndustryFocus { get; set; } = new List<AdvisorIndustryFocusRequest>();
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
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Bio { get; set; }
    public string? ProfilePhotoURL { get; set; }
    public string? MentorshipPhilosophy { get; set; }
    public string? LinkedInURL { get; set; }
    public string? ProfileStatus { get; set; }
    public int TotalMentees { get; set; }
    public float TotalSessionHours { get; set; }
    public float? AverageRating { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public AvailabilityDto? Availability { get; set; }
    public List<AdvisorIndustryFocusDto> IndustryFocus { get; set; } = new();
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
    public string FullName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? ProfilePhotoURL { get; set; }
    public string? Bio { get; set; }
    public float? AverageRating { get; set; }
    
    // UI Need Fields
    public int ReviewCount { get; set; }
    public int CompletedSessions { get; set; }
    public int? YearsOfExperience { get; set; }
    public bool IsVerified { get; set; }
    public string? AvailabilityHint { get; set; }
    public decimal? HourlyRate { get; set; }
    
    // Arrays
    public List<string> Expertise { get; set; } = new();
    public List<string> DomainTags { get; set; } = new();
    public List<string> SuitableFor { get; set; } = new();
    public List<string> SupportedDurations { get; set; } = new();
    public List<AdvisorIndustryFocusDto> Industry { get; set; } = new();
}

// Added Detail Dto
public class AdvisorDetailDto : AdvisorSearchItemDto
{
    public string? MentorshipPhilosophy { get; set; }
    public string? ExperiencesJson { get; set; }
    public List<string> Skills { get; set; } = new();
}

public class AdvisorIndustryFocusDto
{
    public int IndustryId { get; set; }
    public string Industry { get; set; } = null!;
}

public class AdvisorIndustryFocusRequest
{
    public int IndustryId { get; set; }
}

public class AdvisorDto
{
    public int AdvisorID { get; set; }
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Bio { get; set; }
    public string? ProfilePhotoURL { get; set; }
    public string? MentorshipPhilosophy { get; set; }
    public string? LinkedInURL { get; set; }
    public string? ProfileStatus { get; set; }
    public int TotalMentees { get; set; }
    public float TotalSessionHours { get; set; }
    public float? AverageRating { get; set; }
    public string? Expertise { get; set; }
    public int? YearsOfExperience { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<AdvisorIndustryFocusDto> IndustryFocus { get; set; } = new();
}
