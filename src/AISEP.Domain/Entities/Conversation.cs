namespace AISEP.Domain.Entities;

public class Conversation
{
    public int ConversationID { get; set; }
    public int? ConnectionID { get; set; }
    public int? MentorshipID { get; set; }
    public string ConversationStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastMessageAt { get; set; }

    // Navigation properties
    public StartupInvestorConnection? Connection { get; set; }
    public StartupAdvisorMentorship? Mentorship { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
