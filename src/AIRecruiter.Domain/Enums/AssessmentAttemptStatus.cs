namespace AIRecruiter.Domain.Enums;

public enum AssessmentAttemptStatus
{
    InProgress = 1,
    Completed = 2,

    /// <summary>Left in-progress past its time limit without being submitted.</summary>
    Abandoned = 3
}
