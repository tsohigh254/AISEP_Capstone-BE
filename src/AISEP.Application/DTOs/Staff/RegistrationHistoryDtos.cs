namespace AISEP.Application.DTOs.Staff;

public class RegistrationHistoryItemDto
{
    public int ApplicantId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public string RoleType { get; set; } = string.Empty;   // STARTUP | ADVISOR | INVESTOR
    public string Result { get; set; } = string.Empty;     // APPROVED | REJECTED | PENDING_MORE_INFO
    public DateTime? ProcessedAt { get; set; }
    public string? ReviewedBy { get; set; }                // email of staff
    public string? Remarks { get; set; }
}
