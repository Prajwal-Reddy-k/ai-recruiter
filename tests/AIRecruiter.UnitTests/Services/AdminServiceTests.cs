using AIRecruiter.Application.DTOs.Admin;
using AIRecruiter.Application.DTOs.Companies;
using AIRecruiter.Application.DTOs.Reviews;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class AdminServiceTests
{
    private static AdminService CreateSut(AppDbContext db) => new(db, TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateNotifications(db));

    private static async Task<(User Admin, User Recruiter, User Candidate)> SeedAsync(AppDbContext db)
    {
        var admin = new User { FullName = "Ops Admin", Email = "admin@example.com", Role = UserRole.Admin, PasswordHash = "x", SecurityStamp = "s0" };
        var recruiter = new User { FullName = "Rita Recruiter", Email = "rita@example.com", Role = UserRole.Recruiter, PasswordHash = "x", SecurityStamp = "s1" };
        var candidate = new User { FullName = "Casey Candidate", Email = "casey@example.com", Role = UserRole.Candidate, PasswordHash = "x", SecurityStamp = "s2" };
        db.Users.AddRange(admin, recruiter, candidate);
        await db.SaveChangesAsync();
        return (admin, recruiter, candidate);
    }

    [Fact]
    public async Task SuspendUserAsync_ActiveUser_SetsInactiveAndRotatesStamp()
    {
        using var db = TestDbContextFactory.Create();
        var (admin, _, candidate) = await SeedAsync(db);
        var sut = CreateSut(db);
        var originalStamp = candidate.SecurityStamp;

        await sut.SuspendUserAsync(admin.Id, candidate.Id);

        var reloaded = db.Users.First(u => u.Id == candidate.Id);
        Assert.False(reloaded.IsActive);
        Assert.NotEqual(originalStamp, reloaded.SecurityStamp);
    }

    [Fact]
    public async Task SuspendUserAsync_AdminAccount_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var (admin, _, _) = await SeedAsync(db);
        var otherAdmin = new User { FullName = "Other Admin", Email = "other-admin@example.com", Role = UserRole.Admin, PasswordHash = "x" };
        db.Users.Add(otherAdmin);
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<ValidationException>(() => sut.SuspendUserAsync(admin.Id, otherAdmin.Id));
    }

    [Fact]
    public async Task SuspendUserAsync_Self_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var (admin, _, _) = await SeedAsync(db);
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<ValidationException>(() => sut.SuspendUserAsync(admin.Id, admin.Id));
    }

    [Fact]
    public async Task ReactivateUserAsync_SuspendedUser_SetsActive()
    {
        using var db = TestDbContextFactory.Create();
        var (admin, _, candidate) = await SeedAsync(db);
        var sut = CreateSut(db);
        await sut.SuspendUserAsync(admin.Id, candidate.Id);

        await sut.ReactivateUserAsync(admin.Id, candidate.Id);

        Assert.True(db.Users.First(u => u.Id == candidate.Id).IsActive);
    }

    [Fact]
    public async Task GetReportsAsync_ResolvesEntityLabelForEachType()
    {
        using var db = TestDbContextFactory.Create();
        var (admin, recruiter, candidate) = await SeedAsync(db);
        var company = new Company { Name = "Acme Corp" };
        var recruiterProfile = new RecruiterProfile { UserId = recruiter.Id, Company = company };
        db.RecruiterProfiles.Add(recruiterProfile);
        await db.SaveChangesAsync();
        var job = new JobPosting { Title = "Backend Engineer", Description = "role", CompanyId = company.Id, RecruiterProfileId = recruiterProfile.Id };
        db.JobPostings.Add(job);
        await db.SaveChangesAsync();

        db.Reports.Add(new Report { EntityType = ReportedEntityType.Job, EntityId = job.Id, ReportedByUserId = candidate.Id, Reason = ReportReason.Spam });
        db.Reports.Add(new Report { EntityType = ReportedEntityType.Company, EntityId = company.Id, ReportedByUserId = candidate.Id, Reason = ReportReason.FakeCompany });
        db.Reports.Add(new Report { EntityType = ReportedEntityType.User, EntityId = recruiter.Id, ReportedByUserId = candidate.Id, Reason = ReportReason.Harassment });
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var reports = await sut.GetReportsAsync();

        Assert.Contains(reports, r => r.EntityType == "Job" && r.EntityLabel == "Backend Engineer");
        Assert.Contains(reports, r => r.EntityType == "Company" && r.EntityLabel == "Acme Corp");
        Assert.Contains(reports, r => r.EntityType == "User" && r.EntityLabel == "Rita Recruiter");
    }

    [Fact]
    public async Task SetReportStatusAsync_UpdatesStatusAndReviewer()
    {
        using var db = TestDbContextFactory.Create();
        var (admin, _, candidate) = await SeedAsync(db);
        var report = new Report { EntityType = ReportedEntityType.User, EntityId = candidate.Id, ReportedByUserId = candidate.Id, Reason = ReportReason.Other };
        db.Reports.Add(report);
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        await sut.SetReportStatusAsync(admin.Id, report.Id, new SetReportStatusRequest(ReportStatus.UnderReview, "Looking into it"));

        var reloaded = db.Reports.First(r => r.Id == report.Id);
        Assert.Equal(ReportStatus.UnderReview, reloaded.Status);
        Assert.Equal("Looking into it", reloaded.ModerationNote);
        Assert.Equal(admin.Id, reloaded.ReviewedByUserId);
    }

    [Fact]
    public async Task AddReportNoteAsync_DoesNotChangeStatus()
    {
        using var db = TestDbContextFactory.Create();
        var (admin, _, candidate) = await SeedAsync(db);
        var report = new Report { EntityType = ReportedEntityType.User, EntityId = candidate.Id, ReportedByUserId = candidate.Id, Reason = ReportReason.Other, Status = ReportStatus.Open };
        db.Reports.Add(report);
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        await sut.AddReportNoteAsync(admin.Id, report.Id, new AddReportNoteRequest("Internal note only"));

        var reloaded = db.Reports.First(r => r.Id == report.Id);
        Assert.Equal(ReportStatus.Open, reloaded.Status);
        Assert.Equal("Internal note only", reloaded.ModerationNote);
    }

    private static async Task<(User Admin, User Owner, Company Company)> SeedForVerificationAsync(AppDbContext db)
    {
        var admin = new User { FullName = "Ops Admin", Email = "admin2@example.com", Role = UserRole.Admin, PasswordHash = "x", SecurityStamp = "s0" };
        var owner = new User { FullName = "Owen Owner", Email = "owen2@example.com", Role = UserRole.Recruiter, PasswordHash = "x", SecurityStamp = "s1" };
        db.Users.AddRange(admin, owner);
        await db.SaveChangesAsync();

        var company = new Company { Name = "Acme Corp", VerificationStatus = CompanyVerificationStatus.Pending, VerificationSubmittedAtUtc = DateTime.UtcNow };
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = owner.Id, CompanyId = company.Id, CompanyRole = CompanyRole.Owner });
        await db.SaveChangesAsync();

        return (admin, owner, company);
    }

    [Fact]
    public async Task GetPendingCompanyVerificationsAsync_ReturnsOnlyPendingCompanies()
    {
        using var db = TestDbContextFactory.Create();
        var (_, _, company) = await SeedForVerificationAsync(db);
        db.Companies.Add(new Company { Name = "Verified Co", VerificationStatus = CompanyVerificationStatus.Verified });
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var pending = await sut.GetPendingCompanyVerificationsAsync();

        Assert.Single(pending);
        Assert.Equal(company.Id, pending[0].CompanyId);
    }

    [Fact]
    public async Task SetCompanyVerificationStatusAsync_Approve_UpdatesStatusAndNotifiesOwner()
    {
        using var db = TestDbContextFactory.Create();
        var (admin, owner, company) = await SeedForVerificationAsync(db);
        var sut = CreateSut(db);

        await sut.SetCompanyVerificationStatusAsync(admin.Id, company.Id, new SetCompanyVerificationStatusRequest("Verified", null));

        var reloaded = db.Companies.First(c => c.Id == company.Id);
        Assert.Equal(CompanyVerificationStatus.Verified, reloaded.VerificationStatus);
        Assert.Equal(admin.Id, reloaded.VerificationReviewedByUserId);
        Assert.Contains(db.Notifications, n => n.UserId == owner.Id && n.Type.StartsWith("CompanyVerification"));
        Assert.Contains(db.AuditLogEntries, e => e.ActorUserId == admin.Id && e.ActionType == "CompanyVerificationVerified");
    }

    [Fact]
    public async Task SetCompanyVerificationStatusAsync_Reject_RecordsInternalNote()
    {
        using var db = TestDbContextFactory.Create();
        var (admin, _, company) = await SeedForVerificationAsync(db);
        var sut = CreateSut(db);

        await sut.SetCompanyVerificationStatusAsync(admin.Id, company.Id, new SetCompanyVerificationStatusRequest("Rejected", "Business email domain does not match website."));

        var reloaded = db.Companies.First(c => c.Id == company.Id);
        Assert.Equal(CompanyVerificationStatus.Rejected, reloaded.VerificationStatus);
        Assert.Equal("Business email domain does not match website.", reloaded.VerificationNote);
    }

    [Fact]
    public async Task SetCompanyVerificationStatusAsync_NeedsMoreInfo_UpdatesStatus()
    {
        using var db = TestDbContextFactory.Create();
        var (admin, _, company) = await SeedForVerificationAsync(db);
        var sut = CreateSut(db);

        await sut.SetCompanyVerificationStatusAsync(admin.Id, company.Id, new SetCompanyVerificationStatusRequest("NeedsMoreInfo", "Please attach a registration certificate."));

        var reloaded = db.Companies.First(c => c.Id == company.Id);
        Assert.Equal(CompanyVerificationStatus.NeedsMoreInfo, reloaded.VerificationStatus);
    }

    [Fact]
    public async Task SetCompanyVerificationStatusAsync_InvalidStatus_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var (admin, _, company) = await SeedForVerificationAsync(db);
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<ValidationException>(
            () => sut.SetCompanyVerificationStatusAsync(admin.Id, company.Id, new SetCompanyVerificationStatusRequest("NotARealStatus", null)));
    }

    private static async Task<(User Admin, User Candidate, Company Company, CompanyReview Review)> SeedForReviewModerationAsync(AppDbContext db)
    {
        var admin = new User { FullName = "Ops Admin", Email = "admin3@example.com", Role = UserRole.Admin, PasswordHash = "x" };
        var candidateUser = new User { FullName = "Casey Candidate", Email = "casey3@example.com", Role = UserRole.Candidate, PasswordHash = "x" };
        db.Users.AddRange(admin, candidateUser);
        await db.SaveChangesAsync();

        var candidateProfile = new CandidateProfile { UserId = candidateUser.Id };
        db.CandidateProfiles.Add(candidateProfile);
        var company = new Company { Name = "Acme Corp" };
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        var review = new CompanyReview
        {
            CompanyId = company.Id,
            CandidateProfileId = candidateProfile.Id,
            OverallRating = 4, WorkCultureRating = 4, InterviewExperienceRating = 4, WorkLifeBalanceRating = 4, CareerGrowthRating = 4,
            Title = "Good place", Pros = "Pros", Cons = "Cons",
            RelationshipType = ReviewerRelationshipType.Applicant,
            Status = ReviewStatus.Pending,
        };
        db.CompanyReviews.Add(review);
        await db.SaveChangesAsync();

        return (admin, candidateUser, company, review);
    }

    [Fact]
    public async Task GetPendingReviewsAsync_ReturnsPendingAndFlaggedReviews()
    {
        using var db = TestDbContextFactory.Create();
        var (_, _, _, review) = await SeedForReviewModerationAsync(db);
        var sut = CreateSut(db);

        var pending = await sut.GetPendingReviewsAsync();

        Assert.Single(pending);
        Assert.Equal(review.Id, pending[0].Id);
    }

    [Fact]
    public async Task SetReviewStatusAsync_Approve_PublishesReview()
    {
        using var db = TestDbContextFactory.Create();
        var (admin, _, _, review) = await SeedForReviewModerationAsync(db);
        var sut = CreateSut(db);

        await sut.SetReviewStatusAsync(admin.Id, review.Id, new SetReviewStatusRequest("Published", null));

        var reloaded = db.CompanyReviews.First(r => r.Id == review.Id);
        Assert.Equal(ReviewStatus.Published, reloaded.Status);
        Assert.Equal(admin.Id, reloaded.ReviewedByUserId);
    }

    [Fact]
    public async Task SetReviewStatusAsync_InvalidStatus_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var (admin, _, _, review) = await SeedForReviewModerationAsync(db);
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<ValidationException>(
            () => sut.SetReviewStatusAsync(admin.Id, review.Id, new SetReviewStatusRequest("NotAStatus", null)));
    }

    [Fact]
    public async Task GetPendingDeletionRequestsAsync_ReturnsOnlyActiveUsersWithPendingRequest()
    {
        using var db = TestDbContextFactory.Create();
        var pendingUser = new User { FullName = "Pending Deletion", Email = "pending@example.com", Role = UserRole.Candidate, PasswordHash = "x", DeletionRequestedAt = DateTime.UtcNow.AddDays(-1) };
        var normalUser = new User { FullName = "Normal User", Email = "normal@example.com", Role = UserRole.Candidate, PasswordHash = "x" };
        db.Users.AddRange(pendingUser, normalUser);
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var pending = await sut.GetPendingDeletionRequestsAsync();

        Assert.Single(pending);
        Assert.Equal(pendingUser.Id, pending[0].Id);
    }

    [Fact]
    public async Task AdminCancelDeletionRequestAsync_ClearsTheFlag()
    {
        using var db = TestDbContextFactory.Create();
        var (admin, _, _, _) = await SeedForReviewModerationAsync(db);
        var pendingUser = new User { FullName = "Pending Deletion", Email = "pending2@example.com", Role = UserRole.Candidate, PasswordHash = "x", DeletionRequestedAt = DateTime.UtcNow.AddDays(-1) };
        db.Users.Add(pendingUser);
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        await sut.AdminCancelDeletionRequestAsync(admin.Id, pendingUser.Id);

        Assert.Null(db.Users.First(u => u.Id == pendingUser.Id).DeletionRequestedAt);
    }
}
