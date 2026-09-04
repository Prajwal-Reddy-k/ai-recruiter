namespace AIRecruiter.Application.DTOs.SalaryInsights;

/// <summary>One aggregated bucket — grouped by role title + experience band + location.
/// When SampleCount is below the privacy threshold, Min/Median/Max are null and the caller
/// must show "Not enough data yet" rather than any number.</summary>
public record SalaryInsightDto(
    string RoleTitle,
    string ExperienceBand,
    string? City,
    string? State,
    bool IsRemote,
    decimal? Min,
    decimal? Median,
    decimal? Max,
    int SampleCount,
    bool HasEnoughData);

public record SalaryInsightsQuery(string? Role, string? City, string? State, bool? IsRemote, string? ExperienceBand);
