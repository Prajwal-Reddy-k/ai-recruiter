using System.Text;
using AIRecruiter.Application.DTOs.Analytics;
using AIRecruiter.Application.DTOs.Reports;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Application.Matching;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

/// <summary>Company-wide (Pattern B — never per-recruiter-only), date-range-filterable
/// reporting and CSV export. Reuses IAnalyticsService's aggregation approach (grouping over
/// materialized lists) rather than duplicating it wholesale; IAnalyticsService itself is
/// untouched. Every export only ever selects specific safe columns — never raw entities,
/// never resume file contents, tokens, or passwords.</summary>
public class ReportService : IReportService
{
    private readonly AppDbContext _db;
    private readonly IAuditLogService _auditLog;

    public ReportService(AppDbContext db, IAuditLogService auditLog)
    {
        _db = db;
        _auditLog = auditLog;
    }

    public async Task<RecruiterReportDto> GetReportAsync(int recruiterUserId, ReportFilterRequest filter, CancellationToken ct = default)
    {
        var companyId = await GetCallerCompanyIdAsync(recruiterUserId, ct);
        var (jobs, applications) = await LoadCompanyDataAsync(companyId, filter, ct);

        var applicationsByJob = jobs
            .Select(j => new NamedCountDto(j.Title, applications.Count(a => a.JobPostingId == j.Id)))
            .Where(x => x.Count > 0)
            .OrderByDescending(x => x.Count)
            .Take(15)
            .ToList();

        var applicationsByCity = applications
            .GroupBy(a => a.JobPosting.IsRemote ? "Remote — India" : (a.JobPosting.City ?? "Unspecified"))
            .Select(g => new NamedCountDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

        var applicationsByState = applications
            .GroupBy(a => a.JobPosting.IsRemote ? "Remote — India" : (a.JobPosting.State ?? "Unspecified"))
            .Select(g => new NamedCountDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

        var statusFunnel = Enum.GetValues<ApplicationStatus>()
            .Select(status => new NamedCountDto(status.ToString(), applications.Count(a => a.Status == status)))
            .ToList();

        var skillCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var app in applications)
        {
            foreach (var skill in SkillTaxonomy.ParseCsv(app.CandidateProfile.SkillsCsv))
            {
                skillCounts[skill] = skillCounts.GetValueOrDefault(skill, 0) + 1;
            }
        }
        var topSkills = skillCounts.OrderByDescending(kv => kv.Value).Take(10).Select(kv => new NamedCountDto(kv.Key, kv.Value)).ToList();

        var applicationIds = applications.Select(a => a.Id).ToHashSet();
        var interviewScheduledHistory = await _db.ApplicationStatusHistories
            .Where(h => applicationIds.Contains(h.JobApplicationId) && h.ToStatus == ApplicationStatus.InterviewScheduled)
            .Select(h => new { h.JobApplicationId, h.ChangedAt })
            .ToListAsync(ct);

        var applicationsWithInterview = interviewScheduledHistory.Select(h => h.JobApplicationId).Distinct().Count();
        var applicationCreatedAt = applications.ToDictionary(a => a.Id, a => a.CreatedAt);
        var daysToInterview = interviewScheduledHistory
            .Where(h => applicationCreatedAt.ContainsKey(h.JobApplicationId))
            .Select(h => (h.ChangedAt - applicationCreatedAt[h.JobApplicationId]).TotalDays)
            .Where(d => d >= 0)
            .ToList();

        return new RecruiterReportDto(
            JobsCreated: jobs.Count,
            JobsPublished: jobs.Count(j => j.PublishedAt.HasValue),
            JobsClosed: jobs.Count(j => j.Status == JobStatus.Closed),
            TotalApplications: applications.Count,
            ApplicationsByJob: applicationsByJob,
            ApplicationsByCity: applicationsByCity,
            ApplicationsByState: applicationsByState,
            StatusFunnel: statusFunnel,
            InterviewsScheduledCount: applicationsWithInterview,
            InterviewsCompletedCount: applications.Count(a => a.Status == ApplicationStatus.InterviewCompleted || a.Status == ApplicationStatus.Offer || a.Status == ApplicationStatus.Hired),
            InterviewConversionRatePercent: applications.Count == 0 ? null : Math.Round(applicationsWithInterview * 100.0 / applications.Count, 1),
            AverageDaysToInterview: daysToInterview.Count == 0 ? null : Math.Round(daysToInterview.Average(), 1),
            TopCandidateSkills: topSkills,
            OffersMade: applications.Count(a => a.Status == ApplicationStatus.Offer),
            Hires: applications.Count(a => a.Status == ApplicationStatus.Hired));
    }

    public async Task<string> ExportJobsCsvAsync(int recruiterUserId, ReportFilterRequest filter, CancellationToken ct = default)
    {
        var companyId = await GetCallerCompanyIdAsync(recruiterUserId, ct);
        var (jobs, applications) = await LoadCompanyDataAsync(companyId, filter, ct);

        var sb = new StringBuilder();
        sb.AppendLine("Title,Status,JobType,City,State,IsRemote,ApplicantCount,ViewCount,CreatedDate,PublishedDate");
        foreach (var j in jobs.OrderByDescending(j => j.CreatedAt))
        {
            sb.AppendLine(string.Join(",", new[]
            {
                CsvField(j.Title),
                CsvField(j.Status.ToString()),
                CsvField(j.JobType.ToString()),
                CsvField(j.City),
                CsvField(j.State),
                CsvField(j.IsRemote.ToString()),
                CsvField(applications.Count(a => a.JobPostingId == j.Id).ToString()),
                CsvField(j.ViewCount.ToString()),
                CsvField(j.CreatedAt.ToString("yyyy-MM-dd")),
                CsvField(j.PublishedAt?.ToString("yyyy-MM-dd") ?? ""),
            }));
        }

        await LogExportAsync(recruiterUserId, companyId, "Jobs", jobs.Count, ct);
        return sb.ToString();
    }

    public async Task<string> ExportApplicantsCsvAsync(int recruiterUserId, ReportFilterRequest filter, CancellationToken ct = default)
    {
        var companyId = await GetCallerCompanyIdAsync(recruiterUserId, ct);
        var (_, applications) = await LoadCompanyDataAsync(companyId, filter, ct);

        var sb = new StringBuilder();
        sb.AppendLine("CandidateName,JobTitle,Status,City,State,ExperienceYears,MatchScore,AppliedDate");
        foreach (var a in applications.OrderByDescending(a => a.CreatedAt))
        {
            var c = a.CandidateProfile;
            sb.AppendLine(string.Join(",", new[]
            {
                CsvField(c.User.FullName),
                CsvField(a.JobPosting.Title),
                CsvField(a.Status.ToString()),
                CsvField(c.City),
                CsvField(c.State),
                CsvField(c.TotalExperienceYears?.ToString() ?? ""),
                CsvField(a.MatchScore.HasValue ? ((int)a.MatchScore.Value).ToString() : ""),
                CsvField(a.CreatedAt.ToString("yyyy-MM-dd")),
            }));
        }

        await LogExportAsync(recruiterUserId, companyId, "Applicants", applications.Count, ct);
        return sb.ToString();
    }

    public async Task<string> ExportInterviewScheduleCsvAsync(int recruiterUserId, ReportFilterRequest filter, CancellationToken ct = default)
    {
        var companyId = await GetCallerCompanyIdAsync(recruiterUserId, ct);

        var query = _db.Interviews
            .Include(i => i.JobApplication).ThenInclude(a => a.JobPosting)
            .Include(i => i.JobApplication).ThenInclude(a => a.CandidateProfile).ThenInclude(c => c.User)
            .Where(i => i.JobApplication.JobPosting.CompanyId == companyId)
            .AsQueryable();

        if (filter.FromUtc.HasValue) query = query.Where(i => i.ScheduledStartUtc >= filter.FromUtc.Value);
        if (filter.ToUtc.HasValue) query = query.Where(i => i.ScheduledStartUtc <= filter.ToUtc.Value);

        var interviews = await query.OrderBy(i => i.ScheduledStartUtc).ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("CandidateName,JobTitle,ScheduledStartUtc,ScheduledEndUtc,Type,Status");
        foreach (var i in interviews)
        {
            sb.AppendLine(string.Join(",", new[]
            {
                CsvField(i.JobApplication.CandidateProfile.User.FullName),
                CsvField(i.JobApplication.JobPosting.Title),
                CsvField(i.ScheduledStartUtc.ToString("yyyy-MM-dd HH:mm")),
                CsvField(i.ScheduledEndUtc.ToString("yyyy-MM-dd HH:mm")),
                CsvField(i.Type.ToString()),
                CsvField(i.Status.ToString()),
            }));
        }

        await LogExportAsync(recruiterUserId, companyId, "InterviewSchedule", interviews.Count, ct);
        return sb.ToString();
    }

    public async Task<string> ExportFunnelSummaryCsvAsync(int recruiterUserId, ReportFilterRequest filter, CancellationToken ct = default)
    {
        var companyId = await GetCallerCompanyIdAsync(recruiterUserId, ct);
        var (_, applications) = await LoadCompanyDataAsync(companyId, filter, ct);

        var sb = new StringBuilder();
        sb.AppendLine("Status,Count");
        foreach (var status in Enum.GetValues<ApplicationStatus>())
        {
            sb.AppendLine($"{CsvField(status.ToString())},{applications.Count(a => a.Status == status)}");
        }

        await LogExportAsync(recruiterUserId, companyId, "FunnelSummary", applications.Count, ct);
        return sb.ToString();
    }

    private async Task<int> GetCallerCompanyIdAsync(int recruiterUserId, CancellationToken ct)
    {
        var companyId = await _db.RecruiterProfiles
            .Where(r => r.UserId == recruiterUserId)
            .Select(r => (int?)r.CompanyId)
            .FirstOrDefaultAsync(ct);

        return companyId ?? throw new ConflictException("NOT_ONBOARDED", "Complete company onboarding first.");
    }

    private async Task<(List<JobPosting> Jobs, List<JobApplication> Applications)> LoadCompanyDataAsync(int companyId, ReportFilterRequest filter, CancellationToken ct)
    {
        var jobsQuery = _db.JobPostings.Where(j => j.CompanyId == companyId).AsQueryable();
        if (filter.FromUtc.HasValue) jobsQuery = jobsQuery.Where(j => j.CreatedAt >= filter.FromUtc.Value);
        if (filter.ToUtc.HasValue) jobsQuery = jobsQuery.Where(j => j.CreatedAt <= filter.ToUtc.Value);
        var jobs = await jobsQuery.ToListAsync(ct);

        var applicationsQuery = _db.JobApplications
            .Include(a => a.CandidateProfile).ThenInclude(c => c.User)
            .Include(a => a.JobPosting)
            .Where(a => a.JobPosting.CompanyId == companyId)
            .AsQueryable();
        if (filter.FromUtc.HasValue) applicationsQuery = applicationsQuery.Where(a => a.CreatedAt >= filter.FromUtc.Value);
        if (filter.ToUtc.HasValue) applicationsQuery = applicationsQuery.Where(a => a.CreatedAt <= filter.ToUtc.Value);
        var applications = await applicationsQuery.ToListAsync(ct);

        return (jobs, applications);
    }

    private async Task LogExportAsync(int recruiterUserId, int companyId, string reportType, int rowCount, CancellationToken ct)
    {
        await _auditLog.LogAsync(recruiterUserId, "Recruiter", "ReportExported", "Company", companyId, new { ReportType = reportType, RowCount = rowCount }, ct);
    }

    private static string CsvField(string? value)
    {
        value ??= string.Empty;
        var needsQuoting = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        var escaped = value.Replace("\"", "\"\"");
        return needsQuoting ? $"\"{escaped}\"" : escaped;
    }
}
