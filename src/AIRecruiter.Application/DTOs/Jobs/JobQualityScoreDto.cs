namespace AIRecruiter.Application.DTOs.Jobs;

public record JobQualitySuggestionDto(string Label, string Tip);

public record JobQualityScoreDto(int Score, IReadOnlyList<JobQualitySuggestionDto> Suggestions);
