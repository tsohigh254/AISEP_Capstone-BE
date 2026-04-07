using System.Text.Json.Serialization;
using AISEP.Domain.Enums;

namespace AISEP.Application.QueryParams;

public class AuditLogQueryParams : BaseQueryParams
{
    [JsonPropertyName("actionType")]
    public string? ActionType { get; set; }

    [JsonPropertyName("entityType")]
    public string? EntityType { get; set; }

    [JsonPropertyName("userId")]
    public int? UserId { get; set; }

    [JsonPropertyName("dateFrom")]
    public DateTime? DateFrom { get; set; }

    [JsonPropertyName("dateTo")]
    public DateTime? DateTo { get; set; }
}

public class ViolationQueryParams : BaseQueryParams
{
    [JsonPropertyName("status")]
    public ModerationStatus? Status { get; set; }

    [JsonPropertyName("severity")]
    public string? Severity { get; set; }

    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }
}

public class IncidentQueryParams : BaseQueryParams
{
    [JsonPropertyName("status")]
    public IncidentStatus? Status { get; set; }

    [JsonPropertyName("severity")]
    public IncidentSeverity? Severity { get; set; }
}
