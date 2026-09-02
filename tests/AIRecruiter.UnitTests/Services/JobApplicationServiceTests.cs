using AIRecruiter.Application.DTOs.Matching;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Application.Validation;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Options;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;
using Microsoft.Extensions.Options;
using Moq;

namespace AIRecruiter.UnitTests.Services;

public class JobApplicationServiceTests
{
    private static JobApplicationService CreateSut(AppDbContext db, IResumeMatchingService? matcher = null)
    {
        matcher ??= Mock.Of<IResumeMatchingService>(m =>
            m.CalculateMatch(It.IsAny<string>(), It.IsAny<JobMatchInput>(), It.IsAny<CandidateMatchInput>())
            == new ResumeMatchResult(70, new List<string> { "C#" }, new List<string>(), new List<string>(), "explanation"));

        var resumeStorage = new Mock<IResumeStorage>();
        var extractorFactory = new Mock<IResumeTextExtractorFactory>();
        var validator = new ResumeFileValidator();
        var candidateProfileService = TestServiceFactory.CreateCandidateProfileService(
            db, resumeStorage.Object, extractorFactory.Object, validator);

        return new JobApplicationService(
            db, matcher, candidateProfileService, TestServiceFactory.CreateNotifications(db), TestServiceFactory.CreateAuditLog(db));
    }

    private static async Task<(User Candidate, User Recruiter, JobPosting Job, CandidateProfile Profile)> SeedAsync(AppDbContext db, string? resumeText = "C# developer")
    {
        var candidateUser = new User { FullName = "Casey Candidate", Email = "casey@example.com", Role = UserRole.Candidate };
        var recruiterUser = new User { FullName = "Rita Recruiter", Email = "rita@example.com", Role = UserRole.Recruiter };
        db.Users.AddRange(candidateUser, recruiterUser);
        await db.SaveChangesAsync();

        var company = new Company { Name = "Acme Corp" };
        var recruiterProfile = new RecruiterProfile { UserId = recruiterUser.Id, Company = company };
        db.RecruiterProfiles.Add(recruiterProfile);

        var candidateProfile = new CandidateProfile
        {
            UserId = candidateUser.Id,
            ResumeExtractedText = resumeText,
        };
        db.CandidateProfiles.Add(candidateProfile);
        await db.SaveChangesAsync();

        var job = new JobPosting
        {
            Title = "Backend Engineer",
            Description = "C# role",
            RequiredSkillsCsv = "C#",
            Status = JobStatus.Open,
            CompanyId = company.Id,
            RecruiterProfileId = recruiterProfile.Id,
        };
        db.JobPostings.Add(job);
        await db.SaveChangesAsync();

        return (candidateUser, recruiterUser, job, candidateProfile);
    }

    [Fact]
    public async Task ApplyAsync_WithResumeOnFile_ComputesAndStoresMatchScore()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var sut = CreateSut(db);

        var result = await sut.ApplyAsync(candidate.Id, job.Id, "Excited to apply");

