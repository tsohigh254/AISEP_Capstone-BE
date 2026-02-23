namespace AISEP.Application.DTOs.MasterData;

// Industry DTOs
public class IndustryDto
{
    public int IndustryID { get; set; }
    public string IndustryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ParentIndustryID { get; set; }
    public List<IndustryDto> SubIndustries { get; set; } = new();
}

public class IndustrySimpleDto
{
    public int IndustryID { get; set; }
    public string IndustryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ParentIndustryID { get; set; }
}

// Startup Stage DTOs
public class StartupStageDto
{
    public string StageName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

// Role DTOs (for dropdown, simplified)
public class RoleSimpleDto
{
    public int RoleID { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
