using AIRecruiter.Application.DTOs.Users;

namespace AIRecruiter.Application.Interfaces;

/// <summary>Editing a user's own identity fields (name, phone) — distinct from
/// IRecruiterOnboardingService (company data) and ICandidateProfileService (candidate-specific
/// profile fields), since FullName/PhoneNumber live on User and apply to any role.</summary>
public interface IUserProfileService
{
    Task<UserDetailsDto> GetMyDetailsAsync(int userId, CancellationToken ct = default);
    Task<UserDetailsDto> UpdateMyDetailsAsync(int userId, UpdateUserDetailsRequest request, CancellationToken ct = default);
    Task ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct = default);

    /// <summary>Starts the 14-day grace period (see JobLifecycleSweepService) — the account
    /// stays active and usable until the sweep deactivates it, unless cancelled first.</summary>
    Task<AccountDeletionStatusDto> RequestAccountDeletionAsync(int userId, RequestAccountDeletionRequest request, CancellationToken ct = default);

    Task<AccountDeletionStatusDto> CancelAccountDeletionAsync(int userId, CancellationToken ct = default);
    Task<PrivacySummaryDto> GetPrivacySummaryAsync(int userId, CancellationToken ct = default);
}
