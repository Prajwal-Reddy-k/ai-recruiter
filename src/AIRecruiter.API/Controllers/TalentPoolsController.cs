using AIRecruiter.API.Extensions;
using AIRecruiter.Application.DTOs.TalentPools;
using AIRecruiter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

[ApiController]
[Route("api/talent-pools")]
[Authorize(Roles = "Recruiter")]
public class TalentPoolsController : ControllerBase
{
    private readonly ITalentPoolService _pools;

    public TalentPoolsController(ITalentPoolService pools)
    {
        _pools = pools;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TalentPoolDto>>> GetPools(CancellationToken ct) =>
        Ok(await _pools.GetPoolsAsync(User.GetUserId(), ct));

    [HttpPost]
    public async Task<ActionResult<TalentPoolDto>> Create(CreateTalentPoolRequest request, CancellationToken ct) =>
        Ok(await _pools.CreatePoolAsync(User.GetUserId(), request, ct));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TalentPoolDto>> Rename(int id, RenameTalentPoolRequest request, CancellationToken ct) =>
        Ok(await _pools.RenamePoolAsync(User.GetUserId(), id, request, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _pools.DeletePoolAsync(User.GetUserId(), id, ct);
        return NoContent();
    }

    [HttpGet("{id:int}/candidates")]
    public async Task<ActionResult<IReadOnlyList<TalentPoolCandidateDto>>> GetCandidates(int id, CancellationToken ct) =>
        Ok(await _pools.GetPoolCandidatesAsync(User.GetUserId(), id, ct));

    [HttpPost("{id:int}/candidates")]
    public async Task<IActionResult> AddCandidate(int id, AddCandidateToPoolRequest request, CancellationToken ct)
    {
        await _pools.AddCandidateAsync(User.GetUserId(), id, request, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}/candidates/{candidateProfileId:int}")]
    public async Task<IActionResult> RemoveCandidate(int id, int candidateProfileId, CancellationToken ct)
    {
        await _pools.RemoveCandidateAsync(User.GetUserId(), id, candidateProfileId, ct);
        return NoContent();
    }

    [HttpPatch("{id:int}/candidates/{candidateProfileId:int}")]
    public async Task<IActionResult> UpdateCandidateNotes(int id, int candidateProfileId, UpdatePoolCandidateNotesRequest request, CancellationToken ct)
    {
        await _pools.UpdateCandidateNotesAsync(User.GetUserId(), id, candidateProfileId, request, ct);
        return NoContent();
    }
}
