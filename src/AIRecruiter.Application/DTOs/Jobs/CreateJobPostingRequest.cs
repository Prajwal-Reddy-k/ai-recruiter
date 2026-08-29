using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Application.DTOs.Jobs;

public record CreateJobPostingRequest(
    string Title,
    string Description,
    string? RequiredSkillsCsv,
    int? MinExperienceYears,
    int? MaxExperienceYears,
    decimal? MinSalary,
    decimal? MaxSalary,
    string? City,
    string? State,
    string? Locality,
    bool IsRemote,
    JobType JobType,
    bool SaveAsDraft = false);

public record UpdateJobPostingRequest(
    string Title,
    string Description,
    string? RequiredSkillsCsv,
    int? MinExperienceYears,
    int? MaxExperienceYears,
    decimal? MinSalary,
    decimal? MaxSalary,
    string? City,
    string? State,
    string? Locality,
    bool IsRemote,
    JobType JobType);

public record RecruiterJobSummaryDto(JobPostingDto Job, int ApplicationCount);
