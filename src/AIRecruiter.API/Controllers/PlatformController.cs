using AIRecruiter.Application.DTOs.Platform;
using AIRecruiter.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

/// <summary>Public, unauthenticated platform-wide stats for the landing page.</summary>
[ApiController]
[Route("api/platform")]
public class PlatformController : ControllerBase
{
    private readonly IPlatformStatsService _stats;

    public PlatformController(IPlatformStatsService stats)
    {
        _stats = stats;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<PlatformStatsDto>> GetStats(CancellationToken ct) => Ok(await _stats.GetStatsAsync(ct));
}
