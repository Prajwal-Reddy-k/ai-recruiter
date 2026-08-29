namespace AIRecruiter.Application.DTOs.Matching;

public record JobMatchInput(
    string Title,
    string Description,
    string? RequiredSkillsCsv,
    int? MinExperienceYears);

public record CandidateMatchInput(
    int? TotalExperienceYears,
    string? Education);

public record ResumeMatchResult(
    int OverallScore,
    IReadOnlyList<string> MatchedSkills,
    IReadOnlyList<string> MissingSkills,
    IReadOnlyList<string> SuggestedImprovements,
    string Explanation);
