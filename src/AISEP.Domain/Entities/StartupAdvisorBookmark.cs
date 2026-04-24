namespace AISEP.Domain.Entities;

/// <summary>
/// Represents a startup bookmarking / saving an advisor for later reference.
/// Unique constraint on (StartupID, AdvisorID) prevents duplicate bookmarks.
/// </summary>
public class StartupAdvisorBookmark
{
    public int BookmarkID { get; set; }
    public int StartupID { get; set; }
    public int AdvisorID { get; set; }

    /// <summary>UserID of the startup owner who created the bookmark.</summary>
    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Startup Startup { get; set; } = null!;
    public Advisor Advisor { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
}
