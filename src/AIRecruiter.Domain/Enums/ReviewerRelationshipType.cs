namespace AIRecruiter.Domain.Enums;

/// <summary>Self-selected by the reviewer, then cross-checked against their real application
/// history (see CompanyReviewService) so it can't be misrepresented.</summary>
public enum ReviewerRelationshipType
{
    Applicant = 1,
    Interviewed = 2,
    ReceivedOffer = 3,
    Hired = 4
}
