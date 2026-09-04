using AIRecruiter.Domain.Common;

namespace AIRecruiter.Domain.Entities;

public class CompanyFollow : BaseEntity
{
    public int CandidateProfileId { get; set; }
    public CandidateProfile CandidateProfile { get; set; } = null!;

    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public bool NotifyOnNewJob { get; set; } = true;
}
