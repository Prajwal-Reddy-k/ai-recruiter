using AIRecruiter.Application.DTOs.Admin;
using AIRecruiter.Application.DTOs.Jobs;

namespace AIRecruiter.Application.Interfaces;

public interface IAdminService
{
    Task<IReadOnlyList<AdminUserDto>> GetUsersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AdminCompanyDto>> GetCompaniesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AdminJobDto>> GetJobsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<JobReportDto>> GetReportsAsync(CancellationToken ct = default);
    Task<JobPostingDto> ModerateJobAsync(int adminUserId, int jobId, ModerateJobRequest request, CancellationToken ct = default);
    Task ResolveReportAsync(int adminUserId, int reportId, ResolveReportRequest request, CancellationToken ct = default);
}
