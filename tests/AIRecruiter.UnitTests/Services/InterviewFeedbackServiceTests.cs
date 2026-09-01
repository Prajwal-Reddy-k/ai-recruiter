using AIRecruiter.Application.DTOs.InterviewFeedback;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class InterviewFeedbackServiceTests
{
    private static InterviewFeedbackService CreateSut(AppDbContext db) => new(db, TestServiceFactory.CreateAuditLog(db));

    private static async Task<(User Owner, User Interviewer, User Unrelated, Interview Interview)> SeedAsync(AppDbContext db)
    {
        var owner = new User { FullName = "Owner Recruiter", Email = "owner@example.com", Role = UserRole.Recruiter };
        var interviewer = new User { FullName = "Ian Interviewer", Email = "interviewer@example.com", Role = UserRole.Recruiter };
        var unrelated = new User { FullName = "Unrelated Recruiter", Email = "unrelated@example.com", Role = UserRole.Recruiter };
        var candidateUser = new User { FullName = "Casey Candidate", Email = "casey@example.com", Role = UserRole.Candidate };
        db.Users.AddRange(owner, interviewer, unrelated, candidateUser);
        await db.SaveChangesAsync();

        var company = new Company { Name = "Acme Corp" };
        var ownerProfile = new RecruiterProfile { UserId = owner.Id, Company = company, CompanyRole = CompanyRole.Owner };
        var interviewerProfile = new RecruiterProfile { UserId = interviewer.Id, Company = company, CompanyRole = CompanyRole.Interviewer };
        var unrelatedProfile = new RecruiterProfile { UserId = unrelated.Id, Company = new Company { Name = "Other Co" }, CompanyRole = CompanyRole.Owner };
        var candidateProfile = new CandidateProfile { UserId = candidateUser.Id };
        db.RecruiterProfiles.AddRange(ownerProfile, interviewerProfile, unrelatedProfile);
        db.CandidateProfiles.Add(candidateProfile);
        await db.SaveChangesAsync();

        var job = new JobPosting { Title = "Backend Engineer", Description = "role", Status = JobStatus.Open, CompanyId = company.Id, RecruiterProfileId = ownerProfile.Id };
        db.JobPostings.Add(job);
        await db.SaveChangesAsync();

        var application = new JobApplication { JobPostingId = job.Id, CandidateProfileId = candidateProfile.Id, Status = ApplicationStatus.InterviewCompleted };
        db.JobApplications.Add(application);
        await db.SaveChangesAsync();

        var interview = new Interview
        {
            JobApplicationId = application.Id,
            ScheduledStartUtc = DateTime.UtcNow.AddDays(-1),
            ScheduledEndUtc = DateTime.UtcNow.AddDays(-1).AddMinutes(30),
            Status = InterviewStatus.Completed,
            CreatedByUserId = owner.Id,
        };
        db.Interviews.Add(interview);
        await db.SaveChangesAsync();

        db.InterviewAssignments.Add(new InterviewAssignment { InterviewId = interview.Id, RecruiterProfileId = interviewerProfile.Id, AssignedByUserId = owner.Id });
        await db.SaveChangesAsync();

        return (owner, interviewer, unrelated, interview);
    }

    private static UpsertInterviewFeedbackRequest ValidRequest(int score = 4) => new(
        score, score, score, score, InterviewRecommendation.Yes, "Strong fundamentals", "Limited system-design depth", "Would hire.");

    [Fact]
    public async Task SubmitAsync_OwningRecruiter_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (owner, _, _, interview) = await SeedAsync(db);
        var sut = CreateSut(db);

        var result = await sut.SubmitAsync(owner.Id, interview.Id, ValidRequest());

        Assert.False(result.IsDraft);
        Assert.NotNull(result.SubmittedAtUtc);
    }

    [Fact]
    public async Task SubmitAsync_AssignedInterviewer_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (_, interviewer, _, interview) = await SeedAsync(db);
        var sut = CreateSut(db);

        var result = await sut.SubmitAsync(interviewer.Id, interview.Id, ValidRequest());

        Assert.Equal("Yes", result.Recommendation);
    }

    [Fact]
    public async Task SubmitAsync_UnrelatedRecruiter_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (_, _, unrelated, interview) = await SeedAsync(db);
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.SubmitAsync(unrelated.Id, interview.Id, ValidRequest()));
    }

    [Fact]
    public async Task SaveDraftAsync_InvalidScore_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var (owner, _, _, interview) = await SeedAsync(db);
        var sut = CreateSut(db);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => sut.SaveDraftAsync(owner.Id, interview.Id, ValidRequest(score: 6)));
        Assert.NotNull(ex.FieldErrors);
    }

    [Fact]
    public async Task SubmitAsync_EditingAlreadySubmittedFeedback_WritesEditHistory()
    {
        using var db = TestDbContextFactory.Create();
        var (owner, _, _, interview) = await SeedAsync(db);
        var sut = CreateSut(db);

        await sut.SubmitAsync(owner.Id, interview.Id, ValidRequest(score: 3));
        await sut.SubmitAsync(owner.Id, interview.Id, ValidRequest(score: 5));

        Assert.Single(db.InterviewFeedbackEditHistories);
    }

    [Fact]
    public async Task GetSummaryAsync_UnrelatedRecruiter_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (owner, _, unrelated, interview) = await SeedAsync(db);
        var sut = CreateSut(db);
        await sut.SubmitAsync(owner.Id, interview.Id, ValidRequest());

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.GetSummaryAsync(unrelated.Id, interview.Id));
    }

    [Fact]
    public async Task GetSummaryAsync_AssignedInterviewer_SeesOwnerAndOwnScorecards()
    {
        using var db = TestDbContextFactory.Create();
        var (owner, interviewer, _, interview) = await SeedAsync(db);
        var sut = CreateSut(db);
        await sut.SubmitAsync(owner.Id, interview.Id, ValidRequest(score: 4));
        await sut.SubmitAsync(interviewer.Id, interview.Id, ValidRequest(score: 3));

        var summary = await sut.GetSummaryAsync(interviewer.Id, interview.Id);

        Assert.Equal(2, summary.Scorecards.Count);
        Assert.Equal(3.5, summary.AverageTechnicalScore);
    }

    [Fact]
    public async Task GetSummaryAsync_DraftFeedback_ExcludedFromSummary()
    {
        using var db = TestDbContextFactory.Create();
        var (owner, _, _, interview) = await SeedAsync(db);
        var sut = CreateSut(db);
        await sut.SaveDraftAsync(owner.Id, interview.Id, ValidRequest());

        var summary = await sut.GetSummaryAsync(owner.Id, interview.Id);

        Assert.Empty(summary.Scorecards);
    }
}
