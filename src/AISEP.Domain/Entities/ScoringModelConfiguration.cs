namespace AISEP.Domain.Entities;

public class ScoringModelConfiguration
{
    public int ConfigID { get; set; }
    public string Version { get; set; } = string.Empty;
    public float TeamWeight { get; set; }
    public float MarketWeight { get; set; }
    public float ProductWeight { get; set; }
    public float TractionWeight { get; set; }
    public float FinancialWeight { get; set; }
    public string? ApplicableStage { get; set; }
    public string? ChangeNotes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? ActivatedAt { get; set; }

    // Navigation properties
    public User? CreatedByUser { get; set; }
    public ICollection<StartupPotentialScore> PotentialScores { get; set; } = new List<StartupPotentialScore>();
}
