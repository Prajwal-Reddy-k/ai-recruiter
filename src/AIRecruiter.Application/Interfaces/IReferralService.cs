using AIRecruiter.Application.DTOs.Referrals;

namespace AIRecruiter.Application.Interfaces;

public interface IReferralService
{
    Task<CreateReferralResponse> CreateAsync(int referrerUserId, string ipAddress, CreateReferralRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ReferralDto>> GetMyReferralsAsync(int referrerUserId, CancellationToken ct = default);
    Task<IReadOnlyList<ReferralDto>> GetCompanyReferralsAsync(int recruiterUserId, CancellationToken ct = default);

    /// <summary>Anonymous — no caller identity involved.</summary>
    Task<ReferralTokenPreviewDto> ResolveTokenAsync(string rawToken, CancellationToken ct = default);
}
