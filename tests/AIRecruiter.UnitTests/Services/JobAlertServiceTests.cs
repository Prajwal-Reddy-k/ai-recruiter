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

    [Fact]
    public async Task CreateAsync_ExactDuplicateConfiguration_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var candidate = await SeedCandidateAsync(db);
        var sut = TestServiceFactory.CreateJobAlerts(db);
        var request = new UpsertJobAlertRequest("C#", "Karnataka", "Bengaluru", false, JobType.FullTime, 2, true, "My search", "backend");
        await sut.CreateAsync(candidate.Id, request);

        await Assert.ThrowsAsync<ConflictException>(() => sut.CreateAsync(candidate.Id, request));
    }

    [Fact]
    public async Task CreateAsync_DifferentKeyword_IsNotADuplicate()
    {
        using var db = TestDbContextFactory.Create();
        var candidate = await SeedCandidateAsync(db);
        var sut = TestServiceFactory.CreateJobAlerts(db);
        await sut.CreateAsync(candidate.Id, new UpsertJobAlertRequest("C#", null, null, null, null, null, true, null, "backend"));

        var second = await sut.CreateAsync(candidate.Id, new UpsertJobAlertRequest("C#", null, null, null, null, null, true, null, "frontend"));

        Assert.NotNull(second);
    }

    [Fact]
    public async Task DuplicateAsync_CopiesConfigurationAsNonDefault()
    {
        using var db = TestDbContextFactory.Create();
        var candidate = await SeedCandidateAsync(db);
        var sut = TestServiceFactory.CreateJobAlerts(db);
        var original = await sut.CreateAsync(candidate.Id, new UpsertJobAlertRequest("C#", "Karnataka", "Bengaluru", false, JobType.FullTime, 2, true, "My search"));
        await sut.SetDefaultAsync(candidate.Id, original.Id);

        var copy = await sut.DuplicateAsync(candidate.Id, original.Id);

        Assert.Equal("My search (copy)", copy.Name);
        Assert.False(copy.IsDefault);
        Assert.Equal("C#", copy.SkillsCsv);
    }

    [Fact]
    public async Task SetDefaultAsync_ClearsDefaultOnOtherAlerts()
    {
        using var db = TestDbContextFactory.Create();
        var candidate = await SeedCandidateAsync(db);
        var sut = TestServiceFactory.CreateJobAlerts(db);
        var first = await sut.CreateAsync(candidate.Id, new UpsertJobAlertRequest("C#", null, null, null, null, null, true, "First"));
        var second = await sut.CreateAsync(candidate.Id, new UpsertJobAlertRequest("React", null, null, null, null, null, true, "Second"));
        await sut.SetDefaultAsync(candidate.Id, first.Id);

        await sut.SetDefaultAsync(candidate.Id, second.Id);

        var all = await sut.GetMyAlertsAsync(candidate.Id);
        Assert.True(all.First(a => a.Id == second.Id).IsDefault);
        Assert.False(all.First(a => a.Id == first.Id).IsDefault);
    }
}
