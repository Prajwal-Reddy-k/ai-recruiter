using System.Globalization;
using System.Net.Http.Json;
using AIRecruiter.Application.DTOs.Locations;
using AIRecruiter.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AIRecruiter.Infrastructure.Locations;

public class NominatimLocationSearchService : ILocationSearchService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly NominatimRateGate _rateGate;
    private readonly ILogger<NominatimLocationSearchService> _logger;

    public NominatimLocationSearchService(IHttpClientFactory httpClientFactory, IMemoryCache cache, NominatimRateGate rateGate, ILogger<NominatimLocationSearchService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("Nominatim");
        _cache = cache;
        _rateGate = rateGate;
        _logger = logger;
    }

    public async Task<IReadOnlyList<LocationSuggestionDto>> SearchAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 3)
        {
            return Array.Empty<LocationSuggestionDto>();
        }

        var normalizedQuery = query.Trim().ToLowerInvariant();
        var cacheKey = $"nominatim:{normalizedQuery}";

        if (_cache.TryGetValue(cacheKey, out List<LocationSuggestionDto>? cached) && cached is not null)
        {
            return cached;
        }

        try
        {
            await _rateGate.WaitAsync(ct);

            var url = $"search?format=jsonv2&limit=5&q={Uri.EscapeDataString(query.Trim())}";
            var results = await _httpClient.GetFromJsonAsync<List<NominatimResult>>(url, ct) ?? new List<NominatimResult>();

            var suggestions = results
                .Where(r => r.DisplayName is not null && r.Lat is not null && r.Lon is not null)
                .Select(r => new LocationSuggestionDto(
                    r.DisplayName!,
                    double.Parse(r.Lat!, CultureInfo.InvariantCulture),
                    double.Parse(r.Lon!, CultureInfo.InvariantCulture)))
                .ToList();

            _cache.Set(cacheKey, suggestions, CacheDuration);
            return suggestions;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            // Nominatim being unreachable/slow/erroring must never break the UI — the
            // caller falls back to plain-text location entry.
            _logger.LogWarning(ex, "Nominatim location search failed for query {Query}", query);
            return Array.Empty<LocationSuggestionDto>();
        }
    }
}
