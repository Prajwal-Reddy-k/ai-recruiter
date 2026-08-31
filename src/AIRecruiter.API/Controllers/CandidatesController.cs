using AIRecruiter.API.Extensions;
using AIRecruiter.Application.DTOs.Candidates;
using AIRecruiter.Application.DTOs.SavedJobs;
using AIRecruiter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

[ApiController]
[Route("api/candidates")]
[Authorize(Roles = "Candidate")]
public class CandidatesController : ControllerBase
{
    private readonly ICandidateProfileService _profileService;
    private readonly ISavedJobService _savedJobService;
    private readonly IJobAlertService _jobAlertService;

    public CandidatesController(ICandidateProfileService profileService, ISavedJobService savedJobService, IJobAlertService jobAlertService)
    {
        _profileService = profileService;
        _savedJobService = savedJobService;
        _jobAlertService = jobAlertService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<CandidateProfileDto>> GetMyProfile(CancellationToken ct)
    {
        var profile = await _profileService.GetMyProfileAsync(User.GetUserId(), ct);
        return Ok(profile);
    }

    [HttpPut("me")]
    public async Task<ActionResult<CandidateProfileDto>> UpsertMyProfile(UpsertCandidateProfileRequest request, CancellationToken ct)
    {
        var profile = await _profileService.UpsertMyProfileAsync(User.GetUserId(), request, ct);
        return Ok(profile);
    }

    [HttpPost("me/resume")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<CandidateProfileDto>> UploadResume(IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var profile = await _profileService.UploadResumeAsync(
            User.GetUserId(), stream, file.FileName, file.ContentType, file.Length, ct);
        return Ok(profile);
    }

    [HttpGet("me/resume/download")]
    public async Task<IActionResult> DownloadMyResume(CancellationToken ct)
    {
        var (content, fileName, contentType) = await _profileService.DownloadOwnResumeAsync(User.GetUserId(), ct);
        return File(content, contentType, fileName);
    }

    [HttpPost("me/avatar")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<CandidateProfileDto>> UploadAvatar(IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var profile = await _profileService.UploadAvatarAsync(
            User.GetUserId(), stream, file.FileName, file.ContentType, file.Length, ct);
        return Ok(profile);
    }

    [HttpDelete("me/avatar")]
    public async Task<ActionResult<CandidateProfileDto>> RemoveAvatar(CancellationToken ct)
    {
        var profile = await _profileService.RemoveAvatarAsync(User.GetUserId(), ct);
        return Ok(profile);
    }

    [AllowAnonymous]
    [HttpGet("{candidateProfileId:int}/avatar")]
    public async Task<IActionResult> GetAvatar(int candidateProfileId, CancellationToken ct)
    {
        var (content, contentType) = await _profileService.OpenAvatarAsync(candidateProfileId, ct);
        Response.Headers.CacheControl = "public, max-age=300";
        return File(content, contentType);
    }

    [HttpGet("me/saved-jobs")]
    public async Task<IActionResult> GetSavedJobs(CancellationToken ct)
    {
        return Ok(await _savedJobService.GetMySavedJobsWithDatesAsync(User.GetUserId(), ct));
    }

    [HttpPost("me/saved-jobs/{jobId:int}")]
    public async Task<IActionResult> SaveJob(int jobId, CancellationToken ct)
    {
        await _savedJobService.SaveAsync(User.GetUserId(), jobId, ct);
        return NoContent();
    }

    [HttpDelete("me/saved-jobs/{jobId:int}")]
    public async Task<IActionResult> UnsaveJob(int jobId, CancellationToken ct)
    {
        await _savedJobService.UnsaveAsync(User.GetUserId(), jobId, ct);
        return NoContent();
    }

    [HttpGet("me/alerts")]
    public async Task<IActionResult> GetAlerts(CancellationToken ct)
    {
        return Ok(await _jobAlertService.GetMyAlertsAsync(User.GetUserId(), ct));
    }

    [HttpGet("me/alerts/matches")]
    public async Task<IActionResult> GetAlertMatches(CancellationToken ct)
    {
        return Ok(await _jobAlertService.GetMatchingJobsAsync(User.GetUserId(), 10, ct));
    }

    [HttpPost("me/alerts")]
    public async Task<IActionResult> CreateAlert(UpsertJobAlertRequest request, CancellationToken ct)
    {
        var alert = await _jobAlertService.CreateAsync(User.GetUserId(), request, ct);
        return Ok(alert);
    }

    [HttpPut("me/alerts/{alertId:int}")]
    public async Task<IActionResult> UpdateAlert(int alertId, UpsertJobAlertRequest request, CancellationToken ct)
    {
        var alert = await _jobAlertService.UpdateAsync(User.GetUserId(), alertId, request, ct);
        return Ok(alert);
    }

    [HttpPatch("me/alerts/{alertId:int}/active")]
    public async Task<IActionResult> SetAlertActive(int alertId, SetAlertActiveRequest request, CancellationToken ct)
    {
        var alert = await _jobAlertService.SetActiveAsync(User.GetUserId(), alertId, request.IsActive, ct);
        return Ok(alert);
    }

    [HttpDelete("me/alerts/{alertId:int}")]
    public async Task<IActionResult> DeleteAlert(int alertId, CancellationToken ct)
    {
        await _jobAlertService.DeleteAsync(User.GetUserId(), alertId, ct);
        return NoContent();
    }
}

public record SetAlertActiveRequest(bool IsActive);
