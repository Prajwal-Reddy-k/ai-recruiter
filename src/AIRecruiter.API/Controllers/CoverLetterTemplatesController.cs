using AIRecruiter.API.Extensions;
using AIRecruiter.Application.DTOs.CoverLetters;
using AIRecruiter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

[ApiController]
[Route("api/cover-letter-templates")]
[Authorize(Roles = "Candidate")]
public class CoverLetterTemplatesController : ControllerBase
{
    private readonly ICoverLetterTemplateService _templates;

    public CoverLetterTemplatesController(ICoverLetterTemplateService templates)
    {
        _templates = templates;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CoverLetterTemplateDto>>> GetMine(CancellationToken ct) =>
        Ok(await _templates.GetMyTemplatesAsync(User.GetUserId(), ct));

    [HttpPost]
    public async Task<ActionResult<CoverLetterTemplateDto>> Create(UpsertCoverLetterTemplateRequest request, CancellationToken ct) =>
        Ok(await _templates.CreateAsync(User.GetUserId(), request, ct));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CoverLetterTemplateDto>> Update(int id, UpsertCoverLetterTemplateRequest request, CancellationToken ct) =>
        Ok(await _templates.UpdateAsync(User.GetUserId(), id, request, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _templates.DeleteAsync(User.GetUserId(), id, ct);
        return NoContent();
    }
}