        Assert.Equal(70, result.MatchScore);
    }

    [Fact]
    public async Task ApplyAsync_CoverNoteExceedsMaxLength_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var sut = CreateSut(db);

        var tooLong = new string('a', 4001);
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            sut.ApplyAsync(candidate.Id, job.Id, tooLong));

        Assert.True(ex.FieldErrors!.ContainsKey("coverNote"));
    }

    [Fact]
    public async Task ApplyAsync_NoResumeOnFile_AppliesWithoutScore()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db, resumeText: null);
        var sut = CreateSut(db);

        var result = await sut.ApplyAsync(candidate.Id, job.Id, null);

        Assert.Null(result.MatchScore);
    }

    [Fact]
    public async Task ApplyAsync_SecondApplicationToSameJob_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var sut = CreateSut(db);

        await sut.ApplyAsync(candidate.Id, job.Id, null);

        await Assert.ThrowsAsync<ConflictException>(() => sut.ApplyAsync(candidate.Id, job.Id, null));
    }

    [Fact]
    public async Task GetApplicationDetailAsync_OwningCandidate_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var sut = CreateSut(db);
        var application = await sut.ApplyAsync(candidate.Id, job.Id, null);

        var detail = await sut.GetApplicationDetailAsync(candidate.Id, "Candidate", application.Id);

        Assert.Equal(application.Id, detail.Id);
    }

    [Fact]
    public async Task GetApplicationDetailAsync_OwningRecruiter_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, recruiter, job, _) = await SeedAsync(db);
        var sut = CreateSut(db);
        var application = await sut.ApplyAsync(candidate.Id, job.Id, null);

        var detail = await sut.GetApplicationDetailAsync(recruiter.Id, "Recruiter", application.Id);

        Assert.Equal(application.Id, detail.Id);
    }

    [Fact]
    public async Task GetApplicationDetailAsync_WrongCandidate_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var sut = CreateSut(db);
        var application = await sut.ApplyAsync(candidate.Id, job.Id, null);

        var otherCandidate = new User { FullName = "Other Candidate", Email = "other@example.com", Role = UserRole.Candidate };
        db.Users.Add(otherCandidate);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(
            () => sut.GetApplicationDetailAsync(otherCandidate.Id, "Candidate", application.Id));
    }

    [Fact]
    public async Task GetApplicationDetailAsync_NonOwningRecruiter_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var sut = CreateSut(db);
        var application = await sut.ApplyAsync(candidate.Id, job.Id, null);

        var otherRecruiterUser = new User { FullName = "Other Recruiter", Email = "otherrec@example.com", Role = UserRole.Recruiter };
        db.Users.Add(otherRecruiterUser);
        await db.SaveChangesAsync();
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = otherRecruiterUser.Id, Company = new Company { Name = "Other Co" } });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(
            () => sut.GetApplicationDetailAsync(otherRecruiterUser.Id, "Recruiter", application.Id));
    }

    [Fact]
    public async Task ApplyAsync_NoCandidateProfile_ThrowsNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var (_, recruiterUser, job, _) = await SeedAsync(db);
        var sut = CreateSut(db);

        var strangerUser = new User { FullName = "Stranger", Email = "stranger@example.com", Role = UserRole.Candidate };
        db.Users.Add(strangerUser);
        await db.SaveChangesAsync();
        // Note: no CandidateProfile created for strangerUser.

        await Assert.ThrowsAsync<NotFoundException>(() => sut.ApplyAsync(strangerUser.Id, job.Id, null));
    }

    [Fact]
    public async Task UpdateStatusAsync_OwningRecruiter_UpdatesStatus()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, recruiter, job, _) = await SeedAsync(db);
        var sut = CreateSut(db);
        var application = await sut.ApplyAsync(candidate.Id, job.Id, null);

        var updated = await sut.UpdateStatusAsync(recruiter.Id, application.Id, ApplicationStatus.Shortlisted);

        Assert.Equal("Shortlisted", updated.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_NonOwningRecruiter_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var sut = CreateSut(db);
        var application = await sut.ApplyAsync(candidate.Id, job.Id, null);

        var otherRecruiterUser = new User { FullName = "Other Recruiter", Email = "other-rec2@example.com", Role = UserRole.Recruiter };
        db.Users.Add(otherRecruiterUser);
        await db.SaveChangesAsync();
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = otherRecruiterUser.Id, Company = new Company { Name = "Other Co 2" } });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(
            () => sut.UpdateStatusAsync(otherRecruiterUser.Id, application.Id, ApplicationStatus.Rejected));
    }

    [Fact]
    public async Task WithdrawAsync_OwningCandidate_SetsStatusToWithdrawn()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var sut = CreateSut(db);
        var application = await sut.ApplyAsync(candidate.Id, job.Id, null);

        var updated = await sut.WithdrawAsync(candidate.Id, application.Id);

        Assert.Equal("Withdrawn", updated.Status);
    }

    [Fact]
    public async Task WithdrawAsync_NonOwningCandidate_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var sut = CreateSut(db);
        var application = await sut.ApplyAsync(candidate.Id, job.Id, null);

        var otherCandidate = new User { FullName = "Other Candidate 2", Email = "other-cand2@example.com", Role = UserRole.Candidate };
        db.Users.Add(otherCandidate);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.WithdrawAsync(otherCandidate.Id, application.Id));
    }

    [Theory]
    [InlineData(ApplicationStatus.Hired)]
    [InlineData(ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.Withdrawn)]
    public async Task WithdrawAsync_FinalStatus_ThrowsConflict(ApplicationStatus finalStatus)
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var sut = CreateSut(db);
        var application = await sut.ApplyAsync(candidate.Id, job.Id, null);
        var entity = db.JobApplications.First(a => a.Id == application.Id);
        entity.Status = finalStatus;
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ConflictException>(() => sut.WithdrawAsync(candidate.Id, application.Id));
        Assert.Equal("APPLICATION_FINAL", ex.ErrorCode);
    }

    [Fact]
    public async Task WithdrawAsync_EarlyStatus_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var sut = CreateSut(db);
        var application = await sut.ApplyAsync(candidate.Id, job.Id, null);
        var entity = db.JobApplications.First(a => a.Id == application.Id);
        entity.Status = ApplicationStatus.Shortlisted;
        await db.SaveChangesAsync();

        var updated = await sut.WithdrawAsync(candidate.Id, application.Id);

        Assert.Equal("Withdrawn", updated.Status);
    }

    [Theory]
    [InlineData(JobStatus.Closed)]
    [InlineData(JobStatus.Archived)]
    [InlineData(JobStatus.Draft)]
    public async Task ApplyAsync_JobNotOpen_ThrowsConflict(JobStatus status)
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        job.Status = status;
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => sut.ApplyAsync(candidate.Id, job.Id, null));
        Assert.Equal("JOB_CLOSED", ex.ErrorCode);
    }

    [Fact]
    public async Task ApplyAsync_JobHidden_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        job.ModerationStatus = ModerationStatus.Hidden;
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => sut.ApplyAsync(candidate.Id, job.Id, null));
        Assert.Equal("JOB_CLOSED", ex.ErrorCode);
    }

    [Fact]
    public async Task ApplyAsync_PastDeadline_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        job.ApplicationDeadlineUtc = DateTime.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => sut.ApplyAsync(candidate.Id, job.Id, null));
        Assert.Equal("JOB_EXPIRED", ex.ErrorCode);
    }

    [Fact]
    public async Task ApplyAsync_FutureDeadline_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        job.ApplicationDeadlineUtc = DateTime.UtcNow.AddDays(7);
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var result = await sut.ApplyAsync(candidate.Id, job.Id, null);

        Assert.NotNull(result);
    }
}
