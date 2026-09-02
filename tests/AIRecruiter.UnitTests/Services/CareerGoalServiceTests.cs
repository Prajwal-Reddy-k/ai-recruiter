using AIRecruiter.Application.DTOs.CareerGoals;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class CareerGoalServiceTests
{
    private static CareerGoalService CreateSut(AppDbContext db) => TestServiceFactory.CreateCareerGoalService(db);

    private static async Task<User> SeedUserAsync(AppDbContext db, string email = "casey@example.com")
    {
        var user = new User { FullName = "Casey Candidate", Email = email, Role = UserRole.Candidate, PasswordHash = "x" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static UpsertCareerGoalRequest ValidRequest(string status = "InProgress") =>
        new("Senior Backend Engineer", "Kubernetes", null, "Karnataka", "Bengaluru", false, DateTime.UtcNow.AddMonths(6), 25, status, "Focusing on distributed systems.");

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesGoal()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var sut = CreateSut(db);

        var dto = await sut.CreateAsync(user.Id, ValidRequest());

        Assert.Equal("Senior Backend Engineer", dto.TargetRole);
        Assert.Equal("InProgress", dto.Status);
        Assert.Single(db.CareerGoals);
    }

    [Fact]
    public async Task Create_NoTargetFieldsProvided_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var sut = CreateSut(db);

        var request = new UpsertCareerGoalRequest(null, null, null, null, null, false, null, 0, "InProgress", null);
        var ex = await Assert.ThrowsAsync<ValidationException>(() => sut.CreateAsync(user.Id, request));

        Assert.True(ex.FieldErrors!.ContainsKey("targetRole"));
    }

    [Fact]
    public async Task Create_InvalidLocationPairing_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var sut = CreateSut(db);

        var request = new UpsertCareerGoalRequest("Backend Engineer", null, null, "Karnataka", "Notarealcityanywhere", false, null, 0, "InProgress", null);
        await Assert.ThrowsAsync<ValidationException>(() => sut.CreateAsync(user.Id, request));
    }

    [Fact]
    public async Task Create_ProgressOutOfRange_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var sut = CreateSut(db);

        var request = new UpsertCareerGoalRequest("Backend Engineer", null, null, null, null, false, null, 150, "InProgress", null);
        var ex = await Assert.ThrowsAsync<ValidationException>(() => sut.CreateAsync(user.Id, request));

        Assert.True(ex.FieldErrors!.ContainsKey("progressPercent"));
    }

    [Fact]
    public async Task UpdateAsync_OtherCandidatesGoal_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var owner = await SeedUserAsync(db, "owner@example.com");
        var intruder = await SeedUserAsync(db, "intruder@example.com");
        var sut = CreateSut(db);

        var goal = await sut.CreateAsync(owner.Id, ValidRequest());

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.UpdateAsync(intruder.Id, goal.Id, ValidRequest()));
    }

    [Fact]
    public async Task GetMyGoals_FilteredByStatus_ReturnsOnlyMatching()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var sut = CreateSut(db);

        await sut.CreateAsync(user.Id, ValidRequest("InProgress"));
        await sut.CreateAsync(user.Id, ValidRequest("Paused"));

        var pausedOnly = await sut.GetMyGoalsAsync(user.Id, "Paused");

        Assert.Single(pausedOnly.Goals);
        Assert.Equal("Paused", pausedOnly.Goals[0].Status);
    }

    [Fact]
    public async Task GetMyGoals_SuggestionsReflectMissingResumeSignal()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var sut = CreateSut(db);

        await sut.CreateAsync(user.Id, ValidRequest());
        var summary = await sut.GetMyGoalsAsync(user.Id, null);

        var goal = Assert.Single(summary.Goals);
        Assert.Contains(goal.Suggestions, s => s.Label.Contains("resume", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetMyGoals_CompletedGoal_HasNoSuggestions()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var sut = CreateSut(db);

        await sut.CreateAsync(user.Id, ValidRequest("Completed"));
        var summary = await sut.GetMyGoalsAsync(user.Id, null);

        Assert.Empty(summary.Goals.Single().Suggestions);
    }

    [Fact]
    public async Task DeleteAsync_OwnGoal_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var sut = CreateSut(db);

        var goal = await sut.CreateAsync(user.Id, ValidRequest());
        await sut.DeleteAsync(user.Id, goal.Id);

        Assert.Empty(db.CareerGoals);
    }
}
