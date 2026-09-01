using AIRecruiter.Application.DTOs.InterviewFeedback;

namespace AIRecruiter.Application.Interfaces;

public interface IInterviewFeedbackService
{
    Task<InterviewFeedbackDto> SaveDraftAsync(int userId, int interviewId, UpsertInterviewFeedbackRequest request, CancellationToken ct = default);
    Task<InterviewFeedbackDto> SubmitAsync(int userId, int interviewId, UpsertInterviewFeedbackRequest request, CancellationToken ct = default);
    Task<InterviewFeedbackDto?> GetMyFeedbackAsync(int userId, int interviewId, CancellationToken ct = default);
    Task<InterviewFeedbackSummaryDto> GetSummaryAsync(int userId, int interviewId, CancellationToken ct = default);
}
