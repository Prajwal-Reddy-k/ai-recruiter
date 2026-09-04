using AIRecruiter.Domain.Common;
using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Domain.Entities;

/// <summary>Anonymous-to-the-company candidate review of a company, gated by a real
/// application relationship (see CompanyReviewService.SubmitAsync). Reviewer identity is
/// never exposed on any recruiter- or public-facing DTO — only Admin's moderation view
/// includes it, the same way ModerationService already handles reporter identity.</summary>
public class CompanyReview : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public int CandidateProfileId { get; set; }
    public CandidateProfile CandidateProfile { get; set; } = null!;

    public int OverallRating { get; set; }
    public int WorkCultureRating { get; set; }
    public int InterviewExperienceRating { get; set; }
    public int WorkLifeBalanceRating { get; set; }
    public int CareerGrowthRating { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Pros { get; set; } = string.Empty;
    public string Cons { get; set; } = string.Empty;
    public string? AdviceToManagement { get; set; }

    public ReviewerRelationshipType RelationshipType { get; set; }
    public ReviewStatus Status { get; set; } = ReviewStatus.Pending;
    public string? ModerationNote { get; set; }
    public int? ReviewedByUserId { get; set; }
    public User? ReviewedByUser { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public string? RecruiterResponse { get; set; }
    public int? RecruiterResponseByUserId { get; set; }
    public User? RecruiterResponseByUser { get; set; }
    public DateTime? RecruiterRespondedAt { get; set; }
}
