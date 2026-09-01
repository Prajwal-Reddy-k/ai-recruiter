using AIRecruiter.Domain.Common;
using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Domain.Entities;

/// <summary>A reusable job-posting starting point, private to the company that created it.
/// "Create a Draft job from a template" copies these fields into a new JobPosting, mirroring
/// how JobPostingService.DuplicateAsync already clones one job into another.</summary>
public class JobTemplate : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public int RecruiterProfileId { get; set; }
    public RecruiterProfile RecruiterProfile { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Responsibilities { get; set; }
    public string? RequiredSkillsCsv { get; set; }
    public string? PreferredSkillsCsv { get; set; }
    public int? MinExperienceYears { get; set; }
    public int? MaxExperienceYears { get; set; }
    public JobType EmploymentType { get; set; } = JobType.FullTime;
    public bool SalaryVisible { get; set; }
    public decimal? MinSalary { get; set; }
    public decimal? MaxSalary { get; set; }

    public string? DefaultCity { get; set; }
    public string? DefaultState { get; set; }
    public string? DefaultLocality { get; set; }
    public bool DefaultIsRemote { get; set; }
}
