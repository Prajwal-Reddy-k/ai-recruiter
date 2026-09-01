using AIRecruiter.Application.DTOs.Reports;

namespace AIRecruiter.Application.Interfaces;

/// <summary>Company-wide (not per-recruiter), date-range-filterable reporting on top of the
/// same tables IAnalyticsService already reads — that service is untouched; this one adds
/// company-wide scope, date filtering, and CSV exports on top.</summary>
public interface IReportService
{
    Task<RecruiterReportDto> GetReportAsync(int recruiterUserId, ReportFilterRequest filter, CancellationToken ct = default);
    Task<string> ExportJobsCsvAsync(int recruiterUserId, ReportFilterRequest filter, CancellationToken ct = default);
    Task<string> ExportApplicantsCsvAsync(int recruiterUserId, ReportFilterRequest filter, CancellationToken ct = default);
    Task<string> ExportInterviewScheduleCsvAsync(int recruiterUserId, ReportFilterRequest filter, CancellationToken ct = default);
    Task<string> ExportFunnelSummaryCsvAsync(int recruiterUserId, ReportFilterRequest filter, CancellationToken ct = default);
}
