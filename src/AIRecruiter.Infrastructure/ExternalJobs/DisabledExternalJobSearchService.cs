using AIRecruiter.Application.DTOs.ExternalJobs;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;

namespace AIRecruiter.Infrastructure.ExternalJobs;

/// <summary>Used when Adzuna credentials are absent, so the rest of the app stays fully operational.</summary>
public class DisabledExternalJobSearchService : IExternalJobSearchService
{
    public bool IsAvailable => false;

    public Task<ExternalJobSearchResult> SearchAsync(ExternalJobSearchRequest request, CancellationToken ct = default)
    {
        throw new ExternalServiceUnavailableException("External job search is not configured on this server.");
    }
}
