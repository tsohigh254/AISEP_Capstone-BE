using AISEP.Domain.Enums;

namespace AISEP.Domain.Entities;

public class Incident
{
    public int IncidentID { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IncidentSeverity Severity { get; set; }
    public IncidentStatus Status { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int? ResolvedBy { get; set; }
    public string? RollbackNotes { get; set; }
    public bool IsRolledBack { get; set; }

    // Navigation properties
    public User? CreatedByUser { get; set; }
    public User? ResolvedByUser { get; set; }
}
