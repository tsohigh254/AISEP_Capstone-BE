namespace AISEP.Application.DTOs.MasterData;

// Industry DTOs
public class IndustryDto
{
    public int IndustryID { get; set; }
    public string IndustryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ParentIndustryID { get; set; }
    public bool IsActive { get; set; } = true;
    public int StartupCount { get; set; }
    public int InvestorCount { get; set; }
    public string? ParentIndustryName { get; set; }
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
    public int StageID { get; set; }
    public string StageName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderIndex { get; set; }
    public bool IsActive { get; set; } = true;
    public int StartupCount { get; set; }
    public int InvestorCount { get; set; }
}

// Role DTOs (for dropdown, simplified)
public class RoleSimpleDto
{
    public int RoleID { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

// ========== STAFF MANAGEMENT DTOs ==========

public class ManageIndustryRequest
{
    public string IndustryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ParentIndustryID { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ManageStageRequest
{
    public string StageName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderIndex { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ReorderStageRequest
{
    public List<StageOrderDto> Orders { get; set; } = new();
}

public class StageOrderDto
{
    public int StageID { get; set; }
    public int OrderIndex { get; set; }
}
