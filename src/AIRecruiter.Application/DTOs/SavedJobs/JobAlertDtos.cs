using AIRecruiter.Application.DTOs.Jobs;
using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Application.DTOs.SavedJobs;

public record UpsertJobAlertRequest(
    string? SkillsCsv,
    string? State,
    string? City,
    bool? IsRemote,
    JobType? JobType,
    int? MinExperienceYears,
    bool IsActive = true);

public record JobAlertDto(
    int Id,
    string? SkillsCsv,
    string? State,
    string? City,
    bool? IsRemote,
    string? JobType,
    int? MinExperienceYears,
    bool IsActive,
    int MatchingJobCount,
    IReadOnlyList<JobPostingDto> MatchingJobs,
    DateTime CreatedAt);
