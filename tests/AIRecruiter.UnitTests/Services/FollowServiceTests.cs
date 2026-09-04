using AIRecruiter.Application.DTOs.Companies;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class FollowServiceTests
{
    private static async Task<(User Candidate, Company Company)> SeedAsync(AppDbContext db)
    {
        var candidateUser = new User { FullName = "Casey Candidate", Email = "casey@example.com", Role = UserRole.Candidate };
        db.Users.Add(candidateUser);
        await db.SaveChangesAsync();

        db.CandidateProfiles.Add(new CandidateProfile { UserId = candidateUser.Id });
        var company = new Company { Name = "Acme Corp" };
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        return (candidateUser, company);
    }

    [Fact]
    public async Task FollowAsync_CalledTwice_DoesNotCreateDuplicateRow()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, company) = await SeedAsync(db);
        var sut = TestServiceFactory.CreateFollowService(db);

        await sut.FollowAsync(candidate.Id, company.Id);
        await sut.FollowAsync(candidate.Id, company.Id);

        Assert.Single(db.CompanyFollows);
    }

    [Fact]
    public async Task FollowAsync_UnknownCompany_ThrowsNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _) = await SeedAsync(db);
        var sut = TestServiceFactory.CreateFollowService(db);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.FollowAsync(candidate.Id, 9999));
    }

    [Fact]
    public async Task GetMyFollowedCompaniesAsync_OnlyReturnsCallersOwnFollows()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, company) = await SeedAsync(db);
        var sut = TestServiceFactory.CreateFollowService(db);
        await sut.FollowAsync(candidate.Id, company.Id);

        var otherCandidateUser = new User { FullName = "Other Candidate", Email = "other@example.com", Role = UserRole.Candidate };
        db.Users.Add(otherCandidateUser);
        await db.SaveChangesAsync();
        db.CandidateProfiles.Add(new CandidateProfile { UserId = otherCandidateUser.Id });
        await db.SaveChangesAsync();

        var otherResults = await sut.GetMyFollowedCompaniesAsync(otherCandidateUser.Id);
        var ownResults = await sut.GetMyFollowedCompaniesAsync(candidate.Id);

        Assert.Empty(otherResults);
        Assert.Single(ownResults);
        Assert.Equal(company.Id, ownResults[0].CompanyId);
    }

    [Fact]
    public async Task UnfollowAsync_RemovesOnlyCallersOwnFollow()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, company) = await SeedAsync(db);
        var sut = TestServiceFactory.CreateFollowService(db);
        await sut.FollowAsync(candidate.Id, company.Id);

        await sut.UnfollowAsync(candidate.Id, company.Id);

        Assert.Empty(db.CompanyFollows);
    }

    [Fact]
    public async Task UpdateNotifyPreferenceAsync_UnknownFollow_ThrowsNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, company) = await SeedAsync(db);
        var sut = TestServiceFactory.CreateFollowService(db);

        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.UpdateNotifyPreferenceAsync(candidate.Id, company.Id, new UpdateFollowNotifyRequest(false)));
    }

    [Fact]
    public async Task UpdateNotifyPreferenceAsync_TogglesFlag()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, company) = await SeedAsync(db);
        var sut = TestServiceFactory.CreateFollowService(db);
        await sut.FollowAsync(candidate.Id, company.Id);

        await sut.UpdateNotifyPreferenceAsync(candidate.Id, company.Id, new UpdateFollowNotifyRequest(false));

        var followed = await sut.GetMyFollowedCompaniesAsync(candidate.Id);
        Assert.False(followed[0].NotifyOnNewJob);
    }
}
