using AIRecruiter.Application.DTOs.Reviews;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

/// <summary>Anonymous-to-company candidate reviews, gated by a real application relationship
/// and moderated before going public — mirrors ModerationService's "polymorphic report" and
/// AdminService's "review queue" patterns, just for a dedicated review entity instead of the
/// generic Report table (reviews need many more fields than a report does).</summary>
public class CompanyReviewService : ICompanyReviewService
{
    private const int MaxSubmissionsPerDay = 5;
    private static readonly ApplicationStatus[] InterviewedStatuses = { ApplicationStatus.InterviewScheduled, ApplicationStatus.InterviewCompleted, ApplicationStatus.Offer, ApplicationStatus.Hired };
    private static readonly ApplicationStatus[] OfferStatuses = { ApplicationStatus.Offer, ApplicationStatus.Hired };

    private readonly AppDbContext _db;
    private readonly IAuditLogService _auditLog;
    private readonly IIpRateLimiter _rateLimiter;

    public CompanyReviewService(AppDbContext db, IAuditLogService auditLog, IIpRateLimiter rateLimiter)
    {
        _db = db;
        _auditLog = auditLog;
        _rateLimiter = rateLimiter;
    }

    public async Task<ReviewEligibilityDto> GetEligibilityAsync(int candidateUserId, int companyId, CancellationToken ct = default)
    {
        var profile = await _db.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == candidateUserId, ct);
        if (profile is null)
        {
            return new ReviewEligibilityDto(false, false, "Complete your candidate profile first.");
        }

        var alreadyReviewed = await _db.CompanyReviews.AnyAsync(r => r.CandidateProfileId == profile.Id && r.CompanyId == companyId, ct);
        if (alreadyReviewed)
        {
            return new ReviewEligibilityDto(false, true, "You've already reviewed this company.");
        }

        var hasApplied = await _db.JobApplications.AnyAsync(a => a.CandidateProfileId == profile.Id && a.JobPosting.CompanyId == companyId, ct);
        if (!hasApplied)
        {
            return new ReviewEligibilityDto(false, false, "You can only review a company you've applied to.");
        }

