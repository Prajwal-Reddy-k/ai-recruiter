using AIRecruiter.Application.Exceptions;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class PublicProfileServiceTests
{
    private static PublicProfileService CreateSut(AppDbContext db) => TestServiceFactory.CreatePublicProfileService(db);

    private static async Task<CandidateProfile> SeedCandidateAsync(AppDbContext db, ProfileVisibility visibility, string? slug, string email = "casey@example.com")
    {
        var user = new User { FullName = "Casey Candidate", Email = email, Role = UserRole.Candidate, PasswordHash = "x" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var profile = new CandidateProfile
        {
            UserId = user.Id,
            Headline = "Backend Engineer",
            SkillsCsv = "C#, SQL",
            Phone = "9999999999",
            ProfileVisibility = visibility,
            PublicProfileSlug = slug,
        };
        db.CandidateProfiles.Add(profile);
        await db.SaveChangesAsync();
        return profile;
    }

    [Fact]
    public async Task GetBySlug_PrivateProfile_ThrowsNotFound()
    {
        using var db = TestDbContextFactory.Create();
        await SeedCandidateAsync(db, ProfileVisibility.Private, "casey-abc123");
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetBySlugAsync("casey-abc123"));
    }

    [Fact]
    public async Task GetBySlug_VisibleToRecruitersOnly_ThrowsNotFound()
    {
        using var db = TestDbContextFactory.Create();
        // A candidate can have a leftover slug from a past PublicShareable period even
        // after switching back to VisibleToRecruiters — the link must stop working.
        await SeedCandidateAsync(db, ProfileVisibility.VisibleToRecruiters, "casey-abc123");
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetBySlugAsync("casey-abc123"));
    }

    [Fact]
    public async Task GetBySlug_UnknownSlug_ThrowsNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetBySlugAsync("nobody-here"));
    }

    [Fact]
    public async Task GetBySlug_PublicShareable_ReturnsSafeFieldsOnly()
    {
        using var db = TestDbContextFactory.Create();
        await SeedCandidateAsync(db, ProfileVisibility.PublicShareable, "casey-abc123");
        var sut = CreateSut(db);

        var dto = await sut.GetBySlugAsync("casey-abc123");

        Assert.Equal("Casey Candidate", dto.FullName);
        Assert.Equal("Backend Engineer", dto.Headline);
        Assert.Equal("C#, SQL", dto.SkillsCsv);
        Assert.Empty(dto.AssessmentBadges);
        // The DTO's type itself has no Phone/Email/ResumeStorageKey/Salary fields — this
        // assertion documents that guarantee rather than merely re-checking it.
    }

    [Fact]
    public async Task GetBySlug_OnlyOptedInAssessmentBadgesReturned()
    {
        using var db = TestDbContextFactory.Create();
        var profile = await SeedCandidateAsync(db, ProfileVisibility.PublicShareable, "casey-abc123");

        db.SkillAssessmentAttempts.AddRange(
            new SkillAssessmentAttempt
            {
                CandidateProfileId = profile.Id, Category = AssessmentCategory.Java, Status = AssessmentAttemptStatus.Completed,
                ExpiresAt = DateTime.UtcNow, SubmittedAt = DateTime.UtcNow, ScoreCorrectCount = 12, TotalQuestionCount = 15, PercentageScore = 80,
                IsVisibleToRecruiters = true,
            },
            new SkillAssessmentAttempt
            {
                CandidateProfileId = profile.Id, Category = AssessmentCategory.Python, Status = AssessmentAttemptStatus.Completed,
                ExpiresAt = DateTime.UtcNow, SubmittedAt = DateTime.UtcNow, ScoreCorrectCount = 9, TotalQuestionCount = 15, PercentageScore = 60,
                IsVisibleToRecruiters = false,
            });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var dto = await sut.GetBySlugAsync("casey-abc123");

        var badge = Assert.Single(dto.AssessmentBadges);
        Assert.Equal("Java", badge.Category);
    }

    [Fact]
    public async Task GetMyPreviewAsync_WhilePrivate_StillBuildsPreview()
    {
        using var db = TestDbContextFactory.Create();
        var profile = await SeedCandidateAsync(db, ProfileVisibility.Private, null);
        var sut = CreateSut(db);

        var preview = await sut.GetMyPreviewAsync(profile.UserId);

        Assert.False(preview.IsCurrentlyPublic);
        Assert.Null(preview.PublicUrl);
        Assert.Equal("Casey Candidate", preview.Preview.FullName);
    }
}
