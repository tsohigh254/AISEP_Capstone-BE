namespace AISEP.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(int? userId, string actionType, string entityType, int? entityId, string? actionDetails, string ipAddress, string userAgent);
    Task LogAsync(string actionType, string entityType, int? entityId, string? actionDetails);
}