        return new ReviewEligibilityDto(true, false, null);
    }

    public async Task SubmitAsync(int candidateUserId, int companyId, SubmitCompanyReviewRequest request, string? ipAddress, CancellationToken ct = default)
    {
        if (!_rateLimiter.IsAllowed($"CompanyReview:{candidateUserId}", MaxSubmissionsPerDay, TimeSpan.FromDays(1))
            || (ipAddress is not null && !_rateLimiter.IsAllowed($"CompanyReview:ip:{ipAddress}", MaxSubmissionsPerDay, TimeSpan.FromDays(1))))
        {
            throw new RateLimitedException("You've submitted too many reviews recently — please try again later.", retryAfterSeconds: 3600);
        }

        var eligibility = await GetEligibilityAsync(candidateUserId, companyId, ct);
        if (!eligibility.Eligible)
        {
            throw new ConflictException("NOT_ELIGIBLE", eligibility.Reason ?? "You are not eligible to review this company.");
        }

        ValidateRatings(request);

        if (!Enum.TryParse<ReviewerRelationshipType>(request.RelationshipType, out var relationshipType))
        {
            throw new ValidationException("Please fix the highlighted fields.", new Dictionary<string, string> { ["relationshipType"] = "Choose a valid relationship type." });
        }

        var profile = await _db.CandidateProfiles.FirstAsync(c => c.UserId == candidateUserId, ct);
        await EnsureRelationshipClaimIsTruthfulAsync(profile.Id, companyId, relationshipType, ct);

        var review = new CompanyReview
        {
            CompanyId = companyId,
            CandidateProfileId = profile.Id,
            OverallRating = request.OverallRating,
            WorkCultureRating = request.WorkCultureRating,
            InterviewExperienceRating = request.InterviewExperienceRating,
            WorkLifeBalanceRating = request.WorkLifeBalanceRating,
            CareerGrowthRating = request.CareerGrowthRating,
            Title = request.Title.Trim(),
            Pros = request.Pros.Trim(),
            Cons = request.Cons.Trim(),
            AdviceToManagement = string.IsNullOrWhiteSpace(request.AdviceToManagement) ? null : request.AdviceToManagement.Trim(),
            RelationshipType = relationshipType,
            Status = ReviewStatus.Pending,
        };
        _db.CompanyReviews.Add(review);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            var alreadyExists = await _db.CompanyReviews.AnyAsync(r => r.CandidateProfileId == profile.Id && r.CompanyId == companyId, ct);
            if (alreadyExists)
            {
                throw new ConflictException("ALREADY_REVIEWED", "You've already reviewed this company.");
            }
            throw;
        }

        await _auditLog.LogAsync(candidateUserId, "Candidate", "CompanyReviewSubmitted", "CompanyReview", review.Id, new { CompanyId = companyId }, ct);
    }

    public async Task<CompanyReviewsSummaryDto> GetPublishedReviewsAsync(int companyId, string? relationshipTypeFilter, CancellationToken ct = default)
    {
        var query = _db.CompanyReviews.Where(r => r.CompanyId == companyId && r.Status == ReviewStatus.Published);

        if (!string.IsNullOrWhiteSpace(relationshipTypeFilter) && Enum.TryParse<ReviewerRelationshipType>(relationshipTypeFilter, out var filterType))
        {
            query = query.Where(r => r.RelationshipType == filterType);
        }

        var reviews = await query.OrderByDescending(r => r.CreatedAt).ToListAsync(ct);

        var allPublished = await _db.CompanyReviews.Where(r => r.CompanyId == companyId && r.Status == ReviewStatus.Published).ToListAsync(ct);
        var averageRating = allPublished.Count > 0 ? (decimal)allPublished.Average(r => r.OverallRating) : (decimal?)null;
        var breakdown = Enumerable.Range(1, 5)
            .Select(stars => new CompanyReviewRatingBreakdownDto(stars, allPublished.Count(r => r.OverallRating == stars)))
            .OrderByDescending(b => b.Stars)
            .ToList();

        return new CompanyReviewsSummaryDto(
            averageRating, allPublished.Count, breakdown,
            reviews.Select(ToPublicDto).ToList());
    }

    public async Task RespondAsync(int recruiterUserId, int reviewId, RespondToReviewRequest request, CancellationToken ct = default)
    {
        var review = await _db.CompanyReviews.FirstOrDefaultAsync(r => r.Id == reviewId, ct)
            ?? throw new NotFoundException("Review not found.");

        if (review.Status != ReviewStatus.Published)
        {
            throw new ConflictException("REVIEW_NOT_PUBLISHED", "Only a published review can receive a response.");
        }

        var isCompanyRecruiter = await _db.RecruiterProfiles.AnyAsync(r => r.UserId == recruiterUserId && r.CompanyId == review.CompanyId, ct);
        if (!isCompanyRecruiter)
        {
            throw new ForbiddenException("You do not have access to this review.");
        }

        if (string.IsNullOrWhiteSpace(request.Response))
        {
            throw new ValidationException("Please fix the highlighted fields.", new Dictionary<string, string> { ["response"] = "Response cannot be empty." });
        }

        review.RecruiterResponse = request.Response.Trim();
        review.RecruiterResponseByUserId = recruiterUserId;
        review.RecruiterRespondedAt = DateTime.UtcNow;
        review.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(recruiterUserId, "Recruiter", "CompanyReviewResponded", "CompanyReview", review.Id, null, ct);
    }

    private async Task EnsureRelationshipClaimIsTruthfulAsync(int candidateProfileId, int companyId, ReviewerRelationshipType claimed, CancellationToken ct)
    {
        if (claimed == ReviewerRelationshipType.Applicant) return;

        var reachedStatuses = await _db.ApplicationStatusHistories
            .Where(h => h.JobApplication.CandidateProfileId == candidateProfileId && h.JobApplication.JobPosting.CompanyId == companyId)
            .Select(h => h.ToStatus)
            .ToListAsync(ct);

        // A Hired/Offer application's current status already implies the required history —
        // include current statuses too since not every transition necessarily has a history row
        // (e.g. seeded demo applications created directly at a given status).
        var currentStatuses = await _db.JobApplications
            .Where(a => a.CandidateProfileId == candidateProfileId && a.JobPosting.CompanyId == companyId)
            .Select(a => a.Status)
            .ToListAsync(ct);

        var everReached = reachedStatuses.Concat(currentStatuses).ToHashSet();

        var required = claimed switch
        {
            ReviewerRelationshipType.Interviewed => InterviewedStatuses,
            ReviewerRelationshipType.ReceivedOffer => OfferStatuses,
            ReviewerRelationshipType.Hired => new[] { ApplicationStatus.Hired },
            _ => Array.Empty<ApplicationStatus>(),
        };

        if (!required.Any(everReached.Contains))
        {
            throw new ValidationException(
                "This doesn't match your actual application history with this company.",
                new Dictionary<string, string> { ["relationshipType"] = "This doesn't match your actual application history with this company." });
        }
    }

    private static void ValidateRatings(SubmitCompanyReviewRequest request)
    {
        var errors = new Dictionary<string, string>();
        void CheckRating(int value, string field)
        {
            if (value is < 1 or > 5) errors[field] = "Rating must be between 1 and 5.";
        }
        CheckRating(request.OverallRating, "overallRating");
        CheckRating(request.WorkCultureRating, "workCultureRating");
        CheckRating(request.InterviewExperienceRating, "interviewExperienceRating");
        CheckRating(request.WorkLifeBalanceRating, "workLifeBalanceRating");
        CheckRating(request.CareerGrowthRating, "careerGrowthRating");

        if (string.IsNullOrWhiteSpace(request.Title)) errors["title"] = "Title is required.";
        if (string.IsNullOrWhiteSpace(request.Pros)) errors["pros"] = "Pros are required.";
        if (string.IsNullOrWhiteSpace(request.Cons)) errors["cons"] = "Cons are required.";

        if (errors.Count > 0) throw new ValidationException("Please fix the highlighted fields.", errors);
    }

    private static PublicCompanyReviewDto ToPublicDto(CompanyReview r) => new(
        r.Id, r.OverallRating, r.WorkCultureRating, r.InterviewExperienceRating, r.WorkLifeBalanceRating, r.CareerGrowthRating,
        r.Title, r.Pros, r.Cons, r.AdviceToManagement, r.RelationshipType.ToString(),
        r.RecruiterResponse, r.RecruiterRespondedAt, r.CreatedAt);
}
