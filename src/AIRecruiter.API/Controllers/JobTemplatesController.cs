using AIRecruiter.API.Extensions;
using AIRecruiter.Application.DTOs.JobTemplates;
using AIRecruiter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

[ApiController]
[Route("api/recruiters/job-templates")]
[Authorize(Roles = "Recruiter")]
public class JobTemplatesController : ControllerBase
{
    private readonly IJobTemplateService _templates;

    public JobTemplatesController(IJobTemplateService templates)
    {
        _templates = templates;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<JobTemplateDto>>> GetMine([FromQuery] string? search, CancellationToken ct)
    {
        return Ok(await _templates.GetMyTemplatesAsync(User.GetUserId(), search, ct));
    }

    [HttpGet("{templateId:int}")]
    public async Task<ActionResult<JobTemplateDto>> GetById(int templateId, CancellationToken ct)
    {
        return Ok(await _templates.GetByIdAsync(User.GetUserId(), templateId, ct));
    }

    [HttpPost]
    public async Task<ActionResult<JobTemplateDto>> Create(UpsertJobTemplateRequest request, CancellationToken ct)
    {
        return Ok(await _templates.CreateAsync(User.GetUserId(), request, ct));
    }

    [HttpPost("from-job/{jobId:int}")]
    public async Task<ActionResult<JobTemplateDto>> CreateFromJob(int jobId, [FromQuery] string? title, CancellationToken ct)
    {
        return Ok(await _templates.CreateFromJobAsync(User.GetUserId(), jobId, title, ct));
    }

    [HttpPut("{templateId:int}")]
    public async Task<ActionResult<JobTemplateDto>> Update(int templateId, UpsertJobTemplateRequest request, CancellationToken ct)
    {
        return Ok(await _templates.UpdateAsync(User.GetUserId(), templateId, request, ct));
    }

    [HttpDelete("{templateId:int}")]
    public async Task<IActionResult> Delete(int templateId, CancellationToken ct)
    {
        await _templates.DeleteAsync(User.GetUserId(), templateId, ct);
        return NoContent();
    }

    [HttpPost("{templateId:int}/duplicate")]
    public async Task<ActionResult<JobTemplateDto>> Duplicate(int templateId, CancellationToken ct)
    {
        return Ok(await _templates.DuplicateAsync(User.GetUserId(), templateId, ct));
    }

    [HttpPost("{templateId:int}/create-job")]
    public async Task<IActionResult> CreateJob(int templateId, CancellationToken ct)
    {
        return Ok(await _templates.CreateDraftJobFromTemplateAsync(User.GetUserId(), templateId, ct));
    }
}
