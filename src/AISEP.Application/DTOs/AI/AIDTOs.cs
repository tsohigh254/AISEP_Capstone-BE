namespace AISEP.Application.DTOs.AI;

// ═══════════════════════════════════════════════════════════════
//  Requests
// ═══════════════════════════════════════════════════════════════

public class TriggerEvaluationRequest
{
    public int DocumentId { get; set; }
}

// ═══════════════════════════════════════════════════════════════
//  Responses
// ═══════════════════════════════════════════════════════════════

public class EvaluationSubmitResponse
{
    public int EvaluationRunId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class EvaluationStatusResponse
{
    public int Id { get; set; }
    public string StartupId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public float? OverallScore { get; set; }
    public float? OverallConfidence { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public List<EvaluationDocumentStatus> Documents { get; set; } = new();
}

public class EvaluationDocumentStatus
{
    public int Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ExtractionStatus { get; set; }
    public string? Summary { get; set; }
}

public class EvaluationReportResponse
{
    public int Id { get; set; }
    public string StartupId { get; set; } = string.Empty;
    public float OverallScore { get; set; }
    public float? OverallConfidence { get; set; }
    public Dictionary<string, object>? DimensionScores { get; set; }
    public List<string>? Strengths { get; set; }
    public List<string>? Weaknesses { get; set; }
    public List<string>? Recommendations { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class AIScoreLatestResponse
{
    public int ScoreId { get; set; }
    public int StartupId { get; set; }
    public float OverallScore { get; set; }
    public float TeamScore { get; set; }
    public float MarketScore { get; set; }
    public float ProductScore { get; set; }
    public float TractionScore { get; set; }
    public float FinancialScore { get; set; }
    public DateTime CalculatedAt { get; set; }
    public List<SubMetricDto> SubMetrics { get; set; } = new();
    public List<ImprovementRecommendationDto> Recommendations { get; set; } = new();
}

public class SubMetricDto
{
    public string Category { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public string? MetricValue { get; set; }
    public float MetricScore { get; set; }
    public string? Explanation { get; set; }
}

public class ImprovementRecommendationDto
{
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string? RecommendationText { get; set; }
    public string? ExpectedImpact { get; set; }
}

public class AIScoreHistoryResponse
{
    public List<AIScoreLatestResponse> Scores { get; set; } = new();
}

public class RecommendationMatchDto
{
    public string StartupId { get; set; } = string.Empty;
    public string? StartupName { get; set; }
    public float Score { get; set; }
    public string? Explanation { get; set; }
}

public class RecommendationListResponse
{
    public string InvestorId { get; set; } = string.Empty;
    public List<RecommendationMatchDto> Items { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

public class ScoringModelConfigDto
{
    public int ConfigId { get; set; }
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
}

public class CreateScoringModelConfigRequest
{
    public string Version { get; set; } = string.Empty;
    public float TeamWeight { get; set; }
    public float MarketWeight { get; set; }
    public float ProductWeight { get; set; }
    public float TractionWeight { get; set; }
    public float FinancialWeight { get; set; }
    public string? ApplicableStage { get; set; }
    public string? ChangeNotes { get; set; }
}

public class UpdateScoringModelConfigRequest
{
    public float? TeamWeight { get; set; }
    public float? MarketWeight { get; set; }
    public float? ProductWeight { get; set; }
    public float? TractionWeight { get; set; }
    public float? FinancialWeight { get; set; }
    public string? ApplicableStage { get; set; }
    public string? ChangeNotes { get; set; }
}
