using System.Security.Cryptography;
using System.Text;
using AIRecruiter.API.Extensions;
using AIRecruiter.Application.DTOs.Admin;
using AIRecruiter.Application.DTOs.Jobs;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IJobPostingService _jobPostingService;
    private readonly IModerationService _moderation;

    public JobsController(IJobPostingService jobPostingService, IModerationService moderation)
    {
        _jobPostingService = jobPostingService;
        _moderation = moderation;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<JobPostingDto>>> GetOpenJobs([FromQuery] string? search, CancellationToken ct)
    {
        var jobs = await _jobPostingService.GetOpenJobsAsync(search, ct);
        return Ok(jobs);
    }

    [Authorize(Roles = "Recruiter")]
    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<RecruiterJobSummaryDto>>> GetMyJobs(CancellationToken ct)
    {
        var jobs = await _jobPostingService.GetMyJobsAsync(User.GetUserId(), ct);
        return Ok(jobs);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<JobPostingDto>> GetById(int id, CancellationToken ct)
    {
        int? viewerUserId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;
        var viewerKey = viewerUserId.HasValue ? $"u:{viewerUserId}" : HashAnonymousVisitor();
        var isAdminViewer = User.Identity?.IsAuthenticated == true && User.IsInRole("Admin");

        var job = await _jobPostingService.GetByIdAsync(id, viewerKey, viewerUserId, isAdminViewer, ct);
        return job is null ? NotFound() : Ok(job);
    }

    [HttpPost("{id:int}/share")]
    public async Task<IActionResult> RecordShare(int id, CancellationToken ct)
    {
        int? viewerUserId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;
        var visitorKey = viewerUserId.HasValue ? $"u:{viewerUserId}" : HashAnonymousVisitor();
        await _jobPostingService.RecordShareAsync(id, visitorKey, ct);
        return NoContent();
    }

    [Authorize(Roles = "Recruiter")]
    [HttpPost]
    public async Task<ActionResult<JobPostingDto>> Create(CreateJobPostingRequest request, CancellationToken ct)
    {
        var job = await _jobPostingService.CreateAsync(User.GetUserId(), request, ct);
        return CreatedAtAction(nameof(GetById), new { id = job.Id }, job);
    }

    [Authorize(Roles = "Recruiter")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<JobPostingDto>> Update(int id, UpdateJobPostingRequest request, CancellationToken ct)
    {
        var job = await _jobPostingService.UpdateAsync(User.GetUserId(), id, request, ct);
        return Ok(job);
    }

    [Authorize(Roles = "Recruiter")]
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<JobPostingDto>> UpdateStatus(int id, UpdateJobStatusRequest request, CancellationToken ct)
    {
        var job = await _jobPostingService.UpdateStatusAsync(User.GetUserId(), id, request, ct);
        return Ok(job);
    }

    [Authorize(Roles = "Recruiter")]
    [HttpPost("{id:int}/duplicate")]
    public async Task<ActionResult<JobPostingDto>> Duplicate(int id, CancellationToken ct)
    {
        var job = await _jobPostingService.DuplicateAsync(User.GetUserId(), id, ct);
        return CreatedAtAction(nameof(GetById), new { id = job.Id }, job);
    }

    [Authorize]
    [HttpPost("{id:int}/report")]
    public async Task<IActionResult> Report(int id, ReportJobRequest request, CancellationToken ct)
    {
        await _moderation.SubmitReportAsync(User.GetUserId(), new SubmitReportRequest(ReportedEntityType.Job, id, request.Reason, request.Details), ct);
        return NoContent();
    }

    [Authorize(Roles = "Recruiter")]
    [HttpPatch("{id:int}/deadline")]
    public async Task<ActionResult<JobPostingDto>> ExtendDeadline(int id, UpdateJobDeadlineRequest request, CancellationToken ct)
    {
        var job = await _jobPostingService.ExtendDeadlineAsync(User.GetUserId(), id, request.ApplicationDeadlineUtc, ct);
        return Ok(job);
    }

    /// <summary>Derives a non-reversible visitor key for anonymous view de-duplication —
    /// hashes IP + User-Agent, never stores or logs either in raw form.</summary>
    private string HashAnonymousVisitor()
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers.UserAgent.ToString();
        var raw = $"{ip}|{userAgent}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hashBytes)[..24];
    }
}
