using AIRecruiter.Domain.Common;

namespace AIRecruiter.Domain.Entities;

/// <summary>Grants a company teammate (typically an Interviewer) access to one specific
/// interview so they can submit a scorecard for it, without being the job's owning
/// recruiter.</summary>
public class InterviewAssignment : BaseEntity
{
    public int InterviewId { get; set; }
    public Interview Interview { get; set; } = null!;

    public int RecruiterProfileId { get; set; }
    public RecruiterProfile RecruiterProfile { get; set; } = null!;

    public int AssignedByUserId { get; set; }
    public User AssignedByUser { get; set; } = null!;
}
