using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Connection;
using AISEP.Application.Interfaces;
using AISEP.Domain.Entities;
using AISEP.Domain.Enums;
using AISEP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AISEP.Infrastructure.Services;

public class ConnectionsService : IConnectionsService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;
    private readonly ILogger<ConnectionsService> _logger;

    public ConnectionsService(ApplicationDbContext db, IAuditService audit, ILogger<ConnectionsService> logger)
    {
        _db = db;
        _audit = audit;
        _logger = logger;
    }

    // ================================================================
    // CREATE CONNECTION (Investor)
    // ================================================================

    public async Task<ApiResponse<ConnectionDto>> CreateConnectionAsync(int userId, CreateConnectionRequest request)
    {
        var investor = await _db.Investors.AsNoTracking().FirstOrDefaultAsync(i => i.UserID == userId);
        if (investor == null)
            return ApiResponse<ConnectionDto>.ErrorResponse("INVESTOR_PROFILE_NOT_FOUND", "Investor profile not found.");

        if (!investor.AcceptingConnections)
            return ApiResponse<ConnectionDto>.ErrorResponse("INVESTOR_NOT_ACCEPTING_CONNECTIONS",
                "This investor is not currently accepting new connections.");

        var startupExists = await _db.Startups.AnyAsync(s => s.StartupID == request.StartupId);
        if (!startupExists)
            return ApiResponse<ConnectionDto>.ErrorResponse("STARTUP_NOT_FOUND",
                $"Startup with id {request.StartupId} not found.");

        var duplicate = await _db.StartupInvestorConnections.AnyAsync(c =>
            c.StartupID == request.StartupId &&
            c.InvestorID == investor.InvestorID &&
            (c.ConnectionStatus == ConnectionStatus.Requested || c.ConnectionStatus == ConnectionStatus.Accepted));
        if (duplicate)
            return ApiResponse<ConnectionDto>.ErrorResponse("CONNECTION_ALREADY_EXISTS",
                "An active or pending connection with this startup already exists.");

        var conn = new StartupInvestorConnection
        {
            StartupID = request.StartupId,
            InvestorID = investor.InvestorID,
            ConnectionStatus = ConnectionStatus.Requested,
            InitiatedBy = userId,
            PersonalizedMessage = request.Message,
            RequestedAt = DateTime.UtcNow
        };

        _db.StartupInvestorConnections.Add(conn);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("CREATE_CONNECTION", "StartupInvestorConnection", conn.ConnectionID,
            $"InvestorId={investor.InvestorID}, StartupId={request.StartupId}");
        _logger.LogInformation("Connection {ConnId} created by investor {InvestorId} to startup {StartupId}",
            conn.ConnectionID, investor.InvestorID, request.StartupId);

        return ApiResponse<ConnectionDto>.SuccessResponse(MapToDto(conn));
    }

    // ================================================================
    // CREATE CONNECTION (Startup → Investor)
    // ================================================================

    public async Task<ApiResponse<ConnectionDto>> CreateConnectionFromStartupAsync(int userId, CreateStartupToInvestorRequest request)
    {
        var startup = await _db.Startups.AsNoTracking().FirstOrDefaultAsync(s => s.UserID == userId);
        if (startup == null)
            return ApiResponse<ConnectionDto>.ErrorResponse("STARTUP_PROFILE_NOT_FOUND", "Startup profile not found.");

        var investor = await _db.Investors.AsNoTracking().FirstOrDefaultAsync(i => i.InvestorID == request.InvestorId);
        if (investor == null)
            return ApiResponse<ConnectionDto>.ErrorResponse("INVESTOR_NOT_FOUND",
                $"Investor with id {request.InvestorId} not found.");

        if (!investor.AcceptingConnections)
            return ApiResponse<ConnectionDto>.ErrorResponse("INVESTOR_NOT_ACCEPTING_CONNECTIONS",
                "This investor is not currently accepting new connections.");

        var duplicate = await _db.StartupInvestorConnections.AnyAsync(c =>
            c.StartupID == startup.StartupID &&
            c.InvestorID == request.InvestorId &&
            (c.ConnectionStatus == ConnectionStatus.Requested || c.ConnectionStatus == ConnectionStatus.Accepted));
        if (duplicate)
            return ApiResponse<ConnectionDto>.ErrorResponse("CONNECTION_ALREADY_EXISTS",
                "An active or pending connection with this investor already exists.");

        var conn = new StartupInvestorConnection
        {
            StartupID = startup.StartupID,
            InvestorID = request.InvestorId,
            ConnectionStatus = ConnectionStatus.Requested,
            InitiatedBy = userId,
            PersonalizedMessage = request.Message,
            RequestedAt = DateTime.UtcNow
        };

        _db.StartupInvestorConnections.Add(conn);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("CREATE_CONNECTION", "StartupInvestorConnection", conn.ConnectionID,
            $"StartupId={startup.StartupID}, InvestorId={request.InvestorId}");
        _logger.LogInformation("Connection {ConnId} created by startup {StartupId} to investor {InvestorId}",
            conn.ConnectionID, startup.StartupID, request.InvestorId);

        return ApiResponse<ConnectionDto>.SuccessResponse(MapToDto(conn));
    }

    // ================================================================
    // GET SENT CONNECTIONS (both roles — direction: InitiatedBy == me)
    // ================================================================

    public async Task<ApiResponse<PagedResponse<ConnectionListItemDto>>> GetSentAsync(
        int userId, string userType, string? status, int page, int pageSize)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        IQueryable<StartupInvestorConnection> query;

        if (userType == "Investor")
        {
            var investor = await _db.Investors.AsNoTracking().FirstOrDefaultAsync(i => i.UserID == userId);
            if (investor == null)
                return ApiResponse<PagedResponse<ConnectionListItemDto>>.ErrorResponse(
                    "INVESTOR_PROFILE_NOT_FOUND", "Investor profile not found.");

            query = _db.StartupInvestorConnections.AsNoTracking()
                .Include(c => c.Startup).Include(c => c.Investor)
                .Where(c => c.InvestorID == investor.InvestorID && c.InitiatedBy == userId);
        }
        else
        {
            var startup = await _db.Startups.AsNoTracking().FirstOrDefaultAsync(s => s.UserID == userId);
            if (startup == null)
                return ApiResponse<PagedResponse<ConnectionListItemDto>>.ErrorResponse(
                    "STARTUP_PROFILE_NOT_FOUND", "Startup profile not found.");

            query = _db.StartupInvestorConnections.AsNoTracking()
                .Include(c => c.Startup).Include(c => c.Investor)
                .Where(c => c.StartupID == startup.StartupID && c.InitiatedBy == userId);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ConnectionStatus>(status, true, out var statusEnum))
            query = query.Where(c => c.ConnectionStatus == statusEnum);

        query = query.OrderByDescending(c => c.RequestedAt);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(c => MapToListItem(c)).ToListAsync();

        return ApiResponse<PagedResponse<ConnectionListItemDto>>.SuccessResponse(
            new PagedResponse<ConnectionListItemDto>
            {
                Items = items,
                Paging = new PagingInfo { Page = page, PageSize = pageSize, TotalItems = total }
            });
    }

    // ================================================================
    // GET RECEIVED CONNECTIONS (both roles — direction: InitiatedBy != me)
    // ================================================================

    public async Task<ApiResponse<PagedResponse<ConnectionListItemDto>>> GetReceivedAsync(
        int userId, string userType, string? status, int? counterpartId, int page, int pageSize)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        IQueryable<StartupInvestorConnection> query;

        if (userType == "Investor")
        {
            var investor = await _db.Investors.AsNoTracking().FirstOrDefaultAsync(i => i.UserID == userId);
            if (investor == null)
                return ApiResponse<PagedResponse<ConnectionListItemDto>>.ErrorResponse(
                    "INVESTOR_PROFILE_NOT_FOUND", "Investor profile not found.");

            query = _db.StartupInvestorConnections.AsNoTracking()
                .Include(c => c.Startup).Include(c => c.Investor)
                .Where(c => c.InvestorID == investor.InvestorID && c.InitiatedBy != userId);

            if (counterpartId.HasValue)
                query = query.Where(c => c.StartupID == counterpartId.Value);
        }
        else
        {
            var startup = await _db.Startups.AsNoTracking().FirstOrDefaultAsync(s => s.UserID == userId);
            if (startup == null)
                return ApiResponse<PagedResponse<ConnectionListItemDto>>.ErrorResponse(
                    "STARTUP_PROFILE_NOT_FOUND", "Startup profile not found.");

            query = _db.StartupInvestorConnections.AsNoTracking()
                .Include(c => c.Startup).Include(c => c.Investor)
                .Where(c => c.StartupID == startup.StartupID && c.InitiatedBy != userId);

            if (counterpartId.HasValue)
                query = query.Where(c => c.InvestorID == counterpartId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ConnectionStatus>(status, true, out var statusEnum))
            query = query.Where(c => c.ConnectionStatus == statusEnum);

        query = query.OrderByDescending(c => c.RequestedAt);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(c => MapToListItem(c)).ToListAsync();

        return ApiResponse<PagedResponse<ConnectionListItemDto>>.SuccessResponse(
            new PagedResponse<ConnectionListItemDto>
            {
                Items = items,
                Paging = new PagingInfo { Page = page, PageSize = pageSize, TotalItems = total }
            });
    }

    // ================================================================
    // GET CONNECTION DETAIL (Participant / Staff)
    // ================================================================

    public async Task<ApiResponse<ConnectionDetailDto>> GetDetailAsync(int userId, string userType, int connectionId)
    {
        var conn = await _db.StartupInvestorConnections.AsNoTracking()
            .AsSplitQuery()
            .Include(c => c.Startup).Include(c => c.Investor)
            .Include(c => c.InformationRequests.OrderByDescending(ir => ir.RequestedAt))
            .FirstOrDefaultAsync(c => c.ConnectionID == connectionId);

        if (conn == null)
            return ApiResponse<ConnectionDetailDto>.ErrorResponse("CONNECTION_NOT_FOUND", "Connection not found.");

        if (!await IsParticipantOrStaff(userId, userType, conn))
            return ApiResponse<ConnectionDetailDto>.ErrorResponse("CONNECTION_NOT_OWNED",
                "You do not have access to this connection.");

        return ApiResponse<ConnectionDetailDto>.SuccessResponse(MapToDetailDto(conn));
    }

    // ================================================================
    // UPDATE CONNECTION (Investor — only when Sent)
    // ================================================================

    public async Task<ApiResponse<ConnectionDto>> UpdateAsync(int userId, int connectionId, UpdateConnectionRequest request)
    {
        var (conn, error) = await GetConnectionForInvestor(userId, connectionId);
        if (conn == null) return error!;

        if (conn.ConnectionStatus != ConnectionStatus.Requested)
            return ApiResponse<ConnectionDto>.ErrorResponse("INVALID_STATUS_TRANSITION",
                $"Cannot update connection with status '{conn.ConnectionStatus}'. Only 'Requested' can be updated.");

        if (request.Message != null) conn.PersonalizedMessage = request.Message;

        await _db.SaveChangesAsync();

        await _audit.LogAsync("UPDATE_CONNECTION", "StartupInvestorConnection", connectionId, null);
        return ApiResponse<ConnectionDto>.SuccessResponse(MapToDto(conn));
    }

    // ================================================================
    // WITHDRAW CONNECTION (initiator only — both roles)
    // ================================================================

    public async Task<ApiResponse<ConnectionDto>> WithdrawAsync(int userId, string userType, int connectionId)
    {
        var (conn, error) = await GetConnectionAsParticipant(userId, userType, connectionId);
        if (conn == null) return error!;

        if (conn.InitiatedBy != userId)
            return ApiResponse<ConnectionDto>.ErrorResponse("NOT_INITIATOR",
                "Only the party who initiated this connection can withdraw it.");

        if (conn.ConnectionStatus != ConnectionStatus.Requested)
            return ApiResponse<ConnectionDto>.ErrorResponse("INVALID_STATUS_TRANSITION",
                $"Cannot withdraw connection with status '{conn.ConnectionStatus}'. Only 'Requested' can be withdrawn.");

        conn.ConnectionStatus = ConnectionStatus.Withdrawn;
        conn.RespondedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync("WITHDRAW_CONNECTION", "StartupInvestorConnection", connectionId, null);
        _logger.LogInformation("Connection {ConnId} withdrawn by {UserType} {UserId}", connectionId, userType, userId);

        return ApiResponse<ConnectionDto>.SuccessResponse(MapToDto(conn));
    }

    // ================================================================
    // ACCEPT CONNECTION (receiver only — both roles)
    // ================================================================

    public async Task<ApiResponse<ConnectionDto>> AcceptAsync(int userId, string userType, int connectionId)
    {
        var (conn, error) = await GetConnectionAsParticipant(userId, userType, connectionId);
        if (conn == null) return error!;

        if (conn.InitiatedBy == userId)
            return ApiResponse<ConnectionDto>.ErrorResponse("NOT_RECEIVER",
                "You cannot accept a connection you initiated.");

        if (conn.ConnectionStatus != ConnectionStatus.Requested)
            return ApiResponse<ConnectionDto>.ErrorResponse("INVALID_STATUS_TRANSITION",
                $"Cannot accept connection with status '{conn.ConnectionStatus}'. Only 'Requested' can be accepted.");

        conn.ConnectionStatus = ConnectionStatus.Accepted;
        conn.RespondedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync("ACCEPT_CONNECTION", "StartupInvestorConnection", connectionId, null);
        _logger.LogInformation("Connection {ConnId} accepted by {UserType} {UserId}", connectionId, userType, userId);

        return ApiResponse<ConnectionDto>.SuccessResponse(MapToDto(conn));
    }

    // ================================================================
    // REJECT CONNECTION (receiver only — both roles)
    // ================================================================

    public async Task<ApiResponse<ConnectionDto>> RejectAsync(int userId, string userType, int connectionId, string? reason)
    {
        var (conn, error) = await GetConnectionAsParticipant(userId, userType, connectionId);
        if (conn == null) return error!;

        if (conn.InitiatedBy == userId)
            return ApiResponse<ConnectionDto>.ErrorResponse("NOT_RECEIVER",
                "You cannot reject a connection you initiated.");

        if (conn.ConnectionStatus != ConnectionStatus.Requested)
            return ApiResponse<ConnectionDto>.ErrorResponse("INVALID_STATUS_TRANSITION",
                $"Cannot reject connection with status '{conn.ConnectionStatus}'. Only 'Requested' can be rejected.");

        conn.ConnectionStatus = ConnectionStatus.Rejected;
        conn.RespondedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync("REJECT_CONNECTION", "StartupInvestorConnection", connectionId, $"Reason={reason}");
        _logger.LogInformation("Connection {ConnId} rejected by {UserType} {UserId}", connectionId, userType, userId);

        return ApiResponse<ConnectionDto>.SuccessResponse(MapToDto(conn));
    }

    // ================================================================
    // CLOSE CONNECTION (Startup or Investor)
    // ================================================================

    public async Task<ApiResponse<ConnectionDto>> CloseAsync(int userId, string userType, int connectionId)
    {
        var conn = await _db.StartupInvestorConnections
            .Include(c => c.Investor)
            .FirstOrDefaultAsync(c => c.ConnectionID == connectionId);
        if (conn == null)
            return ApiResponse<ConnectionDto>.ErrorResponse("CONNECTION_NOT_FOUND", "Connection not found.");

        if (!await IsParticipantOrStaff(userId, userType, conn))
            return ApiResponse<ConnectionDto>.ErrorResponse("CONNECTION_NOT_OWNED",
                "You do not have access to this connection.");

        if (conn.ConnectionStatus != ConnectionStatus.Accepted)
            return ApiResponse<ConnectionDto>.ErrorResponse("INVALID_STATUS_TRANSITION",
                $"Cannot close connection with status '{conn.ConnectionStatus}'. Only 'Accepted' can be closed.");

        conn.ConnectionStatus = ConnectionStatus.Closed;
        conn.RespondedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync("CLOSE_CONNECTION", "StartupInvestorConnection", connectionId, null);
        _logger.LogInformation("Connection {ConnId} closed", connectionId);

        return ApiResponse<ConnectionDto>.SuccessResponse(MapToDto(conn));
    }

    // ================================================================
    // CREATE INFO REQUEST (Investor)
    // ================================================================

    public async Task<ApiResponse<InfoRequestDto>> CreateInfoRequestAsync(int userId, int connectionId, CreateInfoRequest request)
    {
        var (conn, error) = await GetConnectionForInvestor(userId, connectionId);
        if (conn == null)
            return ApiResponse<InfoRequestDto>.ErrorResponse(error!.Error!.Code, error.Error.Message);

        if (conn.ConnectionStatus != ConnectionStatus.Accepted)
            return ApiResponse<InfoRequestDto>.ErrorResponse("INVALID_STATUS_TRANSITION",
                "Information requests can only be created for accepted connections.");

        var investor = await _db.Investors.AsNoTracking().FirstAsync(i => i.UserID == userId);

        var ir = new InformationRequest
        {
            ConnectionID = connectionId,
            InvestorID = investor.InvestorID,
            RequestType = request.RequestType,
            RequestMessage = request.RequestMessage,
            RequestStatus = RequestStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };

        _db.InformationRequests.Add(ir);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("CREATE_INFO_REQUEST", "InformationRequest", ir.RequestID,
            $"ConnectionId={connectionId}");
        _logger.LogInformation("InfoRequest {ReqId} created for connection {ConnId}", ir.RequestID, connectionId);

        return ApiResponse<InfoRequestDto>.SuccessResponse(MapInfoRequestDto(ir));
    }

    // ================================================================
    // GET INFO REQUESTS (Participant / Staff)
    // ================================================================

    public async Task<ApiResponse<List<InfoRequestDto>>> GetInfoRequestsAsync(int userId, string userType, int connectionId)
    {
        var conn = await _db.StartupInvestorConnections.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ConnectionID == connectionId);
        if (conn == null)
            return ApiResponse<List<InfoRequestDto>>.ErrorResponse("CONNECTION_NOT_FOUND", "Connection not found.");

        if (!await IsParticipantOrStaff(userId, userType, conn))
            return ApiResponse<List<InfoRequestDto>>.ErrorResponse("CONNECTION_NOT_OWNED",
                "You do not have access to this connection.");

        var items = await _db.InformationRequests.AsNoTracking()
            .Where(ir => ir.ConnectionID == connectionId)
            .OrderByDescending(ir => ir.RequestedAt)
            .Select(ir => MapInfoRequestDto(ir))
            .ToListAsync();

        return ApiResponse<List<InfoRequestDto>>.SuccessResponse(items);
    }

    // ================================================================
    // FULFILL INFO REQUEST (Startup)
    // ================================================================

    public async Task<ApiResponse<InfoRequestDto>> FulfillInfoRequestAsync(int userId, int requestId, FulfillInfoRequest request)
    {
        var ir = await _db.InformationRequests.Include(i => i.Connection)
            .FirstOrDefaultAsync(i => i.RequestID == requestId);
        if (ir == null)
            return ApiResponse<InfoRequestDto>.ErrorResponse("INFO_REQUEST_NOT_FOUND", "Information request not found.");

        var startup = await _db.Startups.AsNoTracking().FirstOrDefaultAsync(s => s.UserID == userId);
        if (startup == null || ir.Connection.StartupID != startup.StartupID)
            return ApiResponse<InfoRequestDto>.ErrorResponse("INFO_REQUEST_NOT_OWNED",
                "You are not the startup owner of this connection.");

        if (ir.RequestStatus != RequestStatus.Pending)
            return ApiResponse<InfoRequestDto>.ErrorResponse("INVALID_STATUS_TRANSITION",
                $"Cannot fulfill info request with status '{ir.RequestStatus}'.");

        ir.RequestStatus = RequestStatus.Approved;
        ir.ResponseMessage = request.ResponseMessage;
        ir.ResponseDocumentIDs = request.ResponseDocumentIDs;
        ir.FulfilledAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync("FULFILL_INFO_REQUEST", "InformationRequest", requestId, null);
        _logger.LogInformation("InfoRequest {ReqId} fulfilled", requestId);

        return ApiResponse<InfoRequestDto>.SuccessResponse(MapInfoRequestDto(ir));
    }

    // ================================================================
    // REJECT INFO REQUEST (Startup)
    // ================================================================

    public async Task<ApiResponse<InfoRequestDto>> RejectInfoRequestAsync(int userId, int requestId, string? reason)
    {
        var ir = await _db.InformationRequests.Include(i => i.Connection)
            .FirstOrDefaultAsync(i => i.RequestID == requestId);
        if (ir == null)
            return ApiResponse<InfoRequestDto>.ErrorResponse("INFO_REQUEST_NOT_FOUND", "Information request not found.");

        var startup = await _db.Startups.AsNoTracking().FirstOrDefaultAsync(s => s.UserID == userId);
        if (startup == null || ir.Connection.StartupID != startup.StartupID)
            return ApiResponse<InfoRequestDto>.ErrorResponse("INFO_REQUEST_NOT_OWNED",
                "You are not the startup owner of this connection.");

        if (ir.RequestStatus != RequestStatus.Pending)
            return ApiResponse<InfoRequestDto>.ErrorResponse("INVALID_STATUS_TRANSITION",
                $"Cannot reject info request with status '{ir.RequestStatus}'.");

        ir.RequestStatus = RequestStatus.Rejected;
        ir.ResponseMessage = reason;
        await _db.SaveChangesAsync();

        await _audit.LogAsync("REJECT_INFO_REQUEST", "InformationRequest", requestId, $"Reason={reason}");
        _logger.LogInformation("InfoRequest {ReqId} rejected", requestId);

        return ApiResponse<InfoRequestDto>.SuccessResponse(MapInfoRequestDto(ir));
    }

    // ================================================================
    // PORTFOLIO — GET
    // ================================================================

    public async Task<ApiResponse<PagedResponse<PortfolioCompanyDto>>> GetPortfolioAsync(int userId, int page, int pageSize)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        var investor = await _db.Investors.AsNoTracking().FirstOrDefaultAsync(i => i.UserID == userId);
        if (investor == null)
            return ApiResponse<PagedResponse<PortfolioCompanyDto>>.ErrorResponse(
                "INVESTOR_PROFILE_NOT_FOUND", "Investor profile not found.");

        var query = _db.PortfolioCompanies.AsNoTracking()
            .Where(p => p.InvestorID == investor.InvestorID)
            .OrderByDescending(p => p.InvestmentDate);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => MapPortfolioDto(p)).ToListAsync();

        return ApiResponse<PagedResponse<PortfolioCompanyDto>>.SuccessResponse(
            new PagedResponse<PortfolioCompanyDto>
            {
                Items = items,
                Paging = new PagingInfo { Page = page, PageSize = pageSize, TotalItems = total}
            });
    }

    // ================================================================
    // PORTFOLIO — CREATE
    // ================================================================

    public async Task<ApiResponse<PortfolioCompanyDto>> CreatePortfolioAsync(int userId, CreatePortfolioCompanyRequest request)
    {
        var investor = await _db.Investors.AsNoTracking().FirstOrDefaultAsync(i => i.UserID == userId);
        if (investor == null)
            return ApiResponse<PortfolioCompanyDto>.ErrorResponse("INVESTOR_PROFILE_NOT_FOUND", "Investor profile not found.");

        var pc = new PortfolioCompany
        {
            InvestorID = investor.InvestorID,
            CompanyName = request.CompanyName,
            Industry = request.Industry,
            InvestmentStage = !string.IsNullOrWhiteSpace(request.InvestmentStage) && Enum.TryParse<InvestmentStage>(request.InvestmentStage, true, out var isEnum) ? isEnum : null,
            InvestmentDate = request.InvestmentDate,
            InvestmentAmount = request.InvestmentAmount,
            CurrentStatus = !string.IsNullOrWhiteSpace(request.CurrentStatus) && Enum.TryParse<PortfolioCompanyStatus>(request.CurrentStatus, true, out var csEnum) ? csEnum : null,
            Description = request.Description
        };

        _db.PortfolioCompanies.Add(pc);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("CREATE_PORTFOLIO_COMPANY", "PortfolioCompany", pc.PortfolioID, null);
        return ApiResponse<PortfolioCompanyDto>.SuccessResponse(MapPortfolioDto(pc));
    }

    // ================================================================
    // PORTFOLIO — UPDATE
    // ================================================================

    public async Task<ApiResponse<PortfolioCompanyDto>> UpdatePortfolioAsync(int userId, int portfolioId, UpdatePortfolioCompanyRequest request)
    {
        var investor = await _db.Investors.AsNoTracking().FirstOrDefaultAsync(i => i.UserID == userId);
        if (investor == null)
            return ApiResponse<PortfolioCompanyDto>.ErrorResponse("INVESTOR_PROFILE_NOT_FOUND", "Investor profile not found.");

        var pc = await _db.PortfolioCompanies.FirstOrDefaultAsync(p => p.PortfolioID == portfolioId);
        if (pc == null)
            return ApiResponse<PortfolioCompanyDto>.ErrorResponse("PORTFOLIO_NOT_FOUND", "Portfolio company not found.");

        if (pc.InvestorID != investor.InvestorID)
            return ApiResponse<PortfolioCompanyDto>.ErrorResponse("PORTFOLIO_NOT_OWNED",
                "You do not own this portfolio company record.");

        if (request.CompanyName != null) pc.CompanyName = request.CompanyName;
        if (request.Industry != null) pc.Industry = request.Industry;
        if (request.InvestmentStage != null && Enum.TryParse<InvestmentStage>(request.InvestmentStage, true, out var isEnum2)) pc.InvestmentStage = isEnum2;
        if (request.InvestmentDate.HasValue) pc.InvestmentDate = request.InvestmentDate;
        if (request.InvestmentAmount.HasValue) pc.InvestmentAmount = request.InvestmentAmount;
        if (request.CurrentStatus != null && Enum.TryParse<PortfolioCompanyStatus>(request.CurrentStatus, true, out var csEnum2)) pc.CurrentStatus = csEnum2;
        if (request.ExitType != null && Enum.TryParse<ExitType>(request.ExitType, true, out var etEnum)) pc.ExitType = etEnum;
        if (request.ExitDate.HasValue) pc.ExitDate = request.ExitDate;
        if (request.ExitValue.HasValue) pc.ExitValue = request.ExitValue;
        if (request.Description != null) pc.Description = request.Description;

        await _db.SaveChangesAsync();

        await _audit.LogAsync("UPDATE_PORTFOLIO_COMPANY", "PortfolioCompany", portfolioId, null);
        return ApiResponse<PortfolioCompanyDto>.SuccessResponse(MapPortfolioDto(pc));
    }

    // ================================================================
    // PORTFOLIO — DELETE
    // ================================================================

    public async Task<ApiResponse<PortfolioCompanyDto>> DeletePortfolioAsync(int userId, int portfolioId)
    {
        var investor = await _db.Investors.AsNoTracking().FirstOrDefaultAsync(i => i.UserID == userId);
        if (investor == null)
            return ApiResponse<PortfolioCompanyDto>.ErrorResponse("INVESTOR_PROFILE_NOT_FOUND", "Investor profile not found.");

        var pc = await _db.PortfolioCompanies.FirstOrDefaultAsync(p => p.PortfolioID == portfolioId);
        if (pc == null)
            return ApiResponse<PortfolioCompanyDto>.ErrorResponse("PORTFOLIO_NOT_FOUND", "Portfolio company not found.");

        if (pc.InvestorID != investor.InvestorID)
            return ApiResponse<PortfolioCompanyDto>.ErrorResponse("PORTFOLIO_NOT_OWNED",
                "You do not own this portfolio company record.");

        _db.PortfolioCompanies.Remove(pc);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("DELETE_PORTFOLIO_COMPANY", "PortfolioCompany", portfolioId, null);
        return ApiResponse<PortfolioCompanyDto>.SuccessResponse(MapPortfolioDto(pc));
    }

    // ================================================================
    // HELPERS
    // ================================================================

    // Used by UpdateAsync (still InvestorOnly)
    private async Task<(StartupInvestorConnection? conn, ApiResponse<ConnectionDto>? error)>
        GetConnectionForInvestor(int userId, int connectionId)
    {
        var investor = await _db.Investors.AsNoTracking().FirstOrDefaultAsync(i => i.UserID == userId);
        if (investor == null)
            return (null, ApiResponse<ConnectionDto>.ErrorResponse("INVESTOR_PROFILE_NOT_FOUND", "Investor profile not found."));

        var conn = await _db.StartupInvestorConnections
            .Include(c => c.Investor)
            .FirstOrDefaultAsync(c => c.ConnectionID == connectionId);
        if (conn == null)
            return (null, ApiResponse<ConnectionDto>.ErrorResponse("CONNECTION_NOT_FOUND", "Connection not found."));

        if (conn.InvestorID != investor.InvestorID)
            return (null, ApiResponse<ConnectionDto>.ErrorResponse("CONNECTION_NOT_OWNED",
                "You are not the investor for this connection."));

        return (conn, null);
    }

    // Used by Withdraw/Accept/Reject — works for both roles
    private async Task<(StartupInvestorConnection? conn, ApiResponse<ConnectionDto>? error)>
        GetConnectionAsParticipant(int userId, string userType, int connectionId)
    {
        var conn = await _db.StartupInvestorConnections
            .Include(c => c.Investor)
            .FirstOrDefaultAsync(c => c.ConnectionID == connectionId);
        if (conn == null)
            return (null, ApiResponse<ConnectionDto>.ErrorResponse("CONNECTION_NOT_FOUND", "Connection not found."));

        if (!await IsParticipantOrStaff(userId, userType, conn))
            return (null, ApiResponse<ConnectionDto>.ErrorResponse("CONNECTION_NOT_OWNED",
                "You do not have access to this connection."));

        return (conn, null);
    }

    private async Task<bool> IsParticipantOrStaff(int userId, string userType, StartupInvestorConnection conn)
    {
        if (userType == "Staff" || userType == "Admin") return true;

        if (userType == "Investor")
        {
            var investor = await _db.Investors.AsNoTracking().FirstOrDefaultAsync(i => i.UserID == userId);
            return investor != null && conn.InvestorID == investor.InvestorID;
        }

        if (userType == "Startup")
        {
            var startup = await _db.Startups.AsNoTracking().FirstOrDefaultAsync(s => s.UserID == userId);
            return startup != null && conn.StartupID == startup.StartupID;
        }

        return false;
    }

    // ================================================================
    // MAPPING
    // ================================================================

    private static ConnectionDto MapToDto(StartupInvestorConnection c) => new()
    {
        ConnectionID = c.ConnectionID,
        StartupID = c.StartupID,
        InvestorID = c.InvestorID,
        ConnectionStatus = c.ConnectionStatus.ToString(),
        PersonalizedMessage = c.PersonalizedMessage,
        MatchScore = c.MatchScore,
        InitiatedByRole = c.Investor != null && c.InitiatedBy == c.Investor.UserID ? "INVESTOR" : c.InitiatedBy != null ? "STARTUP" : null,
        RequestedAt = c.RequestedAt,
        RespondedAt = c.RespondedAt
    };

    private static ConnectionListItemDto MapToListItem(StartupInvestorConnection c) => new()
    {
        ConnectionID = c.ConnectionID,
        StartupID = c.StartupID,
        StartupName = c.Startup.CompanyName,
        InvestorID = c.InvestorID,
        InvestorName = c.Investor.FullName,
        ConnectionStatus = c.ConnectionStatus.ToString(),
        PersonalizedMessage = c.PersonalizedMessage,
        MatchScore = c.MatchScore,
        InitiatedByRole = c.InitiatedBy == c.Investor.UserID ? "INVESTOR" : "STARTUP",
        RequestedAt = c.RequestedAt,
        RespondedAt = c.RespondedAt
    };

    private static ConnectionDetailDto MapToDetailDto(StartupInvestorConnection c) => new()
    {
        ConnectionID = c.ConnectionID,
        StartupID = c.StartupID,
        StartupName = c.Startup.CompanyName,
        InvestorID = c.InvestorID,
        InvestorName = c.Investor.FullName,
        ConnectionStatus = c.ConnectionStatus.ToString(),
        PersonalizedMessage = c.PersonalizedMessage,
        AttachedDocumentIDs = c.AttachedDocumentIDs,
        MatchScore = c.MatchScore,
        InitiatedByRole = c.InitiatedBy == c.Investor.UserID ? "INVESTOR" : "STARTUP",
        RequestedAt = c.RequestedAt,
        RespondedAt = c.RespondedAt,
        InformationRequests = c.InformationRequests.Select(MapInfoRequestDto).ToList()
    };

    private static InfoRequestDto MapInfoRequestDto(InformationRequest ir) => new()
    {
        RequestID = ir.RequestID,
        ConnectionID = ir.ConnectionID,
        InvestorID = ir.InvestorID,
        RequestType = ir.RequestType,
        RequestMessage = ir.RequestMessage,
        RequestStatus = ir.RequestStatus.ToString(),
        ResponseMessage = ir.ResponseMessage,
        ResponseDocumentIDs = ir.ResponseDocumentIDs,
        RequestedAt = ir.RequestedAt,
        FulfilledAt = ir.FulfilledAt
    };

    private static PortfolioCompanyDto MapPortfolioDto(PortfolioCompany p) => new()
    {
        PortfolioID = p.PortfolioID,
        InvestorID = p.InvestorID,
        CompanyName = p.CompanyName,
        Industry = p.Industry,
        InvestmentStage = p.InvestmentStage?.ToString(),
        InvestmentDate = p.InvestmentDate,
        InvestmentAmount = p.InvestmentAmount,
        CurrentStatus = p.CurrentStatus?.ToString(),
        ExitType = p.ExitType?.ToString(),
        ExitDate = p.ExitDate,
        ExitValue = p.ExitValue,
        Description = p.Description,
        CompanyLogoURL = p.CompanyLogoURL
    };
}
