using AIRecruiter.Domain.Common;

namespace AIRecruiter.Domain.Entities;

/// <summary>Snapshots the randomized question set actually served for one attempt, so a
/// later review always shows exactly what was asked even as the underlying question bank
/// keeps changing. DisplayOrder is the shuffled order chosen at attempt-start time.</summary>
public class SkillAssessmentAnswer : BaseEntity
{
    public int SkillAssessmentAttemptId { get; set; }
    public SkillAssessmentAttempt Attempt { get; set; } = null!;

    public int SkillAssessmentQuestionId { get; set; }
    public SkillAssessmentQuestion Question { get; set; } = null!;

    public int DisplayOrder { get; set; }
    public int? SelectedOptionIndex { get; set; }
}
