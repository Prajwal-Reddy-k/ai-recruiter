using AIRecruiter.Application.DTOs.Admin;
using AIRecruiter.Application.DTOs.Companies;
using AIRecruiter.Application.DTOs.Jobs;
using AIRecruiter.Application.DTOs.Reviews;

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
    Task<IReadOnlyList<PendingCompanyVerificationDto>> GetPendingCompanyVerificationsAsync(CancellationToken ct = default);
    Task SetCompanyVerificationStatusAsync(int adminUserId, int companyId, SetCompanyVerificationStatusRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<PendingCompanyReviewDto>> GetPendingReviewsAsync(CancellationToken ct = default);
    Task SetReviewStatusAsync(int adminUserId, int reviewId, SetReviewStatusRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<AdminUserDto>> GetPendingDeletionRequestsAsync(CancellationToken ct = default);
    Task AdminCancelDeletionRequestAsync(int adminUserId, int userId, CancellationToken ct = default);
}
