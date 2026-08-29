using AIRecruiter.API.Extensions;
using AIRecruiter.Application.DTOs.Dashboard;
using AIRecruiter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly IAnalyticsService _analyticsService;

    public DashboardController(IDashboardService dashboardService, IAnalyticsService analyticsService)
    {
        _dashboardService = dashboardService;
        _analyticsService = analyticsService;
    }

    [Authorize(Roles = "Candidate")]
    [HttpGet("candidate")]
    public async Task<ActionResult<CandidateDashboardDto>> GetCandidateDashboard(CancellationToken ct)
    {
        var dashboard = await _dashboardService.GetCandidateDashboardAsync(User.GetUserId(), ct);
        return Ok(dashboard);
    }

    [Authorize(Roles = "Recruiter")]
    [HttpGet("recruiter")]
    public async Task<ActionResult<RecruiterDashboardDto>> GetRecruiterDashboard(CancellationToken ct)
    {
        var dashboard = await _dashboardService.GetRecruiterDashboardAsync(User.GetUserId(), ct);
        return Ok(dashboard);
    }

    [Authorize(Roles = "Recruiter")]
    [HttpGet("recruiter/analytics")]
    public async Task<IActionResult> GetRecruiterAnalytics(CancellationToken ct)
    {
        return Ok(await _analyticsService.GetRecruiterAnalyticsAsync(User.GetUserId(), ct));
    }
}
