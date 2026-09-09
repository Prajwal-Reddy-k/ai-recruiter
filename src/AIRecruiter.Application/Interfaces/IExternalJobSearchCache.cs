using AIRecruiter.Application.DTOs.ExternalJobs;

namespace AIRecruiter.Application.Interfaces;

/// <summary>Extracted from AdzunaJobSearchService's previously-inlined IMemoryCache use so a
/// distributed (Redis) implementation can slot in behind the same interface without touching
/// any of that service's search/parsing logic.</summary>
public interface IExternalJobSearchCache
{
    bool TryGet(string key, out ExternalJobSearchResult? result);

    void Set(string key, ExternalJobSearchResult result, TimeSpan ttl);
}
