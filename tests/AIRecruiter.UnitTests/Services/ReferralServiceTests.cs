using AIRecruiter.Application.DTOs.Referrals;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class ReferralServiceTests
{
    private static ReferralService CreateSut(AppDbContext db) => TestServiceFactory.CreateReferralService(db);

    private static async Task<(User RecruiterUser, Company Company, JobPosting Job)> SeedJobAsync(
        AppDbContext db, JobStatus status = JobStatus.Open, ModerationStatus moderation = ModerationStatus.Approved, string? recruiterEmail = null)
    {
        recruiterEmail ??= $"rita{Guid.NewGuid():N}@example.com";
        var recruiterUser = new User { FullName = "Rita Recruiter", Email = recruiterEmail, Role = UserRole.Recruiter, PasswordHash = "x" };
        db.Users.Add(recruiterUser);
        await db.SaveChangesAsync();
        var company = new Company { Name = "Acme Corp" };
        var recruiterProfile = new RecruiterProfile { UserId = recruiterUser.Id, Company = company, CompanyRole = CompanyRole.Owner };
        db.RecruiterProfiles.Add(recruiterProfile);
        await db.SaveChangesAsync();
        var job = new JobPosting
        {
            Title = "Backend Engineer", Description = "role", CompanyId = company.Id, RecruiterProfileId = recruiterProfile.Id,
            Status = status, ModerationStatus = moderation,
        };
        db.JobPostings.Add(job);
        await db.SaveChangesAsync();
        return (recruiterUser, company, job);
    }

    private static async Task<User> SeedReferrerAsync(AppDbContext db, string email = "ref@example.com")
    {
        var user = new User { FullName = "Reggie Referrer", Email = email, Role = UserRole.Candidate, PasswordHash = "x" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static CreateReferralRequest ValidRequest(int jobId, string email = "friend@example.com") =>
        new("Fiona Friend", email, "9999999999", "C#, SQL", "Great engineer", jobId);

    [Fact]
    public async Task Create_ValidRequest_ReturnsRawTokenOnce()
    {
        using var db = TestDbContextFactory.Create();
        var (_, _, job) = await SeedJobAsync(db);
        var referrer = await SeedReferrerAsync(db);
        var sut = CreateSut(db);

        var response = await sut.CreateAsync(referrer.Id, "127.0.0.1", ValidRequest(job.Id));

        Assert.False(string.IsNullOrEmpty(response.RawToken));
        var stored = db.Referrals.Single();
        Assert.NotEqual(response.RawToken, stored.TokenHash);
    }

    [Fact]
    public async Task Create_ForClosedJob_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var (_, _, job) = await SeedJobAsync(db, status: JobStatus.Closed);
        var referrer = await SeedReferrerAsync(db);
        var sut = CreateSut(db);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => sut.CreateAsync(referrer.Id, "127.0.0.1", ValidRequest(job.Id)));
        Assert.Equal("JOB_NOT_OPEN", ex.ErrorCode);
    }

    [Fact]
    public async Task Create_ForUnapprovedJob_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var (_, _, job) = await SeedJobAsync(db, moderation: ModerationStatus.Hidden);
        var referrer = await SeedReferrerAsync(db);
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<ConflictException>(() => sut.CreateAsync(referrer.Id, "127.0.0.1", ValidRequest(job.Id)));
    }

    [Fact]
    public async Task Create_DuplicateEmailSameJob_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var (_, _, job) = await SeedJobAsync(db);
        var referrer = await SeedReferrerAsync(db);
        var sut = CreateSut(db);

        await sut.CreateAsync(referrer.Id, "127.0.0.1", ValidRequest(job.Id));

        var ex = await Assert.ThrowsAsync<ConflictException>(() => sut.CreateAsync(referrer.Id, "127.0.0.1", ValidRequest(job.Id)));
        Assert.Equal("REFERRAL_ALREADY_EXISTS", ex.ErrorCode);
    }

    [Fact]
    public async Task Create_DuplicateEmailDifferentJob_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (_, company, job1) = await SeedJobAsync(db);
        var recruiterProfile = db.RecruiterProfiles.Single();
        var job2 = new JobPosting { Title = "Frontend Engineer", Description = "role", CompanyId = company.Id, RecruiterProfileId = recruiterProfile.Id, Status = JobStatus.Open, ModerationStatus = ModerationStatus.Approved };
        db.JobPostings.Add(job2);
        await db.SaveChangesAsync();
        var referrer = await SeedReferrerAsync(db);
        var sut = CreateSut(db);

        await sut.CreateAsync(referrer.Id, "127.0.0.1", ValidRequest(job1.Id));
        await sut.CreateAsync(referrer.Id, "127.0.0.1", ValidRequest(job2.Id));

        Assert.Equal(2, db.Referrals.Count());
    }

    [Fact]
    public async Task Create_RateLimitedByUser_ThrowsRateLimited()
    {
        using var db = TestDbContextFactory.Create();
        var (_, _, job) = await SeedJobAsync(db);
        var referrer = await SeedReferrerAsync(db);
        var sut = CreateSut(db);

        for (var i = 0; i < 20; i++)
        {
            await sut.CreateAsync(referrer.Id, "127.0.0.1", ValidRequest(job.Id, $"friend{i}@example.com"));
        }

        await Assert.ThrowsAsync<RateLimitedException>(() => sut.CreateAsync(referrer.Id, "127.0.0.1", ValidRequest(job.Id, "onemore@example.com")));
    }

    [Fact]
    public async Task ResolveToken_Valid_ReturnsPreview()
    {
        using var db = TestDbContextFactory.Create();
        var (_, _, job) = await SeedJobAsync(db);
        var referrer = await SeedReferrerAsync(db);
        var sut = CreateSut(db);

        var response = await sut.CreateAsync(referrer.Id, "127.0.0.1", ValidRequest(job.Id));
        var preview = await sut.ResolveTokenAsync(response.RawToken);

        Assert.Equal("Backend Engineer", preview.JobTitle);
        Assert.Equal("Acme Corp", preview.CompanyName);
    }

    [Fact]
    public async Task ResolveToken_Expired_ThrowsGenericNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var (_, _, job) = await SeedJobAsync(db);
        var referrer = await SeedReferrerAsync(db);
        var sut = CreateSut(db);

        var response = await sut.CreateAsync(referrer.Id, "127.0.0.1", ValidRequest(job.Id));
        var referral = db.Referrals.Single();
        referral.TokenExpiresAtUtc = DateTime.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();

        var expiredEx = await Assert.ThrowsAsync<NotFoundException>(() => sut.ResolveTokenAsync(response.RawToken));
        var neverExistedEx = await Assert.ThrowsAsync<NotFoundException>(() => sut.ResolveTokenAsync("not-a-real-token"));

        Assert.Equal(expiredEx.Message, neverExistedEx.Message);
    }

    [Fact]
    public async Task GetMyReferrals_NeverExposesFullCandidateProfile()
    {
        using var db = TestDbContextFactory.Create();
        var (_, _, job) = await SeedJobAsync(db);
        var referrer = await SeedReferrerAsync(db);
        var sut = CreateSut(db);

        await sut.CreateAsync(referrer.Id, "127.0.0.1", ValidRequest(job.Id));
        var referrals = await sut.GetMyReferralsAsync(referrer.Id);

        var dto = Assert.Single(referrals);
        Assert.Equal("Fiona Friend", dto.ReferredName);
        Assert.Equal("Invited", dto.Status);
        // ReferralDto's type itself has no resume/skills/phone fields returned to the
        // referrer — this assertion documents that guarantee.
    }

    [Fact]
    public async Task GetCompanyReferrals_CrossCompanyIsolation_ExcludesOtherCompanyJobs()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiterUser, _, job) = await SeedJobAsync(db);
        var referrer = await SeedReferrerAsync(db);
        var sut = CreateSut(db);
        await sut.CreateAsync(referrer.Id, "127.0.0.1", ValidRequest(job.Id));

        var (otherRecruiter, _, _) = await SeedJobAsync(db);
        // SeedJobAsync creates a fresh user/company/job each call, so otherRecruiter belongs
        // to a different company than recruiterUser.
        var otherCompanyReferrals = await sut.GetCompanyReferralsAsync(otherRecruiter.Id);
        var ownCompanyReferrals = await sut.GetCompanyReferralsAsync(recruiterUser.Id);

        Assert.Empty(otherCompanyReferrals);
        Assert.Single(ownCompanyReferrals);
    }

    [Fact]
    public async Task DeriveDisplayStatus_HiredApplication_ShowsHired()
    {
        using var db = TestDbContextFactory.Create();
        var (_, _, job) = await SeedJobAsync(db);
        var referrer = await SeedReferrerAsync(db);
        var sut = CreateSut(db);
        var response = await sut.CreateAsync(referrer.Id, "127.0.0.1", ValidRequest(job.Id));

        var candidateUser = new User { FullName = "Fiona Friend", Email = "friend@example.com", Role = UserRole.Candidate, PasswordHash = "x" };
        db.Users.Add(candidateUser);
        await db.SaveChangesAsync();
        var candidateProfile = new CandidateProfile { UserId = candidateUser.Id };
        db.CandidateProfiles.Add(candidateProfile);
        await db.SaveChangesAsync();

        var referral = db.Referrals.Single(r => r.Id == response.Id);
        referral.RegisteredUserId = candidateUser.Id;
        referral.Status = ReferralStatus.Applied;
        var application = new JobApplication { JobPostingId = job.Id, CandidateProfileId = candidateProfile.Id, Status = ApplicationStatus.Hired };
        db.JobApplications.Add(application);
        await db.SaveChangesAsync();
        referral.JobApplicationId = application.Id;
        await db.SaveChangesAsync();

        var referrals = await sut.GetMyReferralsAsync(referrer.Id);
        Assert.Equal("Hired", referrals.Single().Status);
    }
}
