using AIRecruiter.API.Extensions;
using AIRecruiter.Application.DTOs.Activity;
using AIRecruiter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

[ApiController]
[Route("api/activity")]
[Authorize]
public class ActivityController : ControllerBase
{
    private readonly IActivityTimelineService _timeline;

    public ActivityController(IActivityTimelineService timeline)
    {
        _timeline = timeline;
    }

    [HttpGet("timeline")]
    public async Task<ActionResult<IReadOnlyList<ActivityTimelineEntryDto>>> GetTimeline(
        [FromQuery] string? type, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        Ok(await _timeline.GetMyTimelineAsync(User.GetUserId(), type, from, to, ct));
}
