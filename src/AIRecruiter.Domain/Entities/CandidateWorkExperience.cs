using AIRecruiter.Domain.Common;

namespace AIRecruiter.Domain.Entities;

/// <summary>A repeatable work-history entry for the resume builder — deliberately separate
/// from CandidateProfile.ExperienceSummary (a single free-text field used elsewhere), so
/// nothing that already reads that field is affected.</summary>
public class CandidateWorkExperience : BaseEntity, IResumeSectionEntry
{

    public int CandidateProfileId { get; set; }
    public CandidateProfile CandidateProfile { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string? Location { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
}
