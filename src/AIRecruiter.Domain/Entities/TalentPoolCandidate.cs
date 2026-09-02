using AIRecruiter.Domain.Common;

namespace AIRecruiter.Domain.Entities;

public class TalentPoolCandidate : BaseEntity
{
    public int TalentPoolId { get; set; }
    public TalentPool TalentPool { get; set; } = null!;

    public int CandidateProfileId { get; set; }
    public CandidateProfile CandidateProfile { get; set; } = null!;

    public int AddedByUserId { get; set; }
    public User AddedByUser { get; set; } = null!;

    public string? Notes { get; set; }
    public string? TagsCsv { get; set; }
}
