using AIRecruiter.Application.DTOs.Admin;
using AIRecruiter.Application.DTOs.Jobs;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
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

    public async Task<IReadOnlyList<ReportDto>> GetReportsAsync(CancellationToken ct = default)
    {
        var reports = await _db.Reports
            .Include(r => r.ReportedByUser)
            .Include(r => r.ReviewedByUser)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        var labels = await ResolveEntityLabelsAsync(reports, ct);

        return reports.Select(r => new ReportDto(
            r.Id,
            r.EntityType.ToString(),
            r.EntityId,
            labels.GetValueOrDefault((r.EntityType, r.EntityId)),
            r.ReportedByUser.FullName,
            r.Reason.ToString(),
            r.Details,
            r.Status.ToString(),
            r.ModerationNote,
            r.ReviewedByUser?.FullName,
            r.ReviewedAt,
            r.CreatedAt)).ToList();
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

    public async Task SetReportStatusAsync(int adminUserId, int reportId, SetReportStatusRequest request, CancellationToken ct = default)
    {
        var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == reportId, ct)
            ?? throw new NotFoundException("Report not found.");

        report.Status = request.Status;
        if (!string.IsNullOrWhiteSpace(request.Note))
        {
            report.ModerationNote = request.Note.Trim();
        }
        report.ReviewedByUserId = adminUserId;
        report.ReviewedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(adminUserId, "Admin", "ReportStatusChanged", "Report", report.Id, new { Status = report.Status.ToString() }, ct);
    }

    public async Task AddReportNoteAsync(int adminUserId, int reportId, AddReportNoteRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Note))
        {
            throw new ValidationException("Note cannot be empty.", new Dictionary<string, string> { ["note"] = "Note cannot be empty." });
        }

        var report = await _db.Reports.FirstOrDefaultAsync(r => r.Id == reportId, ct)
            ?? throw new NotFoundException("Report not found.");

        report.ModerationNote = request.Note.Trim();
        report.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(adminUserId, "Admin", "ReportNoteAdded", "Report", report.Id, null, ct);
    }

    public async Task SuspendUserAsync(int adminUserId, int userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("User not found.");

        if (user.Id == adminUserId)
        {
            throw new ValidationException("You cannot suspend your own account.");
        }
        if (user.Role == UserRole.Admin)
        {
            throw new ValidationException("Admin accounts cannot be suspended.");
        }

        user.IsActive = false;
        // Rotating the security stamp invalidates every JWT already issued to this user —
        // the same mechanism a password reset uses — so suspension takes effect immediately
        // on their very next request, not just on their next login attempt.
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(adminUserId, "Admin", "UserSuspended", "User", user.Id, new { user.Email }, ct);
    }

    public async Task ReactivateUserAsync(int adminUserId, int userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("User not found.");

        user.IsActive = true;
        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(adminUserId, "Admin", "UserReactivated", "User", user.Id, new { user.Email }, ct);
    }

    private async Task<Dictionary<(ReportedEntityType, int), string>> ResolveEntityLabelsAsync(List<Report> reports, CancellationToken ct)
    {
        var labels = new Dictionary<(ReportedEntityType, int), string>();

        var jobIds = reports.Where(r => r.EntityType == ReportedEntityType.Job).Select(r => r.EntityId).Distinct().ToList();
        if (jobIds.Count > 0)
        {
            var jobs = await _db.JobPostings.Where(j => jobIds.Contains(j.Id)).Select(j => new { j.Id, j.Title }).ToListAsync(ct);
            foreach (var j in jobs) labels[(ReportedEntityType.Job, j.Id)] = j.Title;
        }

        var companyIds = reports.Where(r => r.EntityType == ReportedEntityType.Company).Select(r => r.EntityId).Distinct().ToList();
        if (companyIds.Count > 0)
        {
            var companies = await _db.Companies.Where(c => companyIds.Contains(c.Id)).Select(c => new { c.Id, c.Name }).ToListAsync(ct);
            foreach (var c in companies) labels[(ReportedEntityType.Company, c.Id)] = c.Name;
        }

        var userIds = reports.Where(r => r.EntityType == ReportedEntityType.User).Select(r => r.EntityId).Distinct().ToList();
        if (userIds.Count > 0)
        {
            var users = await _db.Users.Where(u => userIds.Contains(u.Id)).Select(u => new { u.Id, u.FullName }).ToListAsync(ct);
            foreach (var u in users) labels[(ReportedEntityType.User, u.Id)] = u.FullName;
        }

        var messageIds = reports.Where(r => r.EntityType == ReportedEntityType.Message).Select(r => r.EntityId).Distinct().ToList();
        if (messageIds.Count > 0)
        {
            var messages = await _db.Messages.Where(m => messageIds.Contains(m.Id)).Select(m => new { m.Id, m.Body }).ToListAsync(ct);
            foreach (var m in messages) labels[(ReportedEntityType.Message, m.Id)] = m.Body.Length > 60 ? m.Body[..60] + "…" : m.Body;
        }

        return labels;
    }
}
