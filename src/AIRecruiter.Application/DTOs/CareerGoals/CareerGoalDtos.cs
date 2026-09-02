namespace AIRecruiter.Application.DTOs.CareerGoals;

public record CareerGoalDto(
    int Id,
    string? TargetRole,
    string? TargetSkill,
    string? TargetCompanyType,
    string? PreferredState,
    string? PreferredCity,
    bool IsLocationRemote,
    DateTime? TargetCompletionDate,
    int ProgressPercent,
    string Status,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<CareerGoalSuggestionDto> Suggestions);

public record UpsertCareerGoalRequest(
    string? TargetRole,
    string? TargetSkill,
    string? TargetCompanyType,
    string? PreferredState,
    string? PreferredCity,
    bool IsLocationRemote,
    DateTime? TargetCompletionDate,
    int ProgressPercent,
    string Status,
    string? Notes);

public record CareerGoalSuggestionDto(string Label, string Tip, string LinkPath);

public record CareerGoalsSummaryDto(IReadOnlyList<CareerGoalDto> Goals);
