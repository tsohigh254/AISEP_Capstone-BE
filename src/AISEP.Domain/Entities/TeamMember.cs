namespace AISEP.Domain.Entities;

public class TeamMember
{
    public int TeamMemberID { get; set; }
    public int StartupID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string? Title { get; set; }
    public string? LinkedInURL { get; set; }
    public string? Bio { get; set; }
    public string? PhotoURL { get; set; }
    public bool IsFounder { get; set; }
    public int? YearsOfExperience { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Startup Startup { get; set; } = null!;
}
