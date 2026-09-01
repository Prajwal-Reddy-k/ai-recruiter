using AIRecruiter.Application.DTOs.Jobs;

namespace AIRecruiter.Application.Interfaces;

public interface IJobPostingService
{
    Task<IReadOnlyList<JobPostingDto>> GetOpenJobsAsync(string? search, CancellationToken ct = default);
    Task<JobPostingDto?> GetByIdAsync(int id, string? viewerKey, int? viewerUserId, bool isAdminViewer = false, CancellationToken ct = default);
    Task<JobPostingDto> CreateAsync(int recruiterUserId, CreateJobPostingRequest request, CancellationToken ct = default);
    Task<JobPostingDto> UpdateAsync(int recruiterUserId, int jobId, UpdateJobPostingRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<RecruiterJobSummaryDto>> GetMyJobsAsync(int recruiterUserId, CancellationToken ct = default);
    Task<JobPostingDto> UpdateStatusAsync(int recruiterUserId, int jobId, UpdateJobStatusRequest request, CancellationToken ct = default);
    Task<JobPostingDto> DuplicateAsync(int recruiterUserId, int jobId, CancellationToken ct = default);
    Task<IReadOnlyList<JobPostingDto>> GetByCompanyAsync(int companyId, CancellationToken ct = default);
    Task<JobPostingDto> ExtendDeadlineAsync(int recruiterUserId, int jobId, DateTime? applicationDeadlineUtc, CancellationToken ct = default);
}
