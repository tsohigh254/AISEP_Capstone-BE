using AISEP.Domain.Enums;

namespace AISEP.Domain.Entities;

public class StartupInvestorConnection
{
    public int ConnectionID { get; set; }
    public int StartupID { get; set; }
    public int InvestorID { get; set; }
    public ConnectionStatus ConnectionStatus { get; set; } = ConnectionStatus.Requested;
    public int? InitiatedBy { get; set; }
    public float? MatchScore { get; set; }
    public string? PersonalizedMessage { get; set; }
    public DateTime? RequestedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? AttachedDocumentIDs { get; set; } // JSON

    // Navigation properties
    public Startup Startup { get; set; } = null!;
    public Investor Investor { get; set; } = null!;
    public ICollection<InformationRequest> InformationRequests { get; set; } = new List<InformationRequest>();
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
}
