using AIRecruiter.Domain.Common;
using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Domain.Entities;

public class JobPosting : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? RequiredSkillsCsv { get; set; }
    public int? MinExperienceYears { get; set; }
    public int? MaxExperienceYears { get; set; }
    public decimal? MinSalary { get; set; }
    public decimal? MaxSalary { get; set; }

    public string? City { get; set; }
    public string? State { get; set; }
    public string? Locality { get; set; }
    public bool IsRemote { get; set; }

    public JobType JobType { get; set; } = JobType.FullTime;
    public JobStatus Status { get; set; } = JobStatus.Draft;
    public ModerationStatus ModerationStatus { get; set; } = ModerationStatus.Approved;
    public int ViewCount { get; set; }
    public DateTime? PublishedAt { get; set; }

    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public int RecruiterProfileId { get; set; }
    public RecruiterProfile RecruiterProfile { get; set; } = null!;

    public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
    public ICollection<SavedJob> SavedByCandidates { get; set; } = new List<SavedJob>();
    public ICollection<JobReport> Reports { get; set; } = new List<JobReport>();
}
