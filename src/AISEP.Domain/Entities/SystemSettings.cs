namespace AISEP.Domain.Entities;

public class SystemSettings
{
    public int SettingID { get; set; }
    public string SettingKey { get; set; } = string.Empty;
    public string SettingValue { get; set; } = string.Empty;
    public string SettingType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User? UpdatedByUser { get; set; }
}
