using AISEP.Application.Const;
using AISEP.Application.Configuration;
using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Mentorship;
using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Domain.Enums;
using AISEP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AISEP.Application.DTOs.Notification;


namespace AISEP.Infrastructure.Services;

public class MentorshipService : IMentorshipService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly ILogger<MentorshipService> _logger;
    private readonly INotificationDeliveryService _notifications;
    private readonly ICloudinaryService _cloudinary;

    public MentorshipService(ApplicationDbContext db, IAuditService audit, ILogger<MentorshipService> logger, INotificationDeliveryService notifications, ICloudinaryService cloudinary)
    {
        _db = db;
        _audit = audit;
        _logger = logger;
        _notifications = notifications;
        _cloudinary = cloudinary;
    }

    // ================================================================
    // CREATE MENTORSHIP REQUEST (Startup)
    // ================================================================

    public async Task<ApiResponse<MentorshipDto>> CreateRequestAsync(int userId, CreateMentorshipRequest request)
    {
        // Startup must have a profile
        var startup = await _db.Startups.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserID == userId);
        if (startup == null)
            return ApiResponse<MentorshipDto>.ErrorResponse("STARTUP_PROFILE_NOT_FOUND",
                "You must create a startup profile first.");

        // Check subscription limits for Startup initiating mentorships
        var totalRequests = await _db.StartupAdvisorMentorships.CountAsync(m => m.StartupID == startup.StartupID);
        int maxRequests = startup.SubscriptionPlan switch
        {
            StartupSubscriptionPlan.Free => 2,
            StartupSubscriptionPlan.Pro => 10,
            StartupSubscriptionPlan.Fundraising => int.MaxValue,
            _ => 2
        };
        if (totalRequests >= maxRequests)
        {
            return ApiResponse<MentorshipDto>.ErrorResponse("SUBSCRIPTION_LIMIT_REACHED", 
                $"Your current subscription plan ({startup.SubscriptionPlan}) allows a maximum of {maxRequests} advisor consultation requests. Please upgrade your plan.");
        }

        // Advisor must exist, be accepting requests, and have both meeting links configured
        var advisor = await _db.Advisors
            .Include(a => a.Availability)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AdvisorID == request.AdvisorId);
        if (advisor == null)
            return ApiResponse<MentorshipDto>.ErrorResponse("ADVISOR_NOT_FOUND",
                $"Advisor with id {request.AdvisorId} not found.");
        if (advisor.Availability == null || !advisor.Availability.IsAcceptingNewMentees)
            return ApiResponse<MentorshipDto>.ErrorResponse("ADVISOR_NOT_ACCEPTING",
                "This advisor is not currently accepting new consultation requests.");
        if (string.IsNullOrWhiteSpace(advisor.GoogleMeetLink) || string.IsNullOrWhiteSpace(advisor.MsTeamsLink))
            return ApiResponse<MentorshipDto>.ErrorResponse("ADVISOR_MEETING_LINKS_MISSING",
                "This advisor has not set up all required meeting links.");

        // Check for existing pending/active mentorship with same advisor
        var duplicate = await _db.StartupAdvisorMentorships.AnyAsync(m =>
            m.StartupID == startup.StartupID &&
            m.AdvisorID == request.AdvisorId &&
            (m.MentorshipStatus == MentorshipStatus.Requested || m.MentorshipStatus == MentorshipStatus.Accepted || m.MentorshipStatus == MentorshipStatus.InProgress));
        if (duplicate)
            return ApiResponse<MentorshipDto>.ErrorResponse("MENTORSHIP_ALREADY_EXISTS",
                "An active or pending mentorship with this advisor already exists.");

        var mentorship = new StartupAdvisorMentorship
        {
            StartupID = startup.StartupID,
            AdvisorID = request.AdvisorId,
            MentorshipStatus = MentorshipStatus.Requested,
            ChallengeDescription = request.ChallengeDescription,
            SpecificQuestions = request.SpecificQuestions,
            PreferredFormat = request.PreferredFormat,
            ExpectedDuration = request.ExpectedDuration,
            ExpectedScope = request.ExpectedScope,
            RequestedAt = DateTime.UtcNow,
            LastUpdatedByRole = "Startup",
            CreatedAt = DateTime.UtcNow
        };

        if (request.RequestedSlots != null && request.RequestedSlots.Count > 0)
        {
            foreach (var slot in request.RequestedSlots)
            {
                mentorship.Sessions.Add(new MentorshipSession
                {   
                    ScheduledStartAt = slot.StartAt.ToUniversalTime(),
                    DurationMinutes = (int)(slot.EndAt - slot.StartAt).TotalMinutes,
                    SessionStatus = "ProposedByStartup",
                    ProposedBy = "Startup",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        _db.StartupAdvisorMentorships.Add(mentorship);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("CREATE_MENTORSHIP_REQUEST", "StartupAdvisorMentorship", mentorship.MentorshipID,
            $"StartupId={startup.StartupID}, AdvisorId={request.AdvisorId}");
        _logger.LogInformation("Mentorship {MentorshipId} requested by startup {StartupId} for advisor {AdvisorId}",
            mentorship.MentorshipID, startup.StartupID, request.AdvisorId);

        // Notify Advisor (reuse already-fetched advisor entity)
        if (advisor != null)
        {
            await _notifications.CreateAndPushAsync(new CreateNotificationRequest
            {
                UserId = advisor.UserID,
                NotificationType = "CONSULTING",
                Title = "Yêu cầu tư vấn mới",
                Message = $"Startup '{startup.CompanyName}' đã gửi yêu cầu tư vấn cho bạn.",
                RelatedEntityType = "Mentorship",
                RelatedEntityId = mentorship.MentorshipID,
                ActionUrl = $"/advisor/requests/{mentorship.MentorshipID}"
            });
        }

        return ApiResponse<MentorshipDto>.SuccessResponse(MapToDto(mentorship));
    }

    // ================================================================
    // LIST MY MENTORSHIPS (Startup/Advisor)
    // ================================================================

    public async Task<ApiResponse<PagedResponse<MentorshipListItemDto>>> GetMyMentorshipsAsync(
        int userId, string userType, string? status, int page, int pageSize, bool? isPayoutEligible = null)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        IQueryable<StartupAdvisorMentorship> query = _db.StartupAdvisorMentorships
            .AsNoTracking()
            .Include(m => m.Startup).ThenInclude(s => s.Industry)
            .Include(m => m.Startup).ThenInclude(s => s.StageRef)
            .Include(m => m.Advisor)
            .Include(m => m.Reports)
            .Include(m => m.Sessions);

        if (string.Equals(userType, "Startup", StringComparison.OrdinalIgnoreCase))
        {
            var startup = await _db.Startups.AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserID == userId);
            if (startup == null)
                return ApiResponse<PagedResponse<MentorshipListItemDto>>.ErrorResponse(
                    "STARTUP_PROFILE_NOT_FOUND", "Startup profile not found.");
            query = query.Where(m => m.StartupID == startup.StartupID);
        }
        else if (string.Equals(userType, "Advisor", StringComparison.OrdinalIgnoreCase))
        {
            var advisor = await _db.Advisors.AsNoTracking()
                .FirstOrDefaultAsync(a => a.UserID == userId);
            if (advisor == null)
                return ApiResponse<PagedResponse<MentorshipListItemDto>>.ErrorResponse(
                    "ADVISOR_PROFILE_NOT_FOUND", "Advisor profile not found.");
            query = query.Where(m => m.AdvisorID == advisor.AdvisorID);
        }
        else if (string.Equals(userType, "Staff", StringComparison.OrdinalIgnoreCase) || 
                 string.Equals(userType, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            // Staff/Admin: see all
        }
        else
        {
            return ApiResponse<PagedResponse<MentorshipListItemDto>>.ErrorResponse(
                "ACCESS_DENIED", "Only Startup, Advisor, Staff, or Admin can view mentorships.");
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<MentorshipStatus>(status, true, out var statusEnum))
            query = query.Where(m => m.MentorshipStatus == statusEnum);

        if (isPayoutEligible.HasValue)
        {
            // Self-healing: For staff viewing payout-eligible, ensure any "stuck" mentorships are recalculated
            // (e.g. ones that were acknowledged before our recent auto-completion fix)
            if (isPayoutEligible.Value && (string.Equals(userType, "Staff", StringComparison.OrdinalIgnoreCase) || string.Equals(userType, "Admin", StringComparison.OrdinalIgnoreCase)))
            {
                var potentiallyStuck = await _db.StartupAdvisorMentorships
                    .Include(m => m.Sessions)
                    .Include(m => m.Reports)
                    .Where(m => !m.IsPayoutEligible && m.Reports.Any(r => r.StartupAcknowledgedAt != null))
                    .ToListAsync();

                if (potentiallyStuck.Any())
                {
                    foreach (var m in potentiallyStuck)
                    {
                        // Also force sessions to Completed if they have an acknowledged report
                        foreach (var s in m.Sessions)
                        {
                            var acknowledgedReport = m.Reports.FirstOrDefault(r => r.SessionID == s.SessionID && r.StartupAcknowledgedAt != null);
                            if (acknowledgedReport != null)
                            {
                                s.SessionStatus = SessionStatusValues.Completed;
                                s.StartupConfirmedConductedAt ??= acknowledgedReport.StartupAcknowledgedAt;
                                s.UpdatedAt = DateTime.UtcNow;
                            }
                        }
                        RecalculateMentorshipStatus(m);
                        RecalculatePayoutEligibility(m);
                    }
                    await _db.SaveChangesAsync();
                }
            }
            query = query.Where(m => m.IsPayoutEligible == isPayoutEligible.Value);
        }

        query = query.OrderByDescending(m => m.CreatedAt);

        var totalItems = await query.CountAsync();

        var queryItems = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = queryItems.Select(m => new MentorshipListItemDto
            {
                MentorshipID = m.MentorshipID,
                StartupID = m.StartupID,
                StartupName = m.Startup?.CompanyName ?? "Unknown",
                StartupLogoUrl = m.Startup?.LogoURL,
                StartupIndustry = m.Startup?.Industry?.IndustryName,
                StartupStage = m.Startup?.StageRef?.StageName,
                AdvisorID = m.AdvisorID,
                AdvisorName = m.Advisor?.FullName ?? "Unknown",
                AdvisorTitle = m.Advisor?.Title,
                AdvisorPhotoURL = m.Advisor?.ProfilePhotoURL,
                Status = m.MentorshipStatus.ToString(),
                ChallengeDescription = m.ChallengeDescription,
                PreferredFormat = m.PreferredFormat,
                RequestedAt = m.RequestedAt,
                CreatedAt = m.CreatedAt,
                HasReport = m.Reports.Any(),
                ReportCount = m.Reports.Count,
                LatestReportSubmittedAt = m.Reports.Any()
                    ? m.Reports.Max(r => r.SubmittedAt)
                    : null,
                HasAdvisorProposedSlot = m.Sessions.Any(s => s.SessionStatus == SessionStatusValues.ProposedByAdvisor),
                SessionAmount = m.SessionAmount,
                PlatformFeeAmount = m.PlatformFeeAmount,
                ActualAmount = m.ActualAmount,
                IsPayoutEligible = m.IsPayoutEligible,
                PayoutReleasedAt = m.PayoutReleasedAt,
                ExpectedDuration = m.ExpectedDuration,
                AdvisorHourlyRate = m.Advisor?.HourlyRate,
            })
            .ToList();

        return ApiResponse<PagedResponse<MentorshipListItemDto>>.SuccessResponse(
            new PagedResponse<MentorshipListItemDto>
            {
                Items = items,
                Paging = new PagingInfo
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                }
            });
    }

    // ================================================================
    // GET MENTORSHIP DETAIL
    // ================================================================

    public async Task<ApiResponse<MentorshipDetailDto>> GetDetailAsync(int userId, string userType, int mentorshipId)
    {
        var mentorship = await _db.StartupAdvisorMentorships
            .AsNoTracking()
            .AsSplitQuery()
            .Include(m => m.Startup).ThenInclude(s => s.Industry)
            .Include(m => m.Startup).ThenInclude(s => s.StageRef)
            .Include(m => m.Advisor)
            .Include(m => m.Sessions.OrderByDescending(s => s.ScheduledStartAt))
            .Include(m => m.Reports.OrderByDescending(r => r.CreatedAt))
            .Include(m => m.Feedbacks.OrderByDescending(f => f.SubmittedAt))
            .FirstOrDefaultAsync(m => m.MentorshipID == mentorshipId);

        if (mentorship == null)
            return ApiResponse<MentorshipDetailDto>.ErrorResponse("MENTORSHIP_NOT_FOUND",
                "Mentorship not found.");

        // Ownership check
        if (!await IsParticipantOrStaff(userId, userType, mentorship))
            return ApiResponse<MentorshipDetailDto>.ErrorResponse("MENTORSHIP_NOT_OWNED",
                "You do not have access to this mentorship.");

        return ApiResponse<MentorshipDetailDto>.SuccessResponse(MapToDetailDto(mentorship, userType));
    }

    public async Task<ApiResponse<MentorshipDetailDto>> GetMentorshipBySessionIdAsync(int userId, string userType, int sessionId)
    {
        var session = await _db.MentorshipSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SessionID == sessionId);

        if (session == null)
            return ApiResponse<MentorshipDetailDto>.ErrorResponse("SESSION_NOT_FOUND", "Session not found.");

        return await GetDetailAsync(userId, userType, session.MentorshipID);
    }

    public async Task<ApiResponse<MentorshipDetailDto>> GetMentorshipByReportIdAsync(int userId, string userType, int reportId)
    {
        var report = await _db.MentorshipReports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ReportID == reportId);

        if (report == null)
            return ApiResponse<MentorshipDetailDto>.ErrorResponse("REPORT_NOT_FOUND", "Mentorship report not found.");

        return await GetDetailAsync(userId, userType, report.MentorshipID);
    }

    // ================================================================
    // ACCEPT MENTORSHIP (Advisor)
    // ================================================================

    public async Task<ApiResponse<MentorshipDto>> AcceptAsync(int userId, int mentorshipId)
    {
        var (mentorship, error) = await GetMentorshipForAdvisor(userId, mentorshipId);
        if (mentorship == null) return error!;
        
        if (mentorship.MentorshipStatus != MentorshipStatus.Requested)
            return ApiResponse<MentorshipDto>.ErrorResponse("INVALID_STATUS_TRANSITION",
                $"Cannot accept mentorship with status '{mentorship.MentorshipStatus}'. Only 'Requested' can be accepted.");

        mentorship.MentorshipStatus = MentorshipStatus.Accepted;
        mentorship.AcceptedAt = DateTime.UtcNow;
        mentorship.LastUpdatedByRole = "Advisor";
        mentorship.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _audit.LogAsync("ACCEPT_MENTORSHIP", "StartupAdvisorMentorship", mentorshipId, null);
        _logger.LogInformation("Mentorship {MentorshipId} accepted by advisor", mentorshipId);

        // Notify Startup
        var startup = await _db.Startups.AsNoTracking().FirstOrDefaultAsync(s => s.StartupID == mentorship.StartupID);
        if (startup != null)
        {
            var advisorName = await _db.Advisors.AsNoTracking()
                .Where(a => a.AdvisorID == mentorship.AdvisorID)
                .Select(a => a.FullName)
                .FirstOrDefaultAsync();

            await _notifications.CreateAndPushAsync(new CreateNotificationRequest
            {
                UserId = startup.UserID,
                NotificationType = "CONSULTING",
                Title = "Yêu cầu tư vấn được chấp nhận",
                Message = $"Advisor {advisorName} đã chấp nhận yêu cầu tư vấn của bạn.",
                RelatedEntityType = "Mentorship",
                RelatedEntityId = mentorshipId,
                ActionUrl = $"/startup/mentorship-requests/{mentorshipId}"
            });
        }

        return ApiResponse<MentorshipDto>.SuccessResponse(MapToDto(mentorship));
    }

    // ================================================================
    // REJECT MENTORSHIP (Advisor)
    // ================================================================

    public async Task<ApiResponse<MentorshipDto>> RejectAsync(int userId, int mentorshipId, string? reason)
    {
        var (mentorship, error) = await GetMentorshipForAdvisor(userId, mentorshipId);
        if (mentorship == null) return error!;

        if (mentorship.MentorshipStatus != MentorshipStatus.Requested)
            return ApiResponse<MentorshipDto>.ErrorResponse("INVALID_STATUS_TRANSITION",
                $"Cannot reject mentorship with status '{mentorship.MentorshipStatus}'. Only 'Requested' can be rejected.");

        mentorship.MentorshipStatus = MentorshipStatus.Rejected;
        mentorship.RejectedAt = DateTime.UtcNow;
        mentorship.RejectedReason = reason;
        mentorship.LastUpdatedByRole = "Advisor";
        mentorship.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _audit.LogAsync("REJECT_MENTORSHIP", "StartupAdvisorMentorship", mentorshipId,
            $"Reason={reason}");
        _logger.LogInformation("Mentorship {MentorshipId} rejected by advisor", mentorshipId);

        // Notify Startup
        var startup = await _db.Startups.AsNoTracking().FirstOrDefaultAsync(s => s.StartupID == mentorship.StartupID);
        if (startup != null)
        {
            var advisorName = await _db.Advisors.AsNoTracking()
                .Where(a => a.AdvisorID == mentorship.AdvisorID)
                .Select(a => a.FullName)
                .FirstOrDefaultAsync();

            await _notifications.CreateAndPushAsync(new CreateNotificationRequest
            {
                UserId = startup.UserID,
                NotificationType = "CONSULTING",
                Title = "Yêu cầu tư vấn bị từ chối",
                Message = $"Advisor {advisorName} đã từ chối yêu cầu tư vấn của bạn. Lý do: {reason ?? "Không có lý do cụ thể."}",
                RelatedEntityType = "Mentorship",
                RelatedEntityId = mentorshipId,
                ActionUrl = $"/startup/mentorship-requests/{mentorshipId}"
            });
        }

        return ApiResponse<MentorshipDto>.SuccessResponse(MapToDto(mentorship));
    }

    public async Task<ApiResponse<MentorshipDto>> CancelAsync(int userId, int mentorshipId, string? reason)
    {
        var startup = await _db.Startups.FirstOrDefaultAsync(s => s.UserID == userId);
        var advisor = await _db.Advisors.FirstOrDefaultAsync(a => a.UserID == userId);
        
        if (startup == null && advisor == null) 
            return ApiResponse<MentorshipDto>.ErrorResponse("UNAUTHORIZED", "User must be a startup or advisor.");

        var mentorship = await _db.StartupAdvisorMentorships
            .FirstOrDefaultAsync(m => m.MentorshipID == mentorshipId && 
            (startup != null ? m.StartupID == startup.StartupID : m.AdvisorID == advisor!.AdvisorID));

        if (mentorship == null) return ApiResponse<MentorshipDto>.ErrorResponse("NOT_FOUND", "Mentorship not found.");

        if (mentorship.MentorshipStatus == MentorshipStatus.Cancelled)
            return ApiResponse<MentorshipDto>.ErrorResponse("INVALID_STATUS_TRANSITION", "Already cancelled.");

        if (mentorship.MentorshipStatus == MentorshipStatus.Completed || mentorship.MentorshipStatus == MentorshipStatus.Rejected)
            return ApiResponse<MentorshipDto>.ErrorResponse("INVALID_STATUS_TRANSITION", $"Cannot cancel mentorship. Currently {mentorship.MentorshipStatus}.");

        mentorship.MentorshipStatus = MentorshipStatus.Cancelled;
        var role = startup != null ? "Startup" : "Advisor";
        mentorship.CancelledAt = DateTime.UtcNow;
        mentorship.CancelledBy = role;
        mentorship.CancellationReason = reason;
        mentorship.LastUpdatedByRole = role;
        mentorship.UpdatedAt = DateTime.UtcNow;

        // Cascade-cancel all sessions that haven't been conducted/completed yet
        var nonCancellableStatuses = new[] {
            SessionStatusValues.Completed,
            SessionStatusValues.Conducted,
            SessionStatusValues.InDispute,
            SessionStatusValues.Resolved
        };
        var pendingSessions = await _db.MentorshipSessions
            .Where(s => s.MentorshipID == mentorshipId && !nonCancellableStatuses.Contains(s.SessionStatus))
            .ToListAsync();
        foreach (var s in pendingSessions)
        {
            s.SessionStatus = SessionStatusValues.Cancelled;
            s.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync("CANCEL_MENTORSHIP", "StartupAdvisorMentorship", mentorship.MentorshipID, reason);

        // Notify the other party about cancellation
        try
        {
            int recipientUserId;
            string cancellerName;
            string actionUrl;
            if (startup != null && mentorship.StartupID == startup.StartupID)
            {
                // Startup cancelled → notify Advisor
                var advisorEntity = await _db.Advisors.AsNoTracking().FirstOrDefaultAsync(a => a.AdvisorID == mentorship.AdvisorID);
                if (advisorEntity != null)
                {
                    recipientUserId = advisorEntity.UserID;
                    cancellerName = startup.CompanyName;
                    actionUrl = $"/advisor/requests/{mentorshipId}";
                    await _notifications.CreateAndPushAsync(new CreateNotificationRequest
                    {
                        UserId = recipientUserId,
                        NotificationType = "CONSULTING",
                        Title = "Y\u00eau c\u1ea7u t\u01b0 v\u1ea5n \u0111\u00e3 b\u1ecb hu\u1ef7",
                        Message = $"Startup '{cancellerName}' \u0111\u00e3 hu\u1ef7 y\u00eau c\u1ea7u t\u01b0 v\u1ea5n. L\u00fd do: {reason ?? "Kh\u00f4ng c\u00f3 l\u00fd do c\u1ee5 th\u1ec3."}",
                        RelatedEntityType = "Mentorship",
                        RelatedEntityId = mentorshipId,
                        ActionUrl = actionUrl
                    });
                }
            }
            else if (advisor != null)
            {
                // Advisor cancelled → notify Startup
                var startupEntity = await _db.Startups.AsNoTracking().FirstOrDefaultAsync(s => s.StartupID == mentorship.StartupID);
                if (startupEntity != null)
                {
                    recipientUserId = startupEntity.UserID;
                    cancellerName = advisor.FullName;
                    actionUrl = $"/startup/mentorship-requests/{mentorshipId}";
                    await _notifications.CreateAndPushAsync(new CreateNotificationRequest
                    {
                        UserId = recipientUserId,
                        NotificationType = "CONSULTING",
                        Title = "Y\u00eau c\u1ea7u t\u01b0 v\u1ea5n \u0111\u00e3 b\u1ecb hu\u1ef7",
                        Message = $"Advisor {cancellerName} \u0111\u00e3 hu\u1ef7 y\u00eau c\u1ea7u t\u01b0 v\u1ea5n. L\u00fd do: {reason ?? "Kh\u00f4ng c\u00f3 l\u00fd do c\u1ee5 th\u1ec3."}",
                        RelatedEntityType = "Mentorship",
                        RelatedEntityId = mentorshipId,
                        ActionUrl = actionUrl
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send cancellation notification for mentorship {MentorshipId}", mentorshipId);
        }

        return ApiResponse<MentorshipDto>.SuccessResponse(MapToDto(mentorship));
    }

        // ================================================================
        public async Task<ApiResponse<PagedResponse<SessionListItemDto>>> GetMySessionsAsync(int userId, string userType, string? status, int page, int pageSize)
        {
            var query = _db.MentorshipSessions
                .Include(s => s.Mentorship).ThenInclude(m => m.Startup)
                .Include(s => s.Mentorship).ThenInclude(m => m.Advisor)
                .Include(s => s.Mentorship).ThenInclude(m => m.Reports)
                .AsNoTracking();

        if (userType == "Startup")
        {
            query = query.Where(s => s.Mentorship.Startup.UserID == userId);
        } 
        else if (userType == "Advisor")
        {
            query = query.Where(s => s.Mentorship.Advisor.UserID == userId);
        }

        if (!string.IsNullOrEmpty(status))
        {
            if (status == SessionStatusValues.Cancelled)
            {
                // Include legacy rows whose parent mentorship was cancelled/rejected
                // but session row itself was never updated in DB
                query = query.Where(s => s.SessionStatus == SessionStatusValues.Cancelled
                    || s.Mentorship.MentorshipStatus == MentorshipStatus.Cancelled
                    || s.Mentorship.MentorshipStatus == MentorshipStatus.Rejected);
            }
            else
            {
                // Exclude sessions whose parent is cancelled (override takes precedence in projection)
                query = query.Where(s => s.SessionStatus == status
                    && s.Mentorship.MentorshipStatus != MentorshipStatus.Cancelled
                    && s.Mentorship.MentorshipStatus != MentorshipStatus.Rejected);
            }
        }
        else
        {
            // Default: exclude unconfirmed slot proposals — only return sessions that have a confirmed schedule
            query = query.Where(s => s.SessionStatus != SessionStatusValues.ProposedByStartup
                                  && s.SessionStatus != SessionStatusValues.ProposedByAdvisor);
        }

        var totalItems = await query.CountAsync();
        var items = await query.OrderByDescending(s => s.ScheduledStartAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(s => new SessionListItemDto
            {
                SessionID = s.SessionID,
                MentorshipID = s.MentorshipID,
                ScheduledStartAt = s.ScheduledStartAt,
                DurationMinutes = s.DurationMinutes,
                SessionFormat = s.SessionFormat,
                MeetingURL = (userType == "Startup" && s.Mentorship.PaymentStatus != PaymentStatus.Completed)
                    ? null
                    : s.MeetingURL,
                // If parent mentorship is Cancelled/Rejected, override session status so legacy
                // rows (created before cascade-cancel was deployed) show correct status.
                SessionStatus = (s.Mentorship.MentorshipStatus == MentorshipStatus.Cancelled
                                 || s.Mentorship.MentorshipStatus == MentorshipStatus.Rejected)
                                    ? SessionStatusValues.Cancelled
                                    : s.SessionStatus,
                TopicsDiscussed = s.TopicsDiscussed,
                KeyInsights = s.KeyInsights,
                ActionItems = s.ActionItems,
                MentorshipStatus = s.Mentorship.MentorshipStatus.ToString(),
                NextSteps = s.NextSteps,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                AdvisorID = s.Mentorship.AdvisorID,
                AdvisorName = s.Mentorship.Advisor.FullName,
                AdvisorProfilePhotoURL = s.Mentorship.Advisor.ProfilePhotoURL,
                Advisor = new SessionAdvisorDto
                {
                    AdvisorID = s.Mentorship.AdvisorID,
                    FullName = s.Mentorship.Advisor.FullName,
                    Title = s.Mentorship.Advisor.Title,
                    ProfilePhotoURL = s.Mentorship.Advisor.ProfilePhotoURL
                },
                StartupID = s.Mentorship.StartupID,
                StartupName = s.Mentorship.Startup.CompanyName,
                MentorshipChallengeDescription = s.Mentorship.ChallengeDescription,
                HasReport = s.Mentorship.Reports.Any(r => r.SessionID == s.SessionID)
            }).ToListAsync();

        return ApiResponse<PagedResponse<SessionListItemDto>>.SuccessResponse(new PagedResponse<SessionListItemDto> { Items = items, Paging = new PagingInfo { Page = page, PageSize = pageSize, TotalItems = totalItems } });
    }

    // CREATE SESSION (Advisor)
    // ================================================================

    public async Task<ApiResponse<SessionDto>> CreateSessionAsync(int userId, int mentorshipId, CreateSessionRequest request)
    {
        var (mentorship, error) = await GetMentorshipForAdvisor(userId, mentorshipId);
        if (mentorship == null)
            return ApiResponse<SessionDto>.ErrorResponse(error!.Error!.Code, error.Error.Message);

        if (mentorship.MentorshipStatus != MentorshipStatus.Requested
            && mentorship.MentorshipStatus != MentorshipStatus.Accepted
            && mentorship.MentorshipStatus != MentorshipStatus.InProgress)
            return ApiResponse<SessionDto>.ErrorResponse("INVALID_STATUS_TRANSITION",
                $"Cannot create session for mentorship with status '{mentorship.MentorshipStatus}'. Must be 'Requested', 'Accepted' or 'InProgress'.");

        var scheduledAt = request.ScheduledStartAt.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(request.ScheduledStartAt, DateTimeKind.Utc)
            : request.ScheduledStartAt.ToUniversalTime();

        // If mentorship is still Requested, advisor is counter-proposing slots.
        // Session stays ProposedByAdvisor until startup confirms one.
        // Mentorship stays Requested — does NOT auto-advance.
        bool isCounterProposal = mentorship.MentorshipStatus == MentorshipStatus.Requested;

        // SessionFormat luôn lấy từ mentorship.PreferredFormat — Advisor không được override
        var resolvedFormat = mentorship.PreferredFormat;

        // Auto-fill meeting URL: pick link matching the resolved format
        var advisor = await _db.Advisors.AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserID == userId);
        var resolvedMeetingUrl = request.MeetingUrl
            ?? ResolveAdvisorMeetingUrl(advisor, resolvedFormat);

        var session = new MentorshipSession
        {
            MentorshipID = mentorshipId,
            ScheduledStartAt = scheduledAt,
            DurationMinutes = request.DurationMinutes,
            SessionFormat = resolvedFormat,
            MeetingURL = resolvedMeetingUrl,
            SessionStatus = isCounterProposal ? SessionStatusValues.ProposedByAdvisor : SessionStatusValues.Scheduled,
            ProposedBy = "Advisor",
            Note = request.Note,
            CreatedAt = DateTime.UtcNow
        };

        _db.MentorshipSessions.Add(session);

        // Cancel tất cả ProposedByStartup slots cũ đang pending — dù Advisor counter-propose hay direct schedule
        // Đảm bảo detail response sạch, FE không cần infer slot nào còn hiệu lực
        var pendingSlotsToCancel = await _db.MentorshipSessions
            .Where(s => s.MentorshipID == mentorshipId
                && s.SessionStatus == SessionStatusValues.ProposedByStartup)
            .ToListAsync();
        foreach (var s in pendingSlotsToCancel)
        {
            s.SessionStatus = SessionStatusValues.Cancelled;
            s.UpdatedAt = DateTime.UtcNow;
        }

        // Only advance mentorship status when advisor is scheduling directly (not counter-proposing)
        if (!isCounterProposal)
        {
            if (mentorship.MentorshipStatus == MentorshipStatus.Accepted)
            {
                mentorship.MentorshipStatus = MentorshipStatus.InProgress;
                mentorship.InProgressAt = DateTime.UtcNow;
                mentorship.LastUpdatedByRole = "Advisor";
                mentorship.UpdatedAt = DateTime.UtcNow;
            }

            // Cancel any remaining ProposedByAdvisor slots too (direct schedule supersedes all pending)
            var pendingAdvisorSlots = await _db.MentorshipSessions
                .Where(s => s.MentorshipID == mentorshipId
                    && s.SessionID != session.SessionID
                    && s.SessionStatus == SessionStatusValues.ProposedByAdvisor)
                .ToListAsync();
            foreach (var s in pendingAdvisorSlots)
            {
                s.SessionStatus = SessionStatusValues.Cancelled;
                s.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();

        await _audit.LogAsync("CREATE_SESSION", "MentorshipSession", session.SessionID,
            $"MentorshipId={mentorshipId}");
        _logger.LogInformation("Session {SessionId} created for mentorship {MentorshipId}",
            session.SessionID, mentorshipId);

        // Notify Startup about the new session
        try
        {
            var startupEntity = await _db.Startups.AsNoTracking().FirstOrDefaultAsync(s => s.StartupID == mentorship.StartupID);
            var advisorName = (await _db.Advisors.AsNoTracking().Where(a => a.UserID == userId).Select(a => a.FullName).FirstOrDefaultAsync()) ?? "Advisor";
            if (startupEntity != null)
            {
                await _notifications.CreateAndPushAsync(new CreateNotificationRequest
                {
                    UserId = startupEntity.UserID,
                    NotificationType = "CONSULTING",
                    Title = isCounterProposal ? "\u0110\u1ec1 xu\u1ea5t l\u1ecbch t\u01b0 v\u1ea5n m\u1edbi" : "L\u1ecbch t\u01b0 v\u1ea5n \u0111\u00e3 \u0111\u01b0\u1ee3c l\u00ean l\u1ecbch",
                    Message = isCounterProposal
                        ? $"Advisor {advisorName} \u0111\u00e3 \u0111\u1ec1 xu\u1ea5t l\u1ecbch t\u01b0 v\u1ea5n m\u1edbi. Vui l\u00f2ng x\u00e1c nh\u1eadn."
                        : $"Advisor {advisorName} \u0111\u00e3 l\u00ean l\u1ecbch bu\u1ed5i t\u01b0 v\u1ea5n m\u1edbi.",
                    RelatedEntityType = "MentorshipSession",
                    RelatedEntityId = session.SessionID,
                    ActionUrl = $"/startup/mentorship-requests/{mentorshipId}"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send session notification for session {SessionId}", session.SessionID);
        }

        return ApiResponse<SessionDto>.SuccessResponse(MapSessionDto(session));
    }

    // ================================================================
    // UPDATE SESSION (Advisor)
    // ================================================================

    public async Task<ApiResponse<SessionDto>> UpdateSessionAsync(int userId, int sessionId, UpdateSessionRequest request)
    {
        var session = await _db.MentorshipSessions
            .Include(s => s.Mentorship)
            .FirstOrDefaultAsync(s => s.SessionID == sessionId);

        if (session == null)
            return ApiResponse<SessionDto>.ErrorResponse("SESSION_NOT_FOUND", "Session not found.");

        // Verify advisor ownership
        var advisor = await _db.Advisors.AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserID == userId);
        if (advisor == null || session.Mentorship.AdvisorID != advisor.AdvisorID)
            return ApiResponse<SessionDto>.ErrorResponse("MENTORSHIP_NOT_OWNED",
                "You are not the advisor for this session's mentorship.");

        // Guard: Cannot update session if mentorship or session is in a terminal or frozen state
        var terminalMentorshipStatuses = new[] { MentorshipStatus.Completed, MentorshipStatus.InDispute, MentorshipStatus.Resolved, MentorshipStatus.Cancelled, MentorshipStatus.Rejected };
        if (terminalMentorshipStatuses.Contains(session.Mentorship.MentorshipStatus))
            return ApiResponse<SessionDto>.ErrorResponse("INVALID_MENTORSHIP_STATUS",
                $"Cannot update session when mentorship status is {session.Mentorship.MentorshipStatus}.");

        var frozenSessionStatuses = new[] { SessionStatusValues.InDispute, SessionStatusValues.Resolved, SessionStatusValues.Cancelled, SessionStatusValues.Completed };
        if (frozenSessionStatuses.Contains(session.SessionStatus))
            return ApiResponse<SessionDto>.ErrorResponse("INVALID_SESSION_STATUS",
                $"Cannot update session when session status is {session.SessionStatus}.");

        if (request.SessionStatus != null && !SessionStatusValues.All.Contains(request.SessionStatus))
            return ApiResponse<SessionDto>.ErrorResponse("INVALID_SESSION_STATUS",
                $"SessionStatus must be one of: {string.Join(", ", SessionStatusValues.All)}");

        if (request.ScheduledStartAt.HasValue) session.ScheduledStartAt = request.ScheduledStartAt.Value;
        if (request.DurationMinutes.HasValue) session.DurationMinutes = request.DurationMinutes.Value;
        // SessionFormat không cho Advisor override — luôn giữ theo mentorship.PreferredFormat
        if (request.MeetingUrl != null) session.MeetingURL = request.MeetingUrl;
        if (request.SessionStatus != null) session.SessionStatus = request.SessionStatus;
        if (request.TopicsDiscussed != null) session.TopicsDiscussed = request.TopicsDiscussed;
        if (request.KeyInsights != null) session.KeyInsights = request.KeyInsights;
        if (request.ActionItems != null) session.ActionItems = request.ActionItems;
        if (request.NextSteps != null) session.NextSteps = request.NextSteps;
        session.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _audit.LogAsync("UPDATE_SESSION", "MentorshipSession", sessionId, null);
        _logger.LogInformation("Session {SessionId} updated", sessionId);

        return ApiResponse<SessionDto>.SuccessResponse(MapSessionDto(session));
    }

    // ================================================================
    // ACCEPT SESSION (Advisor) — chọn 1 slot ProposedByStartup để chốt lịch
    // ================================================================

    public async Task<ApiResponse<SessionDto>> AcceptSessionAsync(int userId, int mentorshipId, int sessionId)
    {
        var (mentorship, error) = await GetMentorshipForAdvisor(userId, mentorshipId);
        if (mentorship == null)
            return ApiResponse<SessionDto>.ErrorResponse(error!.Error!.Code, error.Error.Message);

        var session = await _db.MentorshipSessions
            .FirstOrDefaultAsync(s => s.SessionID == sessionId && s.MentorshipID == mentorshipId);
        if (session == null)
            return ApiResponse<SessionDto>.ErrorResponse("SESSION_NOT_FOUND", "Session not found.");
        if (session.SessionStatus != SessionStatusValues.ProposedByStartup)
            return ApiResponse<SessionDto>.ErrorResponse("INVALID_SESSION_STATUS",
                $"Only sessions with status '{SessionStatusValues.ProposedByStartup}' can be accepted by advisor.");

        // Luôn enforce SessionFormat theo mentorship.PreferredFormat — không cho data cũ ghi đè
        session.SessionFormat = mentorship.PreferredFormat;

        // Luôn re-resolve meeting URL theo đúng format
        var advisorProfile = await _db.Advisors.AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserID == userId);
        session.MeetingURL = ResolveAdvisorMeetingUrl(advisorProfile, session.SessionFormat);

        // Accept the chosen slot
        session.SessionStatus = SessionStatusValues.Scheduled;
        session.UpdatedAt = DateTime.UtcNow;

        // Cancel all other pending slots for this mentorship
        var otherPending = await _db.MentorshipSessions
            .Where(s => s.MentorshipID == mentorshipId
                && s.SessionID != sessionId
                && (s.SessionStatus == SessionStatusValues.ProposedByStartup
                    || s.SessionStatus == SessionStatusValues.ProposedByAdvisor))
            .ToListAsync();
        foreach (var s in otherPending)
        {
            s.SessionStatus = SessionStatusValues.Cancelled;
            s.UpdatedAt = DateTime.UtcNow;
        }

        // Advance mentorship: Requested → Accepted → InProgress
        if (mentorship.MentorshipStatus == MentorshipStatus.Requested)
        {
            mentorship.MentorshipStatus = MentorshipStatus.Accepted;
            mentorship.AcceptedAt = DateTime.UtcNow;
        }
        if (mentorship.MentorshipStatus == MentorshipStatus.Accepted)
        {
            mentorship.MentorshipStatus = MentorshipStatus.InProgress;
            mentorship.InProgressAt = DateTime.UtcNow;
            mentorship.LastUpdatedByRole = "Advisor";
            mentorship.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        await _audit.LogAsync("ACCEPT_SESSION", "MentorshipSession", sessionId,
            $"MentorshipId={mentorshipId}, CancelledOtherSlots={otherPending.Count}");
        _logger.LogInformation("Session {SessionId} accepted by advisor for mentorship {MentorshipId}",
            sessionId, mentorshipId);

        // Notify Startup that advisor accepted their proposed session
        try
        {
            var startupEntity = await _db.Startups.AsNoTracking().FirstOrDefaultAsync(s => s.StartupID == mentorship.StartupID);
            var advisorName = (await _db.Advisors.AsNoTracking().Where(a => a.UserID == userId).Select(a => a.FullName).FirstOrDefaultAsync()) ?? "Advisor";
            if (startupEntity != null)
            {
                await _notifications.CreateAndPushAsync(new CreateNotificationRequest
                {
                    UserId = startupEntity.UserID,
                    NotificationType = "CONSULTING",
                    Title = "L\u1ecbch t\u01b0 v\u1ea5n \u0111\u00e3 \u0111\u01b0\u1ee3c ch\u1ed1t",
                    Message = $"Advisor {advisorName} \u0111\u00e3 ch\u1ea5p nh\u1eadn l\u1ecbch t\u01b0 v\u1ea5n c\u1ee7a b\u1ea1n.",
                    RelatedEntityType = "MentorshipSession",
                    RelatedEntityId = sessionId,
                    ActionUrl = $"/startup/mentorship-requests/{mentorshipId}"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send session accept notification for session {SessionId}", sessionId);
        }

        return ApiResponse<SessionDto>.SuccessResponse(MapSessionDto(session));
    }

    // ================================================================
    // CONFIRM SESSION (Startup) — chọn 1 slot ProposedByAdvisor để chốt lịch
    // ================================================================

    public async Task<ApiResponse<SessionDto>> ConfirmSessionAsync(int userId, int mentorshipId, int sessionId)
    {
        var startup = await _db.Startups.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserID == userId);
        if (startup == null)
            return ApiResponse<SessionDto>.ErrorResponse("STARTUP_PROFILE_NOT_FOUND", "Startup profile not found.");

        var mentorship = await _db.StartupAdvisorMentorships
            .FirstOrDefaultAsync(m => m.MentorshipID == mentorshipId);
        if (mentorship == null)
            return ApiResponse<SessionDto>.ErrorResponse("MENTORSHIP_NOT_FOUND", "Mentorship not found.");
        if (mentorship.StartupID != startup.StartupID)
            return ApiResponse<SessionDto>.ErrorResponse("MENTORSHIP_NOT_OWNED", "You are not the startup for this mentorship.");

        var session = await _db.MentorshipSessions
            .FirstOrDefaultAsync(s => s.SessionID == sessionId && s.MentorshipID == mentorshipId);
        if (session == null)
            return ApiResponse<SessionDto>.ErrorResponse("SESSION_NOT_FOUND", "Session not found.");
        if (session.SessionStatus != SessionStatusValues.ProposedByAdvisor)
            return ApiResponse<SessionDto>.ErrorResponse("INVALID_SESSION_STATUS",
                $"Only sessions with status '{SessionStatusValues.ProposedByAdvisor}' can be confirmed by startup.");

        // Luôn enforce SessionFormat và re-resolve MeetingURL theo mentorship.PreferredFormat
        session.SessionFormat = mentorship.PreferredFormat;
        var advisorProfile2 = await _db.Advisors.AsNoTracking()
            .FirstOrDefaultAsync(a => a.AdvisorID == mentorship.AdvisorID);
        session.MeetingURL = ResolveAdvisorMeetingUrl(advisorProfile2, session.SessionFormat);

        // Confirm the chosen slot
        session.SessionStatus = SessionStatusValues.Scheduled;
        session.UpdatedAt = DateTime.UtcNow;

        // Cancel all other pending slots for this mentorship (ProposedByAdvisor + ProposedByStartup)
        var otherPending = await _db.MentorshipSessions
            .Where(s => s.MentorshipID == mentorshipId
                && s.SessionID != sessionId
                && (s.SessionStatus == SessionStatusValues.ProposedByAdvisor
                    || s.SessionStatus == SessionStatusValues.ProposedByStartup))
            .ToListAsync();
        foreach (var s in otherPending)
        {
            s.SessionStatus = SessionStatusValues.Cancelled;
            s.UpdatedAt = DateTime.UtcNow;
        }

        // Advance mentorship: Requested → Accepted → InProgress
        if (mentorship.MentorshipStatus == MentorshipStatus.Requested)
        {
            mentorship.MentorshipStatus = MentorshipStatus.Accepted;
            mentorship.AcceptedAt = DateTime.UtcNow;
        }
        if (mentorship.MentorshipStatus == MentorshipStatus.Accepted)
        {
            mentorship.MentorshipStatus = MentorshipStatus.InProgress;
            mentorship.InProgressAt = DateTime.UtcNow;
            mentorship.LastUpdatedByRole = "Startup";
            mentorship.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        await _audit.LogAsync("CONFIRM_SESSION", "MentorshipSession", sessionId,
            $"MentorshipId={mentorshipId}, CancelledOtherSlots={otherPending.Count}");
        _logger.LogInformation("Session {SessionId} confirmed by startup for mentorship {MentorshipId}",
            sessionId, mentorshipId);

        // Notify Advisor that startup confirmed a session
        try
        {
            var advisorEntity = await _db.Advisors.AsNoTracking().FirstOrDefaultAsync(a => a.AdvisorID == mentorship.AdvisorID);
            var startupName = startup?.CompanyName ?? "Startup";
            if (advisorEntity != null)
            {
                await _notifications.CreateAndPushAsync(new CreateNotificationRequest
                {
                    UserId = advisorEntity.UserID,
                    NotificationType = "CONSULTING",
                    Title = "Lịch tư vấn đã được xác nhận",
                    Message = $"Startup '{startupName}' đã xác nhận lịch tư vấn của bạn.",
                    RelatedEntityType = "MentorshipSession",
                    RelatedEntityId = sessionId,
                    ActionUrl = $"/advisor/requests/{mentorshipId}"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send session confirm notification for session {SessionId}", sessionId);
        }

        return ApiResponse<SessionDto>.SuccessResponse(MapSessionDto(session, hideMeetingUrl: true));
    }

    // ================================================================
    // CREATE REPORT (Advisor)
    // ================================================================

    public async Task<ApiResponse<ReportDto>> CreateReportAsync(int userId, int mentorshipId, CreateReportRequest request)
    {
        var (mentorship, error) = await GetMentorshipForAdvisor(userId, mentorshipId);
        if (mentorship == null)
            return ApiResponse<ReportDto>.ErrorResponse(error!.Error!.Code, error.Error.Message);

        var advisor = await _db.Advisors.AsNoTracking()
            .FirstAsync(a => a.UserID == userId);

        // SessionID is required for new reports (BA chốt #2)
        if (!request.SessionId.HasValue)
            return ApiResponse<ReportDto>.ErrorResponse("SESSION_REQUIRED",
                "SessionID is required when creating a report.");

        var session = await _db.MentorshipSessions.FirstOrDefaultAsync(s =>
            s.SessionID == request.SessionId.Value && s.MentorshipID == mentorshipId);
        if (session == null)
            return ApiResponse<ReportDto>.ErrorResponse("SESSION_NOT_FOUND",
                "Session not found or does not belong to this mentorship.");

        // Guard: Cannot create report if session or mentorship is in dispute/resolved/cancelled
        if (mentorship.MentorshipStatus == MentorshipStatus.InDispute || mentorship.MentorshipStatus == MentorshipStatus.Resolved || mentorship.MentorshipStatus == MentorshipStatus.Cancelled)
            return ApiResponse<ReportDto>.ErrorResponse("INVALID_MENTORSHIP_STATUS", 
                "Cannot submit report while mentorship is in dispute, resolved, or cancelled.");

        if (session.SessionStatus == SessionStatusValues.InDispute || session.SessionStatus == SessionStatusValues.Resolved || session.SessionStatus == SessionStatusValues.Cancelled)
            return ApiResponse<ReportDto>.ErrorResponse("INVALID_SESSION_STATUS",
                "Cannot submit report for a session that is in dispute, resolved, or cancelled.");

        // Guard: 1 active report (Draft or submitted) per session
        var existingActiveReport = await _db.MentorshipReports
            .Where(r => r.SessionID == request.SessionId.Value
                     && r.MentorshipID == mentorshipId
                     && r.SupersededByReportID == null)
            .FirstOrDefaultAsync();

        if (existingActiveReport != null)
        {
            if (existingActiveReport.ReportReviewStatus == ReportReviewStatus.Draft)
                return ApiResponse<ReportDto>.ErrorResponse("DRAFT_ALREADY_EXISTS",
                    "A draft already exists for this session. Use PATCH to update it.");
            // Allow resubmit only when previous report was Failed or NeedsMoreInfo
            if (existingActiveReport.ReportReviewStatus != ReportReviewStatus.Failed
                && existingActiveReport.ReportReviewStatus != ReportReviewStatus.NeedsMoreInfo)
                return ApiResponse<ReportDto>.ErrorResponse("REPORT_ALREADY_EXISTS",
                    "A report already exists for this session. Cannot create another.");
        }

        string? attachmentsUrl = null;
        if (request.AttachmentFile != null && request.AttachmentFile.Length > 0)
            attachmentsUrl = await _cloudinary.UploadDocument(request.AttachmentFile, CloudinaryFolderSaving.DocumentStorage);

        var isDraft = request.IsDraft;
        var now = DateTime.UtcNow;
        var report = new MentorshipReport
        {
            MentorshipID = mentorshipId,
            SessionID = request.SessionId.Value,
            CreatedByAdvisorID = advisor.AdvisorID,
            ReportSummary = request.ReportSummary,
            DetailedFindings = request.DetailedFindings,
            Recommendations = request.Recommendations,
            AttachmentsURL = attachmentsUrl,
            // Auto-approve: submitted report tự động Passed, không cần Staff duyệt thủ công
            ReportReviewStatus = isDraft ? ReportReviewStatus.Draft : ReportReviewStatus.Passed,
            SubmittedAt = isDraft ? null : now,
            ReviewedAt = isDraft ? null : now,
            CreatedAt = now
        };

        _db.MentorshipReports.Add(report);
        await _db.SaveChangesAsync();

        // Supersede chain on submit: link previous Failed or NeedsMoreInfo report
        MentorshipReport? supersededReport = null;
        if (!isDraft)
        {
            supersededReport = await _db.MentorshipReports
                .Where(r => r.SessionID == request.SessionId.Value
                         && r.MentorshipID == mentorshipId
                         && (r.ReportReviewStatus == ReportReviewStatus.NeedsMoreInfo
                             || r.ReportReviewStatus == ReportReviewStatus.Failed)
                         && r.SupersededByReportID == null
                         && r.ReportID != report.ReportID)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();
            if (supersededReport != null)
            {
                supersededReport.SupersededByReportID = report.ReportID;
            }

            // Auto-complete session nếu Startup đã confirm (Conducted).
            // Nếu chưa confirm (Scheduled/InProgress), để session nguyên —
            // khi Startup confirm sau sẽ tự động Completed (xử lý trong ConfirmConductedAsync).
            if (session != null && session.SessionStatus == SessionStatusValues.Conducted)
            {
                session.SessionStatus = SessionStatusValues.Completed;
                session.UpdatedAt = now;
                await _db.SaveChangesAsync(); // Commit session status before counting
                await UpdateAdvisorStatsAsync(mentorshipId);
            }

            // Recalculate eligibility sau khi report Passed + session Completed
            var mentorshipFull = await _db.StartupAdvisorMentorships
                .Include(m => m.Sessions)
                .Include(m => m.Reports)
                .FirstOrDefaultAsync(m => m.MentorshipID == mentorshipId);
            if (mentorshipFull != null)
            {
                RecalculateMentorshipStatus(mentorshipFull);
                RecalculatePayoutEligibility(mentorshipFull);
            }

            await _db.SaveChangesAsync();
        }

        await _audit.LogAsync(isDraft ? "CREATE_REPORT_DRAFT" : "CREATE_REPORT_AUTO_APPROVED", "MentorshipReport", report.ReportID,
            $"MentorshipId={mentorshipId}, SupersededReportId={supersededReport?.ReportID}");
        _logger.LogInformation("Report {ReportId} created for mentorship {MentorshipId} (isDraft={IsDraft})",
            report.ReportID, mentorshipId, isDraft);

        // Notify Startup — report đã được duyệt tự động, Startup xem được ngay
        var startup = await _db.Startups.AsNoTracking().FirstOrDefaultAsync(s => s.StartupID == mentorship.StartupID);
        if (startup != null && !isDraft)
        {
            await _notifications.CreateAndPushAsync(new CreateNotificationRequest
            {
                UserId = startup.UserID,
                NotificationType = "CONSULTING",
                Title = "Báo cáo tư vấn đã sẵn sàng",
                Message = "Advisor đã hoàn thành báo cáo buổi tư vấn. Bạn có thể xem ngay.",
                RelatedEntityType = "MentorshipReport",
                RelatedEntityId = report.ReportID,
                ActionUrl = $"/startup/mentorship-requests/{mentorshipId}"
            });
        }

        return ApiResponse<ReportDto>.SuccessResponse(MapReportDto(report));
    }

    // ================================================================
    // GET REPORT (Startup/Advisor/Staff/Admin)
    // ================================================================

    public async Task<ApiResponse<ReportDto>> GetReportAsync(int userId, string userType, int reportId)
    {
        var report = await _db.MentorshipReports
            .AsNoTracking()
            .Include(r => r.Mentorship)
            .FirstOrDefaultAsync(r => r.ReportID == reportId);

        if (report == null)
            return ApiResponse<ReportDto>.ErrorResponse("REPORT_NOT_FOUND", "Report not found.");

        // Ownership check
        if (!await IsParticipantOrStaff(userId, userType, report.Mentorship))
            return ApiResponse<ReportDto>.ErrorResponse("MENTORSHIP_NOT_OWNED",
                "You do not have access to this report.");

        // Visibility gate: Startup only sees Passed reports
        if (userType == "Startup" && report.ReportReviewStatus != ReportReviewStatus.Passed)
            return ApiResponse<ReportDto>.ErrorResponse("REPORT_NOT_AVAILABLE",
                "No consulting report available yet.");

        // Draft is only visible to the advisor who created it
        if (report.ReportReviewStatus == ReportReviewStatus.Draft && userType != "Advisor")
            return ApiResponse<ReportDto>.ErrorResponse("REPORT_NOT_AVAILABLE",
                "No consulting report available yet.");

        return ApiResponse<ReportDto>.SuccessResponse(MapReportDto(report, userType));
    }

    // ================================================================
    // UPDATE REPORT DRAFT (Advisor)
    // ================================================================

    public async Task<ApiResponse<ReportDto>> UpdateReportAsync(int userId, int mentorshipId, int reportId, UpdateReportRequest request)
    {
        var (mentorship, error) = await GetMentorshipForAdvisor(userId, mentorshipId);
        if (mentorship == null)
            return ApiResponse<ReportDto>.ErrorResponse(error!.Error!.Code, error.Error.Message);

        var advisor = await _db.Advisors.AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserID == userId);
        if (advisor == null)
            return ApiResponse<ReportDto>.ErrorResponse("ADVISOR_NOT_FOUND", "Advisor profile not found.");

        var report = await _db.MentorshipReports
            .FirstOrDefaultAsync(r => r.ReportID == reportId && r.MentorshipID == mentorshipId);

        if (report == null)
            return ApiResponse<ReportDto>.ErrorResponse("REPORT_NOT_FOUND", "Report not found.");

        if (report.CreatedByAdvisorID != advisor.AdvisorID)
            return ApiResponse<ReportDto>.ErrorResponse("REPORT_NOT_OWNED", "You do not own this report.");

        if (report.ReportReviewStatus != ReportReviewStatus.Draft
            && report.ReportReviewStatus != ReportReviewStatus.NeedsMoreInfo)
            return ApiResponse<ReportDto>.ErrorResponse("REPORT_NOT_EDITABLE",
                "Only Draft or NeedsMoreInfo reports can be updated via PATCH. For Failed reports, submit a new report via POST.");

        // Guard: Cannot update report if session or mentorship is resolved/cancelled/disputed
        if (mentorship.MentorshipStatus == MentorshipStatus.InDispute || mentorship.MentorshipStatus == MentorshipStatus.Resolved || mentorship.MentorshipStatus == MentorshipStatus.Cancelled)
            return ApiResponse<ReportDto>.ErrorResponse("INVALID_MENTORSHIP_STATUS", 
                "Cannot update report while mentorship is in dispute, resolved, or cancelled.");
        
        if (report.SessionID.HasValue)
        {
            var session = await _db.MentorshipSessions.FirstOrDefaultAsync(s => s.SessionID == report.SessionID.Value);
            if (session != null && (session.SessionStatus == SessionStatusValues.InDispute || session.SessionStatus == SessionStatusValues.Resolved || session.SessionStatus == SessionStatusValues.Cancelled))
                return ApiResponse<ReportDto>.ErrorResponse("INVALID_SESSION_STATUS",
                    "Cannot update report for a session that is in dispute, resolved, or cancelled.");
        }

        if (request.ReportSummary != null) report.ReportSummary = request.ReportSummary;
        if (request.DetailedFindings != null) report.DetailedFindings = request.DetailedFindings;
        if (request.Recommendations != null) report.Recommendations = request.Recommendations;

        if (request.AttachmentFile != null && request.AttachmentFile.Length > 0)
            report.AttachmentsURL = await _cloudinary.UploadDocument(request.AttachmentFile, CloudinaryFolderSaving.DocumentStorage);

        bool submitting = !request.IsDraft;
        var now2 = DateTime.UtcNow;

        if (submitting)
        {
            // Auto-approve: submitted report tự động Passed
            report.ReportReviewStatus = ReportReviewStatus.Passed;
            report.SubmittedAt = now2;
            report.ReviewedAt = now2;

            // Auto-complete session nếu Startup đã confirm (Conducted).
            // Nếu chưa confirm, để nguyên — ConfirmConductedAsync sẽ xử lý sau.
            if (report.SessionID.HasValue)
            {
                var session2 = await _db.MentorshipSessions
                    .FirstOrDefaultAsync(s => s.SessionID == report.SessionID.Value);
                if (session2 != null && session2.SessionStatus == SessionStatusValues.Conducted)
                {
                    session2.SessionStatus = SessionStatusValues.Completed;
                    session2.UpdatedAt = now2;
                    await _db.SaveChangesAsync(); // Commit session status before counting
                    await UpdateAdvisorStatsAsync(mentorshipId);
                }
            }

            // Recalculate eligibility
            var mentorshipFull2 = await _db.StartupAdvisorMentorships
                .Include(m => m.Sessions)
                .Include(m => m.Reports)
                .FirstOrDefaultAsync(m => m.MentorshipID == mentorshipId);
            if (mentorshipFull2 != null)
            {
                RecalculateMentorshipStatus(mentorshipFull2);
                RecalculatePayoutEligibility(mentorshipFull2);
            }

            // Notify Startup
            var mentorshipForNotify = mentorshipFull2 ?? mentorship;
            var startup2 = await _db.Startups.AsNoTracking()
                .FirstOrDefaultAsync(s => s.StartupID == mentorshipForNotify.StartupID);
            if (startup2 != null)
            {
                await _notifications.CreateAndPushAsync(new CreateNotificationRequest
                {
                    UserId = startup2.UserID,
                    NotificationType = "CONSULTING",
                    Title = "Báo cáo tư vấn đã sẵn sàng",
                    Message = "Advisor đã hoàn thành báo cáo buổi tư vấn. Bạn có thể xem ngay.",
                    RelatedEntityType = "MentorshipReport",
                    RelatedEntityId = report.ReportID,
                    ActionUrl = $"/startup/mentorship-requests/{mentorshipId}"
                });
            }
        }

        report.UpdatedAt = now2;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(submitting ? "SUBMIT_REPORT_DRAFT_AUTO_APPROVED" : "UPDATE_REPORT_DRAFT",
            "MentorshipReport", report.ReportID, $"MentorshipId={mentorshipId}");

        return ApiResponse<ReportDto>.SuccessResponse(MapReportDto(report));
    }

    // ================================================================
    // ACKNOWLEDGE REPORT (Startup)
    // ================================================================

    public async Task<ApiResponse<ReportDto>> AcknowledgeReportAsync(int userId, int mentorshipId, int reportId)
    {
        var startup = await _db.Startups.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserID == userId);
        if (startup == null)
            return ApiResponse<ReportDto>.ErrorResponse("STARTUP_PROFILE_NOT_FOUND", "Startup profile not found.");

        var mentorship = await _db.StartupAdvisorMentorships
            .FirstOrDefaultAsync(m => m.MentorshipID == mentorshipId && m.StartupID == startup.StartupID);
        if (mentorship == null)
            return ApiResponse<ReportDto>.ErrorResponse("MENTORSHIP_NOT_FOUND", "Mentorship not found.");

        var report = await _db.MentorshipReports
            .FirstOrDefaultAsync(r => r.ReportID == reportId && r.MentorshipID == mentorshipId);
        if (report == null)
            return ApiResponse<ReportDto>.ErrorResponse("REPORT_NOT_FOUND", "Report not found.");

        if (report.ReportReviewStatus != ReportReviewStatus.Passed)
            return ApiResponse<ReportDto>.ErrorResponse("REPORT_NOT_PASSED",
                "Only Passed reports can be acknowledged.");

        if (report.StartupAcknowledgedAt != null)
            return ApiResponse<ReportDto>.ErrorResponse("ALREADY_ACKNOWLEDGED",
                $"Report already acknowledged at {report.StartupAcknowledgedAt:o}.");

        var now = DateTime.UtcNow;
        report.StartupAcknowledgedAt = now;
        report.UpdatedAt = now;

        // Auto-complete session if it was Conducted or InDispute (acknowledgement implies acceptance)
        if (report.Session != null && 
            (report.Session.SessionStatus == SessionStatusValues.Conducted || 
             report.Session.SessionStatus == SessionStatusValues.InDispute))
        {
            report.Session.SessionStatus = SessionStatusValues.Completed;
            report.Session.UpdatedAt = now;
            // Also update stats since it's now completed
            await UpdateAdvisorStatsAsync(mentorshipId);
        }

        // Recalculate payout eligibility
        var mentorshipFull = await _db.StartupAdvisorMentorships
            .Include(m => m.Sessions)
            .Include(m => m.Reports)
            .FirstOrDefaultAsync(m => m.MentorshipID == mentorshipId);
        if (mentorshipFull != null)
        {
            RecalculateMentorshipStatus(mentorshipFull);
            RecalculatePayoutEligibility(mentorshipFull);
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync("ACKNOWLEDGE_REPORT", "MentorshipReport", reportId,
            $"MentorshipId={mentorshipId}");

        return ApiResponse<ReportDto>.SuccessResponse(MapReportDto(report), "Report acknowledged successfully.");
    }

    // ================================================================
    // CREATE FEEDBACK (Startup)
    // ================================================================

    public async Task<ApiResponse<FeedbackDto>> CreateFeedbackAsync(int userId, int mentorshipId, CreateFeedbackRequest request)
    {
        var startup = await _db.Startups.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserID == userId);
        if (startup == null)
            return ApiResponse<FeedbackDto>.ErrorResponse("STARTUP_PROFILE_NOT_FOUND",
                "Startup profile not found.");

        var mentorship = await _db.StartupAdvisorMentorships
            .FirstOrDefaultAsync(m => m.MentorshipID == mentorshipId);
        if (mentorship == null)
            return ApiResponse<FeedbackDto>.ErrorResponse("MENTORSHIP_NOT_FOUND",
                "Mentorship not found.");

        if (mentorship.StartupID != startup.StartupID)
            return ApiResponse<FeedbackDto>.ErrorResponse("MENTORSHIP_NOT_OWNED",
                "You are not the startup owner of this mentorship.");

        // If sessionId provided, validate it belongs to this mentorship
        if (request.SessionId.HasValue)
        {
            var sessionExists = await _db.MentorshipSessions.AnyAsync(s =>
                s.SessionID == request.SessionId.Value && s.MentorshipID == mentorshipId);
            if (!sessionExists)
                return ApiResponse<FeedbackDto>.ErrorResponse("SESSION_NOT_FOUND",
                    "Session not found or does not belong to this mentorship.");
        }

        var feedback = new MentorshipFeedback
        {
            MentorshipID = mentorshipId,
            SessionID = request.SessionId,
            FromRole = "Startup",
            Rating = request.Rating,
            Comment = request.Comment,
            SubmittedAt = DateTime.UtcNow,
            IsPublic = true
        };

        _db.MentorshipFeedbacks.Add(feedback);
        await _db.SaveChangesAsync();

        // Recalculate advisor's AverageRating and ReviewCount
        var advisorId = mentorship.AdvisorID;
        var allRatings = await _db.MentorshipFeedbacks
            .Where(f => f.Mentorship.AdvisorID == advisorId && f.FromRole == "Startup" && f.IsPublic)
            .Select(f => f.Rating)
            .ToListAsync();

        var advisor = await _db.Advisors.FirstOrDefaultAsync(a => a.AdvisorID == advisorId);
        if (advisor != null)
        {
            advisor.ReviewCount = allRatings.Count;
            advisor.AverageRating = allRatings.Count > 0 ? (float)allRatings.Average() : null;
            await _db.SaveChangesAsync();
        }

        await _audit.LogAsync("CREATE_FEEDBACK", "MentorshipFeedback", feedback.FeedbackID,
            $"MentorshipId={mentorshipId}, Rating={request.Rating}");
        _logger.LogInformation("Feedback {FeedbackId} created for mentorship {MentorshipId}",
            feedback.FeedbackID, mentorshipId);

        // Notify Advisor about the feedback
        try
        {
            var advisorEntity = await _db.Advisors.AsNoTracking().FirstOrDefaultAsync(a => a.AdvisorID == mentorship.AdvisorID);
            var startupName = startup?.CompanyName ?? "Startup";
            if (advisorEntity != null)
            {
                await _notifications.CreateAndPushAsync(new CreateNotificationRequest
                {
                    UserId = advisorEntity.UserID,
                    NotificationType = "CONSULTING",
                    Title = "Ph\u1ea3n h\u1ed3i m\u1edbi t\u1eeb startup",
                    Message = $"Startup '{startupName}' \u0111\u00e3 g\u1eedi ph\u1ea3n h\u1ed3i v\u1ec1 bu\u1ed5i t\u01b0 v\u1ea5n. \u0110\u00e1nh gi\u00e1: {request.Rating}/5.",
                    RelatedEntityType = "MentorshipFeedback",
                    RelatedEntityId = feedback.FeedbackID,
                    ActionUrl = $"/advisor/requests/{mentorshipId}"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send feedback notification for feedback {FeedbackId}", feedback.FeedbackID);
        }

        return ApiResponse<FeedbackDto>.SuccessResponse(MapFeedbackDto(feedback));
    }

    // ================================================================
    // HELPERS
    // ================================================================

    private async Task<(StartupAdvisorMentorship? mentorship, ApiResponse<MentorshipDto>? error)>
        GetMentorshipForAdvisor(int userId, int mentorshipId)
    {
        var advisor = await _db.Advisors.AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserID == userId);
        if (advisor == null)
            return (null, ApiResponse<MentorshipDto>.ErrorResponse("ADVISOR_PROFILE_NOT_FOUND",
                "Advisor profile not found."));

        var mentorship = await _db.StartupAdvisorMentorships
            .FirstOrDefaultAsync(m => m.MentorshipID == mentorshipId);
        if (mentorship == null)
            return (null, ApiResponse<MentorshipDto>.ErrorResponse("MENTORSHIP_NOT_FOUND",
                "Mentorship not found."));

        if (mentorship.AdvisorID != advisor.AdvisorID)
            return (null, ApiResponse<MentorshipDto>.ErrorResponse("MENTORSHIP_NOT_OWNED",
                "You are not the advisor for this mentorship."));

        return (mentorship, null);
    }

    /// <summary>Picks the advisor's meeting link that matches the session format.
    /// Falls back to whichever link is available if format is ambiguous or null.</summary>
    private static string? ResolveAdvisorMeetingUrl(Domain.Entities.Advisor? advisor, string? format)
    {
        if (advisor == null) return null;
        if (!string.IsNullOrEmpty(format))
        {
            var f = format.ToLowerInvariant();
            if (f.Contains("google") || f.Contains("meet"))
                return advisor.GoogleMeetLink ?? advisor.MsTeamsLink;
            if (f.Contains("teams") || f.Contains("microsoft"))
                return advisor.MsTeamsLink ?? advisor.GoogleMeetLink;
        }
        return advisor.GoogleMeetLink ?? advisor.MsTeamsLink;
    }

    private async Task<bool> IsParticipantOrStaff(int userId, string userType, StartupAdvisorMentorship mentorship)
    {
        if (string.Equals(userType, "Staff", StringComparison.OrdinalIgnoreCase) || 
            string.Equals(userType, "Admin", StringComparison.OrdinalIgnoreCase)) 
            return true;

        if (string.Equals(userType, "Startup", StringComparison.OrdinalIgnoreCase))
        {
            var startup = await _db.Startups.AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserID == userId);
            return startup != null && mentorship.StartupID == startup.StartupID;
        }

        if (string.Equals(userType, "Advisor", StringComparison.OrdinalIgnoreCase))
        {
            var advisor = await _db.Advisors.AsNoTracking()
                .FirstOrDefaultAsync(a => a.UserID == userId);
            return advisor != null && mentorship.AdvisorID == advisor.AdvisorID;
        }

        return false;
    }

    // ================================================================
    // MAPPING
    // ================================================================

    private static MentorshipDto MapToDto(StartupAdvisorMentorship m) => new()
    {
        MentorshipID = m.MentorshipID,
        StartupID = m.StartupID,
        AdvisorID = m.AdvisorID,
        MentorshipStatus = m.MentorshipStatus.ToString(),
        ChallengeDescription = m.ChallengeDescription,
        SpecificQuestions = m.SpecificQuestions,
        PreferredFormat = m.PreferredFormat,
        ExpectedDuration = m.ExpectedDuration,
        ExpectedScope = m.ExpectedScope,
        RequestedAt = m.RequestedAt,
        AcceptedAt = m.AcceptedAt,
        RejectedAt = m.RejectedAt,
        RejectedReason = m.RejectedReason,
        CreatedAt = m.CreatedAt,
        UpdatedAt = m.UpdatedAt
    };

    private static MentorshipDetailDto MapToDetailDto(StartupAdvisorMentorship m, string? callerType = null) => new()
    {
        MentorshipID = m.MentorshipID,
        StartupID = m.StartupID,
        StartupName = m.Startup.CompanyName,
        StartupLogoUrl = m.Startup.LogoURL,
        StartupIndustry = m.Startup.Industry?.IndustryName,
        StartupStage = m.Startup.StageRef?.StageName,
        AdvisorID = m.AdvisorID,
        AdvisorName = m.Advisor.FullName,
        AdvisorTitle = m.Advisor.Title,
        AdvisorPhotoURL = m.Advisor.ProfilePhotoURL,
        MentorshipStatus = m.MentorshipStatus.ToString(),
        ChallengeDescription = m.ChallengeDescription,
        SpecificQuestions = m.SpecificQuestions,
        PreferredFormat = m.PreferredFormat,
        ExpectedDuration = m.ExpectedDuration,
        ExpectedScope = m.ExpectedScope,
        ObligationSummary = m.ObligationSummary,
        RequestedAt = m.RequestedAt,
        AcceptedAt = m.AcceptedAt,
        RejectedAt = m.RejectedAt,
        RejectedReason = m.RejectedReason,
        CancelledAt = m.CancelledAt,
        CancelledBy = m.CancelledBy,
        CancellationReason = m.CancellationReason,
        CompletedAt = m.CompletedAt,
        CompletionConfirmedByStartup = m.CompletionConfirmedByStartup,
        CompletionConfirmedByAdvisor = m.CompletionConfirmedByAdvisor,
        SessionAmount = m.SessionAmount,
        PaymentStatus = m.PaymentStatus.ToString(),
        PaidAt = m.PaidAt,
        CreatedAt = m.CreatedAt,
        UpdatedAt = m.UpdatedAt,
        IsPayoutEligible = m.IsPayoutEligible,
        PayoutReleasedAt = m.PayoutReleasedAt,
        Sessions = m.Sessions.Select(s => MapSessionDto(s,
            string.Equals(callerType, "Startup", StringComparison.OrdinalIgnoreCase) && m.PaymentStatus != PaymentStatus.Completed)).ToList(),
        Reports = (string.Equals(callerType, "Startup", StringComparison.OrdinalIgnoreCase)
            ? m.Reports.Where(r => r.ReportReviewStatus == ReportReviewStatus.Passed)
            : (string.Equals(callerType, "Staff", StringComparison.OrdinalIgnoreCase) || string.Equals(callerType, "Admin", StringComparison.OrdinalIgnoreCase))
                ? m.Reports.Where(r => r.ReportReviewStatus != ReportReviewStatus.Draft)
                : m.Reports)  // Advisor sees all including own Drafts
            .OrderByDescending(r => r.SubmittedAt ?? r.CreatedAt)
            .Select(r => MapReportDto(r, callerType)).ToList(),
        Feedbacks = m.Feedbacks.Select(MapFeedbackDto).ToList(),
        TimelineEvents = BuildTimeline(m)
    };

    private static List<TimelineEventDto> BuildTimeline(StartupAdvisorMentorship m)
    {
        var events = new List<TimelineEventDto>();

        if (m.RequestedAt.HasValue)
            events.Add(new TimelineEventDto
            {
                Type = "Requested",
                Title = "Yêu cầu đã gửi",
                Description = "Yêu cầu tư vấn đã được tạo và gửi đến cố vấn.",
                Actor = "Startup",
                HappenedAt = m.RequestedAt.Value
            });

        if (m.AcceptedAt.HasValue)
            events.Add(new TimelineEventDto
            {
                Type = "Accepted",
                Title = "Cố vấn đã chấp nhận",
                Description = "Cố vấn đã chấp nhận yêu cầu tư vấn.",
                Actor = "Advisor",
                HappenedAt = m.AcceptedAt.Value
            });

        if (m.InProgressAt.HasValue)
            events.Add(new TimelineEventDto
            {
                Type = "InProgress",
                Title = "Mentorship đã bắt đầu",
                Description = "Buổi tư vấn đầu tiên đã được lập lịch.",
                Actor = "Advisor",
                HappenedAt = m.InProgressAt.Value
            });

        if (m.RejectedAt.HasValue)
            events.Add(new TimelineEventDto
            {
                Type = "Rejected",
                Title = "Yêu cầu bị từ chối",
                Description = string.IsNullOrEmpty(m.RejectedReason)
                    ? "Cố vấn đã từ chối yêu cầu tư vấn."
                    : $"Cố vấn đã từ chối yêu cầu. Lý do: {m.RejectedReason}",
                Actor = "Advisor",
                HappenedAt = m.RejectedAt.Value
            });

        if (m.CancelledAt.HasValue)
        {
            var actor = m.CancelledBy ?? "Unknown";
            events.Add(new TimelineEventDto
            {
                Type = "Cancelled",
                Title = "Yêu cầu đã bị hủy",
                Description = string.IsNullOrEmpty(m.CancellationReason)
                    ? $"{actor} đã hủy yêu cầu tư vấn."
                    : $"{actor} đã hủy yêu cầu. Lý do: {m.CancellationReason}",
                Actor = actor,
                HappenedAt = m.CancelledAt.Value
            });
        }

        if (m.CompletedAt.HasValue)
            events.Add(new TimelineEventDto
            {
                Type = "Completed",
                Title = "Mentorship đã hoàn thành",
                Description = "Mentorship đã hoàn thành thành công.",
                Actor = "Advisor",
                HappenedAt = m.CompletedAt.Value
            });

        return events.OrderBy(e => e.HappenedAt).ToList();
    }

    private static SessionDto MapSessionDto(MentorshipSession s, bool hideMeetingUrl = false) => new()
    {
        SessionID = s.SessionID,
        MentorshipID = s.MentorshipID,
        ScheduledStartAt = s.ScheduledStartAt,
        DurationMinutes = s.DurationMinutes,
        SessionFormat = s.SessionFormat,
        MeetingURL = hideMeetingUrl ? null : s.MeetingURL,
        SessionStatus = s.SessionStatus,
        TopicsDiscussed = s.TopicsDiscussed,
        KeyInsights = s.KeyInsights,
        ActionItems = s.ActionItems,
        NextSteps = s.NextSteps,
        Note = s.Note,
        ProposedBy = s.ProposedBy,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt,
        StartupConfirmedConductedAt = s.StartupConfirmedConductedAt,
        DisputeReason = s.DisputeReason,
        ResolutionNote = s.ResolutionNote,
        MarkedByStaffID = s.MarkedByStaffID,
        MarkedAt = s.MarkedAt
    };

    private static ReportDto MapReportDto(MentorshipReport r, string? callerType = null) => new()
    {
        ReportID = r.ReportID,
        MentorshipID = r.MentorshipID,
        SessionID = r.SessionID,
        CreatedByAdvisorID = r.CreatedByAdvisorID,
        ReportSummary = r.ReportSummary,
        DetailedFindings = r.DetailedFindings,
        Recommendations = r.Recommendations,
        AttachmentsURL = r.AttachmentsURL,
        SubmittedAt = r.SubmittedAt,
        StartupAcknowledgedAt = r.StartupAcknowledgedAt,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
        IsLatestForSession = r.SupersededByReportID == null,
        ReviewStatus = r.ReportReviewStatus.ToString(),
        StaffReviewNote = string.Equals(callerType, "Startup", StringComparison.OrdinalIgnoreCase) ? null : r.StaffReviewNote,
        ReviewedAt = r.ReviewedAt,
        IssueReportDeadlineAt = r.SubmittedAt.HasValue ? r.SubmittedAt.Value.AddHours(24) : null,
        CanSubmitIssueReport = r.SubmittedAt.HasValue && DateTime.UtcNow <= r.SubmittedAt.Value.AddHours(24)
    };

    private static FeedbackDto MapFeedbackDto(MentorshipFeedback f) => new()
    {
        FeedbackID = f.FeedbackID,
        MentorshipID = f.MentorshipID,
        SessionID = f.SessionID,
        FromRole = f.FromRole,
        Rating = f.Rating,
        Comment = f.Comment,
        SubmittedAt = f.SubmittedAt
    };

    // ================================================================
    // COMPLETE MENTORSHIP (Advisor) — DISABLED (BR-09: only Staff marks completed)
    // ================================================================

    public Task<ApiResponse<MentorshipDto>> CompleteAsync(int userId, int mentorshipId)
    {
        return Task.FromResult(ApiResponse<MentorshipDto>.ErrorResponse("COMPLETION_BY_ADVISOR_DISABLED",
            "Mentorship completion is now handled by Operations Staff. Please submit your report and wait for staff review."));
    }

    // ================================================================
    // GET SESSIONS FOR A MENTORSHIP
    // ================================================================

    public async Task<ApiResponse<List<SessionDto>>> GetMentorshipSessionsAsync(int userId, string userType, int mentorshipId)
    {
        var mentorship = await _db.StartupAdvisorMentorships
            .Include(m => m.Startup)
            .Include(m => m.Advisor)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MentorshipID == mentorshipId);

        if (mentorship == null)
            return ApiResponse<List<SessionDto>>.ErrorResponse("MENTORSHIP_NOT_FOUND", "Mentorship not found.");

        if (!await IsParticipantOrStaff(userId, userType, mentorship))
            return ApiResponse<List<SessionDto>>.ErrorResponse("MENTORSHIP_NOT_OWNED", "Access denied.");

        var rawSessions = await _db.MentorshipSessions
            .Where(s => s.MentorshipID == mentorshipId)
            .AsNoTracking()
            .OrderByDescending(s => s.ScheduledStartAt)
            .ToListAsync();

        bool hideMeetingUrl = userType == "Startup" && mentorship.PaymentStatus != PaymentStatus.Completed;
        var sessions = rawSessions.Select(s => MapSessionDto(s, hideMeetingUrl)).ToList();

        return ApiResponse<List<SessionDto>>.SuccessResponse(sessions);
    }

    // ================================================================
    // GET FEEDBACKS FOR A MENTORSHIP
    // ================================================================

    public async Task<ApiResponse<List<FeedbackDto>>> GetMentorshipFeedbacksAsync(int userId, string userType, int mentorshipId)
    {
        var mentorship = await _db.StartupAdvisorMentorships
            .Include(m => m.Startup)
            .Include(m => m.Advisor)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MentorshipID == mentorshipId);

        if (mentorship == null)
            return ApiResponse<List<FeedbackDto>>.ErrorResponse("MENTORSHIP_NOT_FOUND", "Mentorship not found.");

        if (!await IsParticipantOrStaff(userId, userType, mentorship))
            return ApiResponse<List<FeedbackDto>>.ErrorResponse("MENTORSHIP_NOT_OWNED", "Access denied.");

        var feedbacks = await _db.MentorshipFeedbacks
            .Where(f => f.MentorshipID == mentorshipId)
            .AsNoTracking()
            .OrderByDescending(f => f.SubmittedAt)
            .Select(f => MapFeedbackDto(f))
            .ToListAsync();

        return ApiResponse<List<FeedbackDto>>.SuccessResponse(feedbacks);
    }

    // ================================================================
    // STAFF OVERSIGHT — GET REPORTS FOR OVERSIGHT (SVC-1)
    // ================================================================

    public async Task<ApiResponse<PagedResponse<ReportOversightDto>>> GetReportsForOversightAsync(
        string? reviewStatus, int? advisorId, int? startupId,
        DateTime? from, DateTime? to, int page, int pageSize)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        var query = _db.MentorshipReports
            .Include(r => r.Mentorship).ThenInclude(m => m.Startup)
            .Include(r => r.Mentorship).ThenInclude(m => m.Advisor)
            .Include(r => r.Session)
            .Where(r => r.SupersededByReportID == null
                     && r.ReportReviewStatus != ReportReviewStatus.Draft)  // Draft never enters staff queue
            .AsNoTracking();

        // PendingReview không còn xuất hiện trong luồng bình thường (auto-approve).
        // Default: show all (Staff tự filter theo nhu cầu).
        if (!string.IsNullOrEmpty(reviewStatus) && reviewStatus != "all"
            && Enum.TryParse<ReportReviewStatus>(reviewStatus, out var s))
            query = query.Where(r => r.ReportReviewStatus == s);

        if (advisorId.HasValue)
            query = query.Where(r => r.CreatedByAdvisorID == advisorId.Value);
        if (startupId.HasValue)
            query = query.Where(r => r.Mentorship.StartupID == startupId.Value);
        if (from.HasValue)
            query = query.Where(r => r.SubmittedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(r => r.SubmittedAt <= to.Value);

        query = query.OrderByDescending(r => r.SubmittedAt);

        var totalItems = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReportOversightDto
            {
                ReportID = r.ReportID,
                MentorshipID = r.MentorshipID,
                SessionID = r.SessionID,
                AdvisorID = r.CreatedByAdvisorID,
                AdvisorName = r.Mentorship.Advisor.FullName,
                StartupID = r.Mentorship.StartupID,
                StartupName = r.Mentorship.Startup.CompanyName,
                ReportSummary = r.ReportSummary,
                DetailedFindings = r.DetailedFindings,
                Recommendations = r.Recommendations,
                AttachmentsURL = r.AttachmentsURL,
                SubmittedAt = r.SubmittedAt,
                ReviewStatus = r.ReportReviewStatus.ToString(),
                ReviewedByStaffID = r.ReviewedByStaffID,
                StaffReviewNote = r.StaffReviewNote,
                ReviewedAt = r.ReviewedAt,
                SupersededByReportID = r.SupersededByReportID,
                IsLatestForSession = r.SupersededByReportID == null,
                SessionStatus = r.Session != null ? r.Session.SessionStatus : null,
                StartupConfirmedConductedAt = r.Session != null ? r.Session.StartupConfirmedConductedAt : null,
                StartupAcknowledgedAt = r.StartupAcknowledgedAt,
                MentorshipStatus = r.Mentorship.MentorshipStatus.ToString(),
                ChallengeDescription = r.Mentorship.ChallengeDescription
            })
            .ToListAsync();

        return ApiResponse<PagedResponse<ReportOversightDto>>.SuccessResponse(
            new PagedResponse<ReportOversightDto>
            {
                Items = items,
                Paging = new PagingInfo { Page = page, PageSize = pageSize, TotalItems = totalItems }
            });
    }

    // ================================================================
    // STAFF OVERSIGHT — REVIEW REPORT (SVC-2)
    // ================================================================

    public async Task<ApiResponse<ReportReviewResultDto>> ReviewReportAsync(
        int staffUserId, int reportId, ReviewReportRequest request)
    {
        var report = await _db.MentorshipReports
            .Include(r => r.Session)
            .Include(r => r.Mentorship).ThenInclude(m => m.Reports)
            .Include(r => r.Mentorship).ThenInclude(m => m.Sessions)
            .FirstOrDefaultAsync(r => r.ReportID == reportId);

        if (report == null)
            return ApiResponse<ReportReviewResultDto>.ErrorResponse("REPORT_NOT_FOUND", "Report not found.");

        if (!Enum.TryParse<ReportReviewStatus>(request.ReviewStatus, out var newStatus)
            || newStatus == ReportReviewStatus.PendingReview)
            return ApiResponse<ReportReviewResultDto>.ErrorResponse("INVALID_REVIEW_STATUS",
                "Review status must be Passed, Failed, or NeedsMoreInfo.");

        // Guard: session must be Conducted or beyond — cannot review a report for a session that hasn't happened yet
        var allowedSessionStatuses = new[]
        {
            SessionStatusValues.Conducted,
            SessionStatusValues.InDispute,
            SessionStatusValues.Completed,
            SessionStatusValues.Resolved
        };
        if (report.Session == null || !allowedSessionStatuses.Contains(report.Session.SessionStatus))
            return ApiResponse<ReportReviewResultDto>.ErrorResponse("SESSION_NOT_CONDUCTED",
                "Cannot review report — startup has not confirmed the session as conducted yet.");

        report.ReportReviewStatus = newStatus;
        report.ReviewedByStaffID = staffUserId;
        report.StaffReviewNote = request.Note;
        report.ReviewedAt = DateTime.UtcNow;

        // Auto-complete session if report passed and already Conducted
        if (newStatus == ReportReviewStatus.Passed && report.Session != null && report.Session.SessionStatus == SessionStatusValues.Conducted)
        {
            report.Session.SessionStatus = SessionStatusValues.Completed;
            report.Session.MarkedByStaffID = staffUserId;
            report.Session.MarkedAt = DateTime.UtcNow;
            report.Session.UpdatedAt = DateTime.UtcNow;
            await UpdateAdvisorStatsAsync(report.MentorshipID);
        }

        RecalculatePayoutEligibility(report.Mentorship);

        await _db.SaveChangesAsync();
        await _audit.LogAsync("REVIEW_REPORT", "MentorshipReport", reportId, $"Status={newStatus}");

        return ApiResponse<ReportReviewResultDto>.SuccessResponse(new ReportReviewResultDto
        {
            ReportID = report.ReportID,
            MentorshipID = report.MentorshipID,
            ReviewStatus = newStatus.ToString(),
            StaffReviewNote = report.StaffReviewNote,
            ReviewedByStaffID = staffUserId,
            ReviewedAt = report.ReviewedAt
        }, "Report reviewed successfully.");
    }

    // ================================================================
    // STARTUP — CONFIRM CONDUCTED (SVC-3)
    // ================================================================

    public async Task<ApiResponse<SessionDto>> ConfirmConductedAsync(int userId, int mentorshipId, int sessionId)
    {
        var startup = await _db.Startups.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserID == userId);
        if (startup == null)
            return ApiResponse<SessionDto>.ErrorResponse("STARTUP_PROFILE_NOT_FOUND", "Startup profile not found.");

        var mentorship = await _db.StartupAdvisorMentorships
            .FirstOrDefaultAsync(m => m.MentorshipID == mentorshipId);
        if (mentorship == null)
            return ApiResponse<SessionDto>.ErrorResponse("MENTORSHIP_NOT_FOUND", "Mentorship not found.");
        if (mentorship.StartupID != startup.StartupID)
            return ApiResponse<SessionDto>.ErrorResponse("MENTORSHIP_NOT_OWNED",
                "You are not the startup for this mentorship.");

        var session = await _db.MentorshipSessions
            .FirstOrDefaultAsync(s => s.SessionID == sessionId && s.MentorshipID == mentorshipId);
        if (session == null)
            return ApiResponse<SessionDto>.ErrorResponse("SESSION_NOT_FOUND", "Session not found.");

        if (session.SessionStatus != SessionStatusValues.Scheduled
            && session.SessionStatus != SessionStatusValues.InProgress)
            return ApiResponse<SessionDto>.ErrorResponse("INVALID_STATUS_TRANSITION",
                "Session must be Scheduled or InProgress to confirm conducted.");

        if (mentorship.PaymentStatus != PaymentStatus.Completed)
            return ApiResponse<SessionDto>.ErrorResponse("PAYMENT_REQUIRED",
                "Payment must be completed before confirming the session as conducted.");

        var confirmNow = DateTime.UtcNow;
        session.SessionStatus = SessionStatusValues.Conducted;
        session.StartupConfirmedConductedAt = confirmNow;
        session.UpdatedAt = confirmNow;

        // Nếu Advisor đã submit report Passed trước — auto-complete ngay
        var hasPassedReport = await _db.MentorshipReports
            .AnyAsync(r => r.SessionID == sessionId
                        && r.MentorshipID == mentorshipId
                        && r.ReportReviewStatus == ReportReviewStatus.Passed
                        && r.SupersededByReportID == null);
        if (hasPassedReport)
        {
            session.SessionStatus = SessionStatusValues.Completed;
            var mentorshipFull = await _db.StartupAdvisorMentorships
                .Include(m => m.Sessions)
                .Include(m => m.Reports)
                .FirstOrDefaultAsync(m => m.MentorshipID == mentorshipId);
            if (mentorshipFull != null)
            {
                RecalculateMentorshipStatus(mentorshipFull);
                RecalculatePayoutEligibility(mentorshipFull);
                await _db.SaveChangesAsync();
                await UpdateAdvisorStatsAsync(mentorshipId);
            }
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync("CONFIRM_CONDUCTED", "MentorshipSession", sessionId,
            $"MentorshipId={mentorshipId}, AutoCompleted={hasPassedReport}");

        return ApiResponse<SessionDto>.SuccessResponse(MapSessionDto(session),
            "Session confirmed as conducted.");
    }

    // ================================================================
    // STAFF — MARK SESSION COMPLETED (SVC-4)
    // ================================================================

    public async Task<ApiResponse<SessionOversightResultDto>> MarkSessionCompletedAsync(
        int staffUserId, int mentorshipId, int sessionId, string? note)
    {
        var session = await _db.MentorshipSessions
            .Include(s => s.Reports)
            .Include(s => s.Mentorship).ThenInclude(m => m.Sessions)
            .Include(s => s.Mentorship).ThenInclude(m => m.Reports)
            .FirstOrDefaultAsync(s => s.SessionID == sessionId);

        if (session == null || session.MentorshipID != mentorshipId)
            return ApiResponse<SessionOversightResultDto>.ErrorResponse("SESSION_NOT_FOUND",
                "Session not found.");

        if (session.SessionStatus != SessionStatusValues.Conducted)
            return ApiResponse<SessionOversightResultDto>.ErrorResponse("SESSION_NOT_CONDUCTED",
                "Startup must confirm conducted before staff can mark completed.");

        var currentReports = session.Reports
            .Where(r => r.SupersededByReportID == null).ToList();
        if (!currentReports.Any())
            return ApiResponse<SessionOversightResultDto>.ErrorResponse("NO_REPORT",
                "Session must have at least one report.");
        if (currentReports.Any(r => r.ReportReviewStatus != ReportReviewStatus.Passed))
            return ApiResponse<SessionOversightResultDto>.ErrorResponse("REPORTS_NOT_ALL_PASSED",
                "Cannot mark completed — not all reports have passed review.");

        session.SessionStatus = SessionStatusValues.Completed;
        session.MarkedByStaffID = staffUserId;
        session.MarkedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;

        RecalculateMentorshipStatus(session.Mentorship);
        RecalculatePayoutEligibility(session.Mentorship);

        await _db.SaveChangesAsync();
        await UpdateAdvisorStatsAsync(mentorshipId);
        await _audit.LogAsync("STAFF_MARK_SESSION_COMPLETED", "MentorshipSession", sessionId, note);

        return ApiResponse<SessionOversightResultDto>.SuccessResponse(
            MapSessionOversightResult(session), "Session marked as completed.");
    }

    // ================================================================
    // STAFF — MARK SESSION DISPUTE (SVC-5)
    // ================================================================

    public async Task<ApiResponse<SessionOversightResultDto>> MarkSessionDisputeAsync(
        int staffUserId, int mentorshipId, int sessionId, string reason)
    {
        var session = await _db.MentorshipSessions
            .Include(s => s.Mentorship).ThenInclude(m => m.Sessions)
            .Include(s => s.Mentorship).ThenInclude(m => m.Reports)
            .Include(s => s.Mentorship).ThenInclude(m => m.Startup)
            .Include(s => s.Mentorship).ThenInclude(m => m.Advisor)
            .FirstOrDefaultAsync(s => s.SessionID == sessionId);

        if (session == null || session.MentorshipID != mentorshipId)
            return ApiResponse<SessionOversightResultDto>.ErrorResponse("SESSION_NOT_FOUND",
                "Session not found.");

        var allowedStatuses = new[] {
            SessionStatusValues.Scheduled, SessionStatusValues.InProgress,
            SessionStatusValues.Conducted, SessionStatusValues.Completed,
            SessionStatusValues.Resolved
        };
        if (!allowedStatuses.Contains(session.SessionStatus))
            return ApiResponse<SessionOversightResultDto>.ErrorResponse("INVALID_STATUS_TRANSITION",
                "Session state transition is invalid.");

        session.SessionStatus = SessionStatusValues.InDispute;
        session.DisputeReason = reason;
        session.ResolutionNote = null;
        session.MarkedByStaffID = staffUserId;
        session.MarkedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;

        // Link with IssueReports to show up in the Complaints & Disputes page
        // A report could be linked to the Session, the Mentorship, or an AdvisorReport related to this session.
        var issueReport = await _db.IssueReports
            .FirstOrDefaultAsync(ir => (ir.RelatedEntityID == sessionId && ir.RelatedEntityType == "Session") ||
                                       (ir.RelatedEntityID == session.MentorshipID && ir.RelatedEntityType == "Mentorship") ||
                                       (ir.RelatedEntityType == "AdvisorReport" && _db.MentorshipReports.Any(mr => mr.ReportID == ir.RelatedEntityID && mr.SessionID == sessionId)));
        
        if (issueReport == null)
            return ApiResponse<SessionOversightResultDto>.ErrorResponse("NO_EXISTING_REPORT", 
                "Cannot open a dispute without an existing user-initiated issue report. Please ensure the user has reported this session or the entire mentorship.");

        issueReport.Status = IssueReportStatus.Escalated;
        issueReport.UpdatedAt = DateTime.UtcNow;
        if (issueReport.AssignedToStaffID == null)
            issueReport.AssignedToStaffID = staffUserId;

        RecalculateMentorshipStatus(session.Mentorship);

        RecalculatePayoutEligibility(session.Mentorship);

        await _db.SaveChangesAsync();

        // Notify Startup and Advisor
        var startupUserId = session.Mentorship.Startup?.UserID;
        var advisorUserId = session.Mentorship.Advisor?.UserID;

        if (startupUserId.HasValue)
        {
            await _notifications.CreateAndPushAsync(new CreateNotificationRequest
            {
                UserId = startupUserId.Value,
                NotificationType = "CONSULTING",
                Title = "Phiên tư vấn bị tranh chấp",
                Message = $"Phiên tư vấn #{sessionId} của bạn đã được chuyển sang trạng thái tranh chấp và đang được xem xét.",
                RelatedEntityType = "MentorshipSession",
                RelatedEntityId = sessionId,
                ActionUrl = $"/startup/mentorship-requests/{mentorshipId}"
            });
        }

        if (advisorUserId.HasValue)
        {
            await _notifications.CreateAndPushAsync(new CreateNotificationRequest
            {
                UserId = advisorUserId.Value,
                NotificationType = "CONSULTING",
                Title = "Phiên tư vấn bị tranh chấp",
                Message = $"Phiên tư vấn #{sessionId} với Startup '{session.Mentorship.Startup?.CompanyName}' đã bị khiếu nại và đang được xem xét.",
                RelatedEntityType = "MentorshipSession",
                RelatedEntityId = sessionId,
                ActionUrl = $"/advisor/requests/{mentorshipId}"
            });
        }

        await _audit.LogAsync("STAFF_MARK_SESSION_DISPUTE", "MentorshipSession", sessionId, reason);

        return ApiResponse<SessionOversightResultDto>.SuccessResponse(
            MapSessionOversightResult(session, session?.Mentorship), "Session marked as in dispute.");
    }

    // ================================================================
    // STAFF — MARK SESSION RESOLVED (SVC-6)
    // ================================================================

    public async Task<ApiResponse<SessionOversightResultDto>> MarkSessionResolvedAsync(
        int staffUserId, int mentorshipId, int sessionId, ResolveDisputeRequest request)
    {
        try 
        {
            MentorshipSession? session = null;
            StartupAdvisorMentorship? mentorship = null;

            if (sessionId > 0)
            {
                session = await _db.MentorshipSessions
                    .Include(s => s.Mentorship).ThenInclude(m => m.Sessions)
                    .Include(s => s.Mentorship).ThenInclude(m => m.Reports)
                    .Include(s => s.Mentorship).ThenInclude(m => m.Startup)
                    .Include(s => s.Mentorship).ThenInclude(m => m.Advisor)
                    .FirstOrDefaultAsync(s => s.SessionID == sessionId);

                if (session == null || session.MentorshipID != mentorshipId)
                    return ApiResponse<SessionOversightResultDto>.ErrorResponse("SESSION_NOT_FOUND",
                        "Session not found.");
                
                mentorship = session.Mentorship;
            }
            else
            {
                mentorship = await _db.StartupAdvisorMentorships
                    .Include(m => m.Sessions)
                    .Include(m => m.Reports)
                    .Include(m => m.Startup)
                    .Include(m => m.Advisor)
                    .FirstOrDefaultAsync(m => m.MentorshipID == mentorshipId);
                    
                if (mentorship == null)
                    return ApiResponse<SessionOversightResultDto>.ErrorResponse("MENTORSHIP_NOT_FOUND",
                        "Mentorship not found.");
            }

            if (mentorship == null)
                return ApiResponse<SessionOversightResultDto>.ErrorResponse("MENTORSHIP_NOT_FOUND", "Mentorship not found.");

            bool validStatus = false;
            if (session != null && (session.SessionStatus == SessionStatusValues.InDispute || session.SessionStatus == SessionStatusValues.Resolved)) validStatus = true;
            if (mentorship.MentorshipStatus == MentorshipStatus.InDispute || mentorship.MentorshipStatus == MentorshipStatus.Resolved) validStatus = true;

            if (!validStatus)
                return ApiResponse<SessionOversightResultDto>.ErrorResponse("INVALID_STATUS_TRANSITION",
                    "Session or Mentorship must be InDispute or already Resolved to update resolution.");

            // Guard: Must be paid to refund
            if (request.RefundToStartup && mentorship.PaymentStatus != PaymentStatus.Completed)
                return ApiResponse<SessionOversightResultDto>.ErrorResponse("PAYMENT_NOT_COMPLETED",
                    "Cannot refund — startup has not completed payment for this mentorship.");

            // Guard: Cannot refund if already refunded
            if (request.RefundToStartup && mentorship.RefundedAt != null)
                return ApiResponse<SessionOversightResultDto>.ErrorResponse("ALREADY_REFUNDED",
                    "A refund has already been issued for this mentorship.");

            // Guard: Cannot refund if payout already released to advisor
            if (request.RefundToStartup && mentorship.PayoutReleasedAt != null)
                return ApiResponse<SessionOversightResultDto>.ErrorResponse("PAYOUT_ALREADY_RELEASED",
                    "Cannot refund — payout has already been released to advisor.");

            if (session != null)
            {
                session.ResolutionNote = request.Resolution;
                session.MarkedByStaffID = staffUserId;
                session.MarkedAt = DateTime.UtcNow;
                session.UpdatedAt = DateTime.UtcNow;

                if (request.RestoreCompleted)
                {
                    session.SessionStatus = SessionStatusValues.Completed;
                }
                else
                {
                    session.SessionStatus = SessionStatusValues.Resolved;
                }
            }
            else 
            {
                // If resolving at mentorship level, update all sessions that are currently in dispute or conducted
                var sessionsToUpdate = mentorship.Sessions
                    .Where(s => s.SessionStatus == SessionStatusValues.InDispute || s.SessionStatus == SessionStatusValues.Conducted)
                    .ToList();
                
                foreach (var s in sessionsToUpdate)
                {
                    s.SessionStatus = request.RestoreCompleted ? SessionStatusValues.Completed : SessionStatusValues.Resolved;
                    s.ResolutionNote = request.Resolution;
                    s.MarkedByStaffID = staffUserId;
                    s.MarkedAt = DateTime.UtcNow;
                    s.UpdatedAt = DateTime.UtcNow;
                }
            }

            if (request.RestoreCompleted)
            {
                // Automatically release payout to advisor
                await ReleasePayoutAsync(staffUserId, mentorship.MentorshipID);
                
                // For rejections, we recalculate to see if it should be InProgress or Completed
                RecalculateMentorshipStatus(mentorship);
            }

            // Refund logic
            if (request.RefundToStartup)
            {
                var refundAmount = mentorship.SessionAmount; // Refund the whole paid amount

                var wallet = await _db.StartupWallets.FirstOrDefaultAsync(w => w.StartupId == mentorship.StartupID);
                if (wallet == null)
                {
                    wallet = new StartupWallet
                    {
                        StartupId = mentorship.StartupID,
                        CreatedAt = DateTime.UtcNow,
                        Balance = 0,
                        TotalRefunded = 0,
                        TotalWithdrawn = 0
                    };
                    _db.StartupWallets.Add(wallet);
                }

                var tx = new WalletTransaction
                {
                    StartupWallet = wallet,
                    MentorshipID = mentorship.MentorshipID,
                    Amount = refundAmount,
                    Type = TransactionType.Refund,
                    Status = TransactionStatus.Completed,
                    CreatedAt = DateTime.UtcNow
                };
                _db.WalletTransactions.Add(tx);

                wallet.Balance += refundAmount;
                wallet.TotalRefunded += refundAmount;

                // Mark as refunded
                mentorship.RefundedAt = DateTime.UtcNow;

                // Close all remaining sessions since 100% refund issued
                var allSessions = mentorship.Sessions.ToList();
                foreach (var s in allSessions)
                {
                    if (s.SessionStatus != SessionStatusValues.Cancelled && s.SessionStatus != SessionStatusValues.Completed)
                    {
                        s.SessionStatus = SessionStatusValues.Resolved;
                        s.ResolutionNote = "Mentorship fully refunded: " + request.Resolution;
                        s.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

            // ALWAYS mark mentorship as Resolved if we are refunding or if it's currently InDispute
            // OR if a specific session was in dispute and now it's being resolved
            if (request.RefundToStartup || 
                mentorship.MentorshipStatus == MentorshipStatus.InDispute || 
                (session != null && session.SessionStatus == SessionStatusValues.InDispute))
            {
                mentorship.MentorshipStatus = MentorshipStatus.Resolved;
                mentorship.UpdatedAt = DateTime.UtcNow;
            }

            RecalculatePayoutEligibility(mentorship);

            // Update related IssueReport status
            IssueReport? relatedReport = null;
            if (session != null)
            {
                relatedReport = await _db.IssueReports
                    .FirstOrDefaultAsync(ir => ir.RelatedEntityID == sessionId && ir.RelatedEntityType == "Session");
            }
            
            if (relatedReport == null)
            {
                // Also try matching by mentorshipId (Mentorship or Payment disputes)
                relatedReport = await _db.IssueReports
                    .FirstOrDefaultAsync(ir => ir.RelatedEntityID == mentorshipId && 
                                               (ir.RelatedEntityType == "Mentorship" || ir.RelatedEntityType == "Payment"));
            }
            if (relatedReport == null && mentorship.Reports != null && mentorship.Reports.Any())
            {
                // Also try matching by AdvisorReport disputes linked to this mentorship
                var reportIds = mentorship.Reports.Select(r => r.ReportID).ToList();
                if (reportIds.Any())
                {
                    var issueReports = await _db.IssueReports
                        .Where(ir => ir.RelatedEntityType == "AdvisorReport" && ir.RelatedEntityID != null)
                        .ToListAsync();
                        
                    relatedReport = issueReports
                        .FirstOrDefault(ir => ir.RelatedEntityID.HasValue && reportIds.Contains(ir.RelatedEntityID.Value));
                }
            }
            if (relatedReport != null)
            {
                relatedReport.Status = request.RefundToStartup
                    ? IssueReportStatus.Resolved
                    : IssueReportStatus.Dismissed;
                relatedReport.StaffNote = request.Resolution;
                relatedReport.AssignedToStaffID = staffUserId;
                relatedReport.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            // Notify Startup and Advisor
            var startupUserId = mentorship.Startup?.UserID;
            var advisorUserId = mentorship.Advisor?.UserID;
            var resolutionType = request.RestoreCompleted ? "đã được khôi phục hoàn thành" : (request.RefundToStartup ? "đã hoàn tiền cho Startup" : "đã được giải quyết");

            if (startupUserId.HasValue)
            {
                await _notifications.CreateAndPushAsync(new CreateNotificationRequest
                {
                    UserId = startupUserId.Value,
                    NotificationType = "CONSULTING",
                    Title = "Tranh chấp đã được xử lý",
                    Message = session != null 
                        ? $"Tranh chấp tại phiên tư vấn #{sessionId} đã được xử lý: {resolutionType}."
                        : $"Tranh chấp yêu cầu tư vấn #{mentorshipId} đã được xử lý: {resolutionType}.",
                    RelatedEntityType = "MentorshipSession",
                    RelatedEntityId = session?.SessionID ?? mentorshipId,
                    ActionUrl = $"/startup/mentorship-requests/{mentorshipId}"
                });
            }

            if (advisorUserId.HasValue)
            {
                await _notifications.CreateAndPushAsync(new CreateNotificationRequest
                {
                    UserId = advisorUserId.Value,
                    NotificationType = "CONSULTING",
                    Title = "Tranh chấp đã được xử lý",
                    Message = session != null 
                        ? $"Tranh chấp tại phiên tư vấn #{sessionId} của bạn đã được xử lý: {resolutionType}."
                        : $"Tranh chấp yêu cầu tư vấn #{mentorshipId} của bạn đã được xử lý: {resolutionType}.",
                    RelatedEntityType = "MentorshipSession",
                    RelatedEntityId = session?.SessionID ?? mentorshipId,
                    ActionUrl = $"/advisor/requests/{mentorshipId}"
                });
            }

            await _audit.LogAsync("STAFF_RESOLVE_DISPUTE", "Mentorship", mentorshipId, 
                $"SessionId: {sessionId}, Resolution: {request.Resolution}, Refund: {request.RefundToStartup}");

            return ApiResponse<SessionOversightResultDto>.SuccessResponse(
                MapSessionOversightResult(session, mentorship), "Dispute resolved.");
        }
        catch (Exception ex)
        {
            return ApiResponse<SessionOversightResultDto>.ErrorResponse("MARK_RESOLVED_ERROR", 
                $"An error occurred in MarkSessionResolvedAsync: {ex.Message}. Inner: {ex.InnerException?.Message}");
        }
    }

    // ================================================================
    // STAFF — RELEASE PAYOUT (SVC-9)
    // ================================================================

    public async Task<ApiResponse<ReleasePayoutResultDto>> ReleasePayoutAsync(
        int staffUserId, int mentorshipId)
    {
        var mentorship = await _db.StartupAdvisorMentorships
            .Include(m => m.Advisor)
            .FirstOrDefaultAsync(m => m.MentorshipID == mentorshipId);

        if (mentorship == null)
            return ApiResponse<ReleasePayoutResultDto>.ErrorResponse("MENTORSHIP_NOT_FOUND",
                "Mentorship not found.");

        if (!mentorship.IsPayoutEligible)
            return ApiResponse<ReleasePayoutResultDto>.ErrorResponse("NOT_ELIGIBLE",
                "Mentorship is not yet eligible for payout.");

        // Guard: Cannot release payout if already refunded
        if (mentorship.RefundedAt != null)
            return ApiResponse<ReleasePayoutResultDto>.ErrorResponse("ALREADY_REFUNDED",
                "Cannot release payout — this mentorship has been refunded to the startup.");

        // Guard idempotency — tránh credit 2 lần nếu staff bấm 2 lần
        if (mentorship.PayoutReleasedAt != null)
            return ApiResponse<ReleasePayoutResultDto>.ErrorResponse("PAYOUT_ALREADY_RELEASED",
                $"Payout was already released at {mentorship.PayoutReleasedAt:O}.");

        // Tìm hoặc tạo AdvisorWallet
        var wallet = await _db.AdvisorWallets
            .FirstOrDefaultAsync(w => w.AdvisorId == mentorship.AdvisorID);

        if (wallet == null)
            return ApiResponse<ReleasePayoutResultDto>.ErrorResponse("WALLET_NOT_FOUND",
                "Advisor does not have a wallet. Please ask advisor to create one first.");

        var releasedAt = DateTime.UtcNow;
        var amount = mentorship.ActualAmount;

        // Credit vào wallet
        wallet.Balance += amount;
        wallet.TotalEarned += amount;

        // Tạo WalletTransaction ghi nhận khoản deposit
        var transaction = new WalletTransaction
        {
            Wallet = wallet,
            MentorshipID = mentorshipId,
            Amount = amount,
            Type = TransactionType.Deposit,
            Status = TransactionStatus.Completed,
            CreatedAt = releasedAt
        };

        // Đánh dấu mentorship đã release
        mentorship.PayoutReleasedAt = releasedAt;
        mentorship.UpdatedAt = releasedAt;

        _db.AdvisorWallets.Update(wallet);
        _db.WalletTransactions.Add(transaction);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("STAFF_RELEASE_PAYOUT", "StartupAdvisorMentorship", mentorshipId,
            $"Credited {amount} to wallet {wallet.WalletId} for advisor {mentorship.AdvisorID}");

        return ApiResponse<ReleasePayoutResultDto>.SuccessResponse(new ReleasePayoutResultDto
        {
            MentorshipID = mentorshipId,
            CreditedAmount = amount,
            PayoutReleasedAt = releasedAt,
            IsPayoutEligible = mentorship.IsPayoutEligible,
            ReleasedByStaffID = staffUserId
        }, "Payout released successfully.");
    }

    // ================================================================
    // HELPER: Recalculate Mentorship Status from Sessions (SVC-7)
    // ================================================================

    private static void RecalculateMentorshipStatus(StartupAdvisorMentorship mentorship)
    {
        if (mentorship.Sessions == null) return;
        var sessions = mentorship.Sessions
            .Where(s => s.SessionStatus != SessionStatusValues.Cancelled
                      && s.SessionStatus != SessionStatusValues.ProposedByStartup
                      && s.SessionStatus != SessionStatusValues.ProposedByAdvisor)
            .ToList();

        if (!sessions.Any()) return;

        if (mentorship.MentorshipStatus == MentorshipStatus.Resolved || mentorship.MentorshipStatus == MentorshipStatus.Cancelled)
        {
            // Do not recalculate if already in a terminal state that isn't Completed
            return;
        }

        if (sessions.Any(s => s.SessionStatus == SessionStatusValues.InDispute))
        {
            mentorship.MentorshipStatus = MentorshipStatus.InDispute;
        }
        else if (sessions.All(s => s.SessionStatus == SessionStatusValues.Completed))
        {
            mentorship.MentorshipStatus = MentorshipStatus.Completed;
            mentorship.CompletedAt ??= DateTime.UtcNow;
        }
        else
        {
            // Mix: some Scheduled/InProgress/Conducted/Resolved → keep InProgress
            // BA chốt #6: Resolved ≠ Completed — intentional
            if (mentorship.MentorshipStatus == MentorshipStatus.InDispute
                || mentorship.MentorshipStatus == MentorshipStatus.Completed)
            {
                mentorship.MentorshipStatus = MentorshipStatus.InProgress;
            }
        }
        mentorship.UpdatedAt = DateTime.UtcNow;
    }

    // ================================================================
    // HELPER: Recalculate Payout Eligibility (SVC-8)
    // ================================================================

    private static void RecalculatePayoutEligibility(StartupAdvisorMentorship mentorship)
    {
        if (mentorship.Sessions == null || mentorship.Reports == null) return;

        // If already refunded, it's never eligible for payout
        if (mentorship.RefundedAt != null)
        {
            mentorship.IsPayoutEligible = false;
            return;
        }

        var activeSessions = mentorship.Sessions
            .Where(s => s.SessionStatus != SessionStatusValues.Cancelled
                      && s.SessionStatus != SessionStatusValues.ProposedByStartup
                      && s.SessionStatus != SessionStatusValues.ProposedByAdvisor)
            .ToList();

        var allSessionsCompleted = activeSessions.Any()
            && activeSessions.All(s => s.SessionStatus == SessionStatusValues.Completed);

        var allStartupConfirmed = activeSessions.All(s => s.StartupConfirmedConductedAt != null || s.SessionStatus == SessionStatusValues.Completed);

        var currentReports = mentorship.Reports
            .Where(r => r.SupersededByReportID == null);
        var allReportsPassed = currentReports.Any()
            && currentReports.All(r => r.ReportReviewStatus == ReportReviewStatus.Passed);

        var allReportsAcknowledged = currentReports.Any()
            && currentReports
                .Where(r => r.ReportReviewStatus == ReportReviewStatus.Passed)
                .All(r => r.StartupAcknowledgedAt != null);

        var noDispute = !activeSessions.Any(s => s.SessionStatus == SessionStatusValues.InDispute);

        mentorship.IsPayoutEligible =
            allSessionsCompleted && allStartupConfirmed && allReportsPassed && allReportsAcknowledged && noDispute;
    }

    // ================================================================
    // HELPER: Map Session Oversight Result
    // ================================================================

    private static SessionOversightResultDto MapSessionOversightResult(MentorshipSession? session, StartupAdvisorMentorship? mentorship = null)
    {
        var targetMentorship = session?.Mentorship ?? mentorship;
        if (targetMentorship == null) return new SessionOversightResultDto();

        return new SessionOversightResultDto
        {
            SessionID = session?.SessionID ?? 0,
            SessionStatus = session?.SessionStatus ?? string.Empty,
            DisputeReason = session?.DisputeReason,
            ResolutionNote = session?.ResolutionNote,
            MentorshipID = targetMentorship.MentorshipID,
            MentorshipStatus = targetMentorship.MentorshipStatus.ToString(),
            IsPayoutEligible = targetMentorship.IsPayoutEligible,
            MarkedByStaffID = session?.MarkedByStaffID,
            MarkedAt = session?.MarkedAt
        };
    }
    // ================================================================
    // HELPER: Update Advisor Stats (SVC-10)
    // ================================================================

    private async Task UpdateAdvisorStatsAsync(int mentorshipId)
    {
        var mentorship = await _db.StartupAdvisorMentorships
            .Include(m => m.Advisor)
            .FirstOrDefaultAsync(m => m.MentorshipID == mentorshipId);

        if (mentorship == null || mentorship.Advisor == null) return;

        var advisor = mentorship.Advisor;

        // Count all completed sessions for this advisor across all mentorships
        advisor.CompletedSessions = await _db.MentorshipSessions
            .CountAsync(s => s.Mentorship.AdvisorID == advisor.AdvisorID
                          && s.SessionStatus == SessionStatusValues.Completed);

        await _db.SaveChangesAsync();
    }
}


