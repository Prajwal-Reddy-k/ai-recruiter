using AIRecruiter.Application.DTOs.Companies;
using AIRecruiter.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

[ApiController]
[Route("api/companies")]
public class CompaniesController : ControllerBase
{
    private readonly ICompanyService _companyService;

    public CompaniesController(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CompanyProfileDto>> GetPublicProfile(int id, CancellationToken ct)
    {
        var profile = await _companyService.GetPublicProfileAsync(id, ct);
        return Ok(profile);
    }
}
