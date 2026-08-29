using AIRecruiter.Application.DTOs.Interviews;

namespace AIRecruiter.Application.Interfaces;

public interface IInterviewService
{
    Task<InterviewDto> ProposeAsync(int recruiterUserId, int applicationId, ProposeInterviewRequest request, CancellationToken ct = default);
    Task<InterviewDto> RespondAsync(int candidateUserId, int interviewId, RespondInterviewRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<InterviewDto>> GetForApplicationAsync(int userId, string role, int applicationId, CancellationToken ct = default);
    Task<IReadOnlyList<UpcomingInterviewDto>> GetUpcomingAsync(int userId, string role, CancellationToken ct = default);
    Task<(string IcsContent, string FileName)> GetIcsAsync(int userId, string role, int interviewId, CancellationToken ct = default);
}
