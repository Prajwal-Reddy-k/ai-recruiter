using AIRecruiter.API.Extensions;
using AIRecruiter.Application.DTOs.Admin;
using AIRecruiter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

/// <summary>Generic report submission for any of the four reportable entity types
/// (Job/Company/Message/User). Named distinctly from the existing ReportsController, which
/// is unrelated — recruiter analytics/CSV exports at api/recruiters/reports.</summary>
[ApiController]
[Route("api/reports")]
[Authorize]
public class ModerationReportsController : ControllerBase
{
    private readonly IModerationService _moderation;

    public ModerationReportsController(IModerationService moderation)
    {
        _moderation = moderation;
    }

    [HttpPost]
    public async Task<IActionResult> Submit(SubmitReportRequest request, CancellationToken ct)
    {
        await _moderation.SubmitReportAsync(User.GetUserId(), request, ct);
        return NoContent();
    }
}
