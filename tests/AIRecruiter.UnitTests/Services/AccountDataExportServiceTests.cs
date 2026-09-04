using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class AccountDataExportServiceTests
{
    [Fact]
    public async Task GetMyDataExportAsync_IncludesOnlyCallersOwnApplications()
    {
        using var db = TestDbContextFactory.Create();
        var candidateUser = new User { FullName = "Casey Candidate", Email = "casey@example.com", Role = UserRole.Candidate, PasswordHash = "hashed-secret" };
        var otherCandidateUser = new User { FullName = "Other Candidate", Email = "other@example.com", Role = UserRole.Candidate, PasswordHash = "other-hash" };
        var recruiterUser = new User { FullName = "Rita Recruiter", Email = "rita@example.com", Role = UserRole.Recruiter };
        db.Users.AddRange(candidateUser, otherCandidateUser, recruiterUser);
        await db.SaveChangesAsync();

        var candidateProfile = new CandidateProfile { UserId = candidateUser.Id, Headline = "Backend Engineer" };
        var otherProfile = new CandidateProfile { UserId = otherCandidateUser.Id };
        db.CandidateProfiles.AddRange(candidateProfile, otherProfile);
        var company = new Company { Name = "Acme Corp" };
        var recruiterProfile = new RecruiterProfile { UserId = recruiterUser.Id, Company = company };
        db.RecruiterProfiles.Add(recruiterProfile);
        await db.SaveChangesAsync();

        var job = new JobPosting { Title = "Backend Engineer", Description = "role", CompanyId = company.Id, RecruiterProfileId = recruiterProfile.Id, Status = JobStatus.Open };
        db.JobPostings.Add(job);
        await db.SaveChangesAsync();

        db.JobApplications.Add(new JobApplication { JobPostingId = job.Id, CandidateProfileId = candidateProfile.Id, Status = ApplicationStatus.Applied });
        db.JobApplications.Add(new JobApplication { JobPostingId = job.Id, CandidateProfileId = otherProfile.Id, Status = ApplicationStatus.Applied });
        await db.SaveChangesAsync();

        var sut = TestServiceFactory.CreateAccountDataExportService(db);

        var export = await sut.GetMyDataExportAsync(candidateUser.Id);

        Assert.Single(export.Applications);
        Assert.Equal("Backend Engineer", export.CandidateProfile!.Headline);
    }

    [Fact]
    public async Task GetMyDataExportAsync_NeverIncludesPasswordHash()
    {
        using var db = TestDbContextFactory.Create();
        var candidateUser = new User { FullName = "Casey Candidate", Email = "casey@example.com", Role = UserRole.Candidate, PasswordHash = "super-secret-hash" };
        db.Users.Add(candidateUser);
        await db.SaveChangesAsync();
        db.CandidateProfiles.Add(new CandidateProfile { UserId = candidateUser.Id });
        await db.SaveChangesAsync();
        var sut = TestServiceFactory.CreateAccountDataExportService(db);

        var export = await sut.GetMyDataExportAsync(candidateUser.Id);
        var json = System.Text.Json.JsonSerializer.Serialize(export);

        Assert.DoesNotContain("super-secret-hash", json);
        Assert.DoesNotContain("PasswordHash", json);
        Assert.DoesNotContain("SecurityStamp", json);
    }

    [Fact]
    public async Task GetMyDataExportAsCsvAsync_ReturnsFlatSummary()
    {
        using var db = TestDbContextFactory.Create();
        var candidateUser = new User { FullName = "Casey Candidate", Email = "casey@example.com", Role = UserRole.Candidate };
        db.Users.Add(candidateUser);
        await db.SaveChangesAsync();
        db.CandidateProfiles.Add(new CandidateProfile { UserId = candidateUser.Id, City = "Bengaluru" });
        await db.SaveChangesAsync();
        var sut = TestServiceFactory.CreateAccountDataExportService(db);

        var csv = await sut.GetMyDataExportAsCsvAsync(candidateUser.Id);

        Assert.Contains("Casey Candidate", csv);
        Assert.Contains("Bengaluru", csv);
    }
}
