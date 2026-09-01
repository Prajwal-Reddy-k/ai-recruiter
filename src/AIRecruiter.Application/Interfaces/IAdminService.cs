using AIRecruiter.Application.DTOs.Admin;
using AIRecruiter.Application.DTOs.Jobs;

namespace AIRecruiter.Application.Interfaces;

public interface IAdminService
{
    Task<IReadOnlyList<AdminUserDto>> GetUsersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AdminCompanyDto>> GetCompaniesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AdminJobDto>> GetJobsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ReportDto>> GetReportsAsync(CancellationToken ct = default);
    Task<JobPostingDto> ModerateJobAsync(int adminUserId, int jobId, ModerateJobRequest request, CancellationToken ct = default);
    Task SetReportStatusAsync(int adminUserId, int reportId, SetReportStatusRequest request, CancellationToken ct = default);
    Task AddReportNoteAsync(int adminUserId, int reportId, AddReportNoteRequest request, CancellationToken ct = default);
    Task SuspendUserAsync(int adminUserId, int userId, CancellationToken ct = default);
    Task ReactivateUserAsync(int adminUserId, int userId, CancellationToken ct = default);
}
