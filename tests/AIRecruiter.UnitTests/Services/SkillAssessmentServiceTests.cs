using AIRecruiter.Application.DTOs.Assessments;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class SkillAssessmentServiceTests
{
    private static SkillAssessmentService CreateSut(AppDbContext db) => TestServiceFactory.CreateSkillAssessmentService(db);

    private static async Task<User> SeedUserAsync(AppDbContext db, string email = "casey@example.com")
    {
        var user = new User { FullName = "Casey Candidate", Email = email, Role = UserRole.Candidate, PasswordHash = "x" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task SeedQuestionsAsync(AppDbContext db, AssessmentCategory category, int count = 20)
    {
        for (var i = 0; i < count; i++)
        {
            db.SkillAssessmentQuestions.Add(new SkillAssessmentQuestion
            {
                Category = category,
                QuestionText = $"Question {i}?",
                OptionA = "A",
                OptionB = "B",
                OptionC = "C",
                OptionD = "D",
                CorrectOptionIndex = 0,
            });
        }
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task StartAttemptAsync_ValidCategory_CreatesAttemptWithFifteenQuestions()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        await SeedQuestionsAsync(db, AssessmentCategory.Java);
        var sut = CreateSut(db);

        var attempt = await sut.StartAttemptAsync(user.Id, new StartAssessmentAttemptRequest("Java"));

        Assert.Equal(15, attempt.Questions.Count);
        Assert.Equal("Java", attempt.Category);
        Assert.All(attempt.Questions, q => Assert.False(string.IsNullOrEmpty(q.QuestionText)));
    }

    [Fact]
    public async Task StartAttemptAsync_QuestionsAreNotInDatabaseInsertOrder()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        await SeedQuestionsAsync(db, AssessmentCategory.Java, count: 40);
        var sut = CreateSut(db);

        var attempt = await sut.StartAttemptAsync(user.Id, new StartAssessmentAttemptRequest("Java"));
        var chosenTexts = attempt.Questions.Select(q => q.QuestionText).ToList();
        var firstFifteenInInsertOrder = Enumerable.Range(0, 15).Select(i => $"Question {i}?").ToList();

        // With a shuffled draw of 15 from 40, landing on exactly the first 15 inserted, in
        // that exact order, is astronomically unlikely — a mismatch here is proof the
        // selection/order isn't the raw insertion order.
        Assert.NotEqual(firstFifteenInInsertOrder, chosenTexts);
        Assert.Equal(15, chosenTexts.Distinct().Count());
    }

    [Fact]
    public async Task StartAttemptAsync_WhileOneAlreadyInProgress_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        await SeedQuestionsAsync(db, AssessmentCategory.Java);
        await SeedQuestionsAsync(db, AssessmentCategory.Python);
        var sut = CreateSut(db);

        await sut.StartAttemptAsync(user.Id, new StartAssessmentAttemptRequest("Java"));

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.StartAttemptAsync(user.Id, new StartAssessmentAttemptRequest("Python")));

        Assert.Equal("ATTEMPT_ALREADY_ACTIVE", ex.ErrorCode);
    }

    [Fact]
    public async Task StartAttemptAsync_WithinCooldownOfLastCompletedInSameCategory_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        await SeedQuestionsAsync(db, AssessmentCategory.Java);
        var sut = CreateSut(db);

        var attempt = await sut.StartAttemptAsync(user.Id, new StartAssessmentAttemptRequest("Java"));
        await sut.SubmitAttemptAsync(user.Id, attempt.AttemptId);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.StartAttemptAsync(user.Id, new StartAssessmentAttemptRequest("Java")));

        Assert.Equal("COOLDOWN_ACTIVE", ex.ErrorCode);
    }

    [Fact]
    public async Task StartAttemptAsync_DifferentCategoryAfterSubmitting_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        await SeedQuestionsAsync(db, AssessmentCategory.Java);
        await SeedQuestionsAsync(db, AssessmentCategory.Python);
        var sut = CreateSut(db);

        var first = await sut.StartAttemptAsync(user.Id, new StartAssessmentAttemptRequest("Java"));
        await sut.SubmitAttemptAsync(user.Id, first.AttemptId);

        var second = await sut.StartAttemptAsync(user.Id, new StartAssessmentAttemptRequest("Python"));
        Assert.Equal("Python", second.Category);
    }

    [Fact]
    public async Task AnswerQuestionAsync_AfterExpiry_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        await SeedQuestionsAsync(db, AssessmentCategory.Java);
        var sut = CreateSut(db);

        var attempt = await sut.StartAttemptAsync(user.Id, new StartAssessmentAttemptRequest("Java"));

        var entity = db.SkillAssessmentAttempts.Single(a => a.Id == attempt.AttemptId);
        entity.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();

        var firstAnswerId = attempt.Questions[0].AnswerId;
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.AnswerQuestionAsync(user.Id, attempt.AttemptId, new SubmitAssessmentAnswerRequest(firstAnswerId, 0)));

        Assert.Equal("ATTEMPT_EXPIRED", ex.ErrorCode);
    }

    [Fact]
    public async Task SubmitAttemptAsync_ComputesCorrectScore()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        await SeedQuestionsAsync(db, AssessmentCategory.Java);
        var sut = CreateSut(db);

        var attempt = await sut.StartAttemptAsync(user.Id, new StartAssessmentAttemptRequest("Java"));

        // Answer only the first 5 correctly (index 0, which SeedQuestionsAsync sets as
        // CorrectOptionIndex); leave the rest unanswered.
        foreach (var q in attempt.Questions.Take(5))
        {
            await sut.AnswerQuestionAsync(user.Id, attempt.AttemptId, new SubmitAssessmentAnswerRequest(q.AnswerId, 0));
        }

        var result = await sut.SubmitAttemptAsync(user.Id, attempt.AttemptId);

        Assert.Equal(5, result.ScoreCorrectCount);
        Assert.Equal(15, result.TotalQuestionCount);
        Assert.False(result.IsVisibleToRecruiters);
    }

    [Fact]
    public async Task SubmitAttemptAsync_AlreadyCompleted_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        await SeedQuestionsAsync(db, AssessmentCategory.Java);
        var sut = CreateSut(db);

        var attempt = await sut.StartAttemptAsync(user.Id, new StartAssessmentAttemptRequest("Java"));
        await sut.SubmitAttemptAsync(user.Id, attempt.AttemptId);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => sut.SubmitAttemptAsync(user.Id, attempt.AttemptId));
        Assert.Equal("ATTEMPT_NOT_ACTIVE", ex.ErrorCode);
    }

    [Fact]
    public async Task SetAttemptVisibilityAsync_OnAttemptOwnedByAnotherCandidate_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var owner = await SeedUserAsync(db, "owner@example.com");
        var intruder = await SeedUserAsync(db, "intruder@example.com");
        await SeedQuestionsAsync(db, AssessmentCategory.Java);
        var sut = CreateSut(db);

        var attempt = await sut.StartAttemptAsync(owner.Id, new StartAssessmentAttemptRequest("Java"));
        await sut.SubmitAttemptAsync(owner.Id, attempt.AttemptId);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            sut.SetAttemptVisibilityAsync(intruder.Id, attempt.AttemptId, new SetAttemptVisibilityRequest(true)));
    }

    [Fact]
    public async Task SetAttemptVisibilityAsync_OnIncompleteAttempt_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        await SeedQuestionsAsync(db, AssessmentCategory.Java);
        var sut = CreateSut(db);

        var attempt = await sut.StartAttemptAsync(user.Id, new StartAssessmentAttemptRequest("Java"));

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.SetAttemptVisibilityAsync(user.Id, attempt.AttemptId, new SetAttemptVisibilityRequest(true)));
        Assert.Equal("ATTEMPT_NOT_COMPLETED", ex.ErrorCode);
    }

    [Fact]
    public async Task SetAttemptVisibilityAsync_TrueThenQueriedInHistory_ReflectsOptIn()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        await SeedQuestionsAsync(db, AssessmentCategory.Java);
        var sut = CreateSut(db);

        var attempt = await sut.StartAttemptAsync(user.Id, new StartAssessmentAttemptRequest("Java"));
        await sut.SubmitAttemptAsync(user.Id, attempt.AttemptId);
        await sut.SetAttemptVisibilityAsync(user.Id, attempt.AttemptId, new SetAttemptVisibilityRequest(true));

        var history = await sut.GetMyHistoryAsync(user.Id);
        Assert.True(history.Single().IsVisibleToRecruiters);
    }
}
