using AIRecruiter.Application.DTOs.Admin;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class ModerationServiceTests
{
    private static ModerationService CreateSut(AppDbContext db) => new(db, TestServiceFactory.CreateAuditLog(db));

    [Fact]
    public async Task SubmitReportAsync_ExistingJob_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var reporter = new User { FullName = "Casey", Email = "casey@example.com", Role = UserRole.Candidate, PasswordHash = "x" };
        var recruiter = new User { FullName = "Rita", Email = "rita@example.com", Role = UserRole.Recruiter, PasswordHash = "x" };
        db.Users.AddRange(reporter, recruiter);
        await db.SaveChangesAsync();
        var company = new Company { Name = "Acme" };
        var profile = new RecruiterProfile { UserId = recruiter.Id, Company = company };
        db.RecruiterProfiles.Add(profile);
        await db.SaveChangesAsync();
        var job = new JobPosting { Title = "Role", Description = "d", CompanyId = company.Id, RecruiterProfileId = profile.Id };
        db.JobPostings.Add(job);
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        await sut.SubmitReportAsync(reporter.Id, new SubmitReportRequest(ReportedEntityType.Job, job.Id, ReportReason.FraudulentJob, "Looks fake"));

        Assert.Single(db.Reports);
    }

    [Fact]
    public async Task SubmitReportAsync_NonExistentEntity_ThrowsNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var reporter = new User { FullName = "Casey", Email = "casey@example.com", Role = UserRole.Candidate, PasswordHash = "x" };
        db.Users.Add(reporter);
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            sut.SubmitReportAsync(reporter.Id, new SubmitReportRequest(ReportedEntityType.Job, 9999, ReportReason.Spam, null)));
    }

    [Fact]
    public async Task SubmitReportAsync_DetailsTooLong_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var reporter = new User { FullName = "Casey", Email = "casey@example.com", Role = UserRole.Candidate, PasswordHash = "x" };
        db.Users.Add(reporter);
        await db.SaveChangesAsync();
        var sut = CreateSut(db);
        var tooLong = new string('a', 1001);

        await Assert.ThrowsAsync<ValidationException>(() =>
            sut.SubmitReportAsync(reporter.Id, new SubmitReportRequest(ReportedEntityType.User, reporter.Id, ReportReason.Other, tooLong)));
    }

    [Fact]
    public async Task SubmitReportAsync_ReportAnotherUser_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var reporter = new User { FullName = "Casey", Email = "casey@example.com", Role = UserRole.Candidate, PasswordHash = "x" };
        var target = new User { FullName = "Suspicious User", Email = "suspicious@example.com", Role = UserRole.Recruiter, PasswordHash = "x" };
        db.Users.AddRange(reporter, target);
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        await sut.SubmitReportAsync(reporter.Id, new SubmitReportRequest(ReportedEntityType.User, target.Id, ReportReason.Harassment, "Sent inappropriate messages"));

        var report = Assert.Single(db.Reports);
        Assert.Equal(ReportedEntityType.User, report.EntityType);
        Assert.Equal(ReportStatus.Open, report.Status);
    }
}
