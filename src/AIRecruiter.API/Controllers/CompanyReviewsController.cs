using AIRecruiter.API.Extensions;
using AIRecruiter.Application.DTOs.Reviews;
using AIRecruiter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

[ApiController]
public class CompanyReviewsController : ControllerBase
{
    private readonly ICompanyReviewService _reviews;

    public CompanyReviewsController(ICompanyReviewService reviews)
    {
        _reviews = reviews;
    }

    [HttpGet("api/companies/{id:int}/reviews")]
    [AllowAnonymous]
    public async Task<ActionResult<CompanyReviewsSummaryDto>> GetPublished(int id, [FromQuery] string? relationshipType, CancellationToken ct) =>
        Ok(await _reviews.GetPublishedReviewsAsync(id, relationshipType, ct));

    [HttpGet("api/companies/{id:int}/reviews/eligibility")]
    [Authorize(Roles = "Candidate")]
    public async Task<ActionResult<ReviewEligibilityDto>> GetEligibility(int id, CancellationToken ct) =>
        Ok(await _reviews.GetEligibilityAsync(User.GetUserId(), id, ct));

    [HttpPost("api/companies/{id:int}/reviews")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> Submit(int id, SubmitCompanyReviewRequest request, CancellationToken ct)
    {
        await _reviews.SubmitAsync(User.GetUserId(), id, request, GetClientIp(), ct);
        return NoContent();
    }

    [HttpPost("api/reviews/{id:int}/respond")]
    [Authorize(Roles = "Recruiter")]
    public async Task<IActionResult> Respond(int id, RespondToReviewRequest request, CancellationToken ct)
    {
        await _reviews.RespondAsync(User.GetUserId(), id, request, ct);
        return NoContent();
    }

    private string GetClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
