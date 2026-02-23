namespace AISEP.Application.DTOs.Moderation;

// ── List item (GET /flags) ────────────────────────────────────
public class FlaggedContentListItemDto
{
    public int FlagId { get; set; }
    public string ContentType { get; set; } = null!;
    public int ContentId { get; set; }
    public int? RelatedUserId { get; set; }
    public string FlagReason { get; set; } = null!;
    public string? FlagSource { get; set; }
    public string? Severity { get; set; }
    public string ModerationStatus { get; set; } = null!;
    public DateTime FlaggedAt { get; set; }
}

// ── Detail (GET /flags/{id}) ──────────────────────────────────
public class FlaggedContentDetailDto
{
    public int FlagId { get; set; }
    public string ContentType { get; set; } = null!;
    public int ContentId { get; set; }
    public int? RelatedUserId { get; set; }
    public string? RelatedUserEmail { get; set; }
    public string FlagReason { get; set; } = null!;
    public string? FlagSource { get; set; }
    public string? Severity { get; set; }
    public string? FlagDetails { get; set; }
    public string ModerationStatus { get; set; } = null!;
    public DateTime FlaggedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int? ReviewedBy { get; set; }
    public string? ModerationAction { get; set; }
    public string? ModeratorNotes { get; set; }
    public List<ModerationActionDto> Actions { get; set; } = new();
}

// ── ModerationAction response ─────────────────────────────────
public class ModerationActionDto
{
    public int ActionId { get; set; }
    public int FlagId { get; set; }
    public string ActionType { get; set; } = null!;
    public int? TargetUserId { get; set; }
    public string? ActionDetails { get; set; }
    public string? MessageToUser { get; set; }
    public int? PerformedBy { get; set; }
    public DateTime ActionTakenAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

// ── Requests ─────────────────────────────────────────────────

/// <summary>POST /api/moderation/flags/{id}/assign</summary>
public class AssignFlagRequest
{
    public string? Note { get; set; }
}

/// <summary>POST /api/moderation/flags/{id}/resolve</summary>
public class ResolveFlagRequest
{
    /// <summary>"MarkSafe", "RejectReport", or "Resolved"</summary>
    public string Decision { get; set; } = null!;
    public string? Note { get; set; }
}

/// <summary>POST /api/moderation/flags/{id}/actions</summary>
public class CreateModerationActionRequest
{
    /// <summary>"Warn", "Hide", "Remove", "LockUser", "UnlockUser", "BanUser"</summary>
    public string ActionType { get; set; } = null!;
    public string? ActionNote { get; set; }
    public int? DurationDays { get; set; }
}

/// <summary>POST /api/reports (user-initiated report)</summary>
public class CreateFlagRequest
{
    public string EntityType { get; set; } = null!;
    public int EntityId { get; set; }
    public string Reason { get; set; } = null!;
    public string? Description { get; set; }
}
