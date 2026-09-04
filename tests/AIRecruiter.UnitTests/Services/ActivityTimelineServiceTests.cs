using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class ActivityTimelineServiceTests
{
    private static async Task<User> SeedUserAsync(AppDbContext db, string email = "casey@example.com")
    {
        var user = new User { FullName = "Casey Candidate", Email = email, Role = UserRole.Candidate };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task GetMyTimelineAsync_MergesActionsAndNotifications()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var auditLog = TestServiceFactory.CreateAuditLog(db);
        await auditLog.LogAsync(user.Id, "Candidate", "ProfileUpdated", "CandidateProfile", user.Id);
        db.Notifications.Add(new Notification { UserId = user.Id, Type = "OfferSent", Message = "You've received an offer." });
        await db.SaveChangesAsync();
        var sut = TestServiceFactory.CreateActivityTimelineService(db);

        var timeline = await sut.GetMyTimelineAsync(user.Id, null, null, null);

        Assert.Contains(timeline, e => e.Source == "Action" && e.Type == "ProfileUpdated");
        Assert.Contains(timeline, e => e.Source == "Notification" && e.Type == "OfferSent");
    }

    [Fact]
    public async Task GetMyTimelineAsync_NeverReturnsAnotherUsersEntries()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db, "casey@example.com");
        var otherUser = await SeedUserAsync(db, "other@example.com");
        var auditLog = TestServiceFactory.CreateAuditLog(db);
        await auditLog.LogAsync(otherUser.Id, "Candidate", "ProfileUpdated", "CandidateProfile", otherUser.Id);
        db.Notifications.Add(new Notification { UserId = otherUser.Id, Type = "OfferSent", Message = "You've received an offer." });
        await db.SaveChangesAsync();
        var sut = TestServiceFactory.CreateActivityTimelineService(db);

        var timeline = await sut.GetMyTimelineAsync(user.Id, null, null, null);

        Assert.Empty(timeline);
    }

    [Fact]
    public async Task GetMyTimelineAsync_TypeFilter_AppliesToBothActionsAndNotifications()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var auditLog = TestServiceFactory.CreateAuditLog(db);
        await auditLog.LogAsync(user.Id, "Candidate", "ProfileUpdated", "CandidateProfile", user.Id);
        await auditLog.LogAsync(user.Id, "Candidate", "JobApplicationSubmitted", "JobApplication", 1);
        db.Notifications.Add(new Notification { UserId = user.Id, Type = "OfferSent", Message = "You've received an offer." });
        await db.SaveChangesAsync();
        var sut = TestServiceFactory.CreateActivityTimelineService(db);

        var timeline = await sut.GetMyTimelineAsync(user.Id, "ProfileUpdated", null, null);

        Assert.Single(timeline);
        Assert.Equal("ProfileUpdated", timeline[0].Type);
    }

    [Fact]
    public async Task GetMyTimelineAsync_DateRangeFilter_ExcludesOutOfRangeEntries()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var auditLog = TestServiceFactory.CreateAuditLog(db);
        await auditLog.LogAsync(user.Id, "Candidate", "ProfileUpdated", "CandidateProfile", user.Id);
        await db.SaveChangesAsync();
        var sut = TestServiceFactory.CreateActivityTimelineService(db);

        var future = await sut.GetMyTimelineAsync(user.Id, null, DateTime.UtcNow.AddDays(1), null);
        var now = await sut.GetMyTimelineAsync(user.Id, null, DateTime.UtcNow.AddMinutes(-5), null);

        Assert.Empty(future);
        Assert.Single(now);
    }

    [Fact]
    public async Task GetMyTimelineAsync_OfferEvents_AppearAsActionsForTheActor()
    {
        // Closes the Offer audit-log gap: the recruiter who sends an offer should see it
        // as an "Action" entry in their own timeline, not just as a Notification to the candidate.
        using var db = TestDbContextFactory.Create();
        var recruiter = await SeedUserAsync(db, "rita@example.com");
        var auditLog = TestServiceFactory.CreateAuditLog(db);
        await auditLog.LogAsync(recruiter.Id, "Recruiter", "OfferSent", "Offer", 1);
        await db.SaveChangesAsync();
        var sut = TestServiceFactory.CreateActivityTimelineService(db);

        var timeline = await sut.GetMyTimelineAsync(recruiter.Id, null, null, null);

        Assert.Contains(timeline, e => e.Source == "Action" && e.Type == "OfferSent");
    }

    [Fact]
    public async Task GetMyTimelineAsync_AssessmentEvents_AppearAsActionsForTheActor()
    {
        // Closes the Skill Assessment audit-log gap the same way.
        using var db = TestDbContextFactory.Create();
        var candidate = await SeedUserAsync(db);
        var auditLog = TestServiceFactory.CreateAuditLog(db);
        await auditLog.LogAsync(candidate.Id, "Candidate", "AssessmentAttemptSubmitted", "SkillAssessmentAttempt", 1);
        await db.SaveChangesAsync();
        var sut = TestServiceFactory.CreateActivityTimelineService(db);

        var timeline = await sut.GetMyTimelineAsync(candidate.Id, null, null, null);

        Assert.Contains(timeline, e => e.Source == "Action" && e.Type == "AssessmentAttemptSubmitted");
    }
}
