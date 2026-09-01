using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class CompanyAccessHelperTests
{
    private static async Task<(User Owner, User PlainRecruiter, User OtherCompanyOwner, Company Company)> SeedAsync(AppDbContext db)
    {
        var owner = new User { FullName = "Owner", Email = "owner@example.com", Role = UserRole.Recruiter };
        var plainRecruiter = new User { FullName = "Plain", Email = "plain@example.com", Role = UserRole.Recruiter };
        var otherOwner = new User { FullName = "Other Owner", Email = "other@example.com", Role = UserRole.Recruiter };
        db.Users.AddRange(owner, plainRecruiter, otherOwner);
        await db.SaveChangesAsync();

        var company = new Company { Name = "Acme Corp" };
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = owner.Id, Company = company, CompanyRole = CompanyRole.Owner });
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = plainRecruiter.Id, Company = company, CompanyRole = CompanyRole.Recruiter });
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = otherOwner.Id, Company = new Company { Name = "Other Co" }, CompanyRole = CompanyRole.Owner });
        await db.SaveChangesAsync();

        return (owner, plainRecruiter, otherOwner, company);
    }

    [Fact]
    public async Task ActualOwningRecruiter_AlwaysAllowed()
    {
        using var db = TestDbContextFactory.Create();
        var (owner, _, _, company) = await SeedAsync(db);

        var allowed = await CompanyAccessHelper.IsOwningRecruiterOrCompanyOwnerAsync(db, owner.Id, owner.Id, company.Id, CancellationToken.None);

        Assert.True(allowed);
    }

    [Fact]
    public async Task CompanyOwner_CanAccessColleaguesJob()
    {
        using var db = TestDbContextFactory.Create();
        var (owner, plainRecruiter, _, company) = await SeedAsync(db);

        // owner is not the owning recruiter (plainRecruiter is) but IS the company Owner.
        var allowed = await CompanyAccessHelper.IsOwningRecruiterOrCompanyOwnerAsync(db, owner.Id, plainRecruiter.Id, company.Id, CancellationToken.None);

        Assert.True(allowed);
    }

    [Fact]
    public async Task PlainRecruiter_CannotAccessColleaguesJob()
    {
        using var db = TestDbContextFactory.Create();
        var (owner, plainRecruiter, _, company) = await SeedAsync(db);

        // plainRecruiter is not the owning recruiter (owner is) and is not a company Owner.
        var allowed = await CompanyAccessHelper.IsOwningRecruiterOrCompanyOwnerAsync(db, plainRecruiter.Id, owner.Id, company.Id, CancellationToken.None);

        Assert.False(allowed);
    }

    [Fact]
    public async Task OwnerAtDifferentCompany_CannotAccess()
    {
        using var db = TestDbContextFactory.Create();
        var (owner, _, otherCompanyOwner, company) = await SeedAsync(db);

        var allowed = await CompanyAccessHelper.IsOwningRecruiterOrCompanyOwnerAsync(db, otherCompanyOwner.Id, owner.Id, company.Id, CancellationToken.None);

        Assert.False(allowed);
    }
}
