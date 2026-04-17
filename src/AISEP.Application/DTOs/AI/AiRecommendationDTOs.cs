using System.Text.Json;
using System.Text.Json.Serialization;

namespace AISEP.Application.DTOs.AI;

// ═══════════════════════════════════════════════════════════════
//  Reindex — Startup (sent from .NET to Python)
// ═══════════════════════════════════════════════════════════════

public class PythonReindexStartupRequest
{
    [JsonPropertyName("profile_version")]
    public string ProfileVersion { get; set; } = string.Empty;

    [JsonPropertyName("source_updated_at")]
    public DateTime SourceUpdatedAt { get; set; }

    [JsonPropertyName("startup_name")]
    public string StartupName { get; set; } = string.Empty;

    [JsonPropertyName("tagline")]
    public string? Tagline { get; set; }

    [JsonPropertyName("stage")]
    public string? Stage { get; set; }

    [JsonPropertyName("primary_industry")]
    public string? PrimaryIndustry { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("website")]
    public string? Website { get; set; }

    [JsonPropertyName("product_link")]
    public string? ProductLink { get; set; }

    [JsonPropertyName("demo_link")]
    public string? DemoLink { get; set; }

    [JsonPropertyName("logo_url")]
    public string? LogoUrl { get; set; }

    [JsonPropertyName("problem_statement")]
    public string? ProblemStatement { get; set; }

    [JsonPropertyName("solution_summary")]
    public string? SolutionSummary { get; set; }

    [JsonPropertyName("market_scope")]
    public string? MarketScope { get; set; }

    [JsonPropertyName("product_status")]
    public string? ProductStatus { get; set; }

    [JsonPropertyName("current_needs")]
    public List<string>? CurrentNeeds { get; set; }

    [JsonPropertyName("founder_names")]
    public List<string>? FounderNames { get; set; }

    [JsonPropertyName("founder_roles")]
    public List<string>? FounderRoles { get; set; }

    [JsonPropertyName("team_size")]
    public string? TeamSize { get; set; }

    [JsonPropertyName("validation_status")]
    public string? ValidationStatus { get; set; }

    [JsonPropertyName("optional_short_metric_summary")]
    public string? OptionalShortMetricSummary { get; set; }

    [JsonPropertyName("is_profile_visible_to_investors")]
    public bool IsProfileVisibleToInvestors { get; set; }

    [JsonPropertyName("verification_label")]
    public string? VerificationLabel { get; set; }

    [JsonPropertyName("account_active")]
    public bool AccountActive { get; set; }

    // AI evaluation summary fields
    [JsonPropertyName("ai_evaluation_status")]
    public string? AiEvaluationStatus { get; set; }

    [JsonPropertyName("ai_overall_score")]
    public double? AiOverallScore { get; set; }

    [JsonPropertyName("ai_summary")]
    public string? AiSummary { get; set; }

    [JsonPropertyName("ai_strength_tags")]
    public List<string>? AiStrengthTags { get; set; }

    [JsonPropertyName("ai_weakness_tags")]
    public List<string>? AiWeaknessTags { get; set; }

    [JsonPropertyName("ai_dimension_scores")]
    public Dictionary<string, double>? AiDimensionScores { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }
}

// ═══════════════════════════════════════════════════════════════
//  Reindex — Investor (sent from .NET to Python)
//  Schema aligned with Python /internal/recommendations/reindex/investor/{id}
// ═══════════════════════════════════════════════════════════════

public class PythonReindexInvestorRequest
{
    [JsonPropertyName("profile_version")]
    public string ProfileVersion { get; set; } = string.Empty;

    [JsonPropertyName("source_updated_at")]
    public DateTime SourceUpdatedAt { get; set; }

    // ── Core identity ─────────────────────────────────────────
    [JsonPropertyName("investor_name")]
    public string InvestorName { get; set; } = string.Empty;

    [JsonPropertyName("investor_type")]
    public string? InvestorType { get; set; }

    [JsonPropertyName("organization")]
    public string? Organization { get; set; }

    [JsonPropertyName("role_title")]
    public string? RoleTitle { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("website")]
    public string? Website { get; set; }

    [JsonPropertyName("verification_label")]
    public string? VerificationLabel { get; set; }

    [JsonPropertyName("logo_url")]
    public string? LogoUrl { get; set; }

    // ── Thesis ────────────────────────────────────────────────
    [JsonPropertyName("short_thesis_summary")]
    public string? ShortThesisSummary { get; set; }

    // ── Preferred criteria (all lists) ────────────────────────
    [JsonPropertyName("preferred_industries")]
    public List<string>? PreferredIndustries { get; set; }

    [JsonPropertyName("preferred_stages")]
    public List<string>? PreferredStages { get; set; }

    [JsonPropertyName("preferred_geographies")]
    public List<string>? PreferredGeographies { get; set; }

    [JsonPropertyName("preferred_market_scopes")]
    public List<string>? PreferredMarketScopes { get; set; }

    [JsonPropertyName("preferred_product_maturity")]
    public List<string>? PreferredProductMaturity { get; set; }

    [JsonPropertyName("preferred_validation_level")]
    public List<string>? PreferredValidationLevel { get; set; }

    [JsonPropertyName("preferred_strengths")]
    public List<string>? PreferredStrengths { get; set; }

    // ── Support & deal flags ──────────────────────────────────
    [JsonPropertyName("support_offered")]
    public List<string>? SupportOffered { get; set; }

    [JsonPropertyName("require_verified_startups")]
    public bool? RequireVerifiedStartups { get; set; }

