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

    /// <summary>Up to <paramref name="limit"/> open jobs matching any of the candidate's
    /// active alerts — used for the "alert results" section on the candidate dashboard.</summary>
    Task<IReadOnlyList<JobPostingDto>> GetMatchingJobsAsync(int candidateUserId, int limit = 10, CancellationToken ct = default);
}
