using AIRecruiter.Application.DTOs.Assessments;

namespace AIRecruiter.Application.Interfaces;

public interface ISkillAssessmentService
{
    Task<IReadOnlyList<AssessmentCategorySummaryDto>> GetCategoriesAsync(int userId, CancellationToken ct = default);
    Task<AssessmentAttemptInProgressDto> StartAttemptAsync(int userId, StartAssessmentAttemptRequest request, CancellationToken ct = default);
    Task<AssessmentAttemptInProgressDto> GetActiveAttemptAsync(int userId, int attemptId, CancellationToken ct = default);
    Task AnswerQuestionAsync(int userId, int attemptId, SubmitAssessmentAnswerRequest request, CancellationToken ct = default);
    Task<AssessmentAttemptResultDto> SubmitAttemptAsync(int userId, int attemptId, CancellationToken ct = default);
    Task<IReadOnlyList<AssessmentAttemptHistoryItemDto>> GetMyHistoryAsync(int userId, CancellationToken ct = default);
    Task<AssessmentAttemptResultDto> GetAttemptReviewAsync(int userId, int attemptId, CancellationToken ct = default);
    Task SetAttemptVisibilityAsync(int userId, int attemptId, SetAttemptVisibilityRequest request, CancellationToken ct = default);
}
