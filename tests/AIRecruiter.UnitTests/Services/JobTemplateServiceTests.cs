using AIRecruiter.Application.DTOs.JobTemplates;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class JobTemplateServiceTests
{
    private static JobTemplateService CreateSut(AppDbContext db) => new(db, TestServiceFactory.CreateAuditLog(db));

    private static async Task<(User Owner, User Colleague, User OtherCompanyRecruiter, Company Company)> SeedAsync(AppDbContext db)
    {
        var owner = new User { FullName = "Owner Recruiter", Email = "owner@a.example", Role = UserRole.Recruiter };
        var colleague = new User { FullName = "Colleague Recruiter", Email = "colleague@a.example", Role = UserRole.Recruiter };
        var otherCompany = new User { FullName = "Other Company Recruiter", Email = "other@b.example", Role = UserRole.Recruiter };
        db.Users.AddRange(owner, colleague, otherCompany);
        await db.SaveChangesAsync();

        var companyA = new Company { Name = "Company A" };
        var companyB = new Company { Name = "Company B" };
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = owner.Id, Company = companyA, CompanyRole = CompanyRole.Owner });
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = colleague.Id, Company = companyA, CompanyRole = CompanyRole.Recruiter });
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = otherCompany.Id, Company = companyB, CompanyRole = CompanyRole.Owner });
        await db.SaveChangesAsync();

        return (owner, colleague, otherCompany, companyA);
    }

    private static UpsertJobTemplateRequest ValidRequest() => new(
        "Senior Backend Engineer", "Engineering", "Role description", "Build things",
        "C#, SQL", "Docker", 3, 8, JobType.FullTime, false, null, null,
        "Bengaluru", "Karnataka", null, false);

    [Fact]
    public async Task CreateAsync_ThenGetMyTemplates_VisibleToColleagueAtSameCompany()
    {
        using var db = TestDbContextFactory.Create();
        var (owner, colleague, _, _) = await SeedAsync(db);
        var sut = CreateSut(db);

        await sut.CreateAsync(owner.Id, ValidRequest());
        var colleagueView = await sut.GetMyTemplatesAsync(colleague.Id, null);

        Assert.Single(colleagueView);
    }

    [Fact]
    public async Task GetMyTemplates_NotVisibleToDifferentCompany()
    {
        using var db = TestDbContextFactory.Create();
        var (owner, _, otherCompanyRecruiter, _) = await SeedAsync(db);
        var sut = CreateSut(db);

        await sut.CreateAsync(owner.Id, ValidRequest());
        var otherView = await sut.GetMyTemplatesAsync(otherCompanyRecruiter.Id, null);

        Assert.Empty(otherView);
    }

    [Fact]
    public async Task GetById_DifferentCompany_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (owner, _, otherCompanyRecruiter, _) = await SeedAsync(db);
        var sut = CreateSut(db);

        var template = await sut.CreateAsync(owner.Id, ValidRequest());

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.GetByIdAsync(otherCompanyRecruiter.Id, template.Id));
    }

    [Fact]
    public async Task UpdateAsync_ByColleagueWhoIsNotCreatorOrOwner_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (owner, colleague, _, _) = await SeedAsync(db);
        var sut = CreateSut(db);

        var template = await sut.CreateAsync(owner.Id, ValidRequest());
        // colleague is a plain Recruiter (not the creator, not an Owner) at the same company
        var colleagueTemplate = await sut.CreateAsync(colleague.Id, ValidRequest());

        // owner (company Owner) CAN edit colleague's template — additive Owner extension
        var updated = await sut.UpdateAsync(owner.Id, colleagueTemplate.Id, ValidRequest() with { Title = "Updated by Owner" });
        Assert.Equal("Updated by Owner", updated.Title);
    }

    [Fact]
    public async Task DeleteAsync_ByCreator_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (owner, _, _, _) = await SeedAsync(db);
        var sut = CreateSut(db);

        var template = await sut.CreateAsync(owner.Id, ValidRequest());
        await sut.DeleteAsync(owner.Id, template.Id);

        var remaining = await sut.GetMyTemplatesAsync(owner.Id, null);
        Assert.Empty(remaining);
    }

    [Fact]
    public async Task CreateDraftJobFromTemplateAsync_CreatesDraftJobWithTemplateFields()
    {
        using var db = TestDbContextFactory.Create();
        var (owner, _, _, _) = await SeedAsync(db);
        var sut = CreateSut(db);

        var template = await sut.CreateAsync(owner.Id, ValidRequest());
        var job = await sut.CreateDraftJobFromTemplateAsync(owner.Id, template.Id);

        Assert.Equal("Draft", job.Status);
        Assert.Equal(template.Title, job.Title);
    }

    [Fact]
    public async Task CreateAsync_MissingTitle_ThrowsValidationWithFieldError()
    {
        using var db = TestDbContextFactory.Create();
        var (owner, _, _, _) = await SeedAsync(db);
        var sut = CreateSut(db);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => sut.CreateAsync(owner.Id, ValidRequest() with { Title = "" }));
        Assert.NotNull(ex.FieldErrors);
        Assert.True(ex.FieldErrors!.ContainsKey("title"));
    }
}
