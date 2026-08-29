using AIRecruiter.Application.DTOs.Analytics;

namespace AIRecruiter.Application.Interfaces;

public interface IAnalyticsService
{
    Task<RecruiterAnalyticsDto> GetRecruiterAnalyticsAsync(int recruiterUserId, CancellationToken ct = default);
}
