namespace AIRecruiter.Application.DTOs.TalentPools;

public record TalentPoolDto(int Id, string Name, int CandidateCount, DateTime CreatedAt);

public record CreateTalentPoolRequest(string Name);

public record RenameTalentPoolRequest(string Name);

public record TalentPoolCandidateDto(
    int CandidateProfileId,
    string FullName,
    string? Headline,
    string? SkillsCsv,
    int? TotalExperienceYears,
    string DisplayLocation,
    string? AvatarUrl,
    string? LatestApplicationJobTitle,
    string? LatestApplicationStatus,
    int? MatchScore,
    string? Notes,
    string? TagsCsv,
    DateTime AddedAt);

public record AddCandidateToPoolRequest(int CandidateProfileId, string? Notes, string? TagsCsv);

public record UpdatePoolCandidateNotesRequest(string? Notes, string? TagsCsv);
