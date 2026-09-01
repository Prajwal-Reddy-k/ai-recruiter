using AIRecruiter.Application.DTOs.Candidates;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class ResumeBuilderServiceTests
{
    private static ResumeBuilderService CreateSut(AppDbContext db) => new(db);

    private static async Task<User> SeedUserAsync(AppDbContext db, string email = "casey@example.com")
    {
        var user = new User { FullName = "Casey Candidate", Email = email, Role = UserRole.Candidate, PasswordHash = "x" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task GetMyResumeAsync_NoExistingProfile_CreatesOneAndReturnsEmptyResume()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var sut = CreateSut(db);

        var resume = await sut.GetMyResumeAsync(user.Id);

        Assert.Empty(resume.WorkExperiences);
        Assert.Single(db.CandidateProfiles);
    }

    [Fact]
    public async Task UpsertSummaryLinksAsync_ValidRequest_SavesFields()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var sut = CreateSut(db);

        var resume = await sut.UpsertSummaryLinksAsync(user.Id, new UpsertResumeSummaryRequest(
            "Experienced backend engineer.", "C#, SQL", "https://linkedin.com/in/casey", "https://github.com/casey", null, "Won hackathon\nShipped v1"));

        Assert.Equal("Experienced backend engineer.", resume.Summary);
        Assert.Equal("Won hackathon\nShipped v1", resume.AchievementsText);
    }

    [Fact]
    public async Task UpsertSummaryLinksAsync_InvalidLinkedInDomain_ThrowsValidationWithFieldError()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var sut = CreateSut(db);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            sut.UpsertSummaryLinksAsync(user.Id, new UpsertResumeSummaryRequest(null, null, "https://notlinkedin.com/casey", null, null, null)));

        Assert.True(ex.FieldErrors!.ContainsKey("linkedInUrl"));
    }

    [Fact]
    public async Task AddExperienceAsync_ValidRequest_AddsEntryAndReturnsIt()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var sut = CreateSut(db);

        var resume = await sut.AddExperienceAsync(user.Id, new UpsertWorkExperienceRequest(
            "Backend Engineer", "Acme Corp", "Bengaluru", DateTime.UtcNow.AddYears(-2), null, "Built APIs."));

        var entry = Assert.Single(resume.WorkExperiences);
        Assert.Equal("Backend Engineer", entry.Title);
        Assert.Equal(0, entry.DisplayOrder);
    }

    [Fact]
    public async Task AddExperienceAsync_MissingTitle_ThrowsValidationWithFieldError()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var sut = CreateSut(db);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            sut.AddExperienceAsync(user.Id, new UpsertWorkExperienceRequest("", "Acme", null, DateTime.UtcNow, null, null)));

        Assert.True(ex.FieldErrors!.ContainsKey("title"));
    }

    [Fact]
    public async Task AddExperienceAsync_EndDateBeforeStartDate_ThrowsValidationWithFieldError()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var sut = CreateSut(db);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            sut.AddExperienceAsync(user.Id, new UpsertWorkExperienceRequest("Engineer", "Acme", null, DateTime.UtcNow, DateTime.UtcNow.AddYears(-1), null)));

        Assert.True(ex.FieldErrors!.ContainsKey("endDate"));
    }

    [Fact]
    public async Task UpdateExperienceAsync_AnotherUsersEntry_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var owner = await SeedUserAsync(db, "owner@example.com");
        var attacker = await SeedUserAsync(db, "attacker@example.com");
        var sut = CreateSut(db);
        var resume = await sut.AddExperienceAsync(owner.Id, new UpsertWorkExperienceRequest("Engineer", "Acme", null, DateTime.UtcNow.AddYears(-1), null, null));
        var entryId = resume.WorkExperiences.Single().Id;

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            sut.UpdateExperienceAsync(attacker.Id, entryId, new UpsertWorkExperienceRequest("Hacked", "Acme", null, DateTime.UtcNow.AddYears(-1), null, null)));
    }

    [Fact]
    public async Task DeleteExperienceAsync_AnotherUsersEntry_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var owner = await SeedUserAsync(db, "owner@example.com");
        var attacker = await SeedUserAsync(db, "attacker@example.com");
        var sut = CreateSut(db);
        var resume = await sut.AddExperienceAsync(owner.Id, new UpsertWorkExperienceRequest("Engineer", "Acme", null, DateTime.UtcNow.AddYears(-1), null, null));
        var entryId = resume.WorkExperiences.Single().Id;

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.DeleteExperienceAsync(attacker.Id, entryId));

        Assert.Single(db.CandidateWorkExperiences);
    }

    [Fact]
    public async Task DeleteExperienceAsync_Owner_RemovesEntry()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var sut = CreateSut(db);
        var resume = await sut.AddExperienceAsync(user.Id, new UpsertWorkExperienceRequest("Engineer", "Acme", null, DateTime.UtcNow.AddYears(-1), null, null));
        var entryId = resume.WorkExperiences.Single().Id;

        var updated = await sut.DeleteExperienceAsync(user.Id, entryId);

        Assert.Empty(updated.WorkExperiences);
    }

    [Fact]
    public async Task ReorderExperienceAsync_ValidOrder_UpdatesDisplayOrder()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var sut = CreateSut(db);
        var first = await sut.AddExperienceAsync(user.Id, new UpsertWorkExperienceRequest("First", "Acme", null, DateTime.UtcNow.AddYears(-3), null, null));
        var firstId = first.WorkExperiences.Single().Id;
        var second = await sut.AddExperienceAsync(user.Id, new UpsertWorkExperienceRequest("Second", "Acme", null, DateTime.UtcNow.AddYears(-1), null, null));
        var secondId = second.WorkExperiences.Last().Id;

        var reordered = await sut.ReorderExperienceAsync(user.Id, new ReorderRequest(new[] { secondId, firstId }));

        Assert.Equal("Second", reordered.WorkExperiences[0].Title);
        Assert.Equal("First", reordered.WorkExperiences[1].Title);
    }

    [Fact]
    public async Task ReorderExperienceAsync_MismatchedIds_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var sut = CreateSut(db);
        await sut.AddExperienceAsync(user.Id, new UpsertWorkExperienceRequest("Engineer", "Acme", null, DateTime.UtcNow.AddYears(-1), null, null));

        await Assert.ThrowsAsync<ValidationException>(() => sut.ReorderExperienceAsync(user.Id, new ReorderRequest(new[] { 9999 })));
    }

    [Fact]
    public async Task AddEducationAsync_ValidRequest_AddsEntry()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var sut = CreateSut(db);

        var resume = await sut.AddEducationAsync(user.Id, new UpsertEducationEntryRequest("IIT Bombay", "B.Tech", "Computer Science", null, null, null, null));

        Assert.Single(resume.Educations);
    }

    [Fact]
    public async Task AddCertificationAsync_ExpiryBeforeIssue_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var sut = CreateSut(db);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            sut.AddCertificationAsync(user.Id, new UpsertCertificationRequest("AWS Certified", "AWS", DateTime.UtcNow, DateTime.UtcNow.AddYears(-1), null)));

        Assert.True(ex.FieldErrors!.ContainsKey("expiryDate"));
    }

    [Fact]
    public async Task AddProjectAsync_ValidRequest_AddsEntry()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var sut = CreateSut(db);

        var resume = await sut.AddProjectAsync(user.Id, new UpsertProjectRequest("Portfolio site", "A personal site.", "https://example.com", "React, TypeScript"));

        Assert.Single(resume.Projects);
    }

    [Fact]
    public async Task GetMyResumeAsync_StrengthReflectsFilledSections()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var sut = CreateSut(db);
        await sut.AddExperienceAsync(user.Id, new UpsertWorkExperienceRequest("Engineer", "Acme", null, DateTime.UtcNow.AddYears(-1), null, null));

        var resume = await sut.GetMyResumeAsync(user.Id);

        Assert.True(resume.Strength.Score > 0);
        Assert.DoesNotContain(resume.Strength.MissingItems, i => i.Label.Contains("work experience", StringComparison.OrdinalIgnoreCase));
    }
}
