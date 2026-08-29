using AIRecruiter.Application.DTOs.Jobs;
using AIRecruiter.Application.DTOs.SavedJobs;

namespace AIRecruiter.Application.Interfaces;

public interface ISavedJobService
{
    Task SaveAsync(int candidateUserId, int jobId, CancellationToken ct = default);
    Task UnsaveAsync(int candidateUserId, int jobId, CancellationToken ct = default);
    Task<IReadOnlyList<JobPostingDto>> GetMySavedJobsAsync(int candidateUserId, CancellationToken ct = default);
    Task<IReadOnlyList<SavedJobDto>> GetMySavedJobsWithDatesAsync(int candidateUserId, CancellationToken ct = default);
    Task<IReadOnlyList<int>> GetMySavedJobIdsAsync(int candidateUserId, CancellationToken ct = default);
}
