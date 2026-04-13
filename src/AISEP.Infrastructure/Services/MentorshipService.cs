using AISEP.Application.Const;
using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Mentorship;
using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Domain.Enums;
using AISEP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AISEP.Infrastructure.Services;

public class MentorshipService : IMentorshipService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly ILogger<MentorshipService> _logger;

    public MentorshipService(ApplicationDbContext db, IAuditService audit, ILogger<MentorshipService> logger)
    {
        _db = db;
        _audit = audit;
        _logger = logger;
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

        // Advisor must exist
        var advisorExists = await _db.Advisors.AnyAsync(a => a.AdvisorID == request.AdvisorId);
        if (!advisorExists)
            return ApiResponse<MentorshipDto>.ErrorResponse("ADVISOR_NOT_FOUND",
                $"Advisor with id {request.AdvisorId} not found.");

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

        return ApiResponse<MentorshipDto>.SuccessResponse(MapToDto(mentorship));
    }

    // ================================================================
    // LIST MY MENTORSHIPS (Startup/Advisor)
    // ================================================================

    public async Task<ApiResponse<PagedResponse<MentorshipListItemDto>>> GetMyMentorshipsAsync(
        int userId, string userType, string? status, int page, int pageSize)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        IQueryable<StartupAdvisorMentorship> query = _db.StartupAdvisorMentorships
            .AsNoTracking()
            .Include(m => m.Startup).ThenInclude(s => s.Industry)
            .Include(m => m.Advisor)
            .Include(m => m.Reports);

        if (userType == "Startup")
        {
            var startup = await _db.Startups.AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserID == userId);
            if (startup == null)
                return ApiResponse<PagedResponse<MentorshipListItemDto>>.ErrorResponse(
                    "STARTUP_PROFILE_NOT_FOUND", "Startup profile not found.");
            query = query.Where(m => m.StartupID == startup.StartupID);
        }
        else if (userType == "Advisor")
        {
            var advisor = await _db.Advisors.AsNoTracking()
                .FirstOrDefaultAsync(a => a.UserID == userId);
            if (advisor == null)
                return ApiResponse<PagedResponse<MentorshipListItemDto>>.ErrorResponse(
                    "ADVISOR_PROFILE_NOT_FOUND", "Advisor profile not found.");
            query = query.Where(m => m.AdvisorID == advisor.AdvisorID);
        }
        else if (userType == "Staff" || userType == "Admin")
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
                StartupIndustry = m.Startup?.Industry?.IndustryName,
                StartupStage = m.Startup?.Stage?.ToString(),
                AdvisorID = m.AdvisorID,
                AdvisorName = m.Advisor?.FullName ?? "Unknown",
                Status = m.MentorshipStatus.ToString(),
                ChallengeDescription = m.ChallengeDescription,
                PreferredFormat = m.PreferredFormat,
                RequestedAt = m.RequestedAt,
                CreatedAt = m.CreatedAt,
                HasReport = m.Reports.Any(),
                ReportCount = m.Reports.Count,
                LatestReportSubmittedAt = m.Reports.Any()
                    ? m.Reports.Max(r => r.SubmittedAt)
                    : null
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
            .Include(m => m.Startup)
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

        return ApiResponse<MentorshipDetailDto>.SuccessResponse(MapToDetailDto(mentorship));
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

        // Cascade-cancel all sessions that haven't completed yet
        var pendingSessions = await _db.MentorshipSessions
            .Where(s => s.MentorshipID == mentorshipId && s.SessionStatus != SessionStatusValues.Completed)
            .ToListAsync();
        foreach (var s in pendingSessions)
        {
            s.SessionStatus = SessionStatusValues.Cancelled;
            s.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync("CANCEL_MENTORSHIP", "StartupAdvisorMentorship", mentorship.MentorshipID, reason);

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
                MeetingURL = s.MeetingURL,
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

        // Auto-fill meeting URL from advisor profile if not explicitly provided
        var advisor = await _db.Advisors.AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserID == userId);
        var resolvedMeetingUrl = request.MeetingUrl
            ?? advisor?.GoogleMeetLink
            ?? advisor?.MsTeamsLink;

        var session = new MentorshipSession
        {
            MentorshipID = mentorshipId,
            ScheduledStartAt = scheduledAt,
            DurationMinutes = request.DurationMinutes,
            SessionFormat = request.SessionFormat,
            MeetingURL = resolvedMeetingUrl,
            SessionStatus = isCounterProposal ? SessionStatusValues.ProposedByAdvisor : SessionStatusValues.Scheduled,
            CreatedAt = DateTime.UtcNow
        };

        _db.MentorshipSessions.Add(session);

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
        }

        await _db.SaveChangesAsync();

        await _audit.LogAsync("CREATE_SESSION", "MentorshipSession", session.SessionID,
            $"MentorshipId={mentorshipId}");
        _logger.LogInformation("Session {SessionId} created for mentorship {MentorshipId}",
            session.SessionID, mentorshipId);

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

        if (request.SessionStatus != null && !SessionStatusValues.All.Contains(request.SessionStatus))
            return ApiResponse<SessionDto>.ErrorResponse("INVALID_SESSION_STATUS",
                $"SessionStatus must be one of: {string.Join(", ", SessionStatusValues.All)}");

        if (request.ScheduledStartAt.HasValue) session.ScheduledStartAt = request.ScheduledStartAt.Value;
        if (request.DurationMinutes.HasValue) session.DurationMinutes = request.DurationMinutes.Value;
        if (request.SessionFormat != null) session.SessionFormat = request.SessionFormat;
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

        // Auto-fill meeting URL from advisor profile if session has none
        if (string.IsNullOrEmpty(session.MeetingURL))
        {
            var advisorProfile = await _db.Advisors.AsNoTracking()
                .FirstOrDefaultAsync(a => a.UserID == userId);
            session.MeetingURL = advisorProfile?.GoogleMeetLink ?? advisorProfile?.MsTeamsLink;
        }

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

        return ApiResponse<SessionDto>.SuccessResponse(MapSessionDto(session));
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

        // If sessionId provided, validate it belongs to this mentorship
        if (request.SessionId.HasValue)
        {
            var sessionExists = await _db.MentorshipSessions.AnyAsync(s =>
                s.SessionID == request.SessionId.Value && s.MentorshipID == mentorshipId);
            if (!sessionExists)
                return ApiResponse<ReportDto>.ErrorResponse("SESSION_NOT_FOUND",
                    "Session not found or does not belong to this mentorship.");
        }

        var report = new MentorshipReport
        {
            MentorshipID = mentorshipId,
            SessionID = request.SessionId,
            CreatedByAdvisorID = advisor.AdvisorID,
            ReportSummary = request.ReportSummary,
            DetailedFindings = request.DetailedFindings,
            Recommendations = request.Recommendations,
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _db.MentorshipReports.Add(report);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("CREATE_REPORT", "MentorshipReport", report.ReportID,
            $"MentorshipId={mentorshipId}");
        _logger.LogInformation("Report {ReportId} created for mentorship {MentorshipId}",
            report.ReportID, mentorshipId);

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

        return ApiResponse<ReportDto>.SuccessResponse(MapReportDto(report));
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
            IsPublic = false
        };

        _db.MentorshipFeedbacks.Add(feedback);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("CREATE_FEEDBACK", "MentorshipFeedback", feedback.FeedbackID,
            $"MentorshipId={mentorshipId}, Rating={request.Rating}");
        _logger.LogInformation("Feedback {FeedbackId} created for mentorship {MentorshipId}",
            feedback.FeedbackID, mentorshipId);

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

    private async Task<bool> IsParticipantOrStaff(int userId, string userType, StartupAdvisorMentorship mentorship)
    {
        if (userType == "Staff" || userType == "Admin") return true;

        if (userType == "Startup")
        {
            var startup = await _db.Startups.AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserID == userId);
            return startup != null && mentorship.StartupID == startup.StartupID;
        }

        if (userType == "Advisor")
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

    private static MentorshipDetailDto MapToDetailDto(StartupAdvisorMentorship m) => new()
    {
        MentorshipID = m.MentorshipID,
        StartupID = m.StartupID,
        StartupName = m.Startup.CompanyName,
        AdvisorID = m.AdvisorID,
        AdvisorName = m.Advisor.FullName,
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
        Sessions = m.Sessions.Select(MapSessionDto).ToList(),
        Reports = m.Reports.Select(MapReportDto).ToList(),
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

    private static SessionDto MapSessionDto(MentorshipSession s) => new()
    {
        SessionID = s.SessionID,
        MentorshipID = s.MentorshipID,
        ScheduledStartAt = s.ScheduledStartAt,
        DurationMinutes = s.DurationMinutes,
        SessionFormat = s.SessionFormat,
        MeetingURL = s.MeetingURL,
        SessionStatus = s.SessionStatus,
        TopicsDiscussed = s.TopicsDiscussed,
        KeyInsights = s.KeyInsights,
        ActionItems = s.ActionItems,
        NextSteps = s.NextSteps,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt
    };

    private static ReportDto MapReportDto(MentorshipReport r) => new()
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
        CreatedAt = r.CreatedAt
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
    // COMPLETE MENTORSHIP (Advisor)
    // ================================================================

    public async Task<ApiResponse<MentorshipDto>> CompleteAsync(int userId, int mentorshipId)
    {
        var (mentorship, error) = await GetMentorshipForAdvisor(userId, mentorshipId);
        if (mentorship == null) return error!;

        if (mentorship.MentorshipStatus != MentorshipStatus.InProgress
            && mentorship.MentorshipStatus != MentorshipStatus.Accepted)
            return ApiResponse<MentorshipDto>.ErrorResponse("INVALID_STATUS_TRANSITION",
                $"Cannot complete mentorship with status '{mentorship.MentorshipStatus}'. Must be 'InProgress' or 'Accepted'.");

        // Must have paid before advisor can mark as complete
        if (mentorship.PaymentStatus != PaymentStatus.Completed)
            return ApiResponse<MentorshipDto>.ErrorResponse("PAYMENT_REQUIRED",
                "Mentorship must be paid before it can be marked as completed.");

        // Must have at least one session that is Scheduled or InProgress (not all cancelled/proposed)
        var hasValidSession = await _db.MentorshipSessions
            .AnyAsync(s => s.MentorshipID == mentorshipId
                && (s.SessionStatus == SessionStatusValues.Scheduled
                    || s.SessionStatus == SessionStatusValues.InProgress
                    || s.SessionStatus == SessionStatusValues.Completed));
        if (!hasValidSession)
            return ApiResponse<MentorshipDto>.ErrorResponse("NO_VALID_SESSION",
                "No confirmed session found for this mentorship.");

        mentorship.MentorshipStatus = MentorshipStatus.Completed;
        mentorship.CompletedAt = DateTime.UtcNow;
        mentorship.CompletionConfirmedByAdvisor = true;
        mentorship.LastUpdatedByRole = "Advisor";
        mentorship.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _audit.LogAsync("COMPLETE_MENTORSHIP", "StartupAdvisorMentorship", mentorshipId, null);

        return ApiResponse<MentorshipDto>.SuccessResponse(MapToDto(mentorship), "Mentorship completed");
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

        var sessions = await _db.MentorshipSessions
            .Where(s => s.MentorshipID == mentorshipId)
            .AsNoTracking()
            .OrderByDescending(s => s.ScheduledStartAt)
            .Select(s => MapSessionDto(s))
            .ToListAsync();

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
}


