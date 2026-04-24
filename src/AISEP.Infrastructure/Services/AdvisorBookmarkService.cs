using AISEP.Application.DTOs.Bookmark;
using AISEP.Application.DTOs.Common;
using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Domain.Enums;
using AISEP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AISEP.Infrastructure.Services;

public class AdvisorBookmarkService : IAdvisorBookmarkService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly ILogger<AdvisorBookmarkService> _logger;

    public AdvisorBookmarkService(
        ApplicationDbContext db,
        IAuditService audit,
        ILogger<AdvisorBookmarkService> logger)
    {
        _db = db;
        _audit = audit;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════
    // POST — Create bookmark
    // ═══════════════════════════════════════════════════════════════

    public async Task<ApiResponse<AdvisorBookmarkDto>> CreateBookmarkAsync(int userId, int advisorId)
    {
        // 1. Resolve startup + user in one query
        var startup = await _db.Startups
            .Include(s => s.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserID == userId);

        if (startup == null)
            return ApiResponse<AdvisorBookmarkDto>.ErrorResponse(
                "STARTUP_PROFILE_NOT_FOUND",
                "Startup profile not found. Please create a startup profile first.");

        // 2. Email must be verified (MSG012)
        if (!startup.User.EmailVerified)
            return ApiResponse<AdvisorBookmarkDto>.ErrorResponse(
                "EMAIL_NOT_VERIFIED",
                "Your email address has not been verified. Please verify your email before bookmarking advisors.");

        // 3. KYC / profile must be approved (MSG005)
        if (startup.ProfileStatus != ProfileStatus.Approved)
            return ApiResponse<AdvisorBookmarkDto>.ErrorResponse(
                "STARTUP_KYC_NOT_APPROVED",
                "Your startup profile must be KYC-approved before you can bookmark advisors.");

        // 4. Advisor must exist and be publicly visible (MSG077)
        var advisor = await _db.Advisors
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AdvisorID == advisorId);

        if (advisor == null ||
            (advisor.ProfileStatus != ProfileStatus.Approved &&
             advisor.ProfileStatus != ProfileStatus.PendingKYC))
            return ApiResponse<AdvisorBookmarkDto>.ErrorResponse(
                "ADVISOR_NOT_FOUND",
                $"Advisor with ID {advisorId} was not found or is not available.");

        // 5. Duplicate check (MSG004)
        var alreadyExists = await _db.StartupAdvisorBookmarks
            .AnyAsync(b => b.StartupID == startup.StartupID && b.AdvisorID == advisorId);

        if (alreadyExists)
            return ApiResponse<AdvisorBookmarkDto>.ErrorResponse(
                "BOOKMARK_ALREADY_EXISTS",
                "You have already bookmarked this advisor.");

        // 6. Create bookmark
        var bookmark = new StartupAdvisorBookmark
        {
            StartupID = startup.StartupID,
            AdvisorID = advisorId,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _db.StartupAdvisorBookmarks.Add(bookmark);
        await _db.SaveChangesAsync();

        // 7. Audit log
        await _audit.LogAsync(
            "STARTUP_BOOKMARK_ADVISOR_CREATED",
            "StartupAdvisorBookmark",
            bookmark.BookmarkID,
            $"StartupId={startup.StartupID}, AdvisorId={advisorId}, Timestamp={bookmark.CreatedAt:O}");

        _logger.LogInformation(
            "Startup {StartupId} bookmarked advisor {AdvisorId} (BookmarkId={BookmarkId})",
            startup.StartupID, advisorId, bookmark.BookmarkID);

        return ApiResponse<AdvisorBookmarkDto>.SuccessResponse(
            new AdvisorBookmarkDto
            {
                BookmarkId = bookmark.BookmarkID,
                AdvisorId = advisorId,
                CreatedAt = bookmark.CreatedAt,
                IsBookmarked = true
            },
            "Advisor bookmarked successfully.");
    }

    // ═══════════════════════════════════════════════════════════════
    // GET — Paginated bookmark list with advisor info
    // ═══════════════════════════════════════════════════════════════

    public async Task<ApiResponse<PagedResponse<AdvisorBookmarkListItemDto>>> GetMyBookmarksAsync(
        int userId, int page, int pageSize)
    {
        var startup = await _db.Startups
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserID == userId);

        if (startup == null)
            return ApiResponse<PagedResponse<AdvisorBookmarkListItemDto>>.ErrorResponse(
                "STARTUP_PROFILE_NOT_FOUND",
                "Startup profile not found.");

        var query = _db.StartupAdvisorBookmarks
            .AsNoTracking()
            .Where(b => b.StartupID == startup.StartupID)
            .Include(b => b.Advisor)
            .OrderByDescending(b => b.CreatedAt);

        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new AdvisorBookmarkListItemDto
            {
                BookmarkId = b.BookmarkID,
                AdvisorId = b.AdvisorID,
                FullName = b.Advisor.FullName,
                Title = b.Advisor.Title,
                ProfilePhotoURL = b.Advisor.ProfilePhotoURL,
                AverageRating = b.Advisor.AverageRating,
                ReviewCount = b.Advisor.ReviewCount,
                YearsOfExperience = b.Advisor.YearsOfExperience,
                IsVerified = b.Advisor.IsVerified,
                HourlyRate = b.Advisor.HourlyRate,
                Expertise = b.Advisor.Expertise != null
                    ? b.Advisor.Expertise.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(e => e.Trim()).ToList()
                    : new List<string>(),
                CreatedAt = b.CreatedAt,
                IsBookmarked = true
            })
            .ToListAsync();

        return ApiResponse<PagedResponse<AdvisorBookmarkListItemDto>>.SuccessResponse(
            new PagedResponse<AdvisorBookmarkListItemDto>
            {
                Items = items,
                Paging = new PagingInfo
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = total
                }
            });
    }

    // ═══════════════════════════════════════════════════════════════
    // GET — IDs only  (bulk saved-status check)
    // ═══════════════════════════════════════════════════════════════

    public async Task<ApiResponse<BookmarkedAdvisorIdsDto>> GetBookmarkedAdvisorIdsAsync(int userId)
    {
        var startup = await _db.Startups
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserID == userId);

        if (startup == null)
            return ApiResponse<BookmarkedAdvisorIdsDto>.SuccessResponse(
                new BookmarkedAdvisorIdsDto { AdvisorIds = new List<int>() });

        var ids = await _db.StartupAdvisorBookmarks
            .AsNoTracking()
            .Where(b => b.StartupID == startup.StartupID)
            .Select(b => b.AdvisorID)
            .ToListAsync();

        return ApiResponse<BookmarkedAdvisorIdsDto>.SuccessResponse(
            new BookmarkedAdvisorIdsDto { AdvisorIds = ids });
    }

    // ═══════════════════════════════════════════════════════════════
    // DELETE — Remove bookmark (unsave)
    // ═══════════════════════════════════════════════════════════════

    public async Task<ApiResponse<bool>> DeleteBookmarkAsync(int userId, int advisorId)
    {
        var startup = await _db.Startups
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserID == userId);

        if (startup == null)
            return ApiResponse<bool>.ErrorResponse(
                "STARTUP_PROFILE_NOT_FOUND",
                "Startup profile not found.");

        var bookmark = await _db.StartupAdvisorBookmarks
            .FirstOrDefaultAsync(b => b.StartupID == startup.StartupID && b.AdvisorID == advisorId);

        if (bookmark == null)
            return ApiResponse<bool>.ErrorResponse(
                "BOOKMARK_NOT_FOUND",
                "Bookmark not found.");

        _db.StartupAdvisorBookmarks.Remove(bookmark);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(
            "STARTUP_BOOKMARK_ADVISOR_DELETED",
            "StartupAdvisorBookmark",
            bookmark.BookmarkID,
            $"StartupId={startup.StartupID}, AdvisorId={advisorId}");

        _logger.LogInformation(
            "Startup {StartupId} removed bookmark for advisor {AdvisorId}",
            startup.StartupID, advisorId);

        return ApiResponse<bool>.SuccessResponse(true, "Bookmark removed successfully.");
    }
}
