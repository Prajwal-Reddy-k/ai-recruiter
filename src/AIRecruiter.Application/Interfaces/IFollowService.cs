using AIRecruiter.Application.DTOs.Companies;

namespace AIRecruiter.Application.Interfaces;

/// <summary>Deliberately has no method returning a company's followers to a recruiter — a
/// candidate's follow list is never exposed to the companies they follow.</summary>
public interface IFollowService
{
    Task FollowAsync(int candidateUserId, int companyId, CancellationToken ct = default);
    Task UnfollowAsync(int candidateUserId, int companyId, CancellationToken ct = default);
    Task<IReadOnlyList<FollowedCompanyDto>> GetMyFollowedCompaniesAsync(int candidateUserId, CancellationToken ct = default);
    Task UpdateNotifyPreferenceAsync(int candidateUserId, int companyId, UpdateFollowNotifyRequest request, CancellationToken ct = default);
}
