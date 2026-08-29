using AIRecruiter.Application.DTOs.ExternalJobs;

namespace AIRecruiter.Application.Interfaces;

/// <summary>
/// Optional, credential-gated external job search (e.g. Adzuna). Implementations must
/// report unavailability gracefully rather than throwing when not configured.
/// </summary>
public interface IExternalJobSearchService
{
    bool IsAvailable { get; }
    Task<ExternalJobSearchResult> SearchAsync(ExternalJobSearchRequest request, CancellationToken ct = default);
}
