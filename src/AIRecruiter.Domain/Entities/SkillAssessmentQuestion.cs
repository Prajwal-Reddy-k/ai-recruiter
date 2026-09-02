using AIRecruiter.Domain.Common;
using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Domain.Entities;

/// <summary>Locally seeded, fictional multiple-choice question bank — not sourced from any
/// real certification body, and completing an assessment built from these questions is never
/// presented as an official certification.</summary>
public class SkillAssessmentQuestion : BaseEntity
{
    public AssessmentCategory Category { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string OptionA { get; set; } = string.Empty;
    public string OptionB { get; set; } = string.Empty;
    public string OptionC { get; set; } = string.Empty;
    public string OptionD { get; set; } = string.Empty;

    /// <summary>0-based index into A/B/C/D.</summary>
    public int CorrectOptionIndex { get; set; }

    /// <summary>Shown only after a candidate submits their attempt, on the per-question review.</summary>
    public string? Explanation { get; set; }
}
