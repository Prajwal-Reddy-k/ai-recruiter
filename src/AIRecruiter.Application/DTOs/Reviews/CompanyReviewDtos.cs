namespace AIRecruiter.Application.DTOs.Reviews;

public record SubmitCompanyReviewRequest(
    int OverallRating,
    int WorkCultureRating,
    int InterviewExperienceRating,
    int WorkLifeBalanceRating,
    int CareerGrowthRating,
    string Title,
    string Pros,
    string Cons,
    string? AdviceToManagement,
    string RelationshipType);

/// <summary>Never includes the reviewer's identity — this is the shape returned by every
/// public/recruiter-facing endpoint. Only the Admin moderation DTO carries reviewer info.</summary>
public record PublicCompanyReviewDto(
    int Id,
    int OverallRating,
    int WorkCultureRating,
    int InterviewExperienceRating,
    int WorkLifeBalanceRating,
    int CareerGrowthRating,
    string Title,
    string Pros,
    string Cons,
    string? AdviceToManagement,
    string RelationshipType,
    string? RecruiterResponse,
    DateTime? RecruiterRespondedAt,
    DateTime CreatedAt);

public record CompanyReviewRatingBreakdownDto(int Stars, int Count);

public record CompanyReviewsSummaryDto(
    decimal? AverageRating,
    int ReviewCount,
    IReadOnlyList<CompanyReviewRatingBreakdownDto> RatingBreakdown,
    IReadOnlyList<PublicCompanyReviewDto> Reviews);

public record ReviewEligibilityDto(bool Eligible, bool AlreadyReviewed, string? Reason);

public record RespondToReviewRequest(string Response);

/// <summary>Admin-only — the one DTO that does carry reviewer identity, needed for abuse
/// handling exactly like Report's ReportedByUser is Admin-only elsewhere.</summary>
public record PendingCompanyReviewDto(
    int Id,
    int CompanyId,
    string CompanyName,
    int OverallRating,
    string Title,
    string Pros,
    string Cons,
    string? AdviceToManagement,
    string RelationshipType,
    string Status,
    string ReviewerName,
    DateTime CreatedAt);

public record SetReviewStatusRequest(string Status, string? ModerationNote);
