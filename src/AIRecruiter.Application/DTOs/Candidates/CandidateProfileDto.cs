namespace AIRecruiter.Application.DTOs.Candidates;

public record CandidateProfileDto(
    int Id,
    string FullName,
    string? Headline,
    string? Summary,
    string? Education,
    string? ExperienceSummary,
    int? TotalExperienceYears,
    string? City,
    string? State,
    string? Locality,
    string DisplayLocation,
    decimal? CurrentSalary,
    decimal? ExpectedSalary,
    string? SkillsCsv,
    string? ResumeOriginalFileName,
    long? ResumeSizeBytes,
    DateTime? ResumeUploadedAt);

public record UpsertCandidateProfileRequest(
    string? Headline,
    string? Summary,
    string? Education,
    string? ExperienceSummary,
    int? TotalExperienceYears,
    string? City,
    string? State,
    string? Locality,
    decimal? CurrentSalary,
    decimal? ExpectedSalary,
    string? SkillsCsv);
