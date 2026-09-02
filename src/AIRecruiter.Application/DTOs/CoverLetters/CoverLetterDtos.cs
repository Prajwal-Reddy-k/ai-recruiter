namespace AIRecruiter.Application.DTOs.CoverLetters;

public record CoverLetterTemplateDto(
    int Id,
    string Title,
    string? Introduction,
    string? SkillsHighlights,
    string? ProjectAchievements,
    string? ClosingMessage,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record UpsertCoverLetterTemplateRequest(
    string Title,
    string? Introduction,
    string? SkillsHighlights,
    string? ProjectAchievements,
    string? ClosingMessage);
