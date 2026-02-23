using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Moderation;
using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AISEP.Infrastructure.Services;

public class ModerationService : IModerationService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly ILogger<ModerationService> _logger;

    // Valid status values matching DB
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
        { "Pending", "InReview", "Resolved", "Rejected" };

    private static readonly HashSet<string> AllowedActionTypes = new(StringComparer.OrdinalIgnoreCase)
        { "Warn", "Hide", "Remove", "LockUser", "UnlockUser", "BanUser", "MarkSafe", "RejectReport" };

    public ModerationService(ApplicationDbContext db, IAuditService audit, ILogger<ModerationService> logger)
    {
        _db = db;
        _audit = audit;
        _logger = logger;
    }

    // ══════════════════════════════════════════════════════════════
    // 1) GET /flags — paged list
    // ══════════════════════════════════════════════════════════════

    public async Task<ApiResponse<PagedResponse<FlaggedContentListItemDto>>> GetFlagsAsync(
        string? status, string? entityType, string? severity, string? q,
        int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.FlaggedContents.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(f => f.ModerationStatus == status);

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(f => f.ContentType == entityType);

        if (!string.IsNullOrWhiteSpace(severity))
            query = query.Where(f => f.Severity == severity);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(f =>
                f.FlagReason.Contains(q) ||
                (f.FlagDetails != null && f.FlagDetails.Contains(q)));

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        var items = await query
            .OrderByDescending(f => f.FlaggedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new FlaggedContentListItemDto
            {
                FlagId = f.FlagID,
                ContentType = f.ContentType,
                ContentId = f.ContentID,
                RelatedUserId = f.RelatedUserID,
                FlagReason = f.FlagReason,
                FlagSource = f.FlagSource,
                Severity = f.Severity,
                ModerationStatus = f.ModerationStatus,
                FlaggedAt = f.FlaggedAt
            })
            .ToListAsync();

        return ApiResponse<PagedResponse<FlaggedContentListItemDto>>.SuccessResponse(
            new PagedResponse<FlaggedContentListItemDto>
            {
                Items = items,
                Paging = new PagingInfo
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = totalPages
                }
            });
    }

    // ══════════════════════════════════════════════════════════════
    // 2) GET /flags/{id} — detail + actions
    // ══════════════════════════════════════════════════════════════

    public async Task<ApiResponse<FlaggedContentDetailDto>> GetFlagDetailAsync(int flagId)
    {
        var flag = await _db.FlaggedContents
            .AsNoTracking()
            .Include(f => f.RelatedUser)
            .Include(f => f.ModerationActions)
            .FirstOrDefaultAsync(f => f.FlagID == flagId);

        if (flag is null)
            return ApiResponse<FlaggedContentDetailDto>.ErrorResponse("FLAG_NOT_FOUND", "Flagged content not found.");

        return ApiResponse<FlaggedContentDetailDto>.SuccessResponse(MapDetail(flag));
    }

    // ══════════════════════════════════════════════════════════════
    // 3) POST /flags/{id}/assign — set InReview
    // ══════════════════════════════════════════════════════════════

    public async Task<ApiResponse<FlaggedContentDetailDto>> AssignAsync(int staffUserId, int flagId, string? note)
    {
        var flag = await _db.FlaggedContents
            .Include(f => f.ModerationActions)
            .Include(f => f.RelatedUser)
            .FirstOrDefaultAsync(f => f.FlagID == flagId);

        if (flag is null)
            return ApiResponse<FlaggedContentDetailDto>.ErrorResponse("FLAG_NOT_FOUND", "Flagged content not found.");

        // Only Pending can transition to InReview
        if (!string.Equals(flag.ModerationStatus, "Pending", StringComparison.OrdinalIgnoreCase))
            return ApiResponse<FlaggedContentDetailDto>.ErrorResponse("INVALID_STATUS_TRANSITION",
                $"Cannot assign flag with status '{flag.ModerationStatus}'. Only Pending flags can be assigned.");

        flag.ModerationStatus = "InReview";
        flag.ReviewedBy = staffUserId;
        if (!string.IsNullOrWhiteSpace(note))
            flag.ModeratorNotes = note;

        // Record action
        var action = new Domain.Entities.ModerationAction
        {
            FlagID = flagId,
            ActionType = "Assign",
            ActionTakenBy = staffUserId,
            ActionTakenAt = DateTime.UtcNow,
            ActionDetails = note
        };
        flag.ModerationActions.Add(action);

        await _db.SaveChangesAsync();
        await _audit.LogAsync("MODERATION_ASSIGN", "FlaggedContent", flagId, $"Staff {staffUserId} assigned flag");

        _logger.LogInformation("Flag {FlagId} assigned to review by staff {StaffId}", flagId, staffUserId);

        return ApiResponse<FlaggedContentDetailDto>.SuccessResponse(MapDetail(flag));
    }

    // ══════════════════════════════════════════════════════════════
    // 4) POST /flags/{id}/resolve
    // ══════════════════════════════════════════════════════════════

    public async Task<ApiResponse<FlaggedContentDetailDto>> ResolveAsync(
        int staffUserId, int flagId, string decision, string? note)
    {
        var flag = await _db.FlaggedContents
            .Include(f => f.ModerationActions)
            .Include(f => f.RelatedUser)
            .FirstOrDefaultAsync(f => f.FlagID == flagId);

        if (flag is null)
            return ApiResponse<FlaggedContentDetailDto>.ErrorResponse("FLAG_NOT_FOUND", "Flagged content not found.");

        // Only Pending or InReview can be resolved
        if (string.Equals(flag.ModerationStatus, "Resolved", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(flag.ModerationStatus, "Rejected", StringComparison.OrdinalIgnoreCase))
            return ApiResponse<FlaggedContentDetailDto>.ErrorResponse("INVALID_STATUS_TRANSITION",
                $"Flag is already '{flag.ModerationStatus}'. Cannot resolve again.");

        // Map decision to status
        var newStatus = decision switch
        {
            "RejectReport" => "Rejected",
            "MarkSafe" => "Resolved",
            "Resolved" => "Resolved",
            _ => "Resolved"
        };

        flag.ModerationStatus = newStatus;
        flag.ReviewedAt = DateTime.UtcNow;
        flag.ReviewedBy = staffUserId;
        flag.ModerationAction = decision;
        if (!string.IsNullOrWhiteSpace(note))
            flag.ModeratorNotes = note;

        // Record action
        var action = new Domain.Entities.ModerationAction
        {
            FlagID = flagId,
            ActionType = decision,
            ActionTakenBy = staffUserId,
            ActionTakenAt = DateTime.UtcNow,
            ActionDetails = note
        };
        flag.ModerationActions.Add(action);

        await _db.SaveChangesAsync();
        await _audit.LogAsync("MODERATION_RESOLVE", "FlaggedContent", flagId,
            $"Decision={decision}, Staff={staffUserId}");

        _logger.LogInformation("Flag {FlagId} resolved as {Decision} by staff {StaffId}",
            flagId, decision, staffUserId);

        return ApiResponse<FlaggedContentDetailDto>.SuccessResponse(MapDetail(flag));
    }

    // ══════════════════════════════════════════════════════════════
    // 5) POST /flags/{id}/actions — create moderation action
    // ══════════════════════════════════════════════════════════════

    public async Task<ApiResponse<ModerationActionDto>> CreateActionAsync(
        int staffUserId, int flagId, CreateModerationActionRequest request)
    {
        var flag = await _db.FlaggedContents
            .Include(f => f.RelatedUser)
            .FirstOrDefaultAsync(f => f.FlagID == flagId);

        if (flag is null)
            return ApiResponse<ModerationActionDto>.ErrorResponse("FLAG_NOT_FOUND", "Flagged content not found.");

        if (!AllowedActionTypes.Contains(request.ActionType))
            return ApiResponse<ModerationActionDto>.ErrorResponse("VALIDATION_ERROR",
                $"Invalid action type '{request.ActionType}'. Allowed: {string.Join(", ", AllowedActionTypes)}");

        DateTime? expiresAt = request.DurationDays.HasValue
            ? DateTime.UtcNow.AddDays(request.DurationDays.Value)
            : null;

        var action = new Domain.Entities.ModerationAction
        {
            FlagID = flagId,
            ActionType = request.ActionType,
            TargetUserID = flag.RelatedUserID,
            ActionDetails = request.ActionNote,
            ActionTakenBy = staffUserId,
            ActionTakenAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };
        flag.ModerationActions.Add(action);

        // ── Side effects ──────────────────────────────────────────
        string? sideEffectNote = null;

        if (string.Equals(request.ActionType, "LockUser", StringComparison.OrdinalIgnoreCase) &&
            flag.RelatedUserID.HasValue)
        {
            var targetUser = await _db.Users.FindAsync(flag.RelatedUserID.Value);
            if (targetUser != null)
            {
                targetUser.IsActive = false;
                sideEffectNote = $"User {targetUser.UserID} deactivated (IsActive=false).";
            }
        }
        else if (string.Equals(request.ActionType, "UnlockUser", StringComparison.OrdinalIgnoreCase) &&
                 flag.RelatedUserID.HasValue)
        {
            var targetUser = await _db.Users.FindAsync(flag.RelatedUserID.Value);
            if (targetUser != null)
            {
                targetUser.IsActive = true;
                sideEffectNote = $"User {targetUser.UserID} reactivated (IsActive=true).";
            }
        }
        else if (string.Equals(request.ActionType, "BanUser", StringComparison.OrdinalIgnoreCase) &&
                 flag.RelatedUserID.HasValue)
        {
            var targetUser = await _db.Users.FindAsync(flag.RelatedUserID.Value);
            if (targetUser != null)
            {
                targetUser.IsActive = false;
                sideEffectNote = $"User {targetUser.UserID} banned (IsActive=false).";
            }
        }
        // Hide/Remove: No visibility/isArchived field on generic entities — action recorded only.

        await _db.SaveChangesAsync();
        await _audit.LogAsync($"MODERATION_ACTION_{request.ActionType.ToUpperInvariant()}",
            "FlaggedContent", flagId,
            $"Staff={staffUserId}" + (sideEffectNote != null ? $", {sideEffectNote}" : ""));

        _logger.LogInformation("Action {ActionType} created on flag {FlagId} by staff {StaffId}. SideEffect: {SE}",
            request.ActionType, flagId, staffUserId, sideEffectNote ?? "none");

        var dto = MapActionDto(action);

        return ApiResponse<ModerationActionDto>.SuccessResponse(dto,
            sideEffectNote ?? "Action recorded.");
    }

    // ══════════════════════════════════════════════════════════════
    // 6) GET /flags/{id}/actions — action history
    // ══════════════════════════════════════════════════════════════

    public async Task<ApiResponse<List<ModerationActionDto>>> GetActionsAsync(int flagId)
    {
        var flagExists = await _db.FlaggedContents.AnyAsync(f => f.FlagID == flagId);
        if (!flagExists)
            return ApiResponse<List<ModerationActionDto>>.ErrorResponse("FLAG_NOT_FOUND",
                "Flagged content not found.");

        var actions = await _db.ModerationActions
            .AsNoTracking()
            .Where(a => a.FlagID == flagId)
            .OrderByDescending(a => a.ActionTakenAt)
            .Select(a => new ModerationActionDto
            {
                ActionId = a.ActionID,
                FlagId = a.FlagID,
                ActionType = a.ActionType,
                TargetUserId = a.TargetUserID,
                ActionDetails = a.ActionDetails,
                MessageToUser = a.MessageToUser,
                PerformedBy = a.ActionTakenBy,
                ActionTakenAt = a.ActionTakenAt,
                ExpiresAt = a.ExpiresAt
            })
            .ToListAsync();

        return ApiResponse<List<ModerationActionDto>>.SuccessResponse(actions);
    }

    // ══════════════════════════════════════════════════════════════
    // 7) POST /api/reports — user-initiated report
    // ══════════════════════════════════════════════════════════════

    public async Task<ApiResponse<FlaggedContentDetailDto>> CreateFlagAsync(int reporterUserId, CreateFlagRequest request)
    {
        var flag = new FlaggedContent
        {
            ContentType = request.EntityType,
            ContentID = request.EntityId,
            RelatedUserID = null, // Could be populated based on entity lookup
            FlagReason = request.Reason,
            FlagSource = "UserReport",
            FlagDetails = request.Description,
            ModerationStatus = "Pending",
            FlaggedAt = DateTime.UtcNow
        };

        // Try to identify the RelatedUserID based on reported entity
        // (optional: could look up the owner of the entity)

        _db.FlaggedContents.Add(flag);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("CREATE_REPORT", "FlaggedContent", flag.FlagID,
            $"Reporter={reporterUserId}, Entity={request.EntityType}:{request.EntityId}");

        _logger.LogInformation("User {UserId} reported {EntityType}:{EntityId} -> Flag {FlagId}",
            reporterUserId, request.EntityType, request.EntityId, flag.FlagID);

        // Re-fetch with includes for consistent response
        var detail = await _db.FlaggedContents
            .AsNoTracking()
            .Include(f => f.RelatedUser)
            .Include(f => f.ModerationActions)
            .FirstAsync(f => f.FlagID == flag.FlagID);

        return ApiResponse<FlaggedContentDetailDto>.SuccessResponse(MapDetail(detail));
    }

    // ══════════════════════════════════════════════════════════════
    // 8) GET /api/reports/me — user's own reports
    // ══════════════════════════════════════════════════════════════

    public Task<ApiResponse<PagedResponse<FlaggedContentListItemDto>>> GetMyReportsAsync(
        int userId, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        // FlaggedContent does not have ReportedByUserId field.
        // The entity uses RelatedUserID (target user, not reporter), and FlagSource
        // for the source type. DB does not store reporter user ID.
        // We'll filter by FlagSource="UserReport" and return all for staff,
        // but for per-user we lack the field. Return empty with message.
        // NOTE: If a reporter field is added later, update this filter.

        // Since DB schema has no ReporterUserId, we cannot filter per-user.
        // Return 501 to indicate the limitation.
        return Task.FromResult(ApiResponse<PagedResponse<FlaggedContentListItemDto>>.ErrorResponse(
            "NOT_IMPLEMENTED",
            "User-specific report history is not supported: the database schema lacks a ReporterUserId field on FlaggedContent."));
    }

    // ══════════════════════════════════════════════════════════════
    // Private mapping helpers
    // ══════════════════════════════════════════════════════════════

    private static FlaggedContentDetailDto MapDetail(FlaggedContent f) => new()
    {
        FlagId = f.FlagID,
        ContentType = f.ContentType,
        ContentId = f.ContentID,
        RelatedUserId = f.RelatedUserID,
        RelatedUserEmail = f.RelatedUser?.Email,
        FlagReason = f.FlagReason,
        FlagSource = f.FlagSource,
        Severity = f.Severity,
        FlagDetails = f.FlagDetails,
        ModerationStatus = f.ModerationStatus,
        FlaggedAt = f.FlaggedAt,
        ReviewedAt = f.ReviewedAt,
        ReviewedBy = f.ReviewedBy,
        ModerationAction = f.ModerationAction,
        ModeratorNotes = f.ModeratorNotes,
        Actions = f.ModerationActions
            .OrderByDescending(a => a.ActionTakenAt)
            .Select(MapActionDto)
            .ToList()
    };

    private static ModerationActionDto MapActionDto(Domain.Entities.ModerationAction a) => new()
    {
        ActionId = a.ActionID,
        FlagId = a.FlagID,
        ActionType = a.ActionType,
        TargetUserId = a.TargetUserID,
        ActionDetails = a.ActionDetails,
        MessageToUser = a.MessageToUser,
        PerformedBy = a.ActionTakenBy,
        ActionTakenAt = a.ActionTakenAt,
        ExpiresAt = a.ExpiresAt
    };
}
