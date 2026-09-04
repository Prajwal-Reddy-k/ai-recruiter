using AIRecruiter.Application.DTOs.Reviews;

namespace AIRecruiter.Application.Interfaces;

public interface ICompanyReviewService
{
    Task<ReviewEligibilityDto> GetEligibilityAsync(int candidateUserId, int companyId, CancellationToken ct = default);
    Task SubmitAsync(int candidateUserId, int companyId, SubmitCompanyReviewRequest request, string? ipAddress, CancellationToken ct = default);
    Task<CompanyReviewsSummaryDto> GetPublishedReviewsAsync(int companyId, string? relationshipTypeFilter, CancellationToken ct = default);
    Task RespondAsync(int recruiterUserId, int reviewId, RespondToReviewRequest request, CancellationToken ct = default);
}
