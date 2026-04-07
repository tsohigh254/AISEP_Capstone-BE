namespace AISEP.Domain.Entities;

public class StartupKycRequestedItem
{
    public int RequestedItemID { get; set; }
    public int SubmissionID { get; set; }
    public string FieldKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public StartupKycSubmission Submission { get; set; } = null!;
}
