using System.Text.Json;
using System.Text.Json.Serialization;
using AISEP.Application.DTOs.AI;
using FluentAssertions;

namespace AISEP.Tests.Services;

public class AiRecommendationDtoTests
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    [Fact]
    public void RecommendationListResponse_Deserializes_PythonContract()
    {
        var json = """
        {
          "investor_id": "42",
          "items": [
            {
              "investor_id": "42",
              "startup_id": "7",
              "startup_name": "Acme",
              "final_match_score": 0.87,
              "match_band": "HIGH",
              "fit_summary_label": "Strong thesis alignment",
              "structured_score": 0.8,
              "semantic_score": 0.75,
              "combined_pre_llm_score": 0.78,
              "rerank_adjustment": 0.09,
              "breakdown": {
                "thesis_fit_score": 0.9,
                "final_match_score": 0.87
              },
              "match_reasons": ["industry match"],
              "positive_reasons": [
                { "type": "positive", "code": "INDUSTRY_MATCH", "text": "Healthtech aligns with thesis" },
                { "type": "positive", "code": "STAGE_MATCH", "text": "Seed stage fit" }
              ],
              "caution_reasons": [
                { "type": "caution", "code": "AI_SCORE_MISSING", "text": "No AI evaluation yet" }
              ],
              "warning_flags": [],
              "generated_at": "2026-04-18T10:00:00Z"
            }
          ],
          "warnings": ["llm rerank capped"],
          "internal_warnings": ["embedding fallback"],
          "generated_at": "2026-04-18T10:00:00Z"
        }
        """;

        var result = JsonSerializer.Deserialize<PythonRecommendationListResponse>(json, JsonOpts);

        result.Should().NotBeNull();
        result!.InvestorId.Should().Be("42");
        result.Items.Should().HaveCount(1);
        result.Warnings.Should().ContainSingle().Which.Should().Be("llm rerank capped");
        result.InternalWarnings.Should().ContainSingle().Which.Should().Be("embedding fallback");

        var match = result.Items[0];
        match.StartupId.Should().Be("7");
        match.StartupName.Should().Be("Acme");
        match.FinalMatchScore.Should().Be(0.87);
        match.MatchBand.Should().Be("HIGH");

        match.PositiveReasons.Should().HaveCount(2);
        match.PositiveReasons![0].Type.Should().Be("positive");
        match.PositiveReasons[0].Code.Should().Be("INDUSTRY_MATCH");
        match.PositiveReasons[0].Text.Should().Be("Healthtech aligns with thesis");

        match.CautionReasons.Should().ContainSingle();
        match.CautionReasons![0].Code.Should().Be("AI_SCORE_MISSING");

        match.Breakdown.Should().NotBeNull();
        match.GeneratedAt.Should().NotBeNull();
    }

    [Fact]
    public void RecommendationExplanation_Deserializes_ResultField()
    {
        var json = """
        {
          "investor_id": "42",
          "startup_id": "7",
          "result": {
            "investor_id": "42",
            "startup_id": "7",
            "startup_name": "Acme",
            "final_match_score": 0.87,
            "match_band": "HIGH",
            "fit_summary_label": "",
            "structured_score": 0.8,
            "semantic_score": 0.75,
            "combined_pre_llm_score": 0.78,
            "rerank_adjustment": 0.09,
            "breakdown": {},
            "match_reasons": [],
            "positive_reasons": [
              { "type": "positive", "code": "THESIS_FIT", "text": "Strong thesis fit" }
            ],
            "caution_reasons": [],
            "warning_flags": []
          },
          "generated_at": "2026-04-18T10:00:00Z"
        }
        """;

        var result = JsonSerializer.Deserialize<PythonRecommendationExplanation>(json, JsonOpts);

        result.Should().NotBeNull();
        result!.InvestorId.Should().Be("42");
        result.StartupId.Should().Be("7");
        result.Result.Should().NotBeNull();
        result.Result!.StartupName.Should().Be("Acme");
        result.Result.PositiveReasons.Should().ContainSingle()
            .Which.Text.Should().Be("Strong thesis fit");
    }

    [Fact]
    public void ReindexResponse_Deserializes_NewShape()
    {
        var json = """
        {
          "success": true,
          "startup_id": "7",
          "profile_version": "v1",
          "source_updated_at": "2026-04-18T10:00:00Z",
          "message": "Startup recommendation document reindexed successfully"
        }
        """;

        var result = JsonSerializer.Deserialize<PythonReindexResponse>(json, JsonOpts);

        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.StartupId.Should().Be("7");
        result.ProfileVersion.Should().Be("v1");
        result.SourceUpdatedAt.Should().NotBeNull();
        result.Message.Should().Contain("reindexed");
    }

    [Fact]
    public void ReindexStartupRequest_Serializes_SnakeCaseKeys_WithoutStartupId()
    {
        var payload = new PythonReindexStartupRequest
        {
            ProfileVersion = "v1",
            SourceUpdatedAt = new DateTime(2026, 4, 18, 10, 0, 0, DateTimeKind.Utc),
            StartupName = "Acme",
            Tagline = "Save the world",
            Stage = "Seed",
            PrimaryIndustry = "Healthtech",
            Website = "https://acme.example",
            LogoUrl = "https://cdn/logo.png",
            CurrentNeeds = new List<string> { "funding", "mentor" },
            OptionalShortMetricSummary = "100 MAU",
            IsProfileVisibleToInvestors = true,
            AccountActive = true,
        };

        var json = JsonSerializer.Serialize(payload, JsonOpts);

        json.Should().Contain("\"profile_version\":\"v1\"");
        json.Should().Contain("\"startup_name\":\"Acme\"");
        json.Should().Contain("\"logo_url\":\"https://cdn/logo.png\"");
        json.Should().Contain("\"current_needs\":[\"funding\",\"mentor\"]");
        json.Should().Contain("\"optional_short_metric_summary\":\"100 MAU\"");
        json.Should().NotContain("\"startup_id\"");
        json.Should().NotContain("\"sub_industry\"");
        json.Should().NotContain("\"description\"");
        json.Should().NotContain("\"funding_amount_sought\"");
    }

    [Fact]
    public void ReindexInvestorRequest_Serializes_WithProfileVersionAndSourceUpdatedAt()
    {
        var payload = new PythonReindexInvestorRequest
        {
            ProfileVersion = "v1",
            SourceUpdatedAt = new DateTime(2026, 4, 18, 10, 0, 0, DateTimeKind.Utc),
            InvestorName = "Jane Doe",
            InvestorType = "angel",
        };

        var json = JsonSerializer.Serialize(payload, JsonOpts);

        json.Should().Contain("\"profile_version\":\"v1\"");
        json.Should().Contain("\"source_updated_at\":");
        json.Should().Contain("\"investor_name\":\"Jane Doe\"");
        json.Should().Contain("\"investor_type\":\"angel\"");
    }
}
