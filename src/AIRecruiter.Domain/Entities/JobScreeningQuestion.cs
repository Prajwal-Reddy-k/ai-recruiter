using AIRecruiter.Domain.Common;
using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Domain.Entities;

/// <summary>A recruiter-authored qualifying question attached to a job posting. Once at least
/// one ScreeningAnswer references this question, its QuestionType/Options become immutable
/// and it can no longer be deleted (enforced in JobPostingService.SyncScreeningQuestionsAsync,
/// backed by a Restrict FK on ScreeningAnswer.JobScreeningQuestionId) — so editing a Published
/// job's questions can never corrupt a prior applicant's submitted answers.</summary>
public class JobScreeningQuestion : BaseEntity
{
    public int JobPostingId { get; set; }
    public JobPosting JobPosting { get; set; } = null!;

    public string QuestionText { get; set; } = string.Empty;
    public ScreeningQuestionType QuestionType { get; set; }
    public bool IsRequired { get; set; } = true;
    public string? HelpText { get; set; }
    public int DisplayOrder { get; set; }

    /// <summary>Recruiter-only reference answer — never exposed to candidates or the public
    /// job view. See JobPostingMapper.ToDto's includePreferredAnswers flag.</summary>
    public string? PreferredAnswer { get; set; }

    public ICollection<ScreeningQuestionOption> Options { get; set; } = new List<ScreeningQuestionOption>();
    public ICollection<ScreeningAnswer> Answers { get; set; } = new List<ScreeningAnswer>();
}
