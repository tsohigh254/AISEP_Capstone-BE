using System.Text.Json.Serialization;

namespace AISEP.Application.QueryParams;

public class AdvisorQueryParams : BaseQueryParams
{
    /// <summary>Filter by expertise keyword (matches any token in comma-separated Expertise column).</summary>
    [JsonPropertyName("expertise")]
    public string? Expertise { get; set; }

    /// <summary>Minimum years of experience (inclusive).</summary>
    [JsonPropertyName("minYearsOfExperience")]
    public int? MinYearsOfExperience { get; set; }

    /// <summary>Maximum years of experience (inclusive).</summary>
    [JsonPropertyName("maxYearsOfExperience")]
    public int? MaxYearsOfExperience { get; set; }

    /// <summary>Minimum average rating (inclusive, 1.0–5.0).</summary>
    [JsonPropertyName("minRating")]
    public float? MinRating { get; set; }

    /// <summary>Sort order: "highest_rated" | "most_experienced" | "newest" (default).</summary>
    [JsonPropertyName("sortBy")]
    public string? SortBy { get; set; }
}
