using AIRecruiter.Application.DTOs.Admin;
using AIRecruiter.Application.DTOs.Jobs;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Infrastructure.Mapping;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly AppDbContext _db;
    private readonly IAuditLogService _auditLog;

    public AdminService(AppDbContext db, IAuditLogService auditLog)
    {
        _db = db;
        _auditLog = auditLog;
    }

    public async Task<IReadOnlyList<AdminUserDto>> GetUsersAsync(CancellationToken ct = default)
    {
        return await _db.Users
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new AdminUserDto(u.Id, u.FullName, u.Email, u.Role.ToString(), u.IsActive, u.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AdminCompanyDto>> GetCompaniesAsync(CancellationToken ct = default)
    {
        return await _db.Companies
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new AdminCompanyDto(
                c.Id, c.Name, c.Industry,
                c.JobPostings.Count, c.Recruiters.Count, c.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AdminJobDto>> GetJobsAsync(CancellationToken ct = default)
    {
        return await _db.JobPostings
            .Include(j => j.Company)
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => new AdminJobDto(
                j.Id, j.Title, j.Company.Name, j.Status.ToString(), j.ModerationStatus.ToString(),
                j.Applications.Count, j.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<JobReportDto>> GetReportsAsync(CancellationToken ct = default)
    {
        var reports = await _db.JobReports
            .Include(r => r.JobPosting)
            .Include(r => r.ReportedByUser)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        return reports.Select(r => new JobReportDto(
            r.Id, r.JobPostingId, r.JobPosting.Title, r.ReportedByUser.FullName,
            r.Reason, r.Status.ToString(), r.ResolutionNote, r.CreatedAt)).ToList();
    }

    public async Task<JobPostingDto> ModerateJobAsync(int adminUserId, int jobId, ModerateJobRequest request, CancellationToken ct = default)
    {
        var job = await _db.JobPostings.Include(j => j.Company).FirstOrDefaultAsync(j => j.Id == jobId, ct)
            ?? throw new NotFoundException("Job posting not found.");

        job.ModerationStatus = request.ModerationStatus;
        job.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(adminUserId, "Admin", "JobModerated", "JobPosting", job.Id, new { job.Title, ModerationStatus = job.ModerationStatus.ToString() }, ct);

        return JobPostingMapper.ToDto(job);
    }

    public async Task ResolveReportAsync(int adminUserId, int reportId, ResolveReportRequest request, CancellationToken ct = default)
    {
        var report = await _db.JobReports.FirstOrDefaultAsync(r => r.Id == reportId, ct)
            ?? throw new NotFoundException("Report not found.");

        report.Status = request.Status;
        report.ResolutionNote = request.ResolutionNote;
        report.ReviewedByUserId = adminUserId;
        report.ReviewedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(adminUserId, "Admin", "ReportResolved", "JobReport", report.Id, new { Status = report.Status.ToString() }, ct);
    }
}
