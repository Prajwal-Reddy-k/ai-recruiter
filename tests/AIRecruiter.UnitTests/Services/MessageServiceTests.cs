using AIRecruiter.Application.DTOs.Messaging;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class MessageServiceTests
{
    private static MessageService CreateSut(AppDbContext db) =>
        new(db, TestServiceFactory.CreateNotifications(db), TestServiceFactory.CreateAuditLog(db), new InMemoryIpRateLimiter());

    private static async Task<(User Recruiter, User Candidate, User OtherCandidate, JobApplication Application)> SeedAsync(AppDbContext db)
    {
        var recruiterUser = new User { FullName = "Rita Recruiter", Email = "rita@example.com", Role = UserRole.Recruiter };
        var candidateUser = new User { FullName = "Casey Candidate", Email = "casey@example.com", Role = UserRole.Candidate };
        var otherCandidateUser = new User { FullName = "Other Candidate", Email = "other-candidate@example.com", Role = UserRole.Candidate };
        db.Users.AddRange(recruiterUser, candidateUser, otherCandidateUser);
        await db.SaveChangesAsync();

        var company = new Company { Name = "Acme Corp" };
        var recruiterProfile = new RecruiterProfile { UserId = recruiterUser.Id, Company = company };
        var candidateProfile = new CandidateProfile { UserId = candidateUser.Id };
        var otherCandidateProfile = new CandidateProfile { UserId = otherCandidateUser.Id };
        db.RecruiterProfiles.Add(recruiterProfile);
        db.CandidateProfiles.AddRange(candidateProfile, otherCandidateProfile);
        await db.SaveChangesAsync();

        var job = new JobPosting { Title = "Backend Engineer", Description = "role", Status = JobStatus.Open, CompanyId = company.Id, RecruiterProfileId = recruiterProfile.Id };
        db.JobPostings.Add(job);
        await db.SaveChangesAsync();

        var application = new JobApplication { JobPostingId = job.Id, CandidateProfileId = candidateProfile.Id, Status = ApplicationStatus.Applied };
        db.JobApplications.Add(application);
        await db.SaveChangesAsync();

        return (recruiterUser, candidateUser, otherCandidateUser, application);
    }

    [Fact]
    public async Task SendMessageAsync_OwningCandidate_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (_, candidate, _, application) = await SeedAsync(db);
        var sut = CreateSut(db);

        var message = await sut.SendMessageAsync(candidate.Id, "Candidate", application.Id, "1.2.3.4", new SendMessageRequest("Hello, when is the interview?"));

        Assert.Equal("Hello, when is the interview?", message.Body);
    }

    [Fact]
    public async Task SendMessageAsync_OwningRecruiter_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, _, _, application) = await SeedAsync(db);
        var sut = CreateSut(db);

        var message = await sut.SendMessageAsync(recruiter.Id, "Recruiter", application.Id, "1.2.3.4", new SendMessageRequest("Thanks for applying!"));

        Assert.Equal("Recruiter", message.SenderRole);
    }

    [Fact]
    public async Task SendMessageAsync_UnrelatedCandidate_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (_, _, otherCandidate, application) = await SeedAsync(db);
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            sut.SendMessageAsync(otherCandidate.Id, "Candidate", application.Id, "1.2.3.4", new SendMessageRequest("Can I see this application?")));
    }

    [Fact]
    public async Task SendMessageAsync_UnrelatedRecruiter_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (_, _, _, application) = await SeedAsync(db);
        var sut = CreateSut(db);

        var otherRecruiter = new User { FullName = "Other Recruiter", Email = "other-recruiter@example.com", Role = UserRole.Recruiter };
        db.Users.Add(otherRecruiter);
        await db.SaveChangesAsync();
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = otherRecruiter.Id, Company = new Company { Name = "Other Co" } });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            sut.SendMessageAsync(otherRecruiter.Id, "Recruiter", application.Id, "1.2.3.4", new SendMessageRequest("Hi")));
    }

    [Fact]
    public async Task GetThreadAsync_OtherCandidate_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, _, otherCandidate, application) = await SeedAsync(db);
        var sut = CreateSut(db);
        await sut.SendMessageAsync(recruiter.Id, "Recruiter", application.Id, "1.2.3.4", new SendMessageRequest("Hi there"));

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.GetThreadAsync(otherCandidate.Id, "Candidate", application.Id));
    }

    [Fact]
    public async Task SendMessageAsync_EmptyBody_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var (_, candidate, _, application) = await SeedAsync(db);
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<ValidationException>(() =>
            sut.SendMessageAsync(candidate.Id, "Candidate", application.Id, "1.2.3.4", new SendMessageRequest("   ")));
    }

    [Fact]
    public async Task SendMessageAsync_TooLong_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var (_, candidate, _, application) = await SeedAsync(db);
        var sut = CreateSut(db);

        var tooLong = new string('a', 2001);

        await Assert.ThrowsAsync<ValidationException>(() =>
            sut.SendMessageAsync(candidate.Id, "Candidate", application.Id, "1.2.3.4", new SendMessageRequest(tooLong)));
    }

    [Fact]
    public async Task SendMessageAsync_ExceedsRateLimit_ThrowsRateLimited()
    {
        using var db = TestDbContextFactory.Create();
        var (_, candidate, _, application) = await SeedAsync(db);
        var sut = CreateSut(db);

        for (var i = 0; i < 20; i++)
        {
            await sut.SendMessageAsync(candidate.Id, "Candidate", application.Id, "1.2.3.4", new SendMessageRequest($"Message {i}"));
        }

        await Assert.ThrowsAsync<RateLimitedException>(() =>
            sut.SendMessageAsync(candidate.Id, "Candidate", application.Id, "1.2.3.4", new SendMessageRequest("One too many")));
    }

    [Fact]
    public async Task MarkThreadReadAsync_ClearsUnreadCountForReader()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, candidate, _, application) = await SeedAsync(db);
        var sut = CreateSut(db);

        await sut.SendMessageAsync(recruiter.Id, "Recruiter", application.Id, "1.2.3.4", new SendMessageRequest("Hi"));
        Assert.Equal(1, await sut.GetUnreadCountAsync(candidate.Id, "Candidate"));

        await sut.MarkThreadReadAsync(candidate.Id, "Candidate", application.Id);

        Assert.Equal(0, await sut.GetUnreadCountAsync(candidate.Id, "Candidate"));
    }

    [Fact]
    public async Task GetMyInboxAsync_OnlyIncludesThreadsWithMessages()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, _, _, application) = await SeedAsync(db);
        var sut = CreateSut(db);

        Assert.Empty(await sut.GetMyInboxAsync(recruiter.Id, "Recruiter"));

        await sut.SendMessageAsync(recruiter.Id, "Recruiter", application.Id, "1.2.3.4", new SendMessageRequest("Hi"));

        var inbox = await sut.GetMyInboxAsync(recruiter.Id, "Recruiter");
        Assert.Single(inbox);
    }
}
