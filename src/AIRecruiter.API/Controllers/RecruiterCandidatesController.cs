using System.Text;
using AIRecruiter.API.Extensions;
using AIRecruiter.Application.DTOs.Candidates;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

[ApiController]
[Route("api/recruiters/candidates")]
[Authorize(Roles = "Recruiter")]
public class RecruiterCandidatesController : ControllerBase
{
    private readonly ICandidateSearchService _candidateSearch;

    public RecruiterCandidatesController(ICandidateSearchService candidateSearch)
    {
        _candidateSearch = candidateSearch;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CandidateSearchResultDto>>> Search(
        [FromQuery] string? skills,
        [FromQuery] string? city,
        [FromQuery] string? state,
        [FromQuery] int? minExperienceYears,
        [FromQuery] int? maxExperienceYears,
        [FromQuery] string? education,
        [FromQuery] ApplicationStatus? status,
        [FromQuery] int? minMatchScore,
        [FromQuery] int? maxMatchScore,
        [FromQuery] CandidateSortOption sort = CandidateSortOption.NewestApplication,
        CancellationToken ct = default)
    {
        var query = new CandidateSearchQuery(skills, city, state, minExperienceYears, maxExperienceYears, education, status, minMatchScore, maxMatchScore, sort);
        var results = await _candidateSearch.SearchAsync(User.GetUserId(), query, ct);
        return Ok(results);
    }

    [HttpGet("discoverable")]
    public async Task<ActionResult<IReadOnlyList<DiscoverableCandidateDto>>> GetDiscoverable(
        [FromQuery] string? skills,
        [FromQuery] string? city,
        [FromQuery] string? state,
        [FromQuery] int? minExperienceYears,
        [FromQuery] AvailabilityStatus? availabilityStatus,
        CancellationToken ct = default)
    {
        var query = new DiscoverCandidatesQuery(skills, city, state, minExperienceYears, availabilityStatus);
        var results = await _candidateSearch.GetDiscoverableCandidatesAsync(query, ct);
        return Ok(results);
    }

    [HttpGet("{candidateProfileId:int}")]
    public async Task<ActionResult<CandidateSearchDetailDto>> GetDetail(int candidateProfileId, CancellationToken ct)
    {
        var detail = await _candidateSearch.GetDetailAsync(User.GetUserId(), candidateProfileId, ct);
        return Ok(detail);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string? skills,
        [FromQuery] string? city,
        [FromQuery] string? state,
        [FromQuery] int? minExperienceYears,
        [FromQuery] int? maxExperienceYears,
        [FromQuery] string? education,
        [FromQuery] ApplicationStatus? status,
        [FromQuery] int? minMatchScore,
        [FromQuery] int? maxMatchScore,
        [FromQuery] CandidateSortOption sort = CandidateSortOption.NewestApplication,
        CancellationToken ct = default)
    {
        var query = new CandidateSearchQuery(skills, city, state, minExperienceYears, maxExperienceYears, education, status, minMatchScore, maxMatchScore, sort);
        var csv = await _candidateSearch.ExportCsvAsync(User.GetUserId(), query, ct);
        var bytes = Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv", $"candidates-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
    }

    [HttpGet("applications/{applicationId:int}/resume")]
    public async Task<IActionResult> DownloadResume(int applicationId, CancellationToken ct)
    {
        var (content, fileName, contentType) = await _candidateSearch.DownloadApplicantResumeAsync(User.GetUserId(), applicationId, ct);
        return File(content, contentType, fileName);
    }
}
