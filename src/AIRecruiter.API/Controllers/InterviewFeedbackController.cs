using AIRecruiter.API.Extensions;
using AIRecruiter.Application.DTOs.InterviewFeedback;
using AIRecruiter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

[ApiController]
[Route("api/interviews/{interviewId:int}/feedback")]
[Authorize(Roles = "Recruiter")]
public class InterviewFeedbackController : ControllerBase
{
    private readonly IInterviewFeedbackService _feedback;

    public InterviewFeedbackController(IInterviewFeedbackService feedback)
    {
        _feedback = feedback;
    }

    [HttpGet("mine")]
    public async Task<ActionResult<InterviewFeedbackDto?>> GetMine(int interviewId, CancellationToken ct)
    {
        return Ok(await _feedback.GetMyFeedbackAsync(User.GetUserId(), interviewId, ct));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<InterviewFeedbackSummaryDto>> GetSummary(int interviewId, CancellationToken ct)
    {
        return Ok(await _feedback.GetSummaryAsync(User.GetUserId(), interviewId, ct));
    }

    [HttpPut("draft")]
    public async Task<ActionResult<InterviewFeedbackDto>> SaveDraft(int interviewId, UpsertInterviewFeedbackRequest request, CancellationToken ct)
    {
        return Ok(await _feedback.SaveDraftAsync(User.GetUserId(), interviewId, request, ct));
    }

    [HttpPost("submit")]
    public async Task<ActionResult<InterviewFeedbackDto>> Submit(int interviewId, UpsertInterviewFeedbackRequest request, CancellationToken ct)
    {
        return Ok(await _feedback.SubmitAsync(User.GetUserId(), interviewId, request, ct));
    }
}
