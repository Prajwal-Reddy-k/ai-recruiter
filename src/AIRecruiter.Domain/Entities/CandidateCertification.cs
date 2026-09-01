using AIRecruiter.Domain.Common;

namespace AIRecruiter.Domain.Entities;

public class CandidateCertification : BaseEntity, IResumeSectionEntry
{
    public int CandidateProfileId { get; set; }
    public CandidateProfile CandidateProfile { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? IssuingOrganization { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? CredentialUrl { get; set; }
    public int DisplayOrder { get; set; }
}
