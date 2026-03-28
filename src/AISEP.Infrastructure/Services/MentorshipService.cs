using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Mentorship;
using AISEP.Application.DTOs.Slot;
using AISEP.Application.Extensions;
using AISEP.Application.Interfaces;
using AISEP.Application.QueryParams;
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
            ExpectedDuration = request.ExpectedDuration,
            RequestedAt = DateTime.UtcNow,
            LastUpdatedByRole = "Startup",
            CreatedAt = DateTime.UtcNow
        };

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
        int userId, string userType, MentorshipQueryParams mentorshipQuery)
    {

        var query = _db.StartupAdvisorMentorships
            .AsNoTracking()
            .Include(m => m.Startup)
            .Include(m => m.Advisor)
            .AsQueryable();

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

        var items = query
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new MentorshipListItemDto
            {
                MentorshipID = m.MentorshipID,
                StartupID = m.StartupID,
                StartupName = m.Startup.CompanyName,
                AdvisorID = m.AdvisorID,
                AdvisorName = m.Advisor.FullName,
                MentorshipStatus = m.MentorshipStatus.ToString(),
                ChallengeDescription = m.ChallengeDescription,
                RequestedAt = m.RequestedAt,
                CreatedAt = m.CreatedAt
            }).Paging(mentorshipQuery.Page, mentorshipQuery.PageSize);

        return ApiResponse<PagedResponse<MentorshipListItemDto>>.SuccessResponse(
            new PagedResponse<MentorshipListItemDto>
            {
                Items = await items.ToListAsync(),
                Paging = new PagingInfo
                {
                    Page = mentorshipQuery.Page,
                    PageSize = mentorshipQuery.PageSize,
                    TotalItems = await query.CountAsync(),
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

    // ================================================================
    // CREATE SESSION (Advisor)
    // ================================================================

    public async Task<ApiResponse<SessionDto>> CreateSessionAsync(int userId, int mentorshipId, CreateSessionRequest request)
    {
        var (mentorship, error) = await GetMentorshipForAdvisor(userId, mentorshipId);
        if (mentorship == null)
            return ApiResponse<SessionDto>.ErrorResponse(error!.Error!.Code, error.Error.Message);

        if (mentorship.MentorshipStatus != MentorshipStatus.Accepted && mentorship.MentorshipStatus != MentorshipStatus.InProgress)
            return ApiResponse<SessionDto>.ErrorResponse("INVALID_STATUS_TRANSITION",
                $"Cannot create session for mentorship with status '{mentorship.MentorshipStatus}'. Must be 'Accepted' or 'InProgress'.");

        var session = new MentorshipSession
        {
            MentorshipID = mentorshipId,
            ScheduledStartAt = request.ScheduledStartAt,
            DurationMinutes = request.DurationMinutes,
            MeetingURL = request.MeetingUrl,
        };

        _db.MentorshipSessions.Add(session);

        // Move mentorship to InProgress if still Accepted
        if (mentorship.MentorshipStatus == MentorshipStatus.Accepted)
        {
            mentorship.MentorshipStatus = MentorshipStatus.InProgress;
            mentorship.LastUpdatedByRole = "Advisor";
            mentorship.UpdatedAt = DateTime.UtcNow;
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

        if (request.ScheduledStartAt.HasValue) session.ScheduledStartAt = request.ScheduledStartAt.Value;
        if (request.DurationMinutes.HasValue) session.DurationMinutes = request.DurationMinutes.Value;
        if (request.MeetingUrl != null) session.MeetingURL = request.MeetingUrl;
        if (request.SessionStatus != null) session.SessionStatus = request.SessionStatus;
        if (request.TopicsDiscussed != null) session.TopicsDiscussed = request.TopicsDiscussed;

        await _db.SaveChangesAsync();

        await _audit.LogAsync("UPDATE_SESSION", "MentorshipSession", sessionId, null);
        _logger.LogInformation("Session {SessionId} updated", sessionId);

        return ApiResponse<SessionDto>.SuccessResponse(MapSessionDto(session));
    }

    // ================================================================
    // GET SESSION 
    // ================================================================

    public async Task<ApiResponse<PagedResponse<SessionDto>>> GetSessions(int userId, string userType, SessionQueryParams sessionQuery)
    {
        var query = _db.MentorshipSessions.AsNoTracking().AsQueryable();

        if (userType == "Startup")
        {
            var startup = await _db.Startups.AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserID == userId);

            if (startup == null)
                return ApiResponse<PagedResponse<SessionDto>>.ErrorResponse(
                    "STARTUP_PROFILE_NOT_FOUND", "Startup profile not found.");

            query = query.Where(m => m.Mentorship.StartupID == startup.StartupID);
        }
        else if (userType == "Advisor")
        {
            var advisor = await _db.Advisors.AsNoTracking()
                .FirstOrDefaultAsync(a => a.UserID == userId);
            if (advisor == null)
                return ApiResponse<PagedResponse<SessionDto>>.ErrorResponse(
                    "ADVISOR_PROFILE_NOT_FOUND", "Advisor profile not found.");
            query = query.Where(m => m.Mentorship.AdvisorID == advisor.AdvisorID);
        }

        var sessionsToDto = query
        .OrderByDescending(s => s.CreatedAt)
        .Select(s => new SessionDto
        {
            SessionID = s.SessionID,
            MentorshipID = s.MentorshipID,
            ScheduledStartAt = s.ScheduledStartAt,
            DurationMinutes = s.DurationMinutes,
            MeetingURL = s.MeetingURL,
            SessionStatus = s.SessionStatus.ToString(),
            TopicsDiscussed = s.TopicsDiscussed,
            CreatedAt = s.CreatedAt,
        }).Paging(sessionQuery.Page, sessionQuery.PageSize);

        return ApiResponse<PagedResponse<SessionDto>>.SuccessResponse(
            new PagedResponse<SessionDto>
            {
                Items = await sessionsToDto.ToListAsync(),
                Paging = new PagingInfo
                {
                    Page = sessionQuery.Page,
                    PageSize = sessionQuery.PageSize,
                    TotalItems = await query.CountAsync(),
                }
            });
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
    // SLOT (Startup)
    // ================================================================

    public async Task<ApiResponse<AvailableSlotDto>> CreateAvailableSlotAsync(int userId, CreateAvailableSlotRequest request)
    {
        var advisor = await _db.Advisors.AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserID == userId);
        if (advisor == null)
            return ApiResponse<AvailableSlotDto>.ErrorResponse("ADVISOR_PROFILE_NOT_FOUND",
                "Advisor profile not found.");

        if (request.EndTime <= request.StartTime)
            return ApiResponse<AvailableSlotDto>.ErrorResponse("INVALID_TIME_RANGE",
                "EndTime must be after StartTime.");

        var slot = new AdvisorAvailableSlot
        {
            AdvisorID = advisor.AdvisorID,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _db.AdvisorAvailableSlots.Add(slot);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("CREATE_AVAILABLE_SLOT", "AdvisorAvailableSlot", slot.SlotID,
            $"AdvisorId={advisor.AdvisorID}, StartTime={request.StartTime}, EndTime={request.EndTime}");
        _logger.LogInformation("Available slot {SlotId} created by advisor {AdvisorId}",
            slot.SlotID, advisor.AdvisorID);

        return ApiResponse<AvailableSlotDto>.SuccessResponse(MapToAvailableSlotDto(slot));
    }

    public async Task<ApiResponse<List<AvailableSlotDto>>> CreateMultipleAvailableSlotsAsync(int userId, CreateMultipleAvailableSlotsRequest request)
    {
        var advisor = await _db.Advisors.AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserID == userId);
        if (advisor == null)
            return ApiResponse<List<AvailableSlotDto>>.ErrorResponse("ADVISOR_PROFILE_NOT_FOUND",
                "Advisor profile not found.");

        if (request.Slots == null || request.Slots.Count == 0)
            return ApiResponse<List<AvailableSlotDto>>.ErrorResponse("EMPTY_SLOTS",
                "At least one slot must be provided.");

        var slots = new List<AdvisorAvailableSlot>();

        foreach (var slotReq in request.Slots)
        {
            if (slotReq.EndTime <= slotReq.StartTime)
                return ApiResponse<List<AvailableSlotDto>>.ErrorResponse("INVALID_TIME_RANGE",
                    $"EndTime must be after StartTime for slot at {slotReq.StartTime}.");

            slots.Add(new AdvisorAvailableSlot
            {
                AdvisorID = advisor.AdvisorID,
                StartTime = slotReq.StartTime,
                EndTime = slotReq.EndTime,
                Notes = slotReq.Notes,
                CreatedAt = DateTime.UtcNow
            });
        }

        _db.AdvisorAvailableSlots.AddRange(slots);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("CREATE_MULTIPLE_AVAILABLE_SLOTS", "AdvisorAvailableSlot", 0,
            $"AdvisorId={advisor.AdvisorID}, Count={slots.Count}");
        _logger.LogInformation("Created {Count} available slots for advisor {AdvisorId}",
            slots.Count, advisor.AdvisorID);

        var dtos = slots.Select(MapToAvailableSlotDto).ToList();
        return ApiResponse<List<AvailableSlotDto>>.SuccessResponse(dtos);
    }

    public async Task<ApiResponse<AvailableSlotDto>> UpdateAvailableSlotAsync(int userId, int slotId, UpdateAvailableSlotRequest request)
    {
        var slot = await _db.AdvisorAvailableSlots
            .FirstOrDefaultAsync(s => s.SlotID == slotId);

        if (slot == null)
            return ApiResponse<AvailableSlotDto>.ErrorResponse("SLOT_NOT_FOUND",
                "Available slot not found.");

        var advisor = await _db.Advisors.AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserID == userId);
        if (advisor == null || slot.AdvisorID != advisor.AdvisorID)
            return ApiResponse<AvailableSlotDto>.ErrorResponse("SLOT_NOT_OWNED",
                "You do not own this available slot.");

        // Cannot update booked slots
        if (slot.IsBooked)
            return ApiResponse<AvailableSlotDto>.ErrorResponse("SLOT_ALREADY_BOOKED",
                "Cannot update a slot that is already booked.");

        if (request.StartTime.HasValue && request.EndTime.HasValue)
        {
            if (request.EndTime.Value <= request.StartTime.Value)
                return ApiResponse<AvailableSlotDto>.ErrorResponse("INVALID_TIME_RANGE",
                    "EndTime must be after StartTime.");
            slot.StartTime = request.StartTime.Value;
            slot.EndTime = request.EndTime.Value;
        }
        else if (request.StartTime.HasValue)
        {
            if (request.StartTime.Value >= slot.EndTime)
                return ApiResponse<AvailableSlotDto>.ErrorResponse("INVALID_TIME_RANGE",
                    "StartTime must be before EndTime.");
            slot.StartTime = request.StartTime.Value;
        }
        else if (request.EndTime.HasValue)
        {
            if (request.EndTime.Value <= slot.StartTime)
                return ApiResponse<AvailableSlotDto>.ErrorResponse("INVALID_TIME_RANGE",
                    "EndTime must be after StartTime.");
            slot.EndTime = request.EndTime.Value;
        }

        if (request.Notes != null)
            slot.Notes = request.Notes;

        slot.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync("UPDATE_AVAILABLE_SLOT", "AdvisorAvailableSlot", slotId, null);
        _logger.LogInformation("Available slot {SlotId} updated", slotId);

        return ApiResponse<AvailableSlotDto>.SuccessResponse(MapToAvailableSlotDto(slot));
    }

    public async Task<ApiResponse<string>> DeleteAvailableSlotAsync(int userId, int slotId)
    {
        var slot = await _db.AdvisorAvailableSlots
            .FirstOrDefaultAsync(s => s.SlotID == slotId);

        if (slot == null)
            return ApiResponse<string>.ErrorResponse("SLOT_NOT_FOUND",
                "Available slot not found.");

        var advisor = await _db.Advisors.AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserID == userId);
        if (advisor == null || slot.AdvisorID != advisor.AdvisorID)
            return ApiResponse<string>.ErrorResponse("SLOT_NOT_OWNED",
                "You do not own this available slot.");

        if (slot.IsBooked)
            return ApiResponse<string>.ErrorResponse("SLOT_ALREADY_BOOKED",
                "Cannot delete a slot that is already booked.");

        _db.AdvisorAvailableSlots.Remove(slot);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("DELETE_AVAILABLE_SLOT", "AdvisorAvailableSlot", slotId, null);
        _logger.LogInformation("Available slot {SlotId} deleted", slotId);

        return ApiResponse<string>.SuccessResponse("Slot deleted successfully.");
    }

    public async Task<ApiResponse<PagedResponse<AvailableSlotDto>>> GetMyAvailableSlotsAsync(int userId, AvailableSlotQueryParams queryParams)
    {
        var advisor = await _db.Advisors.AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserID == userId);
        if (advisor == null)
            return ApiResponse<PagedResponse<AvailableSlotDto>>.ErrorResponse(
                "ADVISOR_PROFILE_NOT_FOUND", "Advisor profile not found.");

        var query = _db.AdvisorAvailableSlots
            .AsNoTracking()
            .Where(s => s.AdvisorID == advisor.AdvisorID)
            .AsQueryable();

        var items = query
            .OrderBy(s => s.StartTime)
            .Select(s => new AvailableSlotDto
            {
                SlotID = s.SlotID,
                AdvisorID = s.AdvisorID,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                IsBooked = s.IsBooked,
                BookedSessionID = s.BookedSessionID,
                Notes = s.Notes,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            }).Paging(queryParams.Page, queryParams.PageSize);

        return ApiResponse<PagedResponse<AvailableSlotDto>>.SuccessResponse(
            new PagedResponse<AvailableSlotDto>
            {
                Items = await items.ToListAsync(),
                Paging = new PagingInfo
                {
                    Page = queryParams.Page,
                    PageSize = queryParams.PageSize,
                    TotalItems = await query.CountAsync(),
                }
            });
    }

    public async Task<ApiResponse<PagedResponse<AvailableSlotDto>>> GetAdvisorAvailableSlotsAsync(int advisorId, AvailableSlotQueryParams queryParams)
    {
        var advisorExists = await _db.Advisors.AnyAsync(a => a.AdvisorID == advisorId);
        if (!advisorExists)
            return ApiResponse<PagedResponse<AvailableSlotDto>>.ErrorResponse(
                "ADVISOR_NOT_FOUND", "Advisor not found.");

        var query = _db.AdvisorAvailableSlots
            .AsNoTracking()
            .Where(s => s.AdvisorID == advisorId && !s.IsBooked)
            .AsQueryable();

        var items = query
            .OrderBy(s => s.StartTime)
            .Select(s => new AvailableSlotDto
            {
                SlotID = s.SlotID,
                AdvisorID = s.AdvisorID,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                IsBooked = s.IsBooked,
                BookedSessionID = s.BookedSessionID,
                Notes = s.Notes,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            }).Paging(queryParams.Page, queryParams.PageSize);

        return ApiResponse<PagedResponse<AvailableSlotDto>>.SuccessResponse(
            new PagedResponse<AvailableSlotDto>
            {
                Items = await items.ToListAsync(),
                Paging = new PagingInfo
                {
                    Page = queryParams.Page,
                    PageSize = queryParams.PageSize,
                    TotalItems = await query.CountAsync(),
                }
            });
    }

    public async Task<ApiResponse<SessionDto>> BookSessionFromSlotAsync(int userId, BookSessionFromSlotRequest request)
    {
        var startup = await _db.Startups.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserID == userId);
        if (startup == null)
            return ApiResponse<SessionDto>.ErrorResponse("STARTUP_PROFILE_NOT_FOUND",
                "Startup profile not found.");

        // Verify mentorship exists and user is the startup owner
        var mentorship = await _db.StartupAdvisorMentorships
            .FirstOrDefaultAsync(m => m.MentorshipID == request.MentorshipID);
        if (mentorship == null)
            return ApiResponse<SessionDto>.ErrorResponse("MENTORSHIP_NOT_FOUND",
                "Mentorship not found.");

        if (mentorship.StartupID != startup.StartupID)
            return ApiResponse<SessionDto>.ErrorResponse("MENTORSHIP_NOT_OWNED",
                "You are not the startup owner of this mentorship.");

        if (mentorship.MentorshipStatus != MentorshipStatus.Accepted && mentorship.MentorshipStatus != MentorshipStatus.InProgress)
            return ApiResponse<SessionDto>.ErrorResponse("INVALID_MENTORSHIP_STATUS",
                $"Cannot book session for mentorship with status '{mentorship.MentorshipStatus}'.");

        // Get available slot
        var slot = await _db.AdvisorAvailableSlots
            .FirstOrDefaultAsync(s => s.SlotID == request.AvailableSlotID);
        if (slot == null)
            return ApiResponse<SessionDto>.ErrorResponse("SLOT_NOT_FOUND",
                "Available slot not found.");

        if (slot.IsBooked)
            return ApiResponse<SessionDto>.ErrorResponse("SLOT_ALREADY_BOOKED",
                "This slot is already booked.");

        if (slot.AdvisorID != mentorship.AdvisorID)
            return ApiResponse<SessionDto>.ErrorResponse("SLOT_MISMATCH",
                "This slot does not belong to the advisor in this mentorship.");

        // Create session
        var durationMinutes = (int)(slot.EndTime - slot.StartTime).TotalMinutes;
        var session = new MentorshipSession
        {
            MentorshipID = request.MentorshipID,
            ScheduledStartAt = slot.StartTime,
            DurationMinutes = durationMinutes,
            MeetingURL = request.MeetingUrl ?? string.Empty,
            SessionStatus = SessionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _db.MentorshipSessions.Add(session);

        // Mark slot as booked
        slot.IsBooked = true;
        slot.BookedSessionID = session.SessionID;
        slot.UpdatedAt = DateTime.UtcNow;

        // Move mentorship to InProgress if Accepted
        if (mentorship.MentorshipStatus == MentorshipStatus.Accepted)
        {
            mentorship.MentorshipStatus = MentorshipStatus.InProgress;
            mentorship.LastUpdatedByRole = "Startup";
            mentorship.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        await _audit.LogAsync("BOOK_SESSION_FROM_SLOT", "MentorshipSession", session.SessionID,
            $"MentorshipId={request.MentorshipID}, SlotId={request.AvailableSlotID}");
        _logger.LogInformation("Session {SessionId} booked from slot {SlotId} by startup {StartupId}",
            session.SessionID, request.AvailableSlotID, startup.StartupID);

        return ApiResponse<SessionDto>.SuccessResponse(MapSessionDto(session));
    }
  
    // ================================================================
    // HELPERS
    // ================================================================

    #region helper method
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

    private static MentorshipDto MapToDto(StartupAdvisorMentorship m) => new()
    {
        MentorshipID = m.MentorshipID,
        StartupID = m.StartupID,
        AdvisorID = m.AdvisorID,
        MentorshipStatus = m.MentorshipStatus.ToString(),
        ChallengeDescription = m.ChallengeDescription,
        ExpectedDuration = m.ExpectedDuration,
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
        ExpectedDuration = m.ExpectedDuration,
        RequestedAt = m.RequestedAt,
        AcceptedAt = m.AcceptedAt,
        RejectedAt = m.RejectedAt,
        RejectedReason = m.RejectedReason,
        CompletionConfirmedByStartup = m.CompletionConfirmedByStartup,
        CompletionConfirmedByAdvisor = m.CompletionConfirmedByAdvisor,
        CreatedAt = m.CreatedAt,
        UpdatedAt = m.UpdatedAt,
    };

    private static SessionDto MapSessionDto(MentorshipSession s) => new()
    {
        SessionID = s.SessionID,
        MentorshipID = s.MentorshipID,
        ScheduledStartAt = s.ScheduledStartAt,
        DurationMinutes = s.DurationMinutes,
        MeetingURL = s.MeetingURL,
        SessionStatus = s.SessionStatus.ToString(),
        TopicsDiscussed = s.TopicsDiscussed,
        CreatedAt = s.CreatedAt,
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

    private static AvailableSlotDto MapToAvailableSlotDto(AdvisorAvailableSlot slot) => new()
    {
        SlotID = slot.SlotID,
        AdvisorID = slot.AdvisorID,
        StartTime = slot.StartTime,
        EndTime = slot.EndTime,
        IsBooked = slot.IsBooked,
        BookedSessionID = slot.BookedSessionID,
        Notes = slot.Notes,
        CreatedAt = slot.CreatedAt,
        UpdatedAt = slot.UpdatedAt
    };
    #endregion
}
