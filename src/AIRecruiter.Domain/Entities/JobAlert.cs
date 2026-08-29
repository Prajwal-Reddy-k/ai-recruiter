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
}
