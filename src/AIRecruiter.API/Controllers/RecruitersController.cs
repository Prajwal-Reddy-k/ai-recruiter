using AIRecruiter.API.Extensions;
using AIRecruiter.Application.DTOs.Recruiters;
using AIRecruiter.Application.DTOs.Users;
using AIRecruiter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

[ApiController]
[Route("api/recruiters")]
[Authorize(Roles = "Recruiter")]
public class RecruitersController : ControllerBase
{
    private readonly IRecruiterOnboardingService _onboardingService;
    private readonly IUserProfileService _userProfile;
    private readonly IAuditLogService _auditLog;

    public RecruitersController(IRecruiterOnboardingService onboardingService, IUserProfileService userProfile, IAuditLogService auditLog)
    {
        _onboardingService = onboardingService;
        _userProfile = userProfile;
        _auditLog = auditLog;
    }

    [HttpGet("me/details")]
    public async Task<ActionResult<UserDetailsDto>> GetMyDetails(CancellationToken ct) =>
        Ok(await _userProfile.GetMyDetailsAsync(User.GetUserId(), ct));

    [HttpPut("me/details")]
    public async Task<ActionResult<UserDetailsDto>> UpdateMyDetails(UpdateUserDetailsRequest request, CancellationToken ct) =>
        Ok(await _userProfile.UpdateMyDetailsAsync(User.GetUserId(), request, ct));

    [HttpGet("me/onboarding-status")]
    public async Task<ActionResult<OnboardingStatusDto>> GetStatus(CancellationToken ct)
    {
        var status = await _onboardingService.GetStatusAsync(User.GetUserId(), ct);
        return Ok(status);
    }

    [HttpPost("me/onboarding")]
    public async Task<ActionResult<OnboardingStatusDto>> Upsert(UpsertRecruiterOnboardingRequest request, CancellationToken ct)
    {
        var status = await _onboardingService.UpsertAsync(User.GetUserId(), request, ct);
        return Ok(status);
    }

    [HttpGet("me/activity")]
    public async Task<IActionResult> GetMyCompanyActivity(CancellationToken ct)
    {
        var status = await _onboardingService.GetStatusAsync(User.GetUserId(), ct);
        if (!status.IsOnboarded || status.CompanyId is null)
        {
            return Ok(Array.Empty<object>());
        }

        return Ok(await _auditLog.GetForCompanyAsync(status.CompanyId.Value, ct));
    }
}
