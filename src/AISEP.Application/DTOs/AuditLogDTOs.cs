namespace AISEP.Application.DTOs;

public record AuditLogResponse(
    int LogId,
    string ActorEmail,
    string ActionType,
    string EntityType,
    int? EntityId,
    string? ActionDetails,
    string IpAddress,
    DateTime CreatedAt
);
