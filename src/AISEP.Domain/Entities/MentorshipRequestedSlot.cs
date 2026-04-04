namespace AISEP.Domain.Entities;

public class MentorshipRequestedSlot
{
    public int SlotID { get; set; }
    public int MentorshipID { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string? Timezone { get; set; }
    public string? Note { get; set; }
    public string ProposedBy { get; set; } = "Startup"; 
    public bool IsActive { get; set; } = true;

    public StartupAdvisorMentorship Mentorship { get; set; } = null!;
}