using System.Net;
using System.Net.Http.Json;
using AIRecruiter.Application.DTOs.ExternalJobs;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Infrastructure.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIRecruiter.Infrastructure.ExternalJobs;

/// <summary>
/// Adzuna-backed external job search. Only registered when Adzuna credentials are
/// configured (see DependencyInjection) — otherwise <see cref="DisabledExternalJobSearchService"/>
/// is used instead so callers never need to null-check.
/// </summary>
public class AdzunaJobSearchService : IExternalJobSearchService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    private readonly HttpClient _httpClient;
    private readonly AdzunaOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AdzunaJobSearchService> _logger;

    public AdzunaJobSearchService(IHttpClientFactory httpClientFactory, IOptions<AdzunaOptions> options, IMemoryCache cache, ILogger<AdzunaJobSearchService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("Adzuna");
        _options = options.Value;
        _cache = cache;
        _logger = logger;
    }

    public bool IsAvailable => _options.IsConfigured;

    public async Task<ExternalJobSearchResult> SearchAsync(ExternalJobSearchRequest request, CancellationToken ct = default)
    {
        if (!IsAvailable)
        {
            throw new ExternalServiceUnavailableException("External job search is not configured.");
        }

        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);
        var cacheKey = $"adzuna:{request.Keywords?.Trim().ToLowerInvariant()}:{request.Location?.Trim().ToLowerInvariant()}:{page}:{pageSize}";

        if (_cache.TryGetValue(cacheKey, out ExternalJobSearchResult? cached) && cached is not null)
        {
            return cached;
        }

        var url = $"v1/api/jobs/{_options.Country}/search/{page}" +
                   $"?app_id={Uri.EscapeDataString(_options.AppId)}" +
                   $"&app_key={Uri.EscapeDataString(_options.AppKey)}" +
                   $"&results_per_page={pageSize}" +
                   $"&content-type=application/json";

        if (!string.IsNullOrWhiteSpace(request.Keywords))
        {
            url += $"&what={Uri.EscapeDataString(request.Keywords)}";
        }
        if (!string.IsNullOrWhiteSpace(request.Location))
        {
            url += $"&where={Uri.EscapeDataString(request.Location)}";
        }

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(url, ct);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("Adzuna request timed out.");
            throw new ExternalServiceUnavailableException("External job search timed out. Please try again shortly.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Adzuna request failed.");
            throw new ExternalServiceUnavailableException("External job search is temporarily unavailable.");
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            int? retryAfter = response.Headers.RetryAfter?.Delta.HasValue == true
                ? (int)response.Headers.RetryAfter!.Delta!.Value.TotalSeconds
                : null;
            throw new ExternalServiceUnavailableException("External job search is rate-limited. Please try again later.", retryAfter);
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Adzuna returned {StatusCode}", response.StatusCode);
            throw new ExternalServiceUnavailableException("External job search is temporarily unavailable.");
        }

        var payload = await response.Content.ReadFromJsonAsync<AdzunaSearchResponse>(cancellationToken: ct)
            ?? new AdzunaSearchResponse();

        var items = payload.Results.Select(r => new ExternalJobListingDto(
            r.Title ?? "Untitled role",
            r.Company?.DisplayName ?? "Unknown company",
            r.Location?.DisplayName,
            r.Description ?? string.Empty,
            r.RedirectUrl ?? string.Empty,
            FormatSalary(r.SalaryMin, r.SalaryMax),
            r.Created,
            "Adzuna")).ToList();

        var result = new ExternalJobSearchResult(items, page, pageSize, payload.Count);

        _cache.Set(cacheKey, result, CacheDuration);

        return result;
    }

    private static string? FormatSalary(decimal? min, decimal? max)
    {
        if (min is null && max is null) return null;
        if (min is not null && max is not null) return $"{min:N0} - {max:N0}";
        return (min ?? max)?.ToString("N0");
    }
}
