namespace AISEP.Domain.Entities;

public class Industry
{
    public int IndustryID { get; set; }
    public string IndustryName { get; set; } = string.Empty;
    public int? ParentIndustryID { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Industry? ParentIndustry { get; set; }
    public ICollection<Industry> SubIndustries { get; set; } = new List<Industry>();
    public ICollection<IndustryTrend> Trends { get; set; } = new List<IndustryTrend>();
    public ICollection<AdvisorIndustryFocus> AdvisorIndustries { get; set; } = new List<AdvisorIndustryFocus>();
}
