using AIRecruiter.API.Extensions;
using AIRecruiter.Application.DTOs.Messaging;
using AIRecruiter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

[ApiController]
[Route("api/messages")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messages;

    public MessagesController(IMessageService messages)
    {
        _messages = messages;
    }

    [HttpGet("inbox")]
    public async Task<ActionResult<IReadOnlyList<ConversationSummaryDto>>> GetInbox(CancellationToken ct)
    {
        return Ok(await _messages.GetMyInboxAsync(User.GetUserId(), User.GetRole(), ct));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount(CancellationToken ct)
    {
        return Ok(await _messages.GetUnreadCountAsync(User.GetUserId(), User.GetRole(), ct));
    }

    [HttpGet("applications/{applicationId:int}")]
    public async Task<ActionResult<IReadOnlyList<MessageDto>>> GetThread(int applicationId, CancellationToken ct)
    {
        return Ok(await _messages.GetThreadAsync(User.GetUserId(), User.GetRole(), applicationId, ct));
    }

    [HttpPost("applications/{applicationId:int}")]
    public async Task<ActionResult<MessageDto>> Send(int applicationId, SendMessageRequest request, CancellationToken ct)
    {
        var message = await _messages.SendMessageAsync(User.GetUserId(), User.GetRole(), applicationId, GetClientIp(), request, ct);
        return Ok(message);
    }

    [HttpPost("applications/{applicationId:int}/read")]
    public async Task<IActionResult> MarkRead(int applicationId, CancellationToken ct)
    {
        await _messages.MarkThreadReadAsync(User.GetUserId(), User.GetRole(), applicationId, ct);
        return NoContent();
    }

    private string GetClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
