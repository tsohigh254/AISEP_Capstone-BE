using AISEP.Application.DTOs.Bookmark;
using AISEP.Application.DTOs.Common;

namespace AISEP.Application.Interfaces;

public interface IAdvisorBookmarkService
{
    /// <summary>
    /// Bookmark an advisor for the current startup user.
    /// Validates email verification, KYC approval, advisor existence, and duplicate.
    /// </summary>
    Task<ApiResponse<AdvisorBookmarkDto>> CreateBookmarkAsync(int userId, int advisorId);

    /// <summary>
    /// Return the paginated list of advisors bookmarked by the current startup user.
    /// </summary>
    Task<ApiResponse<PagedResponse<AdvisorBookmarkListItemDto>>> GetMyBookmarksAsync(int userId, int page, int pageSize);

    /// <summary>
    /// Return only the list of advisor IDs bookmarked by the current startup user.
    /// Useful for FE to check saved status without fetching full advisor data.
    /// </summary>
    Task<ApiResponse<BookmarkedAdvisorIdsDto>> GetBookmarkedAdvisorIdsAsync(int userId);

    /// <summary>
    /// Remove an advisor bookmark (unsave).
    /// </summary>
    Task<ApiResponse<bool>> DeleteBookmarkAsync(int userId, int advisorId);
}
