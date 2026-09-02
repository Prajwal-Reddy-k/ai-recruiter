using AIRecruiter.API.Extensions;
using AIRecruiter.Application.DTOs.Referrals;
using AIRecruiter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

[ApiController]
[Route("api/referrals")]
public class ReferralsController : ControllerBase
{
    private readonly IReferralService _referrals;

    public ReferralsController(IReferralService referrals)
    {
        _referrals = referrals;
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<CreateReferralResponse>> Create(CreateReferralRequest request, CancellationToken ct) =>
        Ok(await _referrals.CreateAsync(User.GetUserId(), GetClientIp(), request, ct));

    [HttpGet("my")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<ReferralDto>>> GetMine(CancellationToken ct) =>
        Ok(await _referrals.GetMyReferralsAsync(User.GetUserId(), ct));

    [HttpGet("company")]
    [Authorize(Roles = "Recruiter")]
    public async Task<ActionResult<IReadOnlyList<ReferralDto>>> GetCompany(CancellationToken ct) =>
        Ok(await _referrals.GetCompanyReferralsAsync(User.GetUserId(), ct));

    [HttpGet("token/{token}")]
    [AllowAnonymous]
    public async Task<ActionResult<ReferralTokenPreviewDto>> ResolveToken(string token, CancellationToken ct) =>
        Ok(await _referrals.ResolveTokenAsync(token, ct));

    private string GetClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
