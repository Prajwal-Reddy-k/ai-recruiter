using AIRecruiter.Domain.Common;

namespace AIRecruiter.Domain.Entities;

public class CandidateProject : BaseEntity, IResumeSectionEntry
{
    public int CandidateProfileId { get; set; }
    public CandidateProfile CandidateProfile { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ProjectUrl { get; set; }
    public string? TechnologiesCsv { get; set; }
    public int DisplayOrder { get; set; }
}
