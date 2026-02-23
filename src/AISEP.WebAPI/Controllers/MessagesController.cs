using AISEP.Application.DTOs.Chat;
using AISEP.Application.DTOs.Common;
using AISEP.Application.Interfaces;
using AISEP.WebAPI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AISEP.WebAPI.Controllers;

[ApiController]
[Route("api/messages")]
[Tags("Messages")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IChatService _chatService;

    public MessagesController(IChatService chatService)
    {
        _chatService = chatService;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    /// <summary>Send a message in a conversation.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<MessageDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<MessageDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<MessageDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<MessageDto>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Send([FromBody] SendMessageRequest request)
    {
        var result = await _chatService.SendMessageAsync(GetCurrentUserId(), request);

        if (!result.Success)
            return result.ToErrorResult();

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Mark a single message as read.</summary>
    [HttpPost("{id:int}/read")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(int id)
    {
        var result = await _chatService.MarkReadAsync(GetCurrentUserId(), id);
        return result.ToActionResult();
    }

    /// <summary>Mark all unread messages in a conversation as read.</summary>
    [HttpPost("read-all")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkReadAll([FromBody] MarkReadAllRequest request)
    {
        var result = await _chatService.MarkReadAllAsync(GetCurrentUserId(), request);
        return result.ToActionResult();
    }
}
