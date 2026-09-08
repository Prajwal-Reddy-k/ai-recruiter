using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class JobViewServiceTests
{
    private static async Task<(User candidateUser, CandidateProfile profile, JobPosting job)> SeedCandidateAndJobAsync(
        AIRecruiter.Infrastructure.Persistence.AppDbContext db, string emailPrefix = "casey")
    {
        var candidateUser = new User { FullName = "Casey Candidate", Email = $"{emailPrefix}@example.com", Role = UserRole.Candidate };
        var recruiterUser = new User { FullName = "Rita Recruiter", Email = $"{emailPrefix}-rita@example.com", Role = UserRole.Recruiter };
        db.Users.AddRange(candidateUser, recruiterUser);
        await db.SaveChangesAsync();

        var company = new Company { Name = "Acme" };
        var recruiterProfile = new RecruiterProfile { UserId = recruiterUser.Id, Company = company };
        var candidateProfile = new CandidateProfile { UserId = candidateUser.Id };
        db.RecruiterProfiles.Add(recruiterProfile);
        db.CandidateProfiles.Add(candidateProfile);
        await db.SaveChangesAsync();

        var job = new JobPosting { Title = "Backend role", Description = "d", Status = JobStatus.Open, CompanyId = company.Id, RecruiterProfileId = recruiterProfile.Id };
        db.JobPostings.Add(job);
        await db.SaveChangesAsync();

        return (candidateUser, candidateProfile, job);
    }

    [Fact]
    public async Task RecordViewAsync_FirstVisit_CreatesRow()
    {
        using var db = TestDbContextFactory.Create();
        var (candidateUser, profile, job) = await SeedCandidateAndJobAsync(db);
        var sut = new JobViewService(db);

        await sut.RecordViewAsync(candidateUser.Id, job.Id);

        var views = db.JobViews.Where(v => v.CandidateProfileId == profile.Id).ToList();
        Assert.Single(views);
        Assert.Equal(job.Id, views[0].JobPostingId);
    }

    [Fact]
    public async Task RecordViewAsync_RepeatVisit_UpdatesViewedAtInsteadOfDuplicating()
    {
        using var db = TestDbContextFactory.Create();
        var (candidateUser, profile, job) = await SeedCandidateAndJobAsync(db);
        var sut = new JobViewService(db);

        await sut.RecordViewAsync(candidateUser.Id, job.Id);
        var firstViewedAt = db.JobViews.Single().ViewedAt;

        await Task.Delay(10);
        await sut.RecordViewAsync(candidateUser.Id, job.Id);

        var views = db.JobViews.Where(v => v.CandidateProfileId == profile.Id).ToList();
        Assert.Single(views);
        Assert.True(views[0].ViewedAt >= firstViewedAt);
    }

    [Fact]
    public async Task GetRecentlyViewedAsync_ReturnsAtMostTenMostRecentFirst()
    {
        using var db = TestDbContextFactory.Create();
        var (candidateUser, profile, _) = await SeedCandidateAndJobAsync(db);

        var company = db.Companies.First();
        var recruiterProfile = db.RecruiterProfiles.First();
        var jobs = new List<JobPosting>();
        for (var i = 0; i < 12; i++)
        {
            var j = new JobPosting { Title = $"Job {i}", Description = "d", Status = JobStatus.Open, CompanyId = company.Id, RecruiterProfileId = recruiterProfile.Id };
            db.JobPostings.Add(j);
            jobs.Add(j);
        }
        await db.SaveChangesAsync();

        var baseTime = DateTime.UtcNow;
        for (var i = 0; i < jobs.Count; i++)
        {
            db.JobViews.Add(new JobView { CandidateProfileId = profile.Id, JobPostingId = jobs[i].Id, ViewedAt = baseTime.AddMinutes(i) });
        }
        await db.SaveChangesAsync();

        var sut = new JobViewService(db);
        var result = await sut.GetRecentlyViewedAsync(candidateUser.Id);

        Assert.Equal(10, result.Count);
        Assert.Equal("Job 11", result[0].Title);
        Assert.Equal("Job 2", result[9].Title);
    }

    [Fact]
    public async Task GetRecentlyViewedAsync_NeverReturnsAnotherCandidatesHistory()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate1User, profile1, job1) = await SeedCandidateAndJobAsync(db, "candidate1");
        var (candidate2User, profile2, job2) = await SeedCandidateAndJobAsync(db, "candidate2");

        db.JobViews.Add(new JobView { CandidateProfileId = profile1.Id, JobPostingId = job1.Id, ViewedAt = DateTime.UtcNow });
        db.JobViews.Add(new JobView { CandidateProfileId = profile2.Id, JobPostingId = job2.Id, ViewedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var sut = new JobViewService(db);
        var result = await sut.GetRecentlyViewedAsync(candidate1User.Id);

        Assert.Single(result);
        Assert.Equal(job1.Id, result[0].Id);
        _ = candidate2User;
    }

    [Fact]
    public async Task RecordViewAsync_NeverTouchesAnonymousViewCountCounter()
    {
        using var db = TestDbContextFactory.Create();
        var (candidateUser, _, job) = await SeedCandidateAndJobAsync(db);
        job.ViewCount = 5;
        await db.SaveChangesAsync();

        var sut = new JobViewService(db);
        await sut.RecordViewAsync(candidateUser.Id, job.Id);

        Assert.Equal(5, db.JobPostings.Single(j => j.Id == job.Id).ViewCount);
    }

    [Fact]
    public async Task RecordViewAsync_NoCandidateProfile_NoOpsRatherThanThrowing()
    {
        using var db = TestDbContextFactory.Create();
        var recruiterUser = new User { FullName = "Rita Recruiter", Email = "rita-noprofile@example.com", Role = UserRole.Recruiter };
        db.Users.Add(recruiterUser);
        await db.SaveChangesAsync();
        var company = new Company { Name = "Acme" };
        var recruiterProfile = new RecruiterProfile { UserId = recruiterUser.Id, Company = company };
        db.RecruiterProfiles.Add(recruiterProfile);
        await db.SaveChangesAsync();
        var job = new JobPosting { Title = "Role", Description = "d", Status = JobStatus.Open, CompanyId = company.Id, RecruiterProfileId = recruiterProfile.Id };
        db.JobPostings.Add(job);
        await db.SaveChangesAsync();

        var sut = new JobViewService(db);
        await sut.RecordViewAsync(recruiterUser.Id, job.Id);

        Assert.Empty(db.JobViews);
    }
}
