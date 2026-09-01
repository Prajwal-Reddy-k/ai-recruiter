using AIRecruiter.Application.DTOs.Jobs;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class JobPostingServiceTests
{
    private static async Task<(User Recruiter, JobPosting Job)> SeedAsync(AppDbContext db)
    {
        var recruiterUser = new User { FullName = "Rita Recruiter", Email = "rita@example.com", Role = UserRole.Recruiter };
        db.Users.Add(recruiterUser);
        await db.SaveChangesAsync();

        var company = new Company { Name = "Acme Corp" };
        var recruiterProfile = new RecruiterProfile { UserId = recruiterUser.Id, Company = company };
        db.RecruiterProfiles.Add(recruiterProfile);
        await db.SaveChangesAsync();

        var job = new JobPosting
        {
            Title = "Backend Engineer",
            Description = "role",
            Status = JobStatus.Open,
            CompanyId = company.Id,
            RecruiterProfileId = recruiterProfile.Id,
        };
        db.JobPostings.Add(job);
        await db.SaveChangesAsync();

        return (recruiterUser, job);
    }

    [Fact]
    public async Task UpdateStatusAsync_OwningRecruiter_ClosesJob()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, job) = await SeedAsync(db);
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup());

        var updated = await sut.UpdateStatusAsync(recruiter.Id, job.Id, new UpdateJobStatusRequest(JobStatus.Closed));

        Assert.Equal("Closed", updated.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_NonOwningRecruiter_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (_, job) = await SeedAsync(db);
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup());

        var otherRecruiterUser = new User { FullName = "Other Recruiter", Email = "other@example.com", Role = UserRole.Recruiter };
        db.Users.Add(otherRecruiterUser);
        await db.SaveChangesAsync();
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = otherRecruiterUser.Id, Company = new Company { Name = "Other Co" } });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(
            () => sut.UpdateStatusAsync(otherRecruiterUser.Id, job.Id, new UpdateJobStatusRequest(JobStatus.Closed)));
    }

    [Fact]
    public async Task UpdateStatusAsync_UnknownJob_ThrowsNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, _) = await SeedAsync(db);
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup());

        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.UpdateStatusAsync(recruiter.Id, 9999, new UpdateJobStatusRequest(JobStatus.Closed)));
    }

    [Fact]
    public async Task UpdateStatusAsync_ArchivedToOpen_ThrowsConflict_InvalidTransition()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, job) = await SeedAsync(db);
        job.Status = JobStatus.Archived;
        await db.SaveChangesAsync();
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup());

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => sut.UpdateStatusAsync(recruiter.Id, job.Id, new UpdateJobStatusRequest(JobStatus.Open)));
        Assert.Equal("INVALID_TRANSITION", ex.ErrorCode);
    }

    [Fact]
    public async Task UpdateStatusAsync_DraftToOpen_SetsPublishedAt()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, job) = await SeedAsync(db);
        job.Status = JobStatus.Draft;
        job.City = "Bengaluru";
        job.State = "Karnataka";
        await db.SaveChangesAsync();
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup());

        var updated = await sut.UpdateStatusAsync(recruiter.Id, job.Id, new UpdateJobStatusRequest(JobStatus.Open));

        Assert.Equal("Open", updated.Status);
        Assert.NotNull(updated.PublishedAt);
    }

    [Fact]
    public async Task DuplicateAsync_OwningRecruiter_CreatesDraftCopy()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, job) = await SeedAsync(db);
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup());

        var copy = await sut.DuplicateAsync(recruiter.Id, job.Id);

        Assert.NotEqual(job.Id, copy.Id);
        Assert.Equal("Draft", copy.Status);
        Assert.Contains("Copy", copy.Title);
    }

    [Fact]
    public async Task DuplicateAsync_NonOwningRecruiter_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (_, job) = await SeedAsync(db);
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup());

        var otherRecruiterUser = new User { FullName = "Other Recruiter 2", Email = "other2@example.com", Role = UserRole.Recruiter };
        db.Users.Add(otherRecruiterUser);
        await db.SaveChangesAsync();
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = otherRecruiterUser.Id, Company = new Company { Name = "Other Co 2" } });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.DuplicateAsync(otherRecruiterUser.Id, job.Id));
    }

    [Fact]
    public async Task GetByIdAsync_SameVisitorWithinWindow_CountsOnce()
    {
        using var db = TestDbContextFactory.Create();
        var (_, job) = await SeedAsync(db);
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup());

        await sut.GetByIdAsync(job.Id, "visitor-1", null);
        var second = await sut.GetByIdAsync(job.Id, "visitor-1", null);

        Assert.Equal(1, second!.ViewCount);
    }

    [Fact]
    public async Task GetByIdAsync_DifferentVisitors_CountsEach()
    {
        using var db = TestDbContextFactory.Create();
        var (_, job) = await SeedAsync(db);
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup());

        await sut.GetByIdAsync(job.Id, "visitor-1", null);
        var second = await sut.GetByIdAsync(job.Id, "visitor-2", null);

        Assert.Equal(2, second!.ViewCount);
    }

    [Fact]
    public async Task GetByIdAsync_OwningRecruiterViewingOwnJob_DoesNotCount()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, job) = await SeedAsync(db);
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup());

        var result = await sut.GetByIdAsync(job.Id, $"u:{recruiter.Id}", recruiter.Id);

        Assert.Equal(0, result!.ViewCount);
    }

    [Fact]
    public async Task GetByIdAsync_HiddenJob_NonOwnerNonAdmin_ReturnsNull()
    {
        using var db = TestDbContextFactory.Create();
        var (_, job) = await SeedAsync(db);
        job.ModerationStatus = ModerationStatus.Hidden;
        await db.SaveChangesAsync();
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup());

        var result = await sut.GetByIdAsync(job.Id, "visitor-1", null);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_HiddenJob_AdminViewer_ReturnsJob()
    {
        using var db = TestDbContextFactory.Create();
        var (_, job) = await SeedAsync(db);
        job.ModerationStatus = ModerationStatus.Hidden;
        await db.SaveChangesAsync();
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup());

        var result = await sut.GetByIdAsync(job.Id, "admin-1", null, isAdminViewer: true);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetByIdAsync_HiddenJob_OwningRecruiter_ReturnsJob()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, job) = await SeedAsync(db);
        job.ModerationStatus = ModerationStatus.Hidden;
        await db.SaveChangesAsync();
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup());

        var result = await sut.GetByIdAsync(job.Id, $"u:{recruiter.Id}", recruiter.Id);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task ExtendDeadlineAsync_OwningRecruiter_FutureDeadline_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, job) = await SeedAsync(db);
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup());
        var deadline = DateTime.UtcNow.AddDays(14);

        var updated = await sut.ExtendDeadlineAsync(recruiter.Id, job.Id, deadline);

        Assert.NotNull(updated.ApplicationDeadlineUtc);
    }

    [Fact]
    public async Task ExtendDeadlineAsync_PastDeadline_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, job) = await SeedAsync(db);
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup());

        await Assert.ThrowsAsync<ValidationException>(
            () => sut.ExtendDeadlineAsync(recruiter.Id, job.Id, DateTime.UtcNow.AddDays(-1)));
    }

    [Fact]
    public async Task ExtendDeadlineAsync_NonOwningRecruiter_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (_, job) = await SeedAsync(db);
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup());

        var otherRecruiterUser = new User { FullName = "Other Recruiter 3", Email = "other3@example.com", Role = UserRole.Recruiter };
        db.Users.Add(otherRecruiterUser);
        await db.SaveChangesAsync();
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = otherRecruiterUser.Id, Company = new Company { Name = "Other Co 3" } });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(
            () => sut.ExtendDeadlineAsync(otherRecruiterUser.Id, job.Id, DateTime.UtcNow.AddDays(7)));
    }
}
