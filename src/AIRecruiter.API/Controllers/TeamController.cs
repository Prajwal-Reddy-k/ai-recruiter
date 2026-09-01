using AIRecruiter.API.Extensions;
using AIRecruiter.Application.DTOs.Team;
using AIRecruiter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

[ApiController]
[Route("api/company/team")]
[Authorize(Roles = "Recruiter")]
public class TeamController : ControllerBase
{
    private readonly ITeamService _team;

    public TeamController(ITeamService team)
    {
        _team = team;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TeamMemberDto>>> GetTeam(CancellationToken ct)
    {
        return Ok(await _team.GetMyCompanyTeamAsync(User.GetUserId(), ct));
    }

    [HttpPost("members")]
    public async Task<ActionResult<TeamMemberDto>> AddMember(AddTeamMemberRequest request, CancellationToken ct)
    {
        return Ok(await _team.AddMemberAsync(User.GetUserId(), request, ct));
    }

    [HttpPut("members/{recruiterProfileId:int}/role")]
    public async Task<ActionResult<TeamMemberDto>> UpdateRole(int recruiterProfileId, UpdateTeamMemberRoleRequest request, CancellationToken ct)
    {
        return Ok(await _team.UpdateRoleAsync(User.GetUserId(), recruiterProfileId, request, ct));
    }

    [HttpDelete("members/{recruiterProfileId:int}")]
    public async Task<IActionResult> RemoveMember(int recruiterProfileId, CancellationToken ct)
    {
        await _team.RemoveMemberAsync(User.GetUserId(), recruiterProfileId, ct);
        return NoContent();
    }

    [HttpGet("jobs/{jobId:int}/assignments")]
    public async Task<ActionResult<IReadOnlyList<JobAssignmentDto>>> GetJobAssignments(int jobId, CancellationToken ct)
    {
        return Ok(await _team.GetJobAssignmentsAsync(User.GetUserId(), jobId, ct));
    }

    [HttpPost("jobs/{jobId:int}/assignments/{recruiterProfileId:int}")]
    public async Task<IActionResult> AssignToJob(int jobId, int recruiterProfileId, CancellationToken ct)
    {
        await _team.AssignToJobAsync(User.GetUserId(), jobId, recruiterProfileId, ct);
        return NoContent();
    }

    [HttpDelete("jobs/{jobId:int}/assignments/{recruiterProfileId:int}")]
    public async Task<IActionResult> UnassignFromJob(int jobId, int recruiterProfileId, CancellationToken ct)
    {
        await _team.UnassignFromJobAsync(User.GetUserId(), jobId, recruiterProfileId, ct);
        return NoContent();
    }

    [HttpGet("interviews/{interviewId:int}/assignments")]
    public async Task<ActionResult<IReadOnlyList<InterviewAssignmentDto>>> GetInterviewAssignments(int interviewId, CancellationToken ct)
    {
        return Ok(await _team.GetInterviewAssignmentsAsync(User.GetUserId(), interviewId, ct));
    }

    [HttpPost("interviews/{interviewId:int}/assignments/{recruiterProfileId:int}")]
    public async Task<IActionResult> AssignToInterview(int interviewId, int recruiterProfileId, CancellationToken ct)
    {
        await _team.AssignToInterviewAsync(User.GetUserId(), interviewId, recruiterProfileId, ct);
        return NoContent();
    }

    [HttpDelete("interviews/{interviewId:int}/assignments/{recruiterProfileId:int}")]
    public async Task<IActionResult> UnassignFromInterview(int interviewId, int recruiterProfileId, CancellationToken ct)
    {
        await _team.UnassignFromInterviewAsync(User.GetUserId(), interviewId, recruiterProfileId, ct);
        return NoContent();
    }
}
