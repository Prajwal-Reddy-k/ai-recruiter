using AIRecruiter.Application.DTOs.Offers;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class OfferServiceTests
{
    private static OfferService CreateSut(AppDbContext db) => TestServiceFactory.CreateOfferService(db);

    private static CreateOfferRequest ValidRequest() => new(
        1800000, "Annual", DateTime.UtcNow.AddMonths(1), "Bengaluru", "Karnataka", false, "FullTime",
        "3 months", "Health insurance", DateTime.UtcNow.AddDays(14), "Welcome aboard!");

    private static async Task<(User CandidateUser, User RecruiterUser, User OwnerUser, JobApplication Application)> SeedAsync(
        AppDbContext db, ApplicationStatus applicationStatus = ApplicationStatus.InterviewCompleted)
    {
        var candidateUser = new User { FullName = "Casey Candidate", Email = "casey@example.com", Role = UserRole.Candidate, PasswordHash = "x" };
        var recruiterUser = new User { FullName = "Rita Recruiter", Email = "rita@example.com", Role = UserRole.Recruiter, PasswordHash = "x" };
        var ownerUser = new User { FullName = "Owen Owner", Email = "owen@example.com", Role = UserRole.Recruiter, PasswordHash = "x" };
        db.Users.AddRange(candidateUser, recruiterUser, ownerUser);
        await db.SaveChangesAsync();

        var company = new Company { Name = "Acme Corp" };
        var recruiterProfile = new RecruiterProfile { UserId = recruiterUser.Id, Company = company, CompanyRole = CompanyRole.Recruiter };
        var ownerProfile = new RecruiterProfile { UserId = ownerUser.Id, Company = company, CompanyRole = CompanyRole.Owner };
        db.RecruiterProfiles.AddRange(recruiterProfile, ownerProfile);

        var candidateProfile = new CandidateProfile { UserId = candidateUser.Id };
        db.CandidateProfiles.Add(candidateProfile);
        await db.SaveChangesAsync();

        var job = new JobPosting
        {
            Title = "Backend Engineer", Description = "role", CompanyId = company.Id, RecruiterProfileId = recruiterProfile.Id,
            Status = JobStatus.Open, ModerationStatus = ModerationStatus.Approved,
        };
        db.JobPostings.Add(job);
        await db.SaveChangesAsync();

        var application = new JobApplication { JobPostingId = job.Id, CandidateProfileId = candidateProfile.Id, Status = applicationStatus };
        db.JobApplications.Add(application);
        await db.SaveChangesAsync();

        return (candidateUser, recruiterUser, ownerUser, application);
    }

    [Fact]
    public async Task CreateDraft_ByOwningRecruiter_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (_, recruiterUser, _, application) = await SeedAsync(db);
        var sut = CreateSut(db);

        var offer = await sut.CreateDraftAsync(recruiterUser.Id, application.Id, ValidRequest());

        Assert.Equal("Draft", offer.Status);
        Assert.Equal(1800000, offer.OfferedSalary);
    }

    [Fact]
    public async Task CreateDraft_ByCompanyOwnerNotOwningRecruiter_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (_, _, ownerUser, application) = await SeedAsync(db);
        var sut = CreateSut(db);

        var offer = await sut.CreateDraftAsync(ownerUser.Id, application.Id, ValidRequest());

        Assert.Equal("Draft", offer.Status);
    }

    [Fact]
    public async Task CreateDraft_ByOtherCompanyRecruiter_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (_, _, _, application) = await SeedAsync(db);
        var outsider = new User { FullName = "Otto Outsider", Email = "otto@example.com", Role = UserRole.Recruiter, PasswordHash = "x" };
        db.Users.Add(outsider);
        await db.SaveChangesAsync();
        var otherCompany = new Company { Name = "Other Co" };
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = outsider.Id, Company = otherCompany, CompanyRole = CompanyRole.Owner });
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.CreateDraftAsync(outsider.Id, application.Id, ValidRequest()));
    }

    [Fact]
    public async Task CreateDraft_ForFinalStatusApplication_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var (_, recruiterUser, _, application) = await SeedAsync(db, ApplicationStatus.Rejected);
        var sut = CreateSut(db);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => sut.CreateDraftAsync(recruiterUser.Id, application.Id, ValidRequest()));
        Assert.Equal("APPLICATION_FINAL", ex.ErrorCode);
    }

    [Fact]
    public async Task CreateDraft_WhenActiveOfferAlreadyExists_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var (_, recruiterUser, _, application) = await SeedAsync(db);
        var sut = CreateSut(db);

        await sut.CreateDraftAsync(recruiterUser.Id, application.Id, ValidRequest());

        var ex = await Assert.ThrowsAsync<ConflictException>(() => sut.CreateDraftAsync(recruiterUser.Id, application.Id, ValidRequest()));
        Assert.Equal("OFFER_ALREADY_ACTIVE", ex.ErrorCode);
    }

    [Fact]
    public async Task Send_FromDraft_SetsSentAndNotifiesCandidate()
    {
        using var db = TestDbContextFactory.Create();
        var (_, recruiterUser, _, application) = await SeedAsync(db);
        var sut = CreateSut(db);

        var draft = await sut.CreateDraftAsync(recruiterUser.Id, application.Id, ValidRequest());
        var sent = await sut.SendAsync(recruiterUser.Id, draft.Id);

        Assert.Equal("Sent", sent.Status);
        Assert.NotNull(sent.SentAtUtc);
        Assert.Single(db.Notifications);
    }

    [Fact]
    public async Task Respond_Accept_UpdatesApplicationStatusToHired_AndOfferStatus()
    {
        using var db = TestDbContextFactory.Create();
        var (candidateUser, recruiterUser, _, application) = await SeedAsync(db);
        var sut = CreateSut(db);

        var draft = await sut.CreateDraftAsync(recruiterUser.Id, application.Id, ValidRequest());
        await sut.SendAsync(recruiterUser.Id, draft.Id);
        var responded = await sut.RespondAsync(candidateUser.Id, draft.Id, new RespondToOfferRequest(true, null));

        Assert.Equal("Accepted", responded.Status);
        var app = db.JobApplications.Single(a => a.Id == application.Id);
        Assert.Equal(ApplicationStatus.Hired, app.Status);
    }

    [Fact]
    public async Task Respond_Decline_UpdatesApplicationStatusToRejected()
    {
        using var db = TestDbContextFactory.Create();
        var (candidateUser, recruiterUser, _, application) = await SeedAsync(db);
        var sut = CreateSut(db);

        var draft = await sut.CreateDraftAsync(recruiterUser.Id, application.Id, ValidRequest());
        await sut.SendAsync(recruiterUser.Id, draft.Id);
        var responded = await sut.RespondAsync(candidateUser.Id, draft.Id, new RespondToOfferRequest(false, "Accepted another offer"));

        Assert.Equal("Declined", responded.Status);
        var app = db.JobApplications.Single(a => a.Id == application.Id);
        Assert.Equal(ApplicationStatus.Rejected, app.Status);
    }

    [Fact]
    public async Task Respond_OnExpiredOffer_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var (candidateUser, recruiterUser, _, application) = await SeedAsync(db);
        var sut = CreateSut(db);

        var draft = await sut.CreateDraftAsync(recruiterUser.Id, application.Id, ValidRequest());
        await sut.SendAsync(recruiterUser.Id, draft.Id);

        var entity = db.Offers.Single(o => o.Id == draft.Id);
        entity.ExpiryDateUtc = DateTime.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ConflictException>(() => sut.RespondAsync(candidateUser.Id, draft.Id, new RespondToOfferRequest(true, null)));
        Assert.Equal("OFFER_EXPIRED", ex.ErrorCode);
    }

    [Fact]
    public async Task Respond_OnWithdrawnOffer_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var (candidateUser, recruiterUser, _, application) = await SeedAsync(db);
        var sut = CreateSut(db);

        var draft = await sut.CreateDraftAsync(recruiterUser.Id, application.Id, ValidRequest());
        await sut.SendAsync(recruiterUser.Id, draft.Id);
        await sut.WithdrawAsync(recruiterUser.Id, draft.Id);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => sut.RespondAsync(candidateUser.Id, draft.Id, new RespondToOfferRequest(true, null)));
        Assert.Equal("OFFER_ALREADY_DECIDED", ex.ErrorCode);
    }

    [Fact]
    public async Task Respond_OnAlreadyDecidedOffer_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var (candidateUser, recruiterUser, _, application) = await SeedAsync(db);
        var sut = CreateSut(db);

        var draft = await sut.CreateDraftAsync(recruiterUser.Id, application.Id, ValidRequest());
        await sut.SendAsync(recruiterUser.Id, draft.Id);
        await sut.RespondAsync(candidateUser.Id, draft.Id, new RespondToOfferRequest(true, null));

        await Assert.ThrowsAsync<ConflictException>(() => sut.RespondAsync(candidateUser.Id, draft.Id, new RespondToOfferRequest(false, null)));
    }

    [Fact]
    public async Task Respond_ByNonOwningCandidate_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (_, recruiterUser, _, application) = await SeedAsync(db);
        var intruder = new User { FullName = "Ivy Intruder", Email = "ivy@example.com", Role = UserRole.Candidate, PasswordHash = "x" };
        db.Users.Add(intruder);
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var draft = await sut.CreateDraftAsync(recruiterUser.Id, application.Id, ValidRequest());
        await sut.SendAsync(recruiterUser.Id, draft.Id);

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.RespondAsync(intruder.Id, draft.Id, new RespondToOfferRequest(true, null)));
    }

    [Fact]
    public async Task Withdraw_ByOwningRecruiter_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (_, recruiterUser, _, application) = await SeedAsync(db);
        var sut = CreateSut(db);

        var draft = await sut.CreateDraftAsync(recruiterUser.Id, application.Id, ValidRequest());
        var withdrawn = await sut.WithdrawAsync(recruiterUser.Id, draft.Id);

        Assert.Equal("Withdrawn", withdrawn.Status);
    }

    [Fact]
    public async Task GetDetail_IncludesStatusHistoryInOrder()
    {
        using var db = TestDbContextFactory.Create();
        var (candidateUser, recruiterUser, _, application) = await SeedAsync(db);
        var sut = CreateSut(db);

        var draft = await sut.CreateDraftAsync(recruiterUser.Id, application.Id, ValidRequest());
        await sut.SendAsync(recruiterUser.Id, draft.Id);
        await sut.RespondAsync(candidateUser.Id, draft.Id, new RespondToOfferRequest(true, "Excited to join"));

        var detail = await sut.GetDetailAsync(recruiterUser.Id, "Recruiter", draft.Id);

        Assert.Equal(2, detail.StatusHistory.Count);
        Assert.Equal("Sent", detail.StatusHistory[0].ToStatus);
        Assert.Equal("Accepted", detail.StatusHistory[1].ToStatus);
    }
}
