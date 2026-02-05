namespace AISEP.Domain.Entities;

public class AdvisorAchievement
{
    public int AchievementID { get; set; }
    public int AdvisorID { get; set; }
    public string AchievementType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? Date { get; set; }
    public string? URL { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Advisor Advisor { get; set; } = null!;
}
