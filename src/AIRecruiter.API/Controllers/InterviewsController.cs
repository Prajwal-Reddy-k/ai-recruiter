using AIRecruiter.API.Extensions;
using AIRecruiter.Application.DTOs.Interviews;
using AIRecruiter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace AIRecruiter.API.Controllers;

[ApiController]
[Authorize]
public class InterviewsController : ControllerBase
{
    private readonly IInterviewService _interviews;

    public InterviewsController(IInterviewService interviews)
    {
        _interviews = interviews;
    }

    [Authorize(Roles = "Recruiter")]
    [HttpPost("api/applications/{applicationId:int}/interviews")]
    public async Task<ActionResult<InterviewDto>> Propose(int applicationId, ProposeInterviewRequest request, CancellationToken ct)
    {
        var interview = await _interviews.ProposeAsync(User.GetUserId(), applicationId, request, ct);
        return Ok(interview);
    }

    [HttpGet("api/applications/{applicationId:int}/interviews")]
    public async Task<ActionResult<IReadOnlyList<InterviewDto>>> GetForApplication(int applicationId, CancellationToken ct)
    {
        var interviews = await _interviews.GetForApplicationAsync(User.GetUserId(), User.GetRole(), applicationId, ct);
        return Ok(interviews);
    }

    [Authorize(Roles = "Candidate")]
    [HttpPost("api/interviews/{interviewId:int}/respond")]
    public async Task<ActionResult<InterviewDto>> Respond(int interviewId, RespondInterviewRequest request, CancellationToken ct)
    {
        var interview = await _interviews.RespondAsync(User.GetUserId(), interviewId, request, ct);
        return Ok(interview);
    }

    [HttpGet("api/interviews/upcoming")]
    public async Task<ActionResult<IReadOnlyList<UpcomingInterviewDto>>> GetUpcoming(CancellationToken ct)
    {
        var interviews = await _interviews.GetUpcomingAsync(User.GetUserId(), User.GetRole(), ct);
        return Ok(interviews);
    }

    [HttpGet("api/interviews/{interviewId:int}/calendar.ics")]
    public async Task<IActionResult> DownloadIcs(int interviewId, CancellationToken ct)
    {
        var (content, fileName) = await _interviews.GetIcsAsync(User.GetUserId(), User.GetRole(), interviewId, ct);
        return File(Encoding.UTF8.GetBytes(content), "text/calendar", fileName);
    }
}
