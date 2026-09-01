namespace AIRecruiter.Application.DTOs.Candidates;

public record MissingProfileItemDto(string Label, string Tip, string LinkPath);

public record ProfileStrengthResult(int Score, IReadOnlyList<MissingProfileItemDto> MissingItems);
