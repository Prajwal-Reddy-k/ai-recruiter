using AIRecruiter.Application.DTOs.SalaryInsights;
using AIRecruiter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

[ApiController]
[Route("api/salary-insights")]
[AllowAnonymous]
public class SalaryInsightsController : ControllerBase
{
    private readonly ISalaryInsightsService _salaryInsights;

    public SalaryInsightsController(ISalaryInsightsService salaryInsights)
    {
        _salaryInsights = salaryInsights;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SalaryInsightDto>>> GetInsights(
        [FromQuery] string? role, [FromQuery] string? city, [FromQuery] string? state,
        [FromQuery] bool? isRemote, [FromQuery] string? experienceBand, CancellationToken ct) =>
        Ok(await _salaryInsights.GetInsightsAsync(new SalaryInsightsQuery(role, city, state, isRemote, experienceBand), ct));
}
