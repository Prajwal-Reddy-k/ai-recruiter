using AIRecruiter.Application.DTOs.Recruiters;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class RecruiterOnboardingServiceTests
{
    private static RecruiterOnboardingService CreateSut(AppDbContext db) =>
        new(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db));

    [Fact]
    public async Task GetStatusAsync_NoProfile_ReturnsNotOnboarded()
    {
        using var db = TestDbContextFactory.Create();
        db.Users.Add(new User { Id = 1, FullName = "Rita Recruiter", Email = "rita@example.com", Role = UserRole.Recruiter });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var status = await sut.GetStatusAsync(1);

        Assert.False(status.IsOnboarded);
    }

    [Fact]
    public async Task UpsertAsync_FirstCall_CreatesCompanyAndProfile()
    {
        using var db = TestDbContextFactory.Create();
        db.Users.Add(new User { Id = 1, FullName = "Rita Recruiter", Email = "rita@example.com", Role = UserRole.Recruiter });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var request = new UpsertRecruiterOnboardingRequest(
            "Acme Corp", "https://acme.example", "Software", "We build things.", "Talent Lead",
            null, "Bengaluru", "Karnataka", null, null, null, null, null);

        var status = await sut.UpsertAsync(1, request);

        Assert.True(status.IsOnboarded);
        Assert.Equal("Acme Corp", status.CompanyName);
        Assert.Equal("Talent Lead", status.Designation);
        Assert.Single(db.RecruiterProfiles);
        Assert.Single(db.Companies);
    }

    [Fact]
    public async Task UpsertAsync_SecondCall_UpdatesExistingCompanyInPlace()
    {
        using var db = TestDbContextFactory.Create();
        db.Users.Add(new User { Id = 1, FullName = "Rita Recruiter", Email = "rita@example.com", Role = UserRole.Recruiter });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        await sut.UpsertAsync(1, new UpsertRecruiterOnboardingRequest(
            "Acme Corp", null, null, null, "Recruiter", null, null, null, null, null, null, null, null));

        var updated = await sut.UpsertAsync(1, new UpsertRecruiterOnboardingRequest(
            "Acme Corp Renamed", null, null, null, "Senior Recruiter", null, null, null, null, null, null, null, null));

        Assert.Equal("Acme Corp Renamed", updated.CompanyName);
        Assert.Equal("Senior Recruiter", updated.Designation);
        Assert.Single(db.RecruiterProfiles);
        Assert.Single(db.Companies);
    }
}
