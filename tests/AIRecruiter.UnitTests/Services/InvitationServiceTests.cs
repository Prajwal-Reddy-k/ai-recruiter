using AIRecruiter.Application.DTOs.Invitations;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class InvitationServiceTests
{
    private static InvitationService CreateSut(AppDbContext db) =>
        new(db, TestServiceFactory.CreateNotifications(db), TestServiceFactory.CreateAuditLog(db), new InMemoryIpRateLimiter());

    private static async Task<(User RecruiterUser, RecruiterProfile RecruiterProfile, Company Company, JobPosting Job)> SeedRecruiterAndJobAsync(AppDbContext db)
    {
        var recruiterUser = new User { FullName = "Rita Recruiter", Email = "rita@example.com", Role = UserRole.Recruiter, PasswordHash = "x" };
        db.Users.Add(recruiterUser);
        await db.SaveChangesAsync();
        var company = new Company { Name = "Acme Corp" };
        var recruiterProfile = new RecruiterProfile { UserId = recruiterUser.Id, Company = company };
        db.RecruiterProfiles.Add(recruiterProfile);
        await db.SaveChangesAsync();
        var job = new JobPosting
        {
            Title = "Backend Engineer",
            Description = "role",
            CompanyId = company.Id,
            RecruiterProfileId = recruiterProfile.Id,
            Status = JobStatus.Open,
            ModerationStatus = ModerationStatus.Approved,
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
    public async Task InviteAsync_DiscoverableCandidate_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiterUser, _, _, job) = await SeedRecruiterAndJobAsync(db);
        var candidate = await SeedCandidateAsync(db, ProfileVisibility.VisibleToRecruiters);
        var sut = CreateSut(db);

        var dto = await sut.InviteAsync(recruiterUser.Id, "127.0.0.1", new InviteCandidateRequest(job.Id, candidate.Id, "Come apply!"));

        Assert.Equal("Sent", dto.Status);
        Assert.Single(db.Invitations);
    }

    [Fact]
    public async Task InviteAsync_PublicShareableCandidate_IsAllowed()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiterUser, _, _, job) = await SeedRecruiterAndJobAsync(db);
        var candidate = await SeedCandidateAsync(db, ProfileVisibility.PublicShareable);
        var sut = CreateSut(db);

        var dto = await sut.InviteAsync(recruiterUser.Id, "127.0.0.1", new InviteCandidateRequest(job.Id, candidate.Id, null));

        Assert.Equal("Sent", dto.Status);
    }

    [Fact]
    public async Task InviteAsync_PrivateCandidate_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiterUser, _, _, job) = await SeedRecruiterAndJobAsync(db);
        var candidate = await SeedCandidateAsync(db, ProfileVisibility.Private);
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            sut.InviteAsync(recruiterUser.Id, "127.0.0.1", new InviteCandidateRequest(job.Id, candidate.Id, null)));
    }

    [Fact]
    public async Task InviteAsync_VisibleAfterApplyingWithoutApplication_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiterUser, _, _, job) = await SeedRecruiterAndJobAsync(db);
        var candidate = await SeedCandidateAsync(db, ProfileVisibility.VisibleAfterApplying);
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            sut.InviteAsync(recruiterUser.Id, "127.0.0.1", new InviteCandidateRequest(job.Id, candidate.Id, null)));
    }

    [Fact]
    public async Task InviteAsync_VisibleAfterApplyingWithApplicationAtCallerCompany_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiterUser, _, _, job) = await SeedRecruiterAndJobAsync(db);
        var candidate = await SeedCandidateAsync(db, ProfileVisibility.VisibleAfterApplying);
        db.JobApplications.Add(new JobApplication { CandidateProfileId = candidate.Id, JobPostingId = job.Id });
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var dto = await sut.InviteAsync(recruiterUser.Id, "127.0.0.1", new InviteCandidateRequest(job.Id, candidate.Id, null));

        Assert.Equal("Sent", dto.Status);
    }

    [Fact]
    public async Task InviteAsync_DuplicateActiveInvitation_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiterUser, _, _, job) = await SeedRecruiterAndJobAsync(db);
        var candidate = await SeedCandidateAsync(db, ProfileVisibility.VisibleToRecruiters);
        var sut = CreateSut(db);
        await sut.InviteAsync(recruiterUser.Id, "127.0.0.1", new InviteCandidateRequest(job.Id, candidate.Id, null));

        await Assert.ThrowsAsync<ConflictException>(() =>
            sut.InviteAsync(recruiterUser.Id, "127.0.0.1", new InviteCandidateRequest(job.Id, candidate.Id, null)));
    }

    [Fact]
    public async Task InviteAsync_OtherCompanyJob_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (_, _, _, job) = await SeedRecruiterAndJobAsync(db);
        var candidate = await SeedCandidateAsync(db, ProfileVisibility.VisibleToRecruiters);

        var outsiderUser = new User { FullName = "Owen Outsider", Email = "owen@example.com", Role = UserRole.Recruiter, PasswordHash = "x" };
        db.Users.Add(outsiderUser);
        await db.SaveChangesAsync();
        var outsiderCompany = new Company { Name = "Other Co" };
        var outsiderProfile = new RecruiterProfile { UserId = outsiderUser.Id, Company = outsiderCompany };
        db.RecruiterProfiles.Add(outsiderProfile);
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            sut.InviteAsync(outsiderUser.Id, "127.0.0.1", new InviteCandidateRequest(job.Id, candidate.Id, null)));
    }

    [Fact]
    public async Task RespondAsync_Accept_SetsAcceptedStatus()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiterUser, _, _, job) = await SeedRecruiterAndJobAsync(db);
        var candidate = await SeedCandidateAsync(db, ProfileVisibility.VisibleToRecruiters);
        var sut = CreateSut(db);
        var invite = await sut.InviteAsync(recruiterUser.Id, "127.0.0.1", new InviteCandidateRequest(job.Id, candidate.Id, null));

        var dto = await sut.RespondAsync(candidate.UserId, invite.Id, accept: true);

        Assert.Equal("Accepted", dto.Status);
    }

    [Fact]
    public async Task RespondAsync_AlreadyResolved_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiterUser, _, _, job) = await SeedRecruiterAndJobAsync(db);
        var candidate = await SeedCandidateAsync(db, ProfileVisibility.VisibleToRecruiters);
        var sut = CreateSut(db);
        var invite = await sut.InviteAsync(recruiterUser.Id, "127.0.0.1", new InviteCandidateRequest(job.Id, candidate.Id, null));
        await sut.RespondAsync(candidate.UserId, invite.Id, accept: false);

        await Assert.ThrowsAsync<ConflictException>(() => sut.RespondAsync(candidate.UserId, invite.Id, accept: true));
    }

    [Fact]
    public async Task RespondAsync_NotOwner_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiterUser, _, _, job) = await SeedRecruiterAndJobAsync(db);
        var candidate = await SeedCandidateAsync(db, ProfileVisibility.VisibleToRecruiters);
        var otherCandidate = await SeedCandidateAsync(db, ProfileVisibility.VisibleToRecruiters, "other@example.com");
        var sut = CreateSut(db);
        var invite = await sut.InviteAsync(recruiterUser.Id, "127.0.0.1", new InviteCandidateRequest(job.Id, candidate.Id, null));

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.RespondAsync(otherCandidate.UserId, invite.Id, accept: true));
    }
}
