using AIRecruiter.Application.Interfaces;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AIRecruiter.Infrastructure.Services;

/// <summary>The only background/scheduled task in the app (none existed before this).
/// Every 15 minutes: auto-closes Open jobs past their application deadline (the "in-app
/// reminder" is the recruiter notification this creates — no email/SMS), expires stale
/// Sent/Viewed invitations, and deactivates accounts whose self-requested deletion grace
/// period has elapsed without being cancelled. A plain PeriodicTimer inside a
/// BackgroundService — no external scheduler dependency, zero-cost.</summary>
public class JobLifecycleSweepService : BackgroundService
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan DeletionGracePeriod = TimeSpan.FromDays(14);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobLifecycleSweepService> _logger;

    public JobLifecycleSweepService(IServiceScopeFactory scopeFactory, ILogger<JobLifecycleSweepService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(SweepInterval);

        // Run once immediately on startup, then on every subsequent tick.
        do
        {
            try
            {
                await RunSweepAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Job lifecycle sweep failed — will retry on the next interval.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunSweepAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var auditLog = scope.ServiceProvider.GetRequiredService<IAuditLogService>();

        var now = DateTime.UtcNow;

        var expiredJobs = await db.JobPostings
            .Include(j => j.RecruiterProfile)
            .Where(j => j.Status == JobStatus.Open && j.ApplicationDeadlineUtc != null && j.ApplicationDeadlineUtc < now)
            .ToListAsync(ct);

        foreach (var job in expiredJobs)
        {
            job.Status = JobStatus.Closed;
            job.UpdatedAt = now;

            await auditLog.LogAsync(null, "System", "JobAutoExpired", "JobPosting", job.Id, new { job.Title }, ct);
            await notifications.NotifyAsync(
                job.RecruiterProfile.UserId,
                "JobExpired",
                $"Your job posting \"{job.Title}\" reached its application deadline and was automatically closed.",
                "JobPosting", job.Id, ct);
        }

        var expiredInvitations = await db.Invitations
            .Where(i => (i.Status == InvitationStatus.Sent || i.Status == InvitationStatus.Viewed) && i.ExpiresAtUtc < now)
            .ToListAsync(ct);

        foreach (var invitation in expiredInvitations)
        {
            invitation.Status = InvitationStatus.Expired;
            invitation.UpdatedAt = now;
        }

        var deletionCutoff = now - DeletionGracePeriod;
        var usersToDeactivate = await db.Users
            .Where(u => u.IsActive && u.DeletionRequestedAt != null && u.DeletionRequestedAt <= deletionCutoff)
            .ToListAsync(ct);

        foreach (var user in usersToDeactivate)
        {
            user.IsActive = false;
            // Same mechanism the old immediate-delete flow used, and the same one
            // AdminService.SuspendUserAsync uses — signs the account out everywhere.
            user.SecurityStamp = Guid.NewGuid().ToString("N");
            await auditLog.LogAsync(null, "System", "AccountDeleted", "User", user.Id, null, ct);
        }

        if (expiredJobs.Count > 0 || expiredInvitations.Count > 0 || usersToDeactivate.Count > 0)
        {
            await db.SaveChangesAsync(ct);
            _logger.LogInformation(
                "Job lifecycle sweep closed {JobCount} expired jobs, expired {InvitationCount} invitations, and deactivated {DeletionCount} accounts past their deletion grace period.",
                expiredJobs.Count, expiredInvitations.Count, usersToDeactivate.Count);
        }
    }
}
