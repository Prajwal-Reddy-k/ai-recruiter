using AIRecruiter.Application.DTOs.Interviews;

namespace AIRecruiter.Application.Interfaces;

public interface IInterviewService
{
    /// <summary>Recruiter schedules a single, concrete interview for an application on one
    /// of their own jobs.</summary>
    Task<InterviewDto> ScheduleAsync(int recruiterUserId, int applicationId, ScheduleInterviewRequest request, CancellationToken ct = default);

    Task<InterviewDto> RescheduleAsync(int recruiterUserId, int interviewId, RescheduleInterviewRequest request, CancellationToken ct = default);
    Task<InterviewDto> CancelAsync(int recruiterUserId, int interviewId, CancellationToken ct = default);
    Task<InterviewDto> CompleteAsync(int recruiterUserId, int interviewId, CancellationToken ct = default);

    Task<InterviewDto> AcceptAsync(int candidateUserId, int interviewId, RespondInterviewRequest request, CancellationToken ct = default);
    Task<InterviewDto> DeclineAsync(int candidateUserId, int interviewId, RespondInterviewRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<InterviewDto>> GetForApplicationAsync(int userId, string role, int applicationId, CancellationToken ct = default);

    /// <summary>Every interview visible to the caller — a candidate's own, or every
    /// interview across the recruiter's whole company (read-only; mutation actions above
    /// still require the caller to be the exact owning recruiter, reflected via
    /// InterviewDto.CanManage). Optional <paramref name="statusFilter"/> narrows by
    /// InterviewStatus name.</summary>
    Task<IReadOnlyList<InterviewDto>> GetMyInterviewsAsync(int userId, string role, string? statusFilter, CancellationToken ct = default);

    Task<IReadOnlyList<UpcomingInterviewDto>> GetUpcomingAsync(int userId, string role, CancellationToken ct = default);
    Task<(string IcsContent, string FileName)> GetIcsAsync(int userId, string role, int interviewId, CancellationToken ct = default);
}