    [JsonPropertyName("require_visible_profiles")]
    public bool? RequireVisibleProfiles { get; set; }

    // ── Tags ─────────────────────────────────────────────────
    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    // ── AI evaluation filter ─────────────────────────────────────
    [JsonPropertyName("preferred_ai_score_range")]
    public PythonAiScoreRange? PreferredAiScoreRange { get; set; }

    [JsonPropertyName("ai_score_importance")]
    public string? AiScoreImportance { get; set; }

    // ── Connection & activity ────────────────────────────────────
    [JsonPropertyName("accepting_connections_status")]
    public string? AcceptingConnectionsStatus { get; set; }

    [JsonPropertyName("recently_active_badge")]
    public bool? RecentlyActiveBadge { get; set; }

    // ── Exclusion ───────────────────────────────────────────────
    [JsonPropertyName("avoid_text")]
    public string? AvoidText { get; set; }
}

// ═══════════════════════════════════════════════════════════════
//  Reindex — Common Response from Python
// ═══════════════════════════════════════════════════════════════

public class PythonAiScoreRange
{
    [JsonPropertyName("min")]
    public float? Min { get; set; }

    [JsonPropertyName("max")]
    public float? Max { get; set; }
}

public class PythonReindexResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("startup_id")]
    public string? StartupId { get; set; }

    [JsonPropertyName("investor_id")]
    public string? InvestorId { get; set; }

    [JsonPropertyName("profile_version")]
    public string? ProfileVersion { get; set; }

    [JsonPropertyName("source_updated_at")]
    public DateTime? SourceUpdatedAt { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

// ═══════════════════════════════════════════════════════════════
//  Recommendation List — Python Response
// ═══════════════════════════════════════════════════════════════

public class PythonRecommendationListResponse
{
    [JsonPropertyName("investor_id")]
    public string InvestorId { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public List<PythonRecommendationMatch> Items { get; set; } = new();

    [JsonPropertyName("warnings")]
    public List<string>? Warnings { get; set; }

    [JsonPropertyName("internal_warnings")]
    public List<string>? InternalWarnings { get; set; }

    [JsonPropertyName("generated_at")]
    public DateTime? GeneratedAt { get; set; }
}

public class PythonRecommendationMatch
{
    [JsonPropertyName("investor_id")]
    public string? InvestorId { get; set; }

    [JsonPropertyName("startup_id")]
    public string StartupId { get; set; } = string.Empty;

    [JsonPropertyName("startup_name")]
    public string? StartupName { get; set; }

    [JsonPropertyName("final_match_score")]
    public double FinalMatchScore { get; set; }

    [JsonPropertyName("structured_score")]
    public double? StructuredScore { get; set; }

    [JsonPropertyName("semantic_score")]
    public double? SemanticScore { get; set; }

    [JsonPropertyName("combined_pre_llm_score")]
    public double? CombinedPreLlmScore { get; set; }

    [JsonPropertyName("rerank_adjustment")]
    public double? RerankAdjustment { get; set; }

    [JsonPropertyName("match_band")]
    public string? MatchBand { get; set; }

    [JsonPropertyName("fit_summary_label")]
    public string? FitSummaryLabel { get; set; }

    [JsonPropertyName("breakdown")]
    public JsonElement? Breakdown { get; set; }

    [JsonPropertyName("match_reasons")]
    public List<string>? MatchReasons { get; set; }

    [JsonPropertyName("positive_reasons")]
    public List<PythonRecommendationReasonItem>? PositiveReasons { get; set; }

    [JsonPropertyName("caution_reasons")]
    public List<PythonRecommendationReasonItem>? CautionReasons { get; set; }

    [JsonPropertyName("warning_flags")]
    public List<string>? WarningFlags { get; set; }

    [JsonPropertyName("generated_at")]
    public DateTime? GeneratedAt { get; set; }
}

public class PythonRecommendationReasonItem
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

// ═══════════════════════════════════════════════════════════════
//  Recommendation Explanation — Python Response
// ═══════════════════════════════════════════════════════════════

public class PythonRecommendationExplanation
{
    [JsonPropertyName("investor_id")]
    public string InvestorId { get; set; } = string.Empty;

    [JsonPropertyName("startup_id")]
    public string StartupId { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public PythonRecommendationMatch? Result { get; set; }

    [JsonPropertyName("generated_at")]
    public DateTime? GeneratedAt { get; set; }
}

// ═══════════════════════════════════════════════════════════════
//  FE-facing DTOs — Recommendation List
// ═══════════════════════════════════════════════════════════════

public class RecommendationListResult
{
    public int InvestorId { get; set; }
    public List<RecommendationMatchResult> Matches { get; set; } = new();
    public List<string>? Warnings { get; set; }
    public DateTime? GeneratedAt { get; set; }
}

public class RecommendationMatchResult
{
    public int StartupId { get; set; }
    public string? StartupName { get; set; }
    public double FinalMatchScore { get; set; }
    public string? MatchBand { get; set; }
    public string? FitSummaryLabel { get; set; }
    public List<string>? MatchReasons { get; set; }
    public List<string>? PositiveReasons { get; set; }
    public List<string>? CautionReasons { get; set; }
    public List<string>? WarningFlags { get; set; }
}

// ═══════════════════════════════════════════════════════════════
//  FE-facing DTOs — Recommendation Explanation
// ═══════════════════════════════════════════════════════════════

public class RecommendationExplanationResult
{
    public int InvestorId { get; set; }
    public int StartupId { get; set; }
    public object? Explanation { get; set; }
    public DateTime? GeneratedAt { get; set; }
}
