using AIRecruiter.Application.DTOs.Companies;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class CompanyVerificationServiceTests
{
    private static SubmitCompanyVerificationRequest ValidRequest() =>
        new("https://acme.example.com", "hr@acme.example.com", "Bengaluru", "Karnataka", "A growing technology company building recruitment software.", "CIN-12345");

    private static async Task<(User Owner, User NonOwner, Company Company)> SeedAsync(AppDbContext db)
    {
        var ownerUser = new User { FullName = "Owen Owner", Email = "owen@example.com", Role = UserRole.Recruiter };
        var nonOwnerUser = new User { FullName = "Nina NonOwner", Email = "nina@example.com", Role = UserRole.Recruiter };
        db.Users.AddRange(ownerUser, nonOwnerUser);
        await db.SaveChangesAsync();

        var company = new Company { Name = "Acme Corp" };
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        db.RecruiterProfiles.AddRange(
            new RecruiterProfile { UserId = ownerUser.Id, CompanyId = company.Id, CompanyRole = CompanyRole.Owner },
            new RecruiterProfile { UserId = nonOwnerUser.Id, CompanyId = company.Id, CompanyRole = CompanyRole.Recruiter });
        await db.SaveChangesAsync();

        return (ownerUser, nonOwnerUser, company);
    }

    [Fact]
    public async Task SubmitAsync_Owner_MovesStatusToPending()
    {
        using var db = TestDbContextFactory.Create();
        var (owner, _, _) = await SeedAsync(db);
        var sut = TestServiceFactory.CreateCompanyVerificationService(db);

        var result = await sut.SubmitAsync(owner.Id, ValidRequest());

        Assert.Equal(CompanyVerificationStatus.Pending.ToString(), result.Status);
    }

    [Fact]
    public async Task SubmitAsync_NonOwnerRecruiter_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (_, nonOwner, _) = await SeedAsync(db);
        var sut = TestServiceFactory.CreateCompanyVerificationService(db);

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.SubmitAsync(nonOwner.Id, ValidRequest()));
    }

    [Fact]
    public async Task SubmitAsync_AlreadyPending_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var (owner, _, _) = await SeedAsync(db);
        var sut = TestServiceFactory.CreateCompanyVerificationService(db);
        await sut.SubmitAsync(owner.Id, ValidRequest());

        await Assert.ThrowsAsync<ConflictException>(() => sut.SubmitAsync(owner.Id, ValidRequest()));
    }

    [Fact]
    public async Task SubmitAsync_AlreadyVerified_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var (owner, _, company) = await SeedAsync(db);
        company.VerificationStatus = CompanyVerificationStatus.Verified;
        await db.SaveChangesAsync();
        var sut = TestServiceFactory.CreateCompanyVerificationService(db);

        await Assert.ThrowsAsync<ConflictException>(() => sut.SubmitAsync(owner.Id, ValidRequest()));
    }

    [Fact]
    public async Task SubmitAsync_AfterRejection_AllowsResubmission()
    {
        using var db = TestDbContextFactory.Create();
        var (owner, _, company) = await SeedAsync(db);
        company.VerificationStatus = CompanyVerificationStatus.Rejected;
        company.VerificationNote = "Please provide a valid business email.";
        await db.SaveChangesAsync();
        var sut = TestServiceFactory.CreateCompanyVerificationService(db);

        var result = await sut.SubmitAsync(owner.Id, ValidRequest());

        Assert.Equal(CompanyVerificationStatus.Pending.ToString(), result.Status);
        Assert.Null(result.Note);
    }

    [Fact]
    public async Task SubmitAsync_InvalidBusinessEmail_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var (owner, _, _) = await SeedAsync(db);
        var sut = TestServiceFactory.CreateCompanyVerificationService(db);
        var request = ValidRequest() with { BusinessEmail = "not-an-email" };

        await Assert.ThrowsAsync<ValidationException>(() => sut.SubmitAsync(owner.Id, request));
    }

    [Fact]
    public async Task GetMyStatusAsync_ReflectsCurrentCompanyStatus()
    {
        using var db = TestDbContextFactory.Create();
        var (owner, _, _) = await SeedAsync(db);
        var sut = TestServiceFactory.CreateCompanyVerificationService(db);

        var result = await sut.GetMyStatusAsync(owner.Id);

        Assert.Equal(CompanyVerificationStatus.NotSubmitted.ToString(), result.Status);
    }
}
