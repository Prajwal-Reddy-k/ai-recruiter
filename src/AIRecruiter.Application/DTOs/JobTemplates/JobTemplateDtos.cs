using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Application.DTOs.JobTemplates;

public record JobTemplateDto(
    int Id,
    string Title,
    string? Department,
    string Description,
    string? Responsibilities,
    string? RequiredSkillsCsv,
    string? PreferredSkillsCsv,
    int? MinExperienceYears,
    int? MaxExperienceYears,
    string EmploymentType,
    bool SalaryVisible,
    decimal? MinSalary,
    decimal? MaxSalary,
    string? DefaultCity,
    string? DefaultState,
    string? DefaultLocality,
    bool DefaultIsRemote,
    string CreatedByName,
    bool CanManage,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record UpsertJobTemplateRequest(
    string Title,
    string? Department,
    string Description,
    string? Responsibilities,
    string? RequiredSkillsCsv,
    string? PreferredSkillsCsv,
    int? MinExperienceYears,
    int? MaxExperienceYears,
    JobType EmploymentType,
    bool SalaryVisible,
    decimal? MinSalary,
    decimal? MaxSalary,
    string? DefaultCity,
    string? DefaultState,
    string? DefaultLocality,
    bool DefaultIsRemote);
