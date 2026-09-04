namespace AIRecruiter.Application.DTOs.Applications;

public record SubmitScreeningAnswerRequest(
    int QuestionId,
    string? TextValue,
    decimal? NumberValue,
    IReadOnlyList<int>? SelectedOptionIds);

/// <summary>PreferredAnswer is only populated when the viewer is the owning recruiter —
/// candidates viewing their own submitted answers never see it (JobApplicationService's
/// includePreferredAnswers flag, driven by the caller's role).</summary>
public record ScreeningAnswerDto(
    int QuestionId,
    string QuestionText,
    string QuestionType,
    bool IsRequired,
    string? TextValue,
    decimal? NumberValue,
    IReadOnlyList<string> SelectedOptionTexts,
    string? PreferredAnswer = null);

/// <summary>Single-filter-at-a-time applicant screening filter — matches the recruiter UI's
/// one question + one filter mode at a time. RequiredAnsweredOnly true = only applicants who
/// answered every required question; false = only those missing at least one.</summary>
public record ApplicantScreeningFilterQuery(
    int? QuestionId,
    string? YesNo,
    int? OptionId,
    decimal? MinNumber,
    decimal? MaxNumber,
    bool? RequiredAnsweredOnly);
