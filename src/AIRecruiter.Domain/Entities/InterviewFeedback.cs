using AIRecruiter.Domain.Common;
using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Domain.Entities;

/// <summary>One recruiter/interviewer's structured feedback for one Interview. While
/// IsDraft is true it can be freely edited; once submitted (IsDraft=false, SubmittedAtUtc
/// set) any further edit must first write a snapshot to InterviewFeedbackEditHistory — this
/// is the "clearly recorded edit history" requirement rather than true field-level
/// immutability, which would need a much heavier versioning UI for a portfolio project.
/// Never exposed to candidates.</summary>
public class InterviewFeedback : BaseEntity
{
    public int InterviewId { get; set; }
    public Interview Interview { get; set; } = null!;

    public int RecruiterProfileId { get; set; }
    public RecruiterProfile RecruiterProfile { get; set; } = null!;

    public int TechnicalScore { get; set; }
    public int CommunicationScore { get; set; }
    public int ProblemSolvingScore { get; set; }
    public int CultureFitScore { get; set; }
    public InterviewRecommendation Recommendation { get; set; } = InterviewRecommendation.Neutral;

    public string? Strengths { get; set; }
    public string? Concerns { get; set; }
    public string? PrivateNotes { get; set; }

    public bool IsDraft { get; set; } = true;
    public DateTime? SubmittedAtUtc { get; set; }

    public ICollection<InterviewFeedbackEditHistory> EditHistory { get; set; } = new List<InterviewFeedbackEditHistory>();
}

/// <summary>A snapshot of an InterviewFeedback row's field values immediately before an edit
/// that happened after it was already submitted — written by InterviewFeedbackService before
/// applying the new values, never after.</summary>
public class InterviewFeedbackEditHistory : BaseEntity
{
    public int InterviewFeedbackId { get; set; }
    public InterviewFeedback InterviewFeedback { get; set; } = null!;

    public int EditedByUserId { get; set; }
    public User EditedByUser { get; set; } = null!;
    public DateTime EditedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>JSON snapshot of the previous field values (scores/recommendation/text) —
    /// small, non-sensitive structured data only, consistent with the audit-log metadata
    /// convention elsewhere in this codebase.</summary>
    public string PreviousValuesJson { get; set; } = string.Empty;
}
