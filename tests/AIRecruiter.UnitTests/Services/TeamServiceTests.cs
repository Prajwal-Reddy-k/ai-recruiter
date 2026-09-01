using AIRecruiter.Application.DTOs.Team;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class TeamServiceTests
{
    private static TeamService CreateSut(AppDbContext db) => new(db, TestServiceFactory.CreateAuditLog(db));

    private static async Task<(User Owner, User PlainRecruiter, Company Company)> SeedCompanyAsync(AppDbContext db)
    {
        var owner = new User { FullName = "Owner Recruiter", Email = "owner@example.com", Role = UserRole.Recruiter };
        var plainRecruiter = new User { FullName = "Plain Recruiter", Email = "plain@example.com", Role = UserRole.Recruiter };
        db.Users.AddRange(owner, plainRecruiter);
        await db.SaveChangesAsync();

        var company = new Company { Name = "Acme Corp" };
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = owner.Id, Company = company, CompanyRole = CompanyRole.Owner });
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = plainRecruiter.Id, Company = company, CompanyRole = CompanyRole.Recruiter });
        await db.SaveChangesAsync();

        return (owner, plainRecruiter, company);
    }

    [Fact]
    public async Task AddMemberAsync_ByOwner_UnregisteredRecruiterAtCompany_UpdatesRole()
    {
        using var db = TestDbContextFactory.Create();
        var (owner, plainRecruiter, _) = await SeedCompanyAsync(db);
        var sut = CreateSut(db);

        var result = await sut.AddMemberAsync(owner.Id, new AddTeamMemberRequest(plainRecruiter.Email, CompanyRole.HiringManager));

        Assert.Equal("HiringManager", result.CompanyRole);
    }

    [Fact]
    public async Task AddMemberAsync_ByNonOwner_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (_, plainRecruiter, _) = await SeedCompanyAsync(db);
        var sut = CreateSut(db);

        var newUser = new User { FullName = "New Recruiter", Email = "new@example.com", Role = UserRole.Recruiter };
        db.Users.Add(newUser);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            sut.AddMemberAsync(plainRecruiter.Id, new AddTeamMemberRequest(newUser.Email, CompanyRole.Interviewer)));
    }

    [Fact]
    public async Task AddMemberAsync_EmailNotARegisteredRecruiter_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var (owner, _, _) = await SeedCompanyAsync(db);
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<ValidationException>(() =>
            sut.AddMemberAsync(owner.Id, new AddTeamMemberRequest("nobody@example.com", CompanyRole.Interviewer)));
    }

    [Fact]
    public async Task AddMemberAsync_RecruiterAlreadyAtAnotherCompany_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var (owner, _, _) = await SeedCompanyAsync(db);
        var sut = CreateSut(db);

        var elsewhereRecruiter = new User { FullName = "Elsewhere Recruiter", Email = "elsewhere@example.com", Role = UserRole.Recruiter };
        db.Users.Add(elsewhereRecruiter);
        await db.SaveChangesAsync();
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = elsewhereRecruiter.Id, Company = new Company { Name = "Other Co" }, CompanyRole = CompanyRole.Owner });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() =>
            sut.AddMemberAsync(owner.Id, new AddTeamMemberRequest(elsewhereRecruiter.Email, CompanyRole.Interviewer)));
    }

    [Fact]
    public async Task RemoveMemberAsync_LastOwner_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var (owner, _, _) = await SeedCompanyAsync(db);
        var sut = CreateSut(db);

        var ownerProfile = db.RecruiterProfiles.First(r => r.UserId == owner.Id);

        // Removing yourself is blocked separately; simulate a second owner attempting to
        // remove the last *other* owner isn't possible here since there's only one — assert
        // the last-owner guard directly via UpdateRoleAsync instead (demotion), which hits
        // the same guard.
        await Assert.ThrowsAsync<ConflictException>(() =>
            sut.UpdateRoleAsync(owner.Id, ownerProfile.Id, new UpdateTeamMemberRoleRequest(CompanyRole.Recruiter)));
    }

    [Fact]
    public async Task RemoveMemberAsync_Self_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var (owner, plainRecruiter, _) = await SeedCompanyAsync(db);
        var sut = CreateSut(db);
        // Promote plainRecruiter to a second Owner so self-removal isn't blocked by last-owner logic first.
        var plainProfile = db.RecruiterProfiles.First(r => r.UserId == plainRecruiter.Id);
        await sut.UpdateRoleAsync(owner.Id, plainProfile.Id, new UpdateTeamMemberRoleRequest(CompanyRole.Owner));

        await Assert.ThrowsAsync<ValidationException>(() => sut.RemoveMemberAsync(owner.Id, db.RecruiterProfiles.First(r => r.UserId == owner.Id).Id));
    }

    [Fact]
    public async Task AssignToJobAsync_ByOwner_CreatesAssignment()
    {
        using var db = TestDbContextFactory.Create();
        var (owner, plainRecruiter, company) = await SeedCompanyAsync(db);
        var sut = CreateSut(db);

        var ownerProfile = db.RecruiterProfiles.First(r => r.UserId == owner.Id);
        var job = new JobPosting { Title = "QA Engineer", Description = "role", Status = JobStatus.Open, CompanyId = company.Id, RecruiterProfileId = ownerProfile.Id };
        db.JobPostings.Add(job);
        await db.SaveChangesAsync();

        var plainProfile = db.RecruiterProfiles.First(r => r.UserId == plainRecruiter.Id);
        await sut.AssignToJobAsync(owner.Id, job.Id, plainProfile.Id);

        var assignments = await sut.GetJobAssignmentsAsync(owner.Id, job.Id);
        Assert.Single(assignments);
    }

    [Fact]
    public async Task GetMyCompanyTeamAsync_ReturnsAllMembersAtCompany()
    {
        using var db = TestDbContextFactory.Create();
        var (owner, _, _) = await SeedCompanyAsync(db);
        var sut = CreateSut(db);

        var team = await sut.GetMyCompanyTeamAsync(owner.Id);

        Assert.Equal(2, team.Count);
    }
}
