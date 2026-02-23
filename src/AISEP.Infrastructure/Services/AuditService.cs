using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AISEP.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditService> _logger;

    public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, ILogger<AuditService> logger)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogAsync(int? userId, string actionType, string entityType, int? entityId, string? actionDetails, string ipAddress, string userAgent)
    {
        try
        {
            var auditLog = new AuditLog
            {
                UserID = userId,
                ActionType = actionType,
                EntityType = entityType,
                EntityID = entityId,
                ActionDetails = actionDetails,
                IPAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Audit: {ActionType} on {EntityType}:{EntityId} by User:{UserId}", 
                actionType, entityType, entityId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save audit log: {ActionType} on {EntityType}:{EntityId}", 
                actionType, entityType, entityId);
        }
    }

    public async Task LogAsync(string actionType, string entityType, int? entityId, string? actionDetails)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        int? userId = null;
        string ipAddress = "unknown";
        string userAgent = "unknown";

        if (httpContext != null)
        {
            var userIdClaim = httpContext.User?.FindFirst("sub")?.Value;
            if (int.TryParse(userIdClaim, out var parsedUserId))
            {
                userId = parsedUserId;
            }

            ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            userAgent = httpContext.Request.Headers.UserAgent.FirstOrDefault() ?? "unknown";
        }

        await LogAsync(userId, actionType, entityType, entityId, actionDetails, ipAddress, userAgent);
    }
}
