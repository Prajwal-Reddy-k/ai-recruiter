using AIRecruiter.Application.DTOs.Feedback;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class FeedbackServiceTests
{
    private static FeedbackService CreateSut(AppDbContext db) =>
        new(db, new InMemoryIpRateLimiter(), TestServiceFactory.CreateAuditLog(db));

    [Fact]
    public async Task SubmitFeedbackAsync_ValidGuestSubmission_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var sut = CreateSut(db);

        await sut.SubmitFeedbackAsync(null, "127.0.0.1", new SubmitFeedbackRequest("Casey Candidate", "casey@example.com", FeedbackCategory.Bug, "The apply button doesn't work on Safari."));

        var feedback = Assert.Single(db.Feedbacks);
        Assert.Null(feedback.SubmittedByUserId);
        Assert.Equal(FeedbackStatus.New, feedback.Status);
    }

    [Fact]
    public async Task SubmitFeedbackAsync_AuthenticatedCaller_CapturesUserId()
    {
        using var db = TestDbContextFactory.Create();
        var user = new User { FullName = "Casey Candidate", Email = "casey@example.com", Role = UserRole.Candidate, PasswordHash = "x" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        await sut.SubmitFeedbackAsync(user.Id, "127.0.0.1", new SubmitFeedbackRequest("Casey Candidate", "casey@example.com", FeedbackCategory.General, "Just wanted to say thanks for the platform!"));

        var feedback = Assert.Single(db.Feedbacks);
        Assert.Equal(user.Id, feedback.SubmittedByUserId);
    }

    [Fact]
    public async Task SubmitFeedbackAsync_InvalidEmail_ThrowsValidationWithFieldError()
    {
        using var db = TestDbContextFactory.Create();
        var sut = CreateSut(db);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            sut.SubmitFeedbackAsync(null, "127.0.0.1", new SubmitFeedbackRequest("Casey", "not-an-email", FeedbackCategory.Other, "Some message here that is long enough.")));

        Assert.True(ex.FieldErrors!.ContainsKey("email"));
    }

    [Fact]
    public async Task SubmitFeedbackAsync_MessageTooShort_ThrowsValidationWithFieldError()
    {
        using var db = TestDbContextFactory.Create();
        var sut = CreateSut(db);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            sut.SubmitFeedbackAsync(null, "127.0.0.1", new SubmitFeedbackRequest("Casey", "casey@example.com", FeedbackCategory.Other, "short")));

        Assert.True(ex.FieldErrors!.ContainsKey("message"));
    }

    [Fact]
    public async Task SubmitFeedbackAsync_ExceedsRateLimit_ThrowsRateLimited()
    {
        using var db = TestDbContextFactory.Create();
        var sut = CreateSut(db);
        var request = new SubmitFeedbackRequest("Casey", "casey@example.com", FeedbackCategory.Other, "Message long enough to pass validation.");

        for (var i = 0; i < 5; i++)
        {
            await sut.SubmitFeedbackAsync(null, "9.9.9.9", request);
        }

        await Assert.ThrowsAsync<RateLimitedException>(() => sut.SubmitFeedbackAsync(null, "9.9.9.9", request));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsSubmitterNameWhenAuthenticated()
    {
        using var db = TestDbContextFactory.Create();
        var user = new User { FullName = "Casey Candidate", Email = "casey@example.com", Role = UserRole.Candidate, PasswordHash = "x" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var sut = CreateSut(db);
        await sut.SubmitFeedbackAsync(user.Id, "127.0.0.1", new SubmitFeedbackRequest("Casey Candidate", "casey@example.com", FeedbackCategory.General, "Loving the new dashboard layout."));

        var all = await sut.GetAllAsync();

        Assert.Single(all);
        Assert.Equal("Casey Candidate", all[0].SubmittedByName);
    }

    [Fact]
    public async Task SetStatusAsync_UpdatesStatus()
    {
        using var db = TestDbContextFactory.Create();
        var admin = new User { FullName = "Ops Admin", Email = "admin@example.com", Role = UserRole.Admin, PasswordHash = "x" };
        db.Users.Add(admin);
        await db.SaveChangesAsync();
        var sut = CreateSut(db);
        await sut.SubmitFeedbackAsync(null, "127.0.0.1", new SubmitFeedbackRequest("Casey", "casey@example.com", FeedbackCategory.Bug, "Message long enough to pass validation."));
        var feedback = db.Feedbacks.First();

        await sut.SetStatusAsync(admin.Id, feedback.Id, new SetFeedbackStatusRequest(FeedbackStatus.Resolved));

        Assert.Equal(FeedbackStatus.Resolved, db.Feedbacks.First(f => f.Id == feedback.Id).Status);
    }

    [Fact]
    public async Task SetStatusAsync_UnknownFeedback_ThrowsNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.SetStatusAsync(1, 9999, new SetFeedbackStatusRequest(FeedbackStatus.Resolved)));
    }
}
