using AIRecruiter.Application.DTOs.Jobs;

namespace AIRecruiter.Application.Interfaces;

public interface IJobViewService
{
    /// <summary>Upserts a per-candidate view record — no-ops silently if the caller has no
    /// candidate profile yet, rather than blocking job viewing.</summary>
    Task RecordViewAsync(int candidateUserId, int jobPostingId, CancellationToken ct = default);

    /// <summary>Most-recent 10 distinct jobs the candidate has viewed, newest first.</summary>
    Task<IReadOnlyList<JobPostingDto>> GetRecentlyViewedAsync(int candidateUserId, CancellationToken ct = default);
}
