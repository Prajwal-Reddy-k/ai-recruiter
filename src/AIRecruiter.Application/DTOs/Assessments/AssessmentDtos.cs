namespace AIRecruiter.Application.DTOs.Assessments;

public record AssessmentCategorySummaryDto(
    string Category,
    int QuestionBankSize,
    int? BestPercentageScore,
    DateTime? LastAttemptAt,
    bool CanAttemptNow,
    DateTime? CooldownEndsAtUtc,
    bool HasActiveAttempt);

public record StartAssessmentAttemptRequest(string Category);

/// <summary>Never carries CorrectOptionIndex — that's only revealed after submit, via the
/// review DTO below.</summary>
public record AssessmentQuestionForAttemptDto(
    int AnswerId,
    int DisplayOrder,
    string QuestionText,
    string OptionA,
    string OptionB,
    string OptionC,
    string OptionD);

public record AssessmentAttemptInProgressDto(
    int AttemptId,
    string Category,
    DateTime StartedAtUtc,
    DateTime ExpiresAtUtc,
    IReadOnlyList<AssessmentQuestionForAttemptDto> Questions,
    IReadOnlyDictionary<int, int> SelectedOptionsByAnswerId);

public record SubmitAssessmentAnswerRequest(int AnswerId, int SelectedOptionIndex);

public record AssessmentReviewQuestionDto(
    string QuestionText,
    string OptionA,
    string OptionB,
    string OptionC,
    string OptionD,
    int CorrectOptionIndex,
    int? SelectedOptionIndex,
    string? Explanation);

public record AssessmentAttemptResultDto(
    int AttemptId,
    string Category,
    int ScoreCorrectCount,
    int TotalQuestionCount,
    decimal PercentageScore,
    DateTime SubmittedAtUtc,
    bool IsVisibleToRecruiters,
    IReadOnlyList<AssessmentReviewQuestionDto> Review);

public record AssessmentAttemptHistoryItemDto(
    int AttemptId,
    string Category,
    int ScoreCorrectCount,
    int TotalQuestionCount,
    decimal PercentageScore,
    DateTime SubmittedAtUtc,
    bool IsVisibleToRecruiters);

public record SetAttemptVisibilityRequest(bool IsVisibleToRecruiters);

public record RecruiterVisibleBadgeDto(string Category, decimal PercentageScore, DateTime SubmittedAtUtc);
