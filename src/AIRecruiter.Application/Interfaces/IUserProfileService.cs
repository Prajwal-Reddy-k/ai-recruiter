using AIRecruiter.Application.DTOs.Users;

namespace AIRecruiter.Application.Interfaces;

/// <summary>Editing a user's own identity fields (name, phone) — distinct from
/// IRecruiterOnboardingService (company data) and ICandidateProfileService (candidate-specific
/// profile fields), since FullName/PhoneNumber live on User and apply to any role.</summary>
public interface IUserProfileService
{
    Task<UserDetailsDto> GetMyDetailsAsync(int userId, CancellationToken ct = default);
    Task<UserDetailsDto> UpdateMyDetailsAsync(int userId, UpdateUserDetailsRequest request, CancellationToken ct = default);
}
