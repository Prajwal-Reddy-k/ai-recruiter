using AIRecruiter.Application.DTOs.Applications;
using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Application.Interfaces;

public interface IJobApplicationService
{
    Task<JobApplicationDto> ApplyAsync(int candidateUserId, int jobPostingId, string? coverNote, CancellationToken ct = default);
    Task<IReadOnlyList<JobApplicationDto>> GetMyApplicationsAsync(int candidateUserId, CancellationToken ct = default);
    Task<JobApplicationDetailDto> GetApplicationDetailAsync(int userId, string role, int applicationId, CancellationToken ct = default);
    Task<IReadOnlyList<JobApplicationDto>> GetApplicationsForJobAsync(int recruiterUserId, int jobPostingId, CancellationToken ct = default);
    Task<(Stream Content, string FileName, string ContentType)> DownloadApplicantResumeAsync(int userId, string role, int applicationId, CancellationToken ct = default);
    Task<JobApplicationDto> UpdateStatusAsync(int recruiterUserId, int applicationId, ApplicationStatus newStatus, string? note = null, CancellationToken ct = default);
    Task<JobApplicationDto> WithdrawAsync(int candidateUserId, int applicationId, CancellationToken ct = default);
}
