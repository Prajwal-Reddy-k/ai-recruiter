using AIRecruiter.Application.DTOs.ExternalJobs;
using AIRecruiter.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace AIRecruiter.Infrastructure.ExternalJobs;

/// <summary>Default, single-instance-portfolio implementation — wraps the exact IMemoryCache
/// calls AdzunaJobSearchService used to make inline, unchanged.</summary>
public class MemoryExternalJobSearchCache : IExternalJobSearchCache
{
    private readonly IMemoryCache _cache;

    public MemoryExternalJobSearchCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    public bool TryGet(string key, out ExternalJobSearchResult? result) => _cache.TryGetValue(key, out result);

    public void Set(string key, ExternalJobSearchResult result, TimeSpan ttl) => _cache.Set(key, result, ttl);
}
