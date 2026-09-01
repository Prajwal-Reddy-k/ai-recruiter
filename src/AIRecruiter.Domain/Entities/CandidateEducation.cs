using AIRecruiter.Domain.Common;

namespace AIRecruiter.Domain.Entities;

/// <summary>A repeatable education entry for the resume builder — deliberately separate from
/// CandidateProfile.Education/GraduationYear (a single free-text pair used elsewhere), so
/// nothing that already reads those fields is affected.</summary>
public class CandidateEducation : BaseEntity, IResumeSectionEntry
{
    public int CandidateProfileId { get; set; }
    public CandidateProfile CandidateProfile { get; set; } = null!;

    public string Institution { get; set; } = string.Empty;
    public string Degree { get; set; } = string.Empty;
    public string? FieldOfStudy { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? GradeOrGpa { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
}
