namespace AISEP.Application.DTOs.Bookmark;

// ─────────────────────────────────────────────────────────────────────────────
// Request
// ─────────────────────────────────────────────────────────────────────────────

public class BookmarkAdvisorRequest
{
    public int AdvisorId { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// Response – single bookmark (POST response)
// ─────────────────────────────────────────────────────────────────────────────

public class AdvisorBookmarkDto
{
    public int BookmarkId { get; set; }
    public int AdvisorId { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsBookmarked { get; set; } = true;
}

// ─────────────────────────────────────────────────────────────────────────────
// Response – list item (GET /me/advisor-bookmarks)
// ─────────────────────────────────────────────────────────────────────────────

public class AdvisorBookmarkListItemDto
{
    public int BookmarkId { get; set; }
    public int AdvisorId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? ProfilePhotoURL { get; set; }
    public float? AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int? YearsOfExperience { get; set; }
    public bool IsVerified { get; set; }
    public decimal? HourlyRate { get; set; }
    public List<string> Expertise { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public bool IsBookmarked { get; set; } = true;
    public string AvailabilityHint { get; set; } = "Available"; // "Available" | "Not available"
}

// ─────────────────────────────────────────────────────────────────────────────
// Response – IDs only (GET /me/advisor-bookmarks/ids)
// ─────────────────────────────────────────────────────────────────────────────

public class BookmarkedAdvisorIdsDto
{
    public List<int> AdvisorIds { get; set; } = new();
}
