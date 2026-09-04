using AIRecruiter.Domain.Common;
using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Domain.Entities;

public class JobAlert : BaseEntity
{
    public int CandidateProfileId { get; set; }
    public CandidateProfile CandidateProfile { get; set; } = null!;

    public string? SkillsCsv { get; set; }
    public string? State { get; set; }
    public string? City { get; set; }
    public bool? IsRemote { get; set; }
    public JobType? JobType { get; set; }
    public int? MinExperienceYears { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>The "advanced saved search" fields — this entity started as a simple job
    /// alert and was extended in place rather than duplicated into a parallel concept.</summary>
    public string? Name { get; set; }
    public string? Keyword { get; set; }
    public decimal? MinSalary { get; set; }
    public decimal? MaxSalary { get; set; }
    public string? SortOption { get; set; }

    /// <summary>At most one saved search per candidate has this set — enforced in
    /// JobAlertService.SetDefaultAsync, not by a DB constraint.</summary>
    public bool IsDefault { get; set; }
}
