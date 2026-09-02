using AIRecruiter.API.Extensions;
using AIRecruiter.Application.DTOs.Assessments;
using AIRecruiter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

[ApiController]
[Route("api/assessments")]
[Authorize(Roles = "Candidate")]
public class SkillAssessmentsController : ControllerBase
{
    private readonly ISkillAssessmentService _assessments;

    public SkillAssessmentsController(ISkillAssessmentService assessments)
    {
        _assessments = assessments;
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<AssessmentCategorySummaryDto>>> GetCategories(CancellationToken ct) =>
        Ok(await _assessments.GetCategoriesAsync(User.GetUserId(), ct));

    [HttpPost("attempts")]
    public async Task<ActionResult<AssessmentAttemptInProgressDto>> StartAttempt(StartAssessmentAttemptRequest request, CancellationToken ct) =>
        Ok(await _assessments.StartAttemptAsync(User.GetUserId(), request, ct));

    [HttpGet("attempts/{id:int}")]
    public async Task<ActionResult<AssessmentAttemptInProgressDto>> GetActiveAttempt(int id, CancellationToken ct) =>
        Ok(await _assessments.GetActiveAttemptAsync(User.GetUserId(), id, ct));

    [HttpPost("attempts/{id:int}/answers")]
    public async Task<IActionResult> AnswerQuestion(int id, SubmitAssessmentAnswerRequest request, CancellationToken ct)
    {
        await _assessments.AnswerQuestionAsync(User.GetUserId(), id, request, ct);
        return NoContent();
    }

    [HttpPost("attempts/{id:int}/submit")]
    public async Task<ActionResult<AssessmentAttemptResultDto>> SubmitAttempt(int id, CancellationToken ct) =>
        Ok(await _assessments.SubmitAttemptAsync(User.GetUserId(), id, ct));

    [HttpGet("attempts/{id:int}/review")]
    public async Task<ActionResult<AssessmentAttemptResultDto>> GetReview(int id, CancellationToken ct) =>
        Ok(await _assessments.GetAttemptReviewAsync(User.GetUserId(), id, ct));

    [HttpPatch("attempts/{id:int}/visibility")]
    public async Task<IActionResult> SetVisibility(int id, SetAttemptVisibilityRequest request, CancellationToken ct)
    {
        await _assessments.SetAttemptVisibilityAsync(User.GetUserId(), id, request, ct);
        return NoContent();
    }

    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<AssessmentAttemptHistoryItemDto>>> GetHistory(CancellationToken ct) =>
        Ok(await _assessments.GetMyHistoryAsync(User.GetUserId(), ct));
}
