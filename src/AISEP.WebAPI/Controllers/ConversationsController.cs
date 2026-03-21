using AISEP.Application.DTOs.Chat;
using AISEP.Application.DTOs.Common;
using AISEP.Application.Interfaces;
using AISEP.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AISEP.WebAPI.Controllers;

[ApiController]
[Route("api/conversations")]
[Tags("Conversations")]
[Authorize]
public class ConversationsController : ControllerBase
{
    private readonly IChatService _chatService;

    public ConversationsController(IChatService chatService)
    {
        _chatService = chatService;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    /// <summary>List my conversations (paged, optional status filter).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ConversationListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyConversations(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _chatService.GetMyConversationsAsync(
            GetCurrentUserId(), status, page, pageSize);
        return result.ToPagedEnvelope();
    }

    /// <summary>Create a new conversation for a connection or mentorship.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ConversationDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ConversationDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ConversationDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ConversationDto>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateConversationRequest request)
    {
        var result = await _chatService.CreateConversationAsync(GetCurrentUserId(), request);

        if (!result.Success)
            return result.ToErrorResult();

        return result.ToCreatedEnvelope();
    }

    /// <summary>Get conversation detail with participants.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ConversationDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ConversationDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetail(int id)
    {
        var result = await _chatService.GetConversationAsync(GetCurrentUserId(), id);
        return result.ToActionResult();
    }

    /// <summary>Close a conversation.</summary>
    [HttpPost("{id:int}/close")]
    [ProducesResponseType(typeof(ApiResponse<ConversationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ConversationDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ConversationDto>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Close(int id)
    {
        var result = await _chatService.CloseConversationAsync(GetCurrentUserId(), id);
        return result.ToActionResult();
    }

    /// <summary>Mark all messages in a conversation as read.</summary>
    [HttpPut("{id:int}/read")]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(int id)
    {
        var result = await _chatService.MarkConversationReadAsync(GetCurrentUserId(), id);
        return result.ToActionResult();
    }

    /// <summary>Get messages in a conversation (paged, newest first).</summary>
    [HttpGet("{id:int}/messages")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<MessageDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<MessageDto>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessages(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _chatService.GetMessagesAsync(
            GetCurrentUserId(), id, page, pageSize);
        return result.ToPagedEnvelope();
    }
}
