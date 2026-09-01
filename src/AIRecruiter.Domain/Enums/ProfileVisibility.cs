namespace AIRecruiter.Domain.Enums;

public enum ProfileVisibility
{
    /// <summary>Discoverable by any recruiter via the candidate-discovery surface, even
    /// before applying anywhere.</summary>
    VisibleToRecruiters = 1,

    /// <summary>The existing, default behavior — visible only to a recruiter at a company
    /// the candidate has actually applied to (via CandidateSearchService's existing
    /// applied-to-this-company gate). Not discoverable ahead of applying.</summary>
    VisibleAfterApplying = 2,

    /// <summary>Never surfaced to recruiters outside their own application detail view.</summary>
    Private = 3
}
