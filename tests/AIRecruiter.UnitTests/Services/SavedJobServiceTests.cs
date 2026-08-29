using AIRecruiter.Application.Exceptions;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class SavedJobServiceTests
{
    private static async Task<(User Candidate, JobPosting Job)> SeedAsync(AppDbContext db)
    {
        var candidateUser = new User { FullName = "Casey Candidate", Email = "casey@example.com", Role = UserRole.Candidate };
        var recruiterUser = new User { FullName = "Rita Recruiter", Email = "rita@example.com", Role = UserRole.Recruiter };
        db.Users.AddRange(candidateUser, recruiterUser);
        await db.SaveChangesAsync();

        db.CandidateProfiles.Add(new CandidateProfile { UserId = candidateUser.Id });

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

        return (candidateUser, job);
    }

    [Fact]
    public async Task SaveAsync_CalledTwice_DoesNotCreateDuplicateRow()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, job) = await SeedAsync(db);
        var sut = TestServiceFactory.CreateSavedJobs(db);

        await sut.SaveAsync(candidate.Id, job.Id);
        await sut.SaveAsync(candidate.Id, job.Id);

        Assert.Single(db.SavedJobs);
    }

    [Fact]
    public async Task SaveAsync_UnknownJob_ThrowsNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _) = await SeedAsync(db);
        var sut = TestServiceFactory.CreateSavedJobs(db);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.SaveAsync(candidate.Id, 9999));
    }

    [Fact]
    public async Task GetMySavedJobsWithDatesAsync_OnlyReturnsCallersOwnSavedJobs()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, job) = await SeedAsync(db);
        var sut = TestServiceFactory.CreateSavedJobs(db);
        await sut.SaveAsync(candidate.Id, job.Id);

        var otherCandidateUser = new User { FullName = "Other Candidate", Email = "other@example.com", Role = UserRole.Candidate };
        db.Users.Add(otherCandidateUser);
        await db.SaveChangesAsync();
        db.CandidateProfiles.Add(new CandidateProfile { UserId = otherCandidateUser.Id });
        await db.SaveChangesAsync();

        var otherResults = await sut.GetMySavedJobsWithDatesAsync(otherCandidateUser.Id);
        var ownResults = await sut.GetMySavedJobsWithDatesAsync(candidate.Id);

        Assert.Empty(otherResults);
        Assert.Single(ownResults);
        Assert.Equal(job.Id, ownResults[0].Job.Id);
    }

    [Fact]
    public async Task UnsaveAsync_RemovesOnlyCallersOwnSavedJob()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, job) = await SeedAsync(db);
        var sut = TestServiceFactory.CreateSavedJobs(db);
        await sut.SaveAsync(candidate.Id, job.Id);

        await sut.UnsaveAsync(candidate.Id, job.Id);

        Assert.Empty(db.SavedJobs);
    }
}
