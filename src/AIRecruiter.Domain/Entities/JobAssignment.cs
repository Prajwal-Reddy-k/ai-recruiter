using AIRecruiter.Domain.Common;

namespace AIRecruiter.Domain.Entities;

/// <summary>Grants a company teammate (typically a HiringManager) access to one specific
/// job's applicants without making them the job's owning recruiter. The job's own
/// RecruiterProfile is always implicitly assigned — this table only covers *additional*
/// people.</summary>
public class JobAssignment : BaseEntity
{
    public int JobPostingId { get; set; }
    public JobPosting JobPosting { get; set; } = null!;

    public int RecruiterProfileId { get; set; }
    public RecruiterProfile RecruiterProfile { get; set; } = null!;

    public int AssignedByUserId { get; set; }
    public User AssignedByUser { get; set; } = null!;
}
