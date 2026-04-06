using AISEP.Application.DTOs;
using AISEP.Application.DTOs.Common;

namespace AISEP.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(int? userId, string actionType, string entityType, int? entityId, string? actionDetails, string ipAddress, string userAgent);
    Task LogAsync(string actionType, string entityType, int? entityId, string? actionDetails);
    Task<PagedData<AuditLogResponse>> GetLogsAsync(string? search, string? actionType, int page, int pageSize, CancellationToken ct);
}
