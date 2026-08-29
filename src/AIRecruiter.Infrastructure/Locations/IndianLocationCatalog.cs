using System.Text.Json;
using System.Text.Json.Serialization;
using AIRecruiter.Application.DTOs.Locations;
using AIRecruiter.Application.Interfaces;

namespace AIRecruiter.Infrastructure.Locations;

/// <summary>
/// Loads the checked-in indian-locations.json (embedded resource) once and serves it from
/// memory. Singleton — the dataset never changes at runtime.
/// </summary>
public class IndianLocationCatalog : IIndianLocationCatalog
{
    private class RawState
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("cities")] public List<string> Cities { get; set; } = new();
    }

    private class RawCatalog
    {
        [JsonPropertyName("states")] public List<RawState> States { get; set; } = new();
    }

    private readonly IndiaLocationCatalogDto _catalog;
    private readonly Dictionary<string, HashSet<string>> _citiesByStateLower;

    public IndianLocationCatalog()
    {
        using var stream = typeof(IndianLocationCatalog).Assembly
            .GetManifestResourceStream("AIRecruiter.Infrastructure.Data.indian-locations.json")
            ?? throw new InvalidOperationException("Embedded India locations dataset not found.");

        var raw = JsonSerializer.Deserialize<RawCatalog>(stream) ?? new RawCatalog();

        _catalog = new IndiaLocationCatalogDto(
            raw.States.Select(s => new IndianStateDto(s.Name, s.Cities)).ToList());

        _citiesByStateLower = raw.States.ToDictionary(
            s => s.Name.ToLowerInvariant(),
            s => new HashSet<string>(s.Cities.Select(c => c.ToLowerInvariant())));
    }

    public IndiaLocationCatalogDto GetCatalog() => _catalog;

    public bool IsValidState(string state) =>
        !string.IsNullOrWhiteSpace(state) && _citiesByStateLower.ContainsKey(state.Trim().ToLowerInvariant());

    public bool IsValidCity(string state, string city)
    {
        if (string.IsNullOrWhiteSpace(state) || string.IsNullOrWhiteSpace(city)) return false;
        return _citiesByStateLower.TryGetValue(state.Trim().ToLowerInvariant(), out var cities)
            && cities.Contains(city.Trim().ToLowerInvariant());
    }
}
