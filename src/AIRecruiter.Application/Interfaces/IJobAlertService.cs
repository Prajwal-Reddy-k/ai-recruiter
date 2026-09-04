using AIRecruiter.Application.DTOs.Jobs;
using AIRecruiter.Application.DTOs.SavedJobs;

namespace AIRecruiter.Application.Interfaces;

public interface IJobAlertService
{
    Task<JobAlertDto> CreateAsync(int candidateUserId, UpsertJobAlertRequest request, CancellationToken ct = default);
    Task<JobAlertDto> UpdateAsync(int candidateUserId, int alertId, UpsertJobAlertRequest request, CancellationToken ct = default);
    Task<JobAlertDto> SetActiveAsync(int candidateUserId, int alertId, bool isActive, CancellationToken ct = default);
    Task<IReadOnlyList<JobAlertDto>> GetMyAlertsAsync(int candidateUserId, CancellationToken ct = default);
    Task DeleteAsync(int candidateUserId, int alertId, CancellationToken ct = default);

    /// <summary>Copies a saved search verbatim (name suffixed "(copy)"), never carrying over
    /// IsDefault — a duplicate is always non-default.</summary>
    Task<JobAlertDto> DuplicateAsync(int candidateUserId, int alertId, CancellationToken ct = default);

    /// <summary>Marks this saved search as the candidate's default dashboard search, clearing
    /// the flag on any other saved search they own.</summary>
    Task<JobAlertDto> SetDefaultAsync(int candidateUserId, int alertId, CancellationToken ct = default);

    /// <summary>Up to <paramref name="limit"/> open jobs matching any of the candidate's
    /// active alerts — used for the "alert results" section on the candidate dashboard.</summary>
    Task<IReadOnlyList<JobPostingDto>> GetMatchingJobsAsync(int candidateUserId, int limit = 10, CancellationToken ct = default);
}
