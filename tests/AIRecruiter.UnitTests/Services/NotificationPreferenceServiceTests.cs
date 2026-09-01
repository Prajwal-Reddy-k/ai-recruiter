using AIRecruiter.Application.DTOs.Users;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class NotificationPreferenceServiceTests
{
    private static NotificationPreferenceService CreateSut(AppDbContext db) => new(db);

    private static async Task<User> SeedUserAsync(AppDbContext db)
    {
        var user = new User { FullName = "Casey Candidate", Email = "casey@example.com", Role = UserRole.Candidate, PasswordHash = "x" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task GetMyPreferencesAsync_NoExistingRow_CreatesDefaultAllEnabled()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var sut = CreateSut(db);

        var result = await sut.GetMyPreferencesAsync(user.Id);

        Assert.True(result.MessagesEnabled);
        Assert.True(result.ApplicationsEnabled);
        Assert.True(result.InterviewsEnabled);
        Assert.True(result.InvitationsEnabled);
        Assert.Single(db.NotificationPreferences);
    }

    [Fact]
    public async Task UpdateMyPreferencesAsync_PersistsChanges()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var sut = CreateSut(db);

        var result = await sut.UpdateMyPreferencesAsync(user.Id, new UpdateNotificationPreferenceRequest(false, true, false, true));

        Assert.False(result.MessagesEnabled);
        Assert.True(result.ApplicationsEnabled);
        Assert.False(result.InterviewsEnabled);
        Assert.True(result.InvitationsEnabled);

        var reloaded = await sut.GetMyPreferencesAsync(user.Id);
        Assert.False(reloaded.MessagesEnabled);
    }

    [Fact]
    public async Task UpdateMyPreferencesAsync_DoesNotCreateDuplicateRow()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var sut = CreateSut(db);

        await sut.GetMyPreferencesAsync(user.Id);
        await sut.UpdateMyPreferencesAsync(user.Id, new UpdateNotificationPreferenceRequest(false, false, false, false));

        Assert.Single(db.NotificationPreferences);
    }
}
