using AIRecruiter.Application.DTOs.Locations;
using AIRecruiter.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

[ApiController]
[Route("api/locations")]
public class LocationsController : ControllerBase
{
    private readonly ILocationSearchService _locationSearchService;

    public LocationsController(ILocationSearchService locationSearchService)
    {
        _locationSearchService = locationSearchService;
    }

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<LocationSuggestionDto>>> Search([FromQuery] string q, CancellationToken ct)
    {
        var results = await _locationSearchService.SearchAsync(q ?? string.Empty, ct);
        return Ok(results);
    }

    [HttpGet("india")]
    public ActionResult<IndiaLocationCatalogDto> GetIndiaCatalog([FromServices] IIndianLocationCatalog catalog)
    {
        return Ok(catalog.GetCatalog());
    }
}
