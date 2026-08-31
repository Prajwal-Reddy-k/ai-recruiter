using AIRecruiter.Domain.Common;
using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Domain.Entities;

/// <summary>A single, concrete interview proposal for one application — one specific time
/// window rather than a set of candidate-chosen slots. JobId/CandidateId/RecruiterId are
/// intentionally not denormalized here; they're always reachable via JobApplication (job,
/// candidate) and CreatedByUser (recruiter), matching how the rest of the domain avoids
/// duplicating foreign keys that are already reachable through an existing relationship.</summary>
public class Interview : BaseEntity
{
    public int JobApplicationId { get; set; }
    public JobApplication JobApplication { get; set; } = null!;

    public DateTime ScheduledStartUtc { get; set; }
    public DateTime ScheduledEndUtc { get; set; }
    public InterviewType Type { get; set; } = InterviewType.Online;

    /// <summary>Meeting link (for Online) or venue address (for Phone/In Person).</summary>
    public string? Location { get; set; }

    public string? RecruiterNote { get; set; }
    public string? CandidateResponseNote { get; set; }

    public InterviewStatus Status { get; set; } = InterviewStatus.Proposed;

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
}
