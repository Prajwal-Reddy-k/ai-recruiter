using AIRecruiter.Application.DTOs.Recruiters;

namespace AIRecruiter.Application.Interfaces;

public interface IRecruiterOnboardingService
{
    Task<OnboardingStatusDto> GetStatusAsync(int userId, CancellationToken ct = default);
    Task<OnboardingStatusDto> UpsertAsync(int userId, UpsertRecruiterOnboardingRequest request, CancellationToken ct = default);
}
