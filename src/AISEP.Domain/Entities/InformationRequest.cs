using AISEP.Domain.Enums;

namespace AISEP.Domain.Entities;

public class InformationRequest
{
    public int RequestID { get; set; }
    public int ConnectionID { get; set; }
    public int InvestorID { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public string? RequestMessage { get; set; }
    public RequestStatus RequestStatus { get; set; } = RequestStatus.Pending;
    public DateTime? RequestedAt { get; set; }
    public DateTime? FulfilledAt { get; set; }
    public string? ResponseDocumentIDs { get; set; } // JSON
    public string? ResponseMessage { get; set; }
    public DateTime? ReminderSentAt { get; set; }

    // Navigation properties
    public StartupInvestorConnection Connection { get; set; } = null!;
    public Investor Investor { get; set; } = null!;
}
