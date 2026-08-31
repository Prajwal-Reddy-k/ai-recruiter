using System.Text;
using AIRecruiter.API.Extensions;
using AIRecruiter.Application.DTOs.Interviews;
using AIRecruiter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<ActionResult<InterviewDto>> Schedule(int applicationId, ScheduleInterviewRequest request, CancellationToken ct)
    {
        var interview = await _interviews.ScheduleAsync(User.GetUserId(), applicationId, request, ct);
        return Ok(interview);
    }

    [HttpGet("api/applications/{applicationId:int}/interviews")]
    public async Task<ActionResult<IReadOnlyList<InterviewDto>>> GetForApplication(int applicationId, CancellationToken ct)
    {
        var interviews = await _interviews.GetForApplicationAsync(User.GetUserId(), User.GetRole(), applicationId, ct);
        return Ok(interviews);
    }

    [Authorize(Roles = "Recruiter")]
    [HttpPut("api/interviews/{interviewId:int}/reschedule")]
    public async Task<ActionResult<InterviewDto>> Reschedule(int interviewId, RescheduleInterviewRequest request, CancellationToken ct)
    {
        var interview = await _interviews.RescheduleAsync(User.GetUserId(), interviewId, request, ct);
        return Ok(interview);
    }

    [Authorize(Roles = "Recruiter")]
    [HttpPost("api/interviews/{interviewId:int}/cancel")]
    public async Task<ActionResult<InterviewDto>> Cancel(int interviewId, CancellationToken ct)
    {
        var interview = await _interviews.CancelAsync(User.GetUserId(), interviewId, ct);
        return Ok(interview);
    }

    [Authorize(Roles = "Recruiter")]
    [HttpPost("api/interviews/{interviewId:int}/complete")]
    public async Task<ActionResult<InterviewDto>> Complete(int interviewId, CancellationToken ct)
    {
        var interview = await _interviews.CompleteAsync(User.GetUserId(), interviewId, ct);
        return Ok(interview);
    }

    [Authorize(Roles = "Candidate")]
    [HttpPost("api/interviews/{interviewId:int}/accept")]
    public async Task<ActionResult<InterviewDto>> Accept(int interviewId, RespondInterviewRequest request, CancellationToken ct)
    {
        var interview = await _interviews.AcceptAsync(User.GetUserId(), interviewId, request, ct);
        return Ok(interview);
    }

    [Authorize(Roles = "Candidate")]
    [HttpPost("api/interviews/{interviewId:int}/decline")]
    public async Task<ActionResult<InterviewDto>> Decline(int interviewId, RespondInterviewRequest request, CancellationToken ct)
    {
        var interview = await _interviews.DeclineAsync(User.GetUserId(), interviewId, request, ct);
        return Ok(interview);
    }

    [HttpGet("api/interviews/mine")]
    public async Task<ActionResult<IReadOnlyList<InterviewDto>>> GetMine([FromQuery] string? status, CancellationToken ct)
    {
        var interviews = await _interviews.GetMyInterviewsAsync(User.GetUserId(), User.GetRole(), status, ct);
        return Ok(interviews);
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
