using AIRecruiter.Application.DTOs.ExternalJobs;
using AIRecruiter.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

public record AvailabilityDto(bool Available);

[ApiController]
[Route("api/external-jobs")]
[Authorize]
public class ExternalJobsController : ControllerBase
{
    private readonly IExternalJobSearchService _externalJobSearchService;

    public ExternalJobsController(IExternalJobSearchService externalJobSearchService)
    {
        _externalJobSearchService = externalJobSearchService;
    }

    [HttpGet("availability")]
    public ActionResult<AvailabilityDto> GetAvailability()
    {
        return Ok(new AvailabilityDto(_externalJobSearchService.IsAvailable));
    }

    [HttpGet("search")]
    public async Task<ActionResult<ExternalJobSearchResult>> Search(
        [FromQuery] string? keywords, [FromQuery] string? location, [FromQuery] int page, [FromQuery] int pageSize, CancellationToken ct)
    {
        var request = new ExternalJobSearchRequest(keywords, location, page <= 0 ? 1 : page, pageSize <= 0 ? 20 : pageSize);
        var result = await _externalJobSearchService.SearchAsync(request, ct);
        return Ok(result);
    }
}
