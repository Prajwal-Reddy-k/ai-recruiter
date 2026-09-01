using AIRecruiter.Application.DTOs.Admin;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class AdminServiceTests
{
    private static AdminService CreateSut(AppDbContext db) => new(db, TestServiceFactory.CreateAuditLog(db));

    private static async Task<(User Admin, User Recruiter, User Candidate)> SeedAsync(AppDbContext db)
    {
        var admin = new User { FullName = "Ops Admin", Email = "admin@example.com", Role = UserRole.Admin, PasswordHash = "x", SecurityStamp = "s0" };
        var recruiter = new User { FullName = "Rita Recruiter", Email = "rita@example.com", Role = UserRole.Recruiter, PasswordHash = "x", SecurityStamp = "s1" };
        var candidate = new User { FullName = "Casey Candidate", Email = "casey@example.com", Role = UserRole.Candidate, PasswordHash = "x", SecurityStamp = "s2" };
        db.Users.AddRange(admin, recruiter, candidate);
        await db.SaveChangesAsync();
        return (admin, recruiter, candidate);
    }

    [Fact]
    public async Task SuspendUserAsync_ActiveUser_SetsInactiveAndRotatesStamp()
    {
        using var db = TestDbContextFactory.Create();
        var (admin, _, candidate) = await SeedAsync(db);
        var sut = CreateSut(db);
        var originalStamp = candidate.SecurityStamp;

        await sut.SuspendUserAsync(admin.Id, candidate.Id);

        var reloaded = db.Users.First(u => u.Id == candidate.Id);
        Assert.False(reloaded.IsActive);
        Assert.NotEqual(originalStamp, reloaded.SecurityStamp);
    }

    [Fact]
    public async Task SuspendUserAsync_AdminAccount_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var (admin, _, _) = await SeedAsync(db);
        var otherAdmin = new User { FullName = "Other Admin", Email = "other-admin@example.com", Role = UserRole.Admin, PasswordHash = "x" };
        db.Users.Add(otherAdmin);
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<ValidationException>(() => sut.SuspendUserAsync(admin.Id, otherAdmin.Id));
    }

    [Fact]
    public async Task SuspendUserAsync_Self_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var (admin, _, _) = await SeedAsync(db);
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<ValidationException>(() => sut.SuspendUserAsync(admin.Id, admin.Id));
    }

    [Fact]
    public async Task ReactivateUserAsync_SuspendedUser_SetsActive()
    {
        using var db = TestDbContextFactory.Create();
        var (admin, _, candidate) = await SeedAsync(db);
        var sut = CreateSut(db);
        await sut.SuspendUserAsync(admin.Id, candidate.Id);

        await sut.ReactivateUserAsync(admin.Id, candidate.Id);

        Assert.True(db.Users.First(u => u.Id == candidate.Id).IsActive);
    }

    [Fact]
    public async Task GetReportsAsync_ResolvesEntityLabelForEachType()
    {
        using var db = TestDbContextFactory.Create();
        var (admin, recruiter, candidate) = await SeedAsync(db);
        var company = new Company { Name = "Acme Corp" };
        var recruiterProfile = new RecruiterProfile { UserId = recruiter.Id, Company = company };
        db.RecruiterProfiles.Add(recruiterProfile);
        await db.SaveChangesAsync();
        var job = new JobPosting { Title = "Backend Engineer", Description = "role", CompanyId = company.Id, RecruiterProfileId = recruiterProfile.Id };
        db.JobPostings.Add(job);
        await db.SaveChangesAsync();

        db.Reports.Add(new Report { EntityType = ReportedEntityType.Job, EntityId = job.Id, ReportedByUserId = candidate.Id, Reason = ReportReason.Spam });
        db.Reports.Add(new Report { EntityType = ReportedEntityType.Company, EntityId = company.Id, ReportedByUserId = candidate.Id, Reason = ReportReason.FakeCompany });
        db.Reports.Add(new Report { EntityType = ReportedEntityType.User, EntityId = recruiter.Id, ReportedByUserId = candidate.Id, Reason = ReportReason.Harassment });
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var reports = await sut.GetReportsAsync();

        Assert.Contains(reports, r => r.EntityType == "Job" && r.EntityLabel == "Backend Engineer");
        Assert.Contains(reports, r => r.EntityType == "Company" && r.EntityLabel == "Acme Corp");
        Assert.Contains(reports, r => r.EntityType == "User" && r.EntityLabel == "Rita Recruiter");
    }

    [Fact]
    public async Task SetReportStatusAsync_UpdatesStatusAndReviewer()
    {
        using var db = TestDbContextFactory.Create();
        var (admin, _, candidate) = await SeedAsync(db);
        var report = new Report { EntityType = ReportedEntityType.User, EntityId = candidate.Id, ReportedByUserId = candidate.Id, Reason = ReportReason.Other };
        db.Reports.Add(report);
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        await sut.SetReportStatusAsync(admin.Id, report.Id, new SetReportStatusRequest(ReportStatus.UnderReview, "Looking into it"));

        var reloaded = db.Reports.First(r => r.Id == report.Id);
        Assert.Equal(ReportStatus.UnderReview, reloaded.Status);
        Assert.Equal("Looking into it", reloaded.ModerationNote);
        Assert.Equal(admin.Id, reloaded.ReviewedByUserId);
    }

    [Fact]
    public async Task AddReportNoteAsync_DoesNotChangeStatus()
    {
        using var db = TestDbContextFactory.Create();
        var (admin, _, candidate) = await SeedAsync(db);
        var report = new Report { EntityType = ReportedEntityType.User, EntityId = candidate.Id, ReportedByUserId = candidate.Id, Reason = ReportReason.Other, Status = ReportStatus.Open };
        db.Reports.Add(report);
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        await sut.AddReportNoteAsync(admin.Id, report.Id, new AddReportNoteRequest("Internal note only"));

        var reloaded = db.Reports.First(r => r.Id == report.Id);
        Assert.Equal(ReportStatus.Open, reloaded.Status);
        Assert.Equal("Internal note only", reloaded.ModerationNote);
    }
}
