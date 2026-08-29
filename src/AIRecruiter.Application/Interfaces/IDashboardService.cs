using AIRecruiter.Application.DTOs.Dashboard;

namespace AIRecruiter.Application.Interfaces;

public interface IDashboardService
{
    Task<CandidateDashboardDto> GetCandidateDashboardAsync(int userId, CancellationToken ct = default);
    Task<RecruiterDashboardDto> GetRecruiterDashboardAsync(int userId, CancellationToken ct = default);
}
