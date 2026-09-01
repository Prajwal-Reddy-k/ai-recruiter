using AIRecruiter.API.Extensions;
using AIRecruiter.Application.DTOs.Invitations;
using AIRecruiter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

[ApiController]
[Route("api/invitations")]
[Authorize]
public class InvitationsController : ControllerBase
{
    private readonly IInvitationService _invitations;

    public InvitationsController(IInvitationService invitations)
    {
        _invitations = invitations;
    }

    [Authorize(Roles = "Recruiter")]
    [HttpPost]
    public async Task<ActionResult<InvitationDto>> Invite(InviteCandidateRequest request, CancellationToken ct)
    {
        var invitation = await _invitations.InviteAsync(User.GetUserId(), GetClientIp(), request, ct);
        return Ok(invitation);
    }

    [Authorize(Roles = "Recruiter")]
    [HttpGet("sent")]
    public async Task<ActionResult<IReadOnlyList<InvitationDto>>> GetSent([FromQuery] int? jobPostingId, CancellationToken ct)
    {
        return Ok(await _invitations.GetSentInvitationsAsync(User.GetUserId(), jobPostingId, ct));
    }

    [Authorize(Roles = "Candidate")]
    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<InvitationDto>>> GetMine(CancellationToken ct)
    {
        return Ok(await _invitations.GetMyInvitationsAsync(User.GetUserId(), ct));
    }

    [Authorize(Roles = "Candidate")]
    [HttpPost("{id:int}/view")]
    public async Task<ActionResult<InvitationDto>> MarkViewed(int id, CancellationToken ct)
    {
        return Ok(await _invitations.MarkViewedAsync(User.GetUserId(), id, ct));
    }

    [Authorize(Roles = "Candidate")]
    [HttpPost("{id:int}/accept")]
    public async Task<ActionResult<InvitationDto>> Accept(int id, CancellationToken ct)
    {
        return Ok(await _invitations.RespondAsync(User.GetUserId(), id, accept: true, ct));
    }

    [Authorize(Roles = "Candidate")]
    [HttpPost("{id:int}/decline")]
    public async Task<ActionResult<InvitationDto>> Decline(int id, CancellationToken ct)
    {
        return Ok(await _invitations.RespondAsync(User.GetUserId(), id, accept: false, ct));
    }

    [Authorize(Roles = "Candidate")]
    [HttpPost("{id:int}/dismiss")]
    public async Task<IActionResult> Dismiss(int id, CancellationToken ct)
    {
        await _invitations.DismissAsync(User.GetUserId(), id, ct);
        return NoContent();
    }

    private string GetClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
