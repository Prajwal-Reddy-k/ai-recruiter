using AIRecruiter.Application.DTOs.CoverLetters;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class CoverLetterTemplateServiceTests
{
    private static CoverLetterTemplateService CreateSut(AppDbContext db) => TestServiceFactory.CreateCoverLetterTemplateService(db);

    private static async Task<User> SeedUserAsync(AppDbContext db, string email = "casey@example.com")
    {
        var user = new User { FullName = "Casey Candidate", Email = email, Role = UserRole.Candidate, PasswordHash = "x" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesTemplate()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var sut = CreateSut(db);

        var dto = await sut.CreateAsync(user.Id, new UpsertCoverLetterTemplateRequest(
            "Backend roles", "I'm a backend engineer.", "C#, SQL, distributed systems", "Shipped a payments platform.", "Looking forward to hearing from you."));

        Assert.Equal("Backend roles", dto.Title);
        Assert.Single(db.CoverLetterTemplates);
    }

    [Fact]
    public async Task CreateAsync_DuplicateTitleSameCandidate_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var sut = CreateSut(db);

        await sut.CreateAsync(user.Id, new UpsertCoverLetterTemplateRequest("Backend roles", null, null, null, null));

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.CreateAsync(user.Id, new UpsertCoverLetterTemplateRequest("backend roles", null, null, null, null)));

        Assert.Equal("DUPLICATE_TEMPLATE_TITLE", ex.ErrorCode);
    }

    [Fact]
    public async Task CreateAsync_TitleTooLong_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var sut = CreateSut(db);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            sut.CreateAsync(user.Id, new UpsertCoverLetterTemplateRequest(new string('a', 101), null, null, null, null)));

        Assert.True(ex.FieldErrors!.ContainsKey("title"));
    }

    [Fact]
    public async Task UpdateAsync_AnotherUsersTemplate_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var owner = await SeedUserAsync(db, "owner@example.com");
        var intruder = await SeedUserAsync(db, "intruder@example.com");
        var sut = CreateSut(db);

        var template = await sut.CreateAsync(owner.Id, new UpsertCoverLetterTemplateRequest("Mine", null, null, null, null));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            sut.UpdateAsync(intruder.Id, template.Id, new UpsertCoverLetterTemplateRequest("Hijacked", null, null, null, null)));
    }

    [Fact]
    public async Task DeleteAsync_OwnTemplate_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var user = await SeedUserAsync(db);
        var sut = CreateSut(db);

        var template = await sut.CreateAsync(user.Id, new UpsertCoverLetterTemplateRequest("Mine", null, null, null, null));
        await sut.DeleteAsync(user.Id, template.Id);

        Assert.Empty(db.CoverLetterTemplates);
    }
}
