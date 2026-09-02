using AIRecruiter.API.Extensions;
using AIRecruiter.Application.DTOs.CareerGoals;
using AIRecruiter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

[ApiController]
[Route("api/career-goals")]
[Authorize(Roles = "Candidate")]
public class CareerGoalsController : ControllerBase
{
    private readonly ICareerGoalService _goals;

    public CareerGoalsController(ICareerGoalService goals)
    {
        _goals = goals;
    }

    [HttpGet]
    public async Task<ActionResult<CareerGoalsSummaryDto>> GetMine([FromQuery] string? status, CancellationToken ct) =>
        Ok(await _goals.GetMyGoalsAsync(User.GetUserId(), status, ct));

    [HttpPost]
    public async Task<ActionResult<CareerGoalDto>> Create(UpsertCareerGoalRequest request, CancellationToken ct) =>
        Ok(await _goals.CreateAsync(User.GetUserId(), request, ct));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CareerGoalDto>> Update(int id, UpsertCareerGoalRequest request, CancellationToken ct) =>
        Ok(await _goals.UpdateAsync(User.GetUserId(), id, request, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _goals.DeleteAsync(User.GetUserId(), id, ct);
        return NoContent();
    }
}
