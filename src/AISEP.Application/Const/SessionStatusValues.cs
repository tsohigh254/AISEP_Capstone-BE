namespace AISEP.Application.Const;

/// <summary>
/// Taxonomy cố định cho MentorshipSession.SessionStatus.
/// </summary>
public static class SessionStatusValues
{
    /// <summary>Slot do startup đề xuất khi tạo request — chờ advisor phản hồi.</summary>
    public const string ProposedByStartup = "ProposedByStartup";

    /// <summary>Slot do advisor đề xuất lại khi không hợp lịch startup — chờ startup chọn và confirm.</summary>
    public const string ProposedByAdvisor = "ProposedByAdvisor";

    /// <summary>Startup đã confirm slot → lịch chốt. Advisor đã lên lịch trực tiếp (không qua đề xuất). Có ScheduledStartAt và MeetingURL.</summary>
    public const string Scheduled = "Scheduled";

    /// <summary>Session đang diễn ra.</summary>
    public const string InProgress = "InProgress";

    /// <summary>Session đã hoàn thành — advisor có thể đã viết report.</summary>
    public const string Completed = "Completed";

    /// <summary>Session đã bị hủy.</summary>
    public const string Cancelled = "Cancelled";

    /// <summary>Startup đã xác nhận buổi tư vấn diễn ra. Prerequisite cho staff mark completed.</summary>
    public const string Conducted = "Conducted";

    /// <summary>Staff đánh dấu session đang tranh chấp.</summary>
    public const string InDispute = "InDispute";

    /// <summary>Staff đã giải quyết tranh chấp (không restore completed).</summary>
    public const string Resolved = "Resolved";

    /// <summary>Tập hợp tất cả giá trị hợp lệ. Dùng để validate input từ advisor.</summary>
    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        ProposedByStartup,
        ProposedByAdvisor,
        Scheduled,
        InProgress,
        Completed,
        Cancelled,
        Conducted,
        InDispute,
        Resolved
    };
}
