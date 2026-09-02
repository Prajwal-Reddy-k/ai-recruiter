using AIRecruiter.Application.DTOs.TalentPools;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class TalentPoolServiceTests
{
    private static TalentPoolService CreateSut(AppDbContext db) => TestServiceFactory.CreateTalentPoolService(db);

    private static async Task<(User RecruiterUser, RecruiterProfile RecruiterProfile, Company Company, JobPosting Job)> SeedRecruiterAndJobAsync(AppDbContext db)
    {
        var recruiterUser = new User { FullName = "Rita Recruiter", Email = "rita@example.com", Role = UserRole.Recruiter, PasswordHash = "x" };
        db.Users.Add(recruiterUser);
        await db.SaveChangesAsync();
        var company = new Company { Name = "Acme Corp" };
        var recruiterProfile = new RecruiterProfile { UserId = recruiterUser.Id, Company = company, CompanyRole = CompanyRole.Owner };
        db.RecruiterProfiles.Add(recruiterProfile);
        await db.SaveChangesAsync();
        var job = new JobPosting
        {
            Title = "Backend Engineer", Description = "role", CompanyId = company.Id, RecruiterProfileId = recruiterProfile.Id,
            Status = JobStatus.Open, ModerationStatus = ModerationStatus.Approved,
        };
        db.JobPostings.Add(job);
        await db.SaveChangesAsync();
        return (recruiterUser, recruiterProfile, company, job);
    }

    private static async Task<CandidateProfile> SeedCandidateAsync(AppDbContext db, ProfileVisibility visibility, string email = "casey@example.com")
    {
        var user = new User { FullName = "Casey Candidate", Email = email, Role = UserRole.Candidate, PasswordHash = "x" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var profile = new CandidateProfile { UserId = user.Id, ProfileVisibility = visibility };
        db.CandidateProfiles.Add(profile);
        await db.SaveChangesAsync();
        return profile;
    }

    [Fact]
    public async Task CreatePool_WithoutCompany_ThrowsNotOnboarded()
    {
        using var db = TestDbContextFactory.Create();
        var user = new User { FullName = "No Company", Email = "nc@example.com", Role = UserRole.Recruiter, PasswordHash = "x" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => sut.CreatePoolAsync(user.Id, new CreateTalentPoolRequest("Backend Talent")));
        Assert.Equal("NOT_ONBOARDED", ex.ErrorCode);
    }

    [Fact]
    public async Task CreatePool_ValidRequest_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiterUser, _, _, _) = await SeedRecruiterAndJobAsync(db);
        var sut = CreateSut(db);

        var pool = await sut.CreatePoolAsync(recruiterUser.Id, new CreateTalentPoolRequest("Backend Talent"));

        Assert.Equal("Backend Talent", pool.Name);
        Assert.Equal(0, pool.CandidateCount);
    }

    [Fact]
    public async Task AddCandidate_PrivateAndNotApplied_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiterUser, _, _, _) = await SeedRecruiterAndJobAsync(db);
        var candidate = await SeedCandidateAsync(db, ProfileVisibility.Private);
        var sut = CreateSut(db);

        var pool = await sut.CreatePoolAsync(recruiterUser.Id, new CreateTalentPoolRequest("Backend Talent"));

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            sut.AddCandidateAsync(recruiterUser.Id, pool.Id, new AddCandidateToPoolRequest(candidate.Id, null, null)));
        Assert.Contains("not visible", ex.Message);
    }

    [Fact]
    public async Task AddCandidate_VisibleToRecruiters_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiterUser, _, _, _) = await SeedRecruiterAndJobAsync(db);
        var candidate = await SeedCandidateAsync(db, ProfileVisibility.VisibleToRecruiters);
        var sut = CreateSut(db);

        var pool = await sut.CreatePoolAsync(recruiterUser.Id, new CreateTalentPoolRequest("Backend Talent"));
        await sut.AddCandidateAsync(recruiterUser.Id, pool.Id, new AddCandidateToPoolRequest(candidate.Id, "Strong candidate", "frontend,react"));

        var members = await sut.GetPoolCandidatesAsync(recruiterUser.Id, pool.Id);
        Assert.Single(members);
        Assert.Equal("Strong candidate", members[0].Notes);
    }

    [Fact]
    public async Task AddCandidate_AlreadyAppliedAtCallerCompany_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiterUser, _, _, job) = await SeedRecruiterAndJobAsync(db);
        var candidate = await SeedCandidateAsync(db, ProfileVisibility.VisibleAfterApplying);
        db.JobApplications.Add(new JobApplication { JobPostingId = job.Id, CandidateProfileId = candidate.Id, Status = ApplicationStatus.Applied });
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var pool = await sut.CreatePoolAsync(recruiterUser.Id, new CreateTalentPoolRequest("Applicants"));
        await sut.AddCandidateAsync(recruiterUser.Id, pool.Id, new AddCandidateToPoolRequest(candidate.Id, null, null));

        var members = await sut.GetPoolCandidatesAsync(recruiterUser.Id, pool.Id);
        Assert.Single(members);
        Assert.Equal("Backend Engineer", members[0].LatestApplicationJobTitle);
    }

    [Fact]
    public async Task AddCandidate_Duplicate_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiterUser, _, _, _) = await SeedRecruiterAndJobAsync(db);
        var candidate = await SeedCandidateAsync(db, ProfileVisibility.VisibleToRecruiters);
        var sut = CreateSut(db);

        var pool = await sut.CreatePoolAsync(recruiterUser.Id, new CreateTalentPoolRequest("Backend Talent"));
        await sut.AddCandidateAsync(recruiterUser.Id, pool.Id, new AddCandidateToPoolRequest(candidate.Id, null, null));

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.AddCandidateAsync(recruiterUser.Id, pool.Id, new AddCandidateToPoolRequest(candidate.Id, null, null)));
        Assert.Equal("ALREADY_IN_POOL", ex.ErrorCode);
    }

    [Fact]
    public async Task GetPoolCandidates_CrossCompanyAccess_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiterUser, _, _, _) = await SeedRecruiterAndJobAsync(db);
        var sut = CreateSut(db);
        var pool = await sut.CreatePoolAsync(recruiterUser.Id, new CreateTalentPoolRequest("Backend Talent"));

        var outsider = new User { FullName = "Otto Outsider", Email = "otto@example.com", Role = UserRole.Recruiter, PasswordHash = "x" };
        db.Users.Add(outsider);
        await db.SaveChangesAsync();
        var otherCompany = new Company { Name = "Other Co" };
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = outsider.Id, Company = otherCompany, CompanyRole = CompanyRole.Owner });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.GetPoolCandidatesAsync(outsider.Id, pool.Id));
    }

    [Fact]
    public async Task RenamePool_ByDifferentCompanyRecruiterOwner_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiterUser, _, company, _) = await SeedRecruiterAndJobAsync(db);
        var sut = CreateSut(db);
        var pool = await sut.CreatePoolAsync(recruiterUser.Id, new CreateTalentPoolRequest("Backend Talent"));

        var colleague = new User { FullName = "Cole Colleague", Email = "cole@example.com", Role = UserRole.Recruiter, PasswordHash = "x" };
        db.Users.Add(colleague);
        await db.SaveChangesAsync();
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = colleague.Id, CompanyId = company.Id, CompanyRole = CompanyRole.Recruiter });
        await db.SaveChangesAsync();

        var renamed = await sut.RenamePoolAsync(colleague.Id, pool.Id, new RenameTalentPoolRequest("Frontend Talent"));
        Assert.Equal("Frontend Talent", renamed.Name);
    }

    [Fact]
    public async Task DeletePool_RemovesPool()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiterUser, _, _, _) = await SeedRecruiterAndJobAsync(db);
        var candidate = await SeedCandidateAsync(db, ProfileVisibility.VisibleToRecruiters);
        var sut = CreateSut(db);

        var pool = await sut.CreatePoolAsync(recruiterUser.Id, new CreateTalentPoolRequest("Backend Talent"));
        await sut.AddCandidateAsync(recruiterUser.Id, pool.Id, new AddCandidateToPoolRequest(candidate.Id, null, null));
        await sut.DeletePoolAsync(recruiterUser.Id, pool.Id);

        Assert.Empty(db.TalentPools);
    }
}
