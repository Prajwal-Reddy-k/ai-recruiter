using AIRecruiter.Application.DTOs.SavedJobs;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class JobAlertServiceTests
{
    private static async Task<User> SeedCandidateAsync(AppDbContext db)
    {
        var candidateUser = new User { FullName = "Casey Candidate", Email = "casey@example.com", Role = UserRole.Candidate };
        db.Users.Add(candidateUser);
        await db.SaveChangesAsync();
        db.CandidateProfiles.Add(new CandidateProfile { UserId = candidateUser.Id });
        await db.SaveChangesAsync();
        return candidateUser;
    }

    [Fact]
    public async Task CreateAsync_DefaultsToActive()
    {
        using var db = TestDbContextFactory.Create();
        var candidate = await SeedCandidateAsync(db);
        var sut = TestServiceFactory.CreateJobAlerts(db);

        var alert = await sut.CreateAsync(candidate.Id, new UpsertJobAlertRequest("C#", null, null, null, null, null));

        Assert.True(alert.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_NonOwningCandidate_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var candidate = await SeedCandidateAsync(db);
        var sut = TestServiceFactory.CreateJobAlerts(db);
        var alert = await sut.CreateAsync(candidate.Id, new UpsertJobAlertRequest("C#", null, null, null, null, null));

        var otherCandidateUser = new User { FullName = "Other Candidate", Email = "other@example.com", Role = UserRole.Candidate };
        db.Users.Add(otherCandidateUser);
        await db.SaveChangesAsync();
        db.CandidateProfiles.Add(new CandidateProfile { UserId = otherCandidateUser.Id });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(
            () => sut.UpdateAsync(otherCandidateUser.Id, alert.Id, new UpsertJobAlertRequest("React", null, null, null, null, null)));
    }

    [Fact]
    public async Task SetActiveAsync_False_ExcludesAlertFromMatches()
    {
        using var db = TestDbContextFactory.Create();
        var candidate = await SeedCandidateAsync(db);
        var sut = TestServiceFactory.CreateJobAlerts(db);
        var alert = await sut.CreateAsync(candidate.Id, new UpsertJobAlertRequest(null, null, null, null, null, null));

        var deactivated = await sut.SetActiveAsync(candidate.Id, alert.Id, false);

        Assert.False(deactivated.IsActive);
        Assert.Equal(0, deactivated.MatchingJobCount);
    }

    [Fact]
    public async Task DeleteAsync_UnknownAlert_ThrowsNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var candidate = await SeedCandidateAsync(db);
        var sut = TestServiceFactory.CreateJobAlerts(db);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.DeleteAsync(candidate.Id, 9999));
    }

    [Fact]
    public async Task GetMatchingJobsAsync_NoActiveAlerts_ReturnsEmpty()
    {
        using var db = TestDbContextFactory.Create();
        var candidate = await SeedCandidateAsync(db);
        var sut = TestServiceFactory.CreateJobAlerts(db);

        var matches = await sut.GetMatchingJobsAsync(candidate.Id);

        Assert.Empty(matches);
    }
}
