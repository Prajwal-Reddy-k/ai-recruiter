using AIRecruiter.API.Extensions;
using AIRecruiter.Application.DTOs.Applications;
using AIRecruiter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

public record ApplyRequest(string? CoverNote, IReadOnlyList<SubmitScreeningAnswerRequest>? Answers = null);

[ApiController]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly IJobApplicationService _applicationService;

    public ApplicationsController(IJobApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    [Authorize(Roles = "Candidate")]
    [HttpPost("api/jobs/{jobId:int}/apply")]
    public async Task<ActionResult<JobApplicationDto>> Apply(int jobId, ApplyRequest? request, CancellationToken ct)
    {
        var application = await _applicationService.ApplyAsync(User.GetUserId(), jobId, request?.CoverNote, request?.Answers, ct);
        return Ok(application);
    }

    [Authorize(Roles = "Candidate")]
    [HttpGet("api/applications/me")]
    public async Task<ActionResult<IReadOnlyList<JobApplicationDto>>> GetMyApplications(CancellationToken ct)
    {
        var applications = await _applicationService.GetMyApplicationsAsync(User.GetUserId(), ct);
        return Ok(applications);
    }

    [HttpGet("api/applications/{id:int}")]
    public async Task<ActionResult<JobApplicationDetailDto>> GetDetail(int id, CancellationToken ct)
    {
        var role = User.GetRole();
        var detail = await _applicationService.GetApplicationDetailAsync(User.GetUserId(), role, id, ct);
        return Ok(detail);
    }

    [HttpGet("api/applications/{id:int}/resume")]
    public async Task<IActionResult> DownloadResume(int id, CancellationToken ct)
    {
        var role = User.GetRole();
        var (content, fileName, contentType) = await _applicationService.DownloadApplicantResumeAsync(User.GetUserId(), role, id, ct);
        return File(content, contentType, fileName);
    }

    [Authorize(Roles = "Recruiter")]
    [HttpGet("api/jobs/{jobId:int}/applications")]
    public async Task<ActionResult<IReadOnlyList<JobApplicationDto>>> GetApplicationsForJob(int jobId, [FromQuery] ApplicantScreeningFilterQuery filter, CancellationToken ct)
    {
        var applications = await _applicationService.GetApplicationsForJobAsync(User.GetUserId(), jobId, filter, ct);
        return Ok(applications);
    }

    [Authorize(Roles = "Recruiter")]
    [HttpPatch("api/applications/{id:int}/status")]
    public async Task<ActionResult<JobApplicationDto>> UpdateStatus(int id, UpdateApplicationStatusRequest request, CancellationToken ct)
    {
        var application = await _applicationService.UpdateStatusAsync(User.GetUserId(), id, request.Status, request.Note, ct);
        return Ok(application);
    }

    [Authorize(Roles = "Candidate")]
    [HttpPost("api/applications/{id:int}/withdraw")]
    public async Task<ActionResult<JobApplicationDto>> Withdraw(int id, CancellationToken ct)
    {
        var application = await _applicationService.WithdrawAsync(User.GetUserId(), id, ct);
        return Ok(application);
    }
}
