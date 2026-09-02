using AIRecruiter.Domain.Common;

namespace AIRecruiter.Domain.Entities;

/// <summary>A reusable cover-letter starting point a candidate fills in once and then
/// merges with safe profile data + a specific job when applying. No AI is involved —
/// merging happens client-side by string concatenation of these sections.</summary>
public class CoverLetterTemplate : BaseEntity
{
    public int CandidateProfileId { get; set; }
    public CandidateProfile CandidateProfile { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Introduction { get; set; }
    public string? SkillsHighlights { get; set; }
    public string? ProjectAchievements { get; set; }
    public string? ClosingMessage { get; set; }
}
