namespace AISEP.Domain.Entities;

public class Stage
{
    public int StageID { get; set; }
    public string StageName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderIndex { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Startup> Startups { get; set; } = new List<Startup>();
    public ICollection<InvestorStageFocus> InvestorStageFocuses { get; set; } = new List<InvestorStageFocus>();
}
