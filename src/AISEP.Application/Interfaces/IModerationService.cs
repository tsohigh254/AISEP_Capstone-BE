using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Moderation;

namespace AISEP.Application.Interfaces;

public interface IModerationService
{
    // ── Flags (Staff / Admin) ────────────────────────────────────
    Task<ApiResponse<PagedResponse<FlaggedContentListItemDto>>> GetFlagsAsync(
        string? status, string? entityType, string? severity, string? q,
        int page, int pageSize);

    Task<ApiResponse<FlaggedContentDetailDto>> GetFlagDetailAsync(int flagId);

    Task<ApiResponse<FlaggedContentDetailDto>> AssignAsync(int staffUserId, int flagId, string? note);

    Task<ApiResponse<FlaggedContentDetailDto>> ResolveAsync(int staffUserId, int flagId, string decision, string? note);

    // ── Actions ──────────────────────────────────────────────────
    Task<ApiResponse<ModerationActionDto>> CreateActionAsync(int staffUserId, int flagId, CreateModerationActionRequest request);

    Task<ApiResponse<List<ModerationActionDto>>> GetActionsAsync(int flagId);

    // ── User-initiated reports ───────────────────────────────────
    Task<ApiResponse<FlaggedContentDetailDto>> CreateFlagAsync(int reporterUserId, CreateFlagRequest request);

    Task<ApiResponse<PagedResponse<FlaggedContentListItemDto>>> GetMyReportsAsync(int userId, int page, int pageSize);
}
