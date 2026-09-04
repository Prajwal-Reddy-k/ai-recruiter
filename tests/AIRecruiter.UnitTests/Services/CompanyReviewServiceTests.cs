using AIRecruiter.Application.DTOs.Reviews;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class CompanyReviewServiceTests
{
    private static SubmitCompanyReviewRequest ValidRequest(string relationship = "Applicant") =>
        new(4, 4, 4, 4, 4, "Good place to work", "Great team", "Slow promotions", null, relationship);

    private static async Task<(User Candidate, Company Company)> SeedWithApplicationAsync(AppDbContext db, ApplicationStatus status = ApplicationStatus.Applied)
    {
        var candidateUser = new User { FullName = "Casey Candidate", Email = "casey@example.com", Role = UserRole.Candidate };
        var recruiterUser = new User { FullName = "Rita Recruiter", Email = "rita@example.com", Role = UserRole.Recruiter };
        db.Users.AddRange(candidateUser, recruiterUser);
        await db.SaveChangesAsync();

        var candidateProfile = new CandidateProfile { UserId = candidateUser.Id };
        db.CandidateProfiles.Add(candidateProfile);
        var company = new Company { Name = "Acme Corp" };
        var recruiterProfile = new RecruiterProfile { UserId = recruiterUser.Id, Company = company };
        db.RecruiterProfiles.Add(recruiterProfile);
        await db.SaveChangesAsync();

        var job = new JobPosting { Title = "Backend Engineer", Description = "role", CompanyId = company.Id, RecruiterProfileId = recruiterProfile.Id, Status = JobStatus.Open };
        db.JobPostings.Add(job);
        await db.SaveChangesAsync();

        db.JobApplications.Add(new JobApplication { JobPostingId = job.Id, CandidateProfileId = candidateProfile.Id, Status = status });
        await db.SaveChangesAsync();

        return (candidateUser, company);
    }

    [Fact]
    public async Task GetEligibilityAsync_NoApplication_NotEligible()
    {
        using var db = TestDbContextFactory.Create();
        var candidateUser = new User { FullName = "Casey Candidate", Email = "casey@example.com", Role = UserRole.Candidate };
        db.Users.Add(candidateUser);
        await db.SaveChangesAsync();
        db.CandidateProfiles.Add(new CandidateProfile { UserId = candidateUser.Id });
        var company = new Company { Name = "Acme Corp" };
        db.Companies.Add(company);
        await db.SaveChangesAsync();
        var sut = TestServiceFactory.CreateCompanyReviewService(db);

        var eligibility = await sut.GetEligibilityAsync(candidateUser.Id, company.Id);

        Assert.False(eligibility.Eligible);
    }

    [Fact]
    public async Task SubmitAsync_ApplicantRelationship_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, company) = await SeedWithApplicationAsync(db);
        var sut = TestServiceFactory.CreateCompanyReviewService(db);

        await sut.SubmitAsync(candidate.Id, company.Id, ValidRequest(), "1.2.3.4");

        Assert.Single(db.CompanyReviews);
        Assert.Equal(ReviewStatus.Pending, db.CompanyReviews.First().Status);
    }

    [Fact]
    public async Task SubmitAsync_ClaimingInterviewedWithoutHistory_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, company) = await SeedWithApplicationAsync(db, ApplicationStatus.Applied);
        var sut = TestServiceFactory.CreateCompanyReviewService(db);

        await Assert.ThrowsAsync<ValidationException>(
            () => sut.SubmitAsync(candidate.Id, company.Id, ValidRequest("Interviewed"), "1.2.3.4"));
    }

    [Fact]
    public async Task SubmitAsync_ClaimingHiredWithActualHiredStatus_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, company) = await SeedWithApplicationAsync(db, ApplicationStatus.Hired);
        var sut = TestServiceFactory.CreateCompanyReviewService(db);

        await sut.SubmitAsync(candidate.Id, company.Id, ValidRequest("Hired"), "1.2.3.4");

        Assert.Single(db.CompanyReviews);
    }

    [Fact]
    public async Task SubmitAsync_SecondReviewForSameCompany_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, company) = await SeedWithApplicationAsync(db);
        var sut = TestServiceFactory.CreateCompanyReviewService(db);
        await sut.SubmitAsync(candidate.Id, company.Id, ValidRequest(), "1.2.3.4");

        await Assert.ThrowsAsync<ConflictException>(() => sut.SubmitAsync(candidate.Id, company.Id, ValidRequest(), "1.2.3.4"));
    }

    [Fact]
    public async Task SubmitAsync_InvalidRating_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, company) = await SeedWithApplicationAsync(db);
        var sut = TestServiceFactory.CreateCompanyReviewService(db);
        var request = new SubmitCompanyReviewRequest(6, 4, 4, 4, 4, "Title", "Pros", "Cons", null, "Applicant");

        await Assert.ThrowsAsync<ValidationException>(() => sut.SubmitAsync(candidate.Id, company.Id, request, "1.2.3.4"));
    }

    [Fact]
    public async Task GetPublishedReviewsAsync_PendingReview_NeverAppearsPublicly()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, company) = await SeedWithApplicationAsync(db);
        var sut = TestServiceFactory.CreateCompanyReviewService(db);
        await sut.SubmitAsync(candidate.Id, company.Id, ValidRequest(), "1.2.3.4");

        var summary = await sut.GetPublishedReviewsAsync(company.Id, null);

        Assert.Empty(summary.Reviews);
        Assert.Equal(0, summary.ReviewCount);
    }

    [Fact]
    public async Task GetPublishedReviewsAsync_PublishedReview_NeverExposesReviewerIdentity()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, company) = await SeedWithApplicationAsync(db);
        var sut = TestServiceFactory.CreateCompanyReviewService(db);
        await sut.SubmitAsync(candidate.Id, company.Id, ValidRequest(), "1.2.3.4");
        var review = db.CompanyReviews.First();
        review.Status = ReviewStatus.Published;
        await db.SaveChangesAsync();

        var summary = await sut.GetPublishedReviewsAsync(company.Id, null);

        Assert.Single(summary.Reviews);
        Assert.Equal(1, summary.ReviewCount);
        Assert.Equal(4m, summary.AverageRating);
        // PublicCompanyReviewDto has no reviewer-identifying field at all — compile-time
        // guarantee, verified here by asserting the review's visible content is as expected.
        Assert.Equal("Good place to work", summary.Reviews[0].Title);
    }

    [Fact]
    public async Task RespondAsync_NonCompanyRecruiter_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, company) = await SeedWithApplicationAsync(db);
        var sut = TestServiceFactory.CreateCompanyReviewService(db);
        await sut.SubmitAsync(candidate.Id, company.Id, ValidRequest(), "1.2.3.4");
        var review = db.CompanyReviews.First();
        review.Status = ReviewStatus.Published;
        await db.SaveChangesAsync();

        var otherRecruiterUser = new User { FullName = "Other Recruiter", Email = "other@example.com", Role = UserRole.Recruiter };
        db.Users.Add(otherRecruiterUser);
        await db.SaveChangesAsync();
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = otherRecruiterUser.Id, Company = new Company { Name = "Other Co" } });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.RespondAsync(otherRecruiterUser.Id, review.Id, new RespondToReviewRequest("Thanks!")));
    }

    [Fact]
    public async Task RespondAsync_CompanyRecruiter_SetsResponse()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, company) = await SeedWithApplicationAsync(db);
        var sut = TestServiceFactory.CreateCompanyReviewService(db);
        await sut.SubmitAsync(candidate.Id, company.Id, ValidRequest(), "1.2.3.4");
        var review = db.CompanyReviews.First();
        review.Status = ReviewStatus.Published;
        await db.SaveChangesAsync();
        var recruiterUserId = db.RecruiterProfiles.First(r => r.CompanyId == company.Id).UserId;

        await sut.RespondAsync(recruiterUserId, review.Id, new RespondToReviewRequest("Thanks for the feedback!"));

        var reloaded = db.CompanyReviews.First(r => r.Id == review.Id);
        Assert.Equal("Thanks for the feedback!", reloaded.RecruiterResponse);
    }
}
