using AIRecruiter.Domain.Common;

namespace AIRecruiter.Domain.Entities;

/// <summary>Real, per-candidate "recently viewed" history — distinct from JobPosting.ViewCount
/// (an anonymous, in-memory-deduplicated aggregate counter used for recruiter analytics). One
/// row per candidate+job, upserted (ViewedAt bumped) on repeat visits rather than duplicated.</summary>
public class JobView : BaseEntity
{
    public int CandidateProfileId { get; set; }
    public CandidateProfile CandidateProfile { get; set; } = null!;

    public int JobPostingId { get; set; }
    public JobPosting JobPosting { get; set; } = null!;

    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;
}
