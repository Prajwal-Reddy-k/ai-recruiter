using AIRecruiter.Domain.Common;
using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Domain.Entities;

/// <summary>A candidate's self-set career-planning target. At least one of TargetRole,
/// TargetSkill, or TargetCompanyType must be set (enforced in the validator, not here) — a
/// goal needs some target to be meaningful.</summary>
public class CareerGoal : BaseEntity
{
    public int CandidateProfileId { get; set; }
    public CandidateProfile CandidateProfile { get; set; } = null!;

    public string? TargetRole { get; set; }
    public string? TargetSkill { get; set; }
    public string? TargetCompanyType { get; set; }

    public string? PreferredState { get; set; }
    public string? PreferredCity { get; set; }
    public bool IsLocationRemote { get; set; }

    public DateTime? TargetCompletionDate { get; set; }
    public int ProgressPercent { get; set; }
    public CareerGoalStatus Status { get; set; } = CareerGoalStatus.InProgress;
    public string? Notes { get; set; }
}
