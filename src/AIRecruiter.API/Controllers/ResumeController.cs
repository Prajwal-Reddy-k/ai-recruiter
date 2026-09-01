using AIRecruiter.API.Extensions;
using AIRecruiter.Application.DTOs.Candidates;
using AIRecruiter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

/// <summary>The structured, in-app resume builder — separate from CandidatesController's
/// free-text profile fields and uploaded-resume-file endpoints.</summary>
[ApiController]
[Route("api/candidates/me/resume-builder")]
[Authorize(Roles = "Candidate")]
public class ResumeController : ControllerBase
{
    private readonly IResumeBuilderService _resume;

    public ResumeController(IResumeBuilderService resume)
    {
        _resume = resume;
    }

    [HttpGet]
    public async Task<ActionResult<ResumeDto>> Get(CancellationToken ct) => Ok(await _resume.GetMyResumeAsync(User.GetUserId(), ct));

    [HttpPut("summary")]
    public async Task<ActionResult<ResumeDto>> UpsertSummary(UpsertResumeSummaryRequest request, CancellationToken ct) =>
        Ok(await _resume.UpsertSummaryLinksAsync(User.GetUserId(), request, ct));

    [HttpPost("experience")]
    public async Task<ActionResult<ResumeDto>> AddExperience(UpsertWorkExperienceRequest request, CancellationToken ct) =>
        Ok(await _resume.AddExperienceAsync(User.GetUserId(), request, ct));

    [HttpPut("experience/{id:int}")]
    public async Task<ActionResult<ResumeDto>> UpdateExperience(int id, UpsertWorkExperienceRequest request, CancellationToken ct) =>
        Ok(await _resume.UpdateExperienceAsync(User.GetUserId(), id, request, ct));

    [HttpDelete("experience/{id:int}")]
    public async Task<ActionResult<ResumeDto>> DeleteExperience(int id, CancellationToken ct) =>
        Ok(await _resume.DeleteExperienceAsync(User.GetUserId(), id, ct));

    [HttpPost("experience/reorder")]
    public async Task<ActionResult<ResumeDto>> ReorderExperience(ReorderRequest request, CancellationToken ct) =>
        Ok(await _resume.ReorderExperienceAsync(User.GetUserId(), request, ct));

    [HttpPost("education")]
    public async Task<ActionResult<ResumeDto>> AddEducation(UpsertEducationEntryRequest request, CancellationToken ct) =>
        Ok(await _resume.AddEducationAsync(User.GetUserId(), request, ct));

    [HttpPut("education/{id:int}")]
    public async Task<ActionResult<ResumeDto>> UpdateEducation(int id, UpsertEducationEntryRequest request, CancellationToken ct) =>
        Ok(await _resume.UpdateEducationAsync(User.GetUserId(), id, request, ct));

    [HttpDelete("education/{id:int}")]
    public async Task<ActionResult<ResumeDto>> DeleteEducation(int id, CancellationToken ct) =>
        Ok(await _resume.DeleteEducationAsync(User.GetUserId(), id, ct));

    [HttpPost("education/reorder")]
    public async Task<ActionResult<ResumeDto>> ReorderEducation(ReorderRequest request, CancellationToken ct) =>
        Ok(await _resume.ReorderEducationAsync(User.GetUserId(), request, ct));

    [HttpPost("certifications")]
    public async Task<ActionResult<ResumeDto>> AddCertification(UpsertCertificationRequest request, CancellationToken ct) =>
        Ok(await _resume.AddCertificationAsync(User.GetUserId(), request, ct));

    [HttpPut("certifications/{id:int}")]
    public async Task<ActionResult<ResumeDto>> UpdateCertification(int id, UpsertCertificationRequest request, CancellationToken ct) =>
        Ok(await _resume.UpdateCertificationAsync(User.GetUserId(), id, request, ct));

    [HttpDelete("certifications/{id:int}")]
    public async Task<ActionResult<ResumeDto>> DeleteCertification(int id, CancellationToken ct) =>
        Ok(await _resume.DeleteCertificationAsync(User.GetUserId(), id, ct));

    [HttpPost("certifications/reorder")]
    public async Task<ActionResult<ResumeDto>> ReorderCertification(ReorderRequest request, CancellationToken ct) =>
        Ok(await _resume.ReorderCertificationAsync(User.GetUserId(), request, ct));

    [HttpPost("projects")]
    public async Task<ActionResult<ResumeDto>> AddProject(UpsertProjectRequest request, CancellationToken ct) =>
        Ok(await _resume.AddProjectAsync(User.GetUserId(), request, ct));

    [HttpPut("projects/{id:int}")]
    public async Task<ActionResult<ResumeDto>> UpdateProject(int id, UpsertProjectRequest request, CancellationToken ct) =>
        Ok(await _resume.UpdateProjectAsync(User.GetUserId(), id, request, ct));

    [HttpDelete("projects/{id:int}")]
    public async Task<ActionResult<ResumeDto>> DeleteProject(int id, CancellationToken ct) =>
        Ok(await _resume.DeleteProjectAsync(User.GetUserId(), id, ct));

    [HttpPost("projects/reorder")]
    public async Task<ActionResult<ResumeDto>> ReorderProject(ReorderRequest request, CancellationToken ct) =>
        Ok(await _resume.ReorderProjectAsync(User.GetUserId(), request, ct));
}
