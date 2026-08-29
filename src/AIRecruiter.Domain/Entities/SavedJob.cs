using AIRecruiter.Domain.Common;

namespace AIRecruiter.Domain.Entities;

public class SavedJob : BaseEntity
{
    public int CandidateProfileId { get; set; }
    public CandidateProfile CandidateProfile { get; set; } = null!;

    public int JobPostingId { get; set; }
    public JobPosting JobPosting { get; set; } = null!;
}
