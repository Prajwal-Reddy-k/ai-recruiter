using AIRecruiter.Application.DTOs.Locations;

namespace AIRecruiter.Application.Interfaces;

/// <summary>
/// Optional location autocomplete backed by OpenStreetMap Nominatim. Must never throw for
/// network/availability failures — callers get an empty list and the UI falls back to
/// plain-text location entry.
/// </summary>
public interface ILocationSearchService
{
    Task<IReadOnlyList<LocationSuggestionDto>> SearchAsync(string query, CancellationToken ct = default);
}
