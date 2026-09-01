using AIRecruiter.Application.DTOs.Users;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class NotificationServiceTests
{
    private static NotificationService CreateSut(AppDbContext db) => new(db);

    private static async Task<User> SeedUserAsync(AppDbContext db)
    {
        var user = new User { FullName = "Casey Candidate", Email = "casey@example.com", Role = UserRole.Candidate, PasswordHash = "x" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task NotifyAsync_DefaultPreferences_CreatesNotification()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var sut = CreateSut(db);

        await sut.NotifyAsync(user.Id, "MessageReceived", "New message");

        Assert.Single(db.Notifications);
    }

    [Fact]
    public async Task NotifyAsync_CategoryDisabled_SkipsNotification()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var prefsService = new NotificationPreferenceService(db);
        await prefsService.UpdateMyPreferencesAsync(user.Id, new UpdateNotificationPreferenceRequest(false, true, true, true));
        var sut = CreateSut(db);

        await sut.NotifyAsync(user.Id, "MessageReceived", "New message");

        Assert.Empty(db.Notifications);
    }

    [Fact]
    public async Task NotifyAsync_OtherCategoriesStillFireWhenOneDisabled()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var prefsService = new NotificationPreferenceService(db);
        await prefsService.UpdateMyPreferencesAsync(user.Id, new UpdateNotificationPreferenceRequest(false, true, true, true));
        var sut = CreateSut(db);

        await sut.NotifyAsync(user.Id, "ApplicationStatusChanged", "Status changed");
        await sut.NotifyAsync(user.Id, "InterviewProposed", "Interview proposed");
        await sut.NotifyAsync(user.Id, "InvitedToApply", "You're invited");

        Assert.Equal(3, db.Notifications.Count());
    }

    [Fact]
    public async Task NotifyAsync_SystemTypeAlwaysFires_RegardlessOfPreferences()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var prefsService = new NotificationPreferenceService(db);
        await prefsService.UpdateMyPreferencesAsync(user.Id, new UpdateNotificationPreferenceRequest(false, false, false, false));
        var sut = CreateSut(db);

        await sut.NotifyAsync(user.Id, "JobExpired", "Your job expired");

        Assert.Single(db.Notifications);
    }
}
