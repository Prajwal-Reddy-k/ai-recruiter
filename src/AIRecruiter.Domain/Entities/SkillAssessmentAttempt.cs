using AIRecruiter.Domain.Common;
using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Domain.Entities;

public class SkillAssessmentAttempt : BaseEntity
{
    public int CandidateProfileId { get; set; }
    public CandidateProfile CandidateProfile { get; set; } = null!;

    public AssessmentCategory Category { get; set; }
    public AssessmentAttemptStatus Status { get; set; } = AssessmentAttemptStatus.InProgress;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }

    /// <summary>StartedAt + the category's time limit — the single source of truth for both
    /// the frontend countdown and the server-side "is this attempt still active" check.</summary>
    public DateTime ExpiresAt { get; set; }

    public int? ScoreCorrectCount { get; set; }
    public int? TotalQuestionCount { get; set; }
    public decimal? PercentageScore { get; set; }

    /// <summary>Candidate-controlled opt-in, defaults false. Only true, Completed attempts are
    /// ever surfaced to recruiters or on the public portfolio profile.</summary>
    public bool IsVisibleToRecruiters { get; set; }

    public ICollection<SkillAssessmentAnswer> Answers { get; set; } = new List<SkillAssessmentAnswer>();
}
