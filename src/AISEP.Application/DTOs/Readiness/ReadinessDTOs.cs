using AISEP.Domain.Enums;

namespace AISEP.Application.DTOs.Readiness;

/// <summary>Full readiness assessment result.</summary>
public class ReadinessResultDto
{
    public int OverallScore { get; set; }
    public string Status { get; set; } = string.Empty;
    public ReadinessDimensionsDto Dimensions { get; set; } = new();
    public List<MissingItemDto> MissingItems { get; set; } = new();
    public List<NextActionDto> NextActions { get; set; } = new();
    public List<AppliedCapDto> AppliedCaps { get; set; } = new();
    public DateTime CalculatedAt { get; set; }
}

/// <summary>Score breakdown by dimension (max values shown in comments).</summary>
public class ReadinessDimensionsDto
{
    /// <summary>Max 25</summary>
    public int Profile { get; set; }
    /// <summary>Max 20</summary>
    public int Kyc { get; set; }
    /// <summary>Max 20</summary>
    public int Documents { get; set; }
    /// <summary>Max 20</summary>
    public int Ai { get; set; }
    /// <summary>Max 15</summary>
    public int Trust { get; set; }
}

/// <summary>A missing item with code and dimension for easy FE filtering/i18n.</summary>
public class MissingItemDto
{
    public string Code { get; set; } = string.Empty;
    public string Dimension { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

/// <summary>Actionable next step for the startup.</summary>
public class NextActionDto
{
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    /// <summary>Suggested FE route. FE may map from Code instead.</summary>
    public string Target { get; set; } = string.Empty;
}

/// <summary>Explains why the overall score was hard-capped.</summary>
public class AppliedCapDto
{
    public string Rule { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CappedAt { get; set; }
}
