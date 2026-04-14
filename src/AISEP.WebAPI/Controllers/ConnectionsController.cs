using AISEP.Application.DTOs.Common;
using AISEP.Application.DTOs.Connection;
using AISEP.Application.Interfaces;
using AISEP.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AISEP.WebAPI.Controllers;

/// <summary>
/// Investor ↔ Startup connection lifecycle, information requests.
/// </summary>
[ApiController]
[Route("api/connections")]
[Tags("Connections")]
[Authorize]
public class ConnectionsController : ControllerBase
{
    private readonly IConnectionsService _svc;

    public ConnectionsController(IConnectionsService svc) => _svc = svc;

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    private string GetCurrentUserType()
        => User.FindFirst("userType")?.Value ?? string.Empty;

    // ================================================================
    // 1a) POST /api/connections (Investor → Startup)
    // ================================================================

    /// <summary>Create a connection offer from the current investor to a startup.</summary>
    [HttpPost]
    [Authorize(Policy = "InvestorOnly")]
    [ProducesResponseType(typeof(ApiResponse<ConnectionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ConnectionDto>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateConnectionRequest request)
    {
        var result = await _svc.CreateConnectionAsync(GetCurrentUserId(), GetCurrentUserType(), request);
        if (!result.Success) return result.ToErrorResult();
        return result.ToCreatedEnvelope();
    }

    // ================================================================
    // 1b) GET /api/connections/can-invite?investorId={id} (Startup)
    // ================================================================

    /// <summary>Check whether the current startup can invite a specific investor. Returns all active blockers.</summary>
    [HttpGet("can-invite")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<CanInviteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CanInvite([FromQuery] int investorId)
    {
        var result = await _svc.CanInviteAsync(GetCurrentUserId(), investorId);
        return result.ToActionResult();
    }

    // ================================================================
    // 1c) POST /api/connections/invite (Startup → Investor)
    // ================================================================

    /// <summary>Create a connection invite from the current startup to an investor.</summary>
    [HttpPost("invite")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<ConnectionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ConnectionDto>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Invite([FromBody] CreateStartupToInvestorRequest request)
    {
        var result = await _svc.CreateConnectionFromStartupAsync(GetCurrentUserId(), request);
        if (!result.Success) return result.ToErrorResult();
        return result.ToCreatedEnvelope();
    }

    // ================================================================
    // 2) GET /api/connections/sent (both roles — connections I initiated)
    // ================================================================

    /// <summary>List connections initiated by the current user (Investor or Startup).</summary>
    [HttpGet("sent")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ConnectionListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSent([FromQuery] string? status, [FromQuery] string? keyword, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _svc.GetSentAsync(GetCurrentUserId(), GetCurrentUserType(), status, keyword, page, pageSize);
        return result.ToPagedEnvelope();
    }

    // ================================================================
    // 3) PUT /api/connections/{id} (Investor — only Requested)
    // ================================================================

    /// <summary>Update a pending connection message. Investor-only, status must be 'Requested'.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "InvestorOnly")]
    [ProducesResponseType(typeof(ApiResponse<ConnectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateConnectionRequest request)
    {
        var result = await _svc.UpdateAsync(GetCurrentUserId(), id, request);
        return result.ToActionResult();
    }

    // ================================================================
    // 4) POST /api/connections/{id}/withdraw (both roles — initiator only)
    // ================================================================

    /// <summary>Withdraw a pending connection. Only the initiating party can withdraw.</summary>
    [HttpPost("{id:int}/withdraw")]
    [ProducesResponseType(typeof(ApiResponse<ConnectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Withdraw(int id)
    {
        var result = await _svc.WithdrawAsync(GetCurrentUserId(), GetCurrentUserType(), id);
        return result.ToActionResult();
    }

    // ================================================================
    // 5) GET /api/connections/received (both roles — connections I received)
    // ================================================================

    /// <summary>List connections received by the current user (Investor or Startup).</summary>
    [HttpGet("received")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ConnectionListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReceived([FromQuery] string? status, [FromQuery] string? keyword, [FromQuery] int? counterpartId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _svc.GetReceivedAsync(GetCurrentUserId(), GetCurrentUserType(), status, keyword, counterpartId, page, pageSize);
        return result.ToPagedEnvelope();
    }

    // ================================================================
    // 6) POST /api/connections/{id}/accept (both roles — receiver only)
    // ================================================================

    /// <summary>Accept a connection. Only the receiving party can accept.</summary>
    [HttpPost("{id:int}/accept")]
    [ProducesResponseType(typeof(ApiResponse<ConnectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Accept(int id)
    {
        var result = await _svc.AcceptAsync(GetCurrentUserId(), GetCurrentUserType(), id);
        return result.ToActionResult();
    }

    // ================================================================
    // 7) POST /api/connections/{id}/reject (both roles — receiver only)
    // ================================================================

    /// <summary>Reject a connection with optional reason. Only the receiving party can reject.</summary>
    [HttpPost("{id:int}/reject")]
    [ProducesResponseType(typeof(ApiResponse<ConnectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectConnectionRequest request)
    {
        var result = await _svc.RejectAsync(GetCurrentUserId(), GetCurrentUserType(), id, request.Reason);
        return result.ToActionResult();
    }

    // ================================================================
    // 8) POST /api/connections/{id}/close (Startup or Investor)
    // ================================================================

    /// <summary>Close an accepted connection. Either participant can close.</summary>
    [HttpPost("{id:int}/close")]
    [ProducesResponseType(typeof(ApiResponse<ConnectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Close(int id)
    {
        var result = await _svc.CloseAsync(GetCurrentUserId(), GetCurrentUserType(), id);
        return result.ToActionResult();
    }

    // ================================================================
    // 9) GET /api/connections/{id} (Participant / Staff)
    // ================================================================

    /// <summary>Get connection detail including information requests. Participants or Staff/Admin.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ConnectionDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ConnectionDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetail(int id)
    {
        var result = await _svc.GetDetailAsync(GetCurrentUserId(), GetCurrentUserType(), id);
        return result.ToActionResult();
    }

    // ================================================================
    // 10) POST /api/connections/{id}/info-requests (Investor)
    // ================================================================

    /// <summary>Create an information request within an accepted connection. Investor-only.</summary>
    [HttpPost("{id:int}/info-requests")]
    [Authorize(Policy = "InvestorOnly")]
    [ProducesResponseType(typeof(ApiResponse<InfoRequestDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateInfoRequest(int id, [FromBody] CreateInfoRequest request)
    {
        var result = await _svc.CreateInfoRequestAsync(GetCurrentUserId(), id, request);
        if (!result.Success) return result.ToErrorResult();
        return result.ToCreatedEnvelope();
    }

    // ================================================================
    // 11) GET /api/connections/{id}/info-requests (Participant / Staff)
    // ================================================================

    /// <summary>List information requests for a connection. Participants or Staff/Admin.</summary>
    [HttpGet("{id:int}/info-requests")]
    [ProducesResponseType(typeof(ApiResponse<List<InfoRequestDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInfoRequests(int id)
    {
        var result = await _svc.GetInfoRequestsAsync(GetCurrentUserId(), GetCurrentUserType(), id);
        return result.ToActionResult();
    }

    // ================================================================
    // 12) POST /api/info-requests/{requestId}/fulfill (Startup)
    // ================================================================

    /// <summary>Fulfill an information request. Startup-only.</summary>
    [HttpPost("/api/info-requests/{requestId:int}/fulfill")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<InfoRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> FulfillInfoRequest(int requestId, [FromBody] FulfillInfoRequest request)
    {
        var result = await _svc.FulfillInfoRequestAsync(GetCurrentUserId(), requestId, request);
        return result.ToActionResult();
    }

    // ================================================================
    // 13) POST /api/info-requests/{requestId}/reject (Startup)
    // ================================================================

    /// <summary>Reject an information request. Startup-only.</summary>
    [HttpPost("/api/info-requests/{requestId:int}/reject")]
    [Authorize(Policy = "StartupOnly")]
    [ProducesResponseType(typeof(ApiResponse<InfoRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RejectInfoRequest(int requestId, [FromBody] RejectConnectionRequest request)
    {
        var result = await _svc.RejectInfoRequestAsync(GetCurrentUserId(), requestId, request.Reason);
        return result.ToActionResult();
    }
}
