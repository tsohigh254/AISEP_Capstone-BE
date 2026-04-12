namespace AISEP.Domain.Entities;

/// <summary>
/// Idempotency record for webhook deliveries received from the Python AI Service.
/// Prevents duplicate processing of the same delivery_id.
/// </summary>
public class AiWebhookDelivery
{
    public int Id { get; set; }

    /// <summary>Deterministic delivery_id from the Python webhook payload.</summary>
    public string DeliveryId { get; set; } = string.Empty;

    /// <summary>FK to AiEvaluationRun.Id (nullable in case we can't resolve).</summary>
    public int? EvaluationRunId { get; set; }

    /// <summary>Raw JSON payload received.</summary>
    public string PayloadJson { get; set; } = string.Empty;

    /// <summary>Whether the delivery was processed successfully.</summary>
    public bool Processed { get; set; }

    /// <summary>Processing notes / error message if any.</summary>
    public string? ProcessingNote { get; set; }

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}
