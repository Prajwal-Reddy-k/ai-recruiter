namespace AIRecruiter.Application.DTOs.Jobs;

/// <summary>Id is null for a brand-new question; present for an existing one being edited
/// in place. Options is only meaningful for SingleChoice/MultipleChoice. PreferredAnswer is
/// recruiter-only reference — never shown to candidates.</summary>
public record UpsertScreeningQuestionRequest(
    int? Id,
    string QuestionText,
    string QuestionType,
    bool IsRequired,
    string? HelpText,
    IReadOnlyList<string>? Options,
    int DisplayOrder,
    string? PreferredAnswer);

public record ScreeningQuestionOptionDto(int Id, string OptionText, int DisplayOrder);

/// <summary>PreferredAnswer defaults to null and is only populated by JobPostingMapper.ToDto
/// when the caller has confirmed the viewer is the owning recruiter/company team — see
/// JobPostingService's includePreferredAnswers flag.</summary>
public record ScreeningQuestionDto(
    int Id,
    string QuestionText,
    string QuestionType,
    bool IsRequired,
    string? HelpText,
    IReadOnlyList<ScreeningQuestionOptionDto> Options,
    int DisplayOrder,
    string? PreferredAnswer = null);
