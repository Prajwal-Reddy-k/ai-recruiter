using AIRecruiter.API.Extensions;
using AIRecruiter.Application.DTOs.Companies;
using AIRecruiter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

[ApiController]
[Route("api/companies/verification")]
[Authorize(Roles = "Recruiter")]
public class CompanyVerificationController : ControllerBase
{
    private readonly ICompanyVerificationService _verification;

    public CompanyVerificationController(ICompanyVerificationService verification)
    {
        _verification = verification;
    }

    [HttpPost]
    public async Task<ActionResult<CompanyVerificationStatusDto>> Submit(SubmitCompanyVerificationRequest request, CancellationToken ct) =>
        Ok(await _verification.SubmitAsync(User.GetUserId(), request, ct));

    [HttpGet]
    public async Task<ActionResult<CompanyVerificationStatusDto>> GetMyStatus(CancellationToken ct) =>
        Ok(await _verification.GetMyStatusAsync(User.GetUserId(), ct));
}
