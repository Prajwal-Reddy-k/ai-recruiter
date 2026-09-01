using AIRecruiter.API.Extensions;
using AIRecruiter.Application.DTOs.Recruiters;
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
    private readonly IAuditLogService _auditLog;

    public RecruitersController(IRecruiterOnboardingService onboardingService, IAuditLogService auditLog)
    {
        _onboardingService = onboardingService;
        _auditLog = auditLog;
    }

    // FullName/PhoneNumber editing lives on AccountController ("api/account/details") now —
    // it applies to any role, not just Recruiter.

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
