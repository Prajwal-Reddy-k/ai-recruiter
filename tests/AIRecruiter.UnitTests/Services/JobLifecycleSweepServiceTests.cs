using AIRecruiter.Application.Interfaces;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIRecruiter.UnitTests.Services;

/// <summary>Drives the real BackgroundService lifecycle (StartAsync runs the sweep
/// immediately, StopAsync cancels the PeriodicTimer wait) so the test never has to wait for
/// the real 15-minute interval and doesn't need any test-only hook on the service itself.</summary>
public class JobLifecycleSweepServiceTests
{
    private static ServiceProvider BuildProvider(AppDbContext db)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        return services.BuildServiceProvider();
    }

    private static async Task RunOneSweepAsync(AppDbContext db)
    {
        using var provider = BuildProvider(db);
        var sut = new JobLifecycleSweepService(provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<JobLifecycleSweepService>.Instance);

        await sut.StartAsync(CancellationToken.None);
        await Task.Delay(1000);
        await sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Sweep_ClosesExpiredOpenJobs()
    {
        using var db = TestDbContextFactory.Create();
        var recruiterUser = new User { FullName = "Rita", Email = "rita@example.com", Role = UserRole.Recruiter, PasswordHash = "x" };
        db.Users.Add(recruiterUser);
        await db.SaveChangesAsync();
        var company = new Company { Name = "Acme" };
        var recruiterProfile = new RecruiterProfile { UserId = recruiterUser.Id, Company = company };
        db.RecruiterProfiles.Add(recruiterProfile);
        await db.SaveChangesAsync();
        var job = new JobPosting
        {
            Title = "Stale Role",
            Description = "d",
            CompanyId = company.Id,
            RecruiterProfileId = recruiterProfile.Id,
            Status = JobStatus.Open,
            ApplicationDeadlineUtc = DateTime.UtcNow.AddDays(-1),
        };
        db.JobPostings.Add(job);
        await db.SaveChangesAsync();

        await RunOneSweepAsync(db);

        var reloaded = db.JobPostings.First(j => j.Id == job.Id);
        Assert.Equal(JobStatus.Closed, reloaded.Status);
    }

    [Fact]
    public async Task Sweep_ExpiresStaleInvitations()
    {
        using var db = TestDbContextFactory.Create();
        var recruiterUser = new User { FullName = "Rita", Email = "rita@example.com", Role = UserRole.Recruiter, PasswordHash = "x" };
        var candidateUser = new User { FullName = "Casey", Email = "casey@example.com", Role = UserRole.Candidate, PasswordHash = "x" };
        db.Users.AddRange(recruiterUser, candidateUser);
        await db.SaveChangesAsync();
        var company = new Company { Name = "Acme" };
        var recruiterProfile = new RecruiterProfile { UserId = recruiterUser.Id, Company = company };
        db.RecruiterProfiles.Add(recruiterProfile);
        var candidate = new CandidateProfile { UserId = candidateUser.Id };
        db.CandidateProfiles.Add(candidate);
        await db.SaveChangesAsync();
        var job = new JobPosting { Title = "Role", Description = "d", CompanyId = company.Id, RecruiterProfileId = recruiterProfile.Id, Status = JobStatus.Open };
        db.JobPostings.Add(job);
        await db.SaveChangesAsync();
        var invitation = new Invitation
        {
            JobPostingId = job.Id,
            CandidateProfileId = candidate.Id,
            InvitedByUserId = recruiterUser.Id,
            Status = InvitationStatus.Sent,
            SentAtUtc = DateTime.UtcNow.AddDays(-20),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-6),
        };
        db.Invitations.Add(invitation);
        await db.SaveChangesAsync();

        await RunOneSweepAsync(db);

        var reloaded = db.Invitations.First(i => i.Id == invitation.Id);
        Assert.Equal(InvitationStatus.Expired, reloaded.Status);
    }

    [Fact]
    public async Task Sweep_DoesNotCloseJobsWithoutDeadlineOrNotYetExpired()
    {
        using var db = TestDbContextFactory.Create();
        var recruiterUser = new User { FullName = "Rita", Email = "rita@example.com", Role = UserRole.Recruiter, PasswordHash = "x" };
        db.Users.Add(recruiterUser);
        await db.SaveChangesAsync();
        var company = new Company { Name = "Acme" };
        var recruiterProfile = new RecruiterProfile { UserId = recruiterUser.Id, Company = company };
        db.RecruiterProfiles.Add(recruiterProfile);
        await db.SaveChangesAsync();
        var noDeadlineJob = new JobPosting { Title = "Evergreen Role", Description = "d", CompanyId = company.Id, RecruiterProfileId = recruiterProfile.Id, Status = JobStatus.Open };
        var futureDeadlineJob = new JobPosting { Title = "Future Role", Description = "d", CompanyId = company.Id, RecruiterProfileId = recruiterProfile.Id, Status = JobStatus.Open, ApplicationDeadlineUtc = DateTime.UtcNow.AddDays(10) };
        db.JobPostings.AddRange(noDeadlineJob, futureDeadlineJob);
        await db.SaveChangesAsync();

        await RunOneSweepAsync(db);

        Assert.Equal(JobStatus.Open, db.JobPostings.First(j => j.Id == noDeadlineJob.Id).Status);
        Assert.Equal(JobStatus.Open, db.JobPostings.First(j => j.Id == futureDeadlineJob.Id).Status);
    }
}
