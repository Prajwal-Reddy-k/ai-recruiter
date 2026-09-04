using AIRecruiter.API.Extensions;
using AIRecruiter.Application.DTOs.Companies;
using AIRecruiter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

[ApiController]
[Route("api/companies")]
[Authorize(Roles = "Candidate")]
public class FollowController : ControllerBase
{
    private readonly IFollowService _follows;

    public FollowController(IFollowService follows)
    {
        _follows = follows;
    }

    [HttpPost("{id:int}/follow")]
    public async Task<IActionResult> Follow(int id, CancellationToken ct)
    {
        await _follows.FollowAsync(User.GetUserId(), id, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}/follow")]
    public async Task<IActionResult> Unfollow(int id, CancellationToken ct)
    {
        await _follows.UnfollowAsync(User.GetUserId(), id, ct);
        return NoContent();
    }

    [HttpGet("followed")]
    public async Task<ActionResult<IReadOnlyList<FollowedCompanyDto>>> GetFollowed(CancellationToken ct) =>
        Ok(await _follows.GetMyFollowedCompaniesAsync(User.GetUserId(), ct));

    [HttpPatch("{id:int}/follow/notify")]
    public async Task<IActionResult> UpdateNotifyPreference(int id, UpdateFollowNotifyRequest request, CancellationToken ct)
    {
        await _follows.UpdateNotifyPreferenceAsync(User.GetUserId(), id, request, ct);
        return NoContent();
    }
}
