using AIRecruiter.Application.DTOs.Jobs;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class JobPostingServiceTests
{
    private static async Task<(User Recruiter, JobPosting Job)> SeedAsync(AppDbContext db)
    {
        var recruiterUser = new User { FullName = "Rita Recruiter", Email = "rita@example.com", Role = UserRole.Recruiter };
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
            Status = JobStatus.Open,
            CompanyId = company.Id,
            RecruiterProfileId = recruiterProfile.Id,
        };
        db.JobPostings.Add(job);
        await db.SaveChangesAsync();

        return (recruiterUser, job);
    }

    [Fact]
    public async Task UpdateStatusAsync_OwningRecruiter_ClosesJob()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, job) = await SeedAsync(db);
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup(), TestServiceFactory.CreateNotifications(db), TestServiceFactory.CreateSalaryInsightsService(db));

        var updated = await sut.UpdateStatusAsync(recruiter.Id, job.Id, new UpdateJobStatusRequest(JobStatus.Closed));

        Assert.Equal("Closed", updated.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_NonOwningRecruiter_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (_, job) = await SeedAsync(db);
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup(), TestServiceFactory.CreateNotifications(db), TestServiceFactory.CreateSalaryInsightsService(db));

        var otherRecruiterUser = new User { FullName = "Other Recruiter", Email = "other@example.com", Role = UserRole.Recruiter };
        db.Users.Add(otherRecruiterUser);
        await db.SaveChangesAsync();
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = otherRecruiterUser.Id, Company = new Company { Name = "Other Co" } });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(
            () => sut.UpdateStatusAsync(otherRecruiterUser.Id, job.Id, new UpdateJobStatusRequest(JobStatus.Closed)));
    }

    [Fact]
    public async Task UpdateStatusAsync_UnknownJob_ThrowsNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, _) = await SeedAsync(db);
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup(), TestServiceFactory.CreateNotifications(db), TestServiceFactory.CreateSalaryInsightsService(db));

        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.UpdateStatusAsync(recruiter.Id, 9999, new UpdateJobStatusRequest(JobStatus.Closed)));
    }

    [Fact]
    public async Task UpdateStatusAsync_ArchivedToOpen_ThrowsConflict_InvalidTransition()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, job) = await SeedAsync(db);
        job.Status = JobStatus.Archived;
        await db.SaveChangesAsync();
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup(), TestServiceFactory.CreateNotifications(db), TestServiceFactory.CreateSalaryInsightsService(db));

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => sut.UpdateStatusAsync(recruiter.Id, job.Id, new UpdateJobStatusRequest(JobStatus.Open)));
        Assert.Equal("INVALID_TRANSITION", ex.ErrorCode);
    }

    [Fact]
    public async Task UpdateStatusAsync_DraftToOpen_SetsPublishedAt()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, job) = await SeedAsync(db);
        job.Status = JobStatus.Draft;
        job.City = "Bengaluru";
        job.State = "Karnataka";
        await db.SaveChangesAsync();
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup(), TestServiceFactory.CreateNotifications(db), TestServiceFactory.CreateSalaryInsightsService(db));

        var updated = await sut.UpdateStatusAsync(recruiter.Id, job.Id, new UpdateJobStatusRequest(JobStatus.Open));

        Assert.Equal("Open", updated.Status);
        Assert.NotNull(updated.PublishedAt);
    }

    [Fact]
    public async Task DuplicateAsync_OwningRecruiter_CreatesDraftCopy()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, job) = await SeedAsync(db);
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup(), TestServiceFactory.CreateNotifications(db), TestServiceFactory.CreateSalaryInsightsService(db));

        var copy = await sut.DuplicateAsync(recruiter.Id, job.Id);

        Assert.NotEqual(job.Id, copy.Id);
        Assert.Equal("Draft", copy.Status);
        Assert.Contains("Copy", copy.Title);
    }

    [Fact]
    public async Task DuplicateAsync_NonOwningRecruiter_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (_, job) = await SeedAsync(db);
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup(), TestServiceFactory.CreateNotifications(db), TestServiceFactory.CreateSalaryInsightsService(db));

        var otherRecruiterUser = new User { FullName = "Other Recruiter 2", Email = "other2@example.com", Role = UserRole.Recruiter };
        db.Users.Add(otherRecruiterUser);
        await db.SaveChangesAsync();
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = otherRecruiterUser.Id, Company = new Company { Name = "Other Co 2" } });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.DuplicateAsync(otherRecruiterUser.Id, job.Id));
    }

    [Fact]
    public async Task GetByIdAsync_SameVisitorWithinWindow_CountsOnce()
    {
        using var db = TestDbContextFactory.Create();
        var (_, job) = await SeedAsync(db);
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup(), TestServiceFactory.CreateNotifications(db), TestServiceFactory.CreateSalaryInsightsService(db));

        await sut.GetByIdAsync(job.Id, "visitor-1", null);
        var second = await sut.GetByIdAsync(job.Id, "visitor-1", null);

        Assert.Equal(1, second!.ViewCount);
    }

    [Fact]
    public async Task GetByIdAsync_DifferentVisitors_CountsEach()
    {
        using var db = TestDbContextFactory.Create();
        var (_, job) = await SeedAsync(db);
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup(), TestServiceFactory.CreateNotifications(db), TestServiceFactory.CreateSalaryInsightsService(db));

        await sut.GetByIdAsync(job.Id, "visitor-1", null);
        var second = await sut.GetByIdAsync(job.Id, "visitor-2", null);

        Assert.Equal(2, second!.ViewCount);
    }

    [Fact]
    public async Task GetByIdAsync_OwningRecruiterViewingOwnJob_DoesNotCount()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, job) = await SeedAsync(db);
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup(), TestServiceFactory.CreateNotifications(db), TestServiceFactory.CreateSalaryInsightsService(db));

        var result = await sut.GetByIdAsync(job.Id, $"u:{recruiter.Id}", recruiter.Id);

        Assert.Equal(0, result!.ViewCount);
    }

    [Fact]
    public async Task GetByIdAsync_HiddenJob_NonOwnerNonAdmin_ReturnsNull()
    {
        using var db = TestDbContextFactory.Create();
        var (_, job) = await SeedAsync(db);
        job.ModerationStatus = ModerationStatus.Hidden;
        await db.SaveChangesAsync();
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup(), TestServiceFactory.CreateNotifications(db), TestServiceFactory.CreateSalaryInsightsService(db));

        var result = await sut.GetByIdAsync(job.Id, "visitor-1", null);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_HiddenJob_AdminViewer_ReturnsJob()
    {
        using var db = TestDbContextFactory.Create();
        var (_, job) = await SeedAsync(db);
        job.ModerationStatus = ModerationStatus.Hidden;
        await db.SaveChangesAsync();
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup(), TestServiceFactory.CreateNotifications(db), TestServiceFactory.CreateSalaryInsightsService(db));

        var result = await sut.GetByIdAsync(job.Id, "admin-1", null, isAdminViewer: true);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetByIdAsync_HiddenJob_OwningRecruiter_ReturnsJob()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, job) = await SeedAsync(db);
        job.ModerationStatus = ModerationStatus.Hidden;
        await db.SaveChangesAsync();
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup(), TestServiceFactory.CreateNotifications(db), TestServiceFactory.CreateSalaryInsightsService(db));

        var result = await sut.GetByIdAsync(job.Id, $"u:{recruiter.Id}", recruiter.Id);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task ExtendDeadlineAsync_OwningRecruiter_FutureDeadline_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, job) = await SeedAsync(db);
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup(), TestServiceFactory.CreateNotifications(db), TestServiceFactory.CreateSalaryInsightsService(db));
        var deadline = DateTime.UtcNow.AddDays(14);

        var updated = await sut.ExtendDeadlineAsync(recruiter.Id, job.Id, deadline);

        Assert.NotNull(updated.ApplicationDeadlineUtc);
    }

    [Fact]
    public async Task ExtendDeadlineAsync_PastDeadline_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, job) = await SeedAsync(db);
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup(), TestServiceFactory.CreateNotifications(db), TestServiceFactory.CreateSalaryInsightsService(db));

        await Assert.ThrowsAsync<ValidationException>(
            () => sut.ExtendDeadlineAsync(recruiter.Id, job.Id, DateTime.UtcNow.AddDays(-1)));
    }

    [Fact]
    public async Task ExtendDeadlineAsync_NonOwningRecruiter_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (_, job) = await SeedAsync(db);
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup(), TestServiceFactory.CreateNotifications(db), TestServiceFactory.CreateSalaryInsightsService(db));

        var otherRecruiterUser = new User { FullName = "Other Recruiter 3", Email = "other3@example.com", Role = UserRole.Recruiter };
        db.Users.Add(otherRecruiterUser);
        await db.SaveChangesAsync();
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = otherRecruiterUser.Id, Company = new Company { Name = "Other Co 3" } });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(
            () => sut.ExtendDeadlineAsync(otherRecruiterUser.Id, job.Id, DateTime.UtcNow.AddDays(7)));
    }

    private static CreateJobPostingRequest DraftRequest(string title = "Backend Engineer", bool saveAsDraft = true) =>
        new(title, "A role description.", "C#,SQL", 2, 5, null, null, null, null, null, true, JobType.FullTime, saveAsDraft);

    [Fact]
    public async Task UpdateStatusAsync_DraftToOpen_NotifiesFollowersOfCompany()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, _) = await SeedAsync(db);
        var recruiterProfile = db.RecruiterProfiles.First(r => r.UserId == recruiter.Id);
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup(), TestServiceFactory.CreateNotifications(db), TestServiceFactory.CreateSalaryInsightsService(db));

        var followerUser = new User { FullName = "Fiona Follower", Email = "fiona@example.com", Role = UserRole.Candidate };
        db.Users.Add(followerUser);
        await db.SaveChangesAsync();
        var followerProfile = new CandidateProfile { UserId = followerUser.Id };
        db.CandidateProfiles.Add(followerProfile);
        await db.SaveChangesAsync();
        db.CompanyFollows.Add(new CompanyFollow { CandidateProfileId = followerProfile.Id, CompanyId = recruiterProfile.CompanyId, NotifyOnNewJob = true });
        await db.SaveChangesAsync();

        var draft = await sut.CreateAsync(recruiter.Id, DraftRequest());
        await sut.UpdateStatusAsync(recruiter.Id, draft.Id, new UpdateJobStatusRequest(JobStatus.Open));

        Assert.Contains(db.Notifications, n => n.UserId == followerUser.Id && n.Type == "CompanyNewJob");
    }

    [Fact]
    public async Task UpdateStatusAsync_ClosedToOpenReopen_DoesNotReNotifyFollowers()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, _) = await SeedAsync(db);
        var recruiterProfile = db.RecruiterProfiles.First(r => r.UserId == recruiter.Id);
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup(), TestServiceFactory.CreateNotifications(db), TestServiceFactory.CreateSalaryInsightsService(db));

        var followerUser = new User { FullName = "Fiona Follower", Email = "fiona2@example.com", Role = UserRole.Candidate };
        db.Users.Add(followerUser);
        await db.SaveChangesAsync();
        var followerProfile = new CandidateProfile { UserId = followerUser.Id };
        db.CandidateProfiles.Add(followerProfile);
        await db.SaveChangesAsync();
        db.CompanyFollows.Add(new CompanyFollow { CandidateProfileId = followerProfile.Id, CompanyId = recruiterProfile.CompanyId, NotifyOnNewJob = true });
        await db.SaveChangesAsync();

        var draft = await sut.CreateAsync(recruiter.Id, DraftRequest());
        await sut.UpdateStatusAsync(recruiter.Id, draft.Id, new UpdateJobStatusRequest(JobStatus.Open));
        await sut.UpdateStatusAsync(recruiter.Id, draft.Id, new UpdateJobStatusRequest(JobStatus.Closed));
        db.Notifications.RemoveRange(db.Notifications);
        await db.SaveChangesAsync();

        await sut.UpdateStatusAsync(recruiter.Id, draft.Id, new UpdateJobStatusRequest(JobStatus.Open));

        Assert.DoesNotContain(db.Notifications, n => n.UserId == followerUser.Id && n.Type == "CompanyNewJob");
    }

    [Fact]
    public async Task RecordShareAsync_DistinctVisitors_IncrementsShareCount()
    {
        using var db = TestDbContextFactory.Create();
        var (_, job) = await SeedAsync(db);
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup(), TestServiceFactory.CreateNotifications(db), TestServiceFactory.CreateSalaryInsightsService(db));

        await sut.RecordShareAsync(job.Id, "visitor-a");
        await sut.RecordShareAsync(job.Id, "visitor-b");

        var reloaded = db.JobPostings.First(j => j.Id == job.Id);
        Assert.Equal(2, reloaded.ShareCount);
    }

    [Fact]
    public async Task RecordShareAsync_SameVisitorTwice_DoesNotDoubleCount()
    {
        using var db = TestDbContextFactory.Create();
        var (_, job) = await SeedAsync(db);
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup(), TestServiceFactory.CreateNotifications(db), TestServiceFactory.CreateSalaryInsightsService(db));

        await sut.RecordShareAsync(job.Id, "visitor-a");
        await sut.RecordShareAsync(job.Id, "visitor-a");

        var reloaded = db.JobPostings.First(j => j.Id == job.Id);
        Assert.Equal(1, reloaded.ShareCount);
    }

    [Fact]
    public async Task CreateAsync_PublishingMatchingJob_NotifiesSavedSearchOwner()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, _) = await SeedAsync(db);
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup(), TestServiceFactory.CreateNotifications(db), TestServiceFactory.CreateSalaryInsightsService(db));

        var searchOwnerUser = new User { FullName = "Sam Searcher", Email = "sam@example.com", Role = UserRole.Candidate };
        db.Users.Add(searchOwnerUser);
        await db.SaveChangesAsync();
        var searchOwnerProfile = new CandidateProfile { UserId = searchOwnerUser.Id };
        db.CandidateProfiles.Add(searchOwnerProfile);
        await db.SaveChangesAsync();
        db.JobAlerts.Add(new JobAlert { CandidateProfileId = searchOwnerProfile.Id, Keyword = "engineer", IsActive = true });
        await db.SaveChangesAsync();

        await sut.CreateAsync(recruiter.Id, DraftRequest("Senior Backend Engineer", saveAsDraft: false));

        Assert.Contains(db.Notifications, n => n.UserId == searchOwnerUser.Id && n.Type == "SavedSearchMatch");
    }

    [Fact]
    public async Task CreateAsync_NonMatchingJob_DoesNotNotifySavedSearchOwner()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, _) = await SeedAsync(db);
        var sut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup(), TestServiceFactory.CreateNotifications(db), TestServiceFactory.CreateSalaryInsightsService(db));

        var searchOwnerUser = new User { FullName = "Sam Searcher", Email = "sam2@example.com", Role = UserRole.Candidate };
        db.Users.Add(searchOwnerUser);
        await db.SaveChangesAsync();
        var searchOwnerProfile = new CandidateProfile { UserId = searchOwnerUser.Id };
        db.CandidateProfiles.Add(searchOwnerProfile);
        await db.SaveChangesAsync();
        db.JobAlerts.Add(new JobAlert { CandidateProfileId = searchOwnerProfile.Id, Keyword = "designer", IsActive = true });
        await db.SaveChangesAsync();

        await sut.CreateAsync(recruiter.Id, DraftRequest("Senior Backend Engineer", saveAsDraft: false));

        Assert.DoesNotContain(db.Notifications, n => n.UserId == searchOwnerUser.Id && n.Type == "SavedSearchMatch");
    }

    private static JobPostingService CreateSut(AppDbContext db) =>
        new(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup(), TestServiceFactory.CreateNotifications(db), TestServiceFactory.CreateSalaryInsightsService(db));

    private static UpsertScreeningQuestionRequest Question(
        int? id = null, string text = "Are you willing to relocate?", string type = "YesNo",
        bool required = true, string? helpText = null, IReadOnlyList<string>? options = null, int order = 0, string? preferredAnswer = null) =>
        new(id, text, type, required, helpText, options, order, preferredAnswer);

    [Fact]
    public async Task CreateAsync_WithValidQuestions_PersistsInOrderWithOptions()
    {
        using var db = TestDbContextFactory.Create();
        var recruiterUser = new User { FullName = "Rita Recruiter", Email = "rita-sq1@example.com", Role = UserRole.Recruiter };
        db.Users.Add(recruiterUser);
        await db.SaveChangesAsync();
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = recruiterUser.Id, Company = new Company { Name = "Acme Corp" } });
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var questions = new[]
        {
            Question(text: "Years of C# experience?", type: "Number", order: 1),
            Question(text: "Preferred stack", type: "SingleChoice", options: new[] { "C#", "Java" }, order: 0, preferredAnswer: "C#"),
        };
        var request = DraftRequest() with { ScreeningQuestions = questions };

        var dto = await sut.CreateAsync(recruiterUser.Id, request);

        Assert.Equal(2, dto.ScreeningQuestions!.Count);
        Assert.Equal("Preferred stack", dto.ScreeningQuestions[0].QuestionText);
        Assert.Equal(2, dto.ScreeningQuestions[0].Options.Count);
        // Public-path default (includePreferredAnswers not requested) still returns the owner's
        // own CreateAsync call, which always includes it since the caller is the owner.
        Assert.Equal("C#", dto.ScreeningQuestions[0].PreferredAnswer);
    }

    [Fact]
    public async Task CreateAsync_TooManyQuestions_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var recruiterUser = new User { FullName = "Rita Recruiter", Email = "rita-sq2@example.com", Role = UserRole.Recruiter };
        db.Users.Add(recruiterUser);
        await db.SaveChangesAsync();
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = recruiterUser.Id, Company = new Company { Name = "Acme Corp" } });
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var questions = Enumerable.Range(0, 11).Select(i => Question(text: $"Question {i}", type: "ShortText", order: i)).ToArray();
        var request = DraftRequest() with { ScreeningQuestions = questions };

        await Assert.ThrowsAsync<ValidationException>(() => sut.CreateAsync(recruiterUser.Id, request));
    }

    [Fact]
    public async Task CreateAsync_SingleChoiceWithOneOption_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var recruiterUser = new User { FullName = "Rita Recruiter", Email = "rita-sq3@example.com", Role = UserRole.Recruiter };
        db.Users.Add(recruiterUser);
        await db.SaveChangesAsync();
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = recruiterUser.Id, Company = new Company { Name = "Acme Corp" } });
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var request = DraftRequest() with { ScreeningQuestions = new[] { Question(type: "SingleChoice", options: new[] { "Only one" }) } };

        await Assert.ThrowsAsync<ValidationException>(() => sut.CreateAsync(recruiterUser.Id, request));
    }

    [Fact]
    public async Task CreateAsync_DuplicateOptionsCaseInsensitive_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var recruiterUser = new User { FullName = "Rita Recruiter", Email = "rita-sq4@example.com", Role = UserRole.Recruiter };
        db.Users.Add(recruiterUser);
        await db.SaveChangesAsync();
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = recruiterUser.Id, Company = new Company { Name = "Acme Corp" } });
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var request = DraftRequest() with { ScreeningQuestions = new[] { Question(type: "SingleChoice", options: new[] { "Yes", "YES" }) } };

        await Assert.ThrowsAsync<ValidationException>(() => sut.CreateAsync(recruiterUser.Id, request));
    }

    private async Task<(User Recruiter, JobPosting Job, JobScreeningQuestion Question)> SeedJobWithQuestionAsync(AppDbContext db, JobStatus status = JobStatus.Open)
    {
        var recruiterUser = new User { FullName = "Rita Recruiter", Email = $"rita-{Guid.NewGuid():N}@example.com", Role = UserRole.Recruiter };
        db.Users.Add(recruiterUser);
        await db.SaveChangesAsync();
        var company = new Company { Name = "Acme Corp" };
        var recruiterProfile = new RecruiterProfile { UserId = recruiterUser.Id, Company = company };
        db.RecruiterProfiles.Add(recruiterProfile);
        await db.SaveChangesAsync();
        var job = new JobPosting { Title = "Backend Engineer", Description = "role", Status = status, CompanyId = company.Id, RecruiterProfileId = recruiterProfile.Id };
        db.JobPostings.Add(job);
        await db.SaveChangesAsync();
        var question = new JobScreeningQuestion { JobPostingId = job.Id, QuestionText = "Relocate?", QuestionType = ScreeningQuestionType.YesNo, IsRequired = true, DisplayOrder = 0 };
        db.JobScreeningQuestions.Add(question);
        await db.SaveChangesAsync();
        return (recruiterUser, job, question);
    }

    [Fact]
    public async Task UpdateAsync_AddingQuestionToPublishedJob_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, job, existing) = await SeedJobWithQuestionAsync(db);
        var sut = CreateSut(db);

        var update = new UpdateJobPostingRequest(job.Title, job.Description, null, null, null, null, null, "Bengaluru", "Karnataka", null, false, JobType.FullTime,
            new[] { Question(existing.Id, existing.QuestionText, "YesNo", existing.IsRequired, order: 0), Question(text: "New question", type: "ShortText", order: 1) });

        var dto = await sut.UpdateAsync(recruiter.Id, job.Id, update);

        Assert.Equal(2, dto.ScreeningQuestions!.Count);
    }

    [Fact]
    public async Task UpdateAsync_EditingTextOnAnsweredQuestion_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, job, question) = await SeedJobWithQuestionAsync(db);
        var candidateUser = new User { FullName = "Casey Candidate", Email = "casey-sq@example.com", Role = UserRole.Candidate };
        db.Users.Add(candidateUser);
        await db.SaveChangesAsync();
        var profile = new CandidateProfile { UserId = candidateUser.Id };
        db.CandidateProfiles.Add(profile);
        await db.SaveChangesAsync();
        var application = new JobApplication { JobPostingId = job.Id, CandidateProfileId = profile.Id };
        db.JobApplications.Add(application);
        await db.SaveChangesAsync();
        db.ScreeningAnswers.Add(new ScreeningAnswer { JobApplicationId = application.Id, JobScreeningQuestionId = question.Id, TextValue = "Yes" });
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var update = new UpdateJobPostingRequest(job.Title, job.Description, null, null, null, null, null, "Bengaluru", "Karnataka", null, false, JobType.FullTime,
            new[] { Question(question.Id, "Willing to relocate within India?", "YesNo", true, order: 0) });

        var dto = await sut.UpdateAsync(recruiter.Id, job.Id, update);

        Assert.Equal("Willing to relocate within India?", dto.ScreeningQuestions![0].QuestionText);
    }

    [Fact]
    public async Task UpdateAsync_ChangingTypeOfAnsweredQuestion_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, job, question) = await SeedJobWithQuestionAsync(db);
        var candidateUser = new User { FullName = "Casey Candidate", Email = "casey-sq2@example.com", Role = UserRole.Candidate };
        db.Users.Add(candidateUser);
        await db.SaveChangesAsync();
        var profile = new CandidateProfile { UserId = candidateUser.Id };
        db.CandidateProfiles.Add(profile);
        await db.SaveChangesAsync();
        var application = new JobApplication { JobPostingId = job.Id, CandidateProfileId = profile.Id };
        db.JobApplications.Add(application);
        await db.SaveChangesAsync();
        db.ScreeningAnswers.Add(new ScreeningAnswer { JobApplicationId = application.Id, JobScreeningQuestionId = question.Id, TextValue = "Yes" });
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var update = new UpdateJobPostingRequest(job.Title, job.Description, null, null, null, null, null, "Bengaluru", "Karnataka", null, false, JobType.FullTime,
            new[] { Question(question.Id, question.QuestionText, "ShortText", true, order: 0) });

        await Assert.ThrowsAsync<ConflictException>(() => sut.UpdateAsync(recruiter.Id, job.Id, update));
    }

    [Fact]
    public async Task UpdateAsync_DeletingAnsweredQuestion_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, job, question) = await SeedJobWithQuestionAsync(db);
        var candidateUser = new User { FullName = "Casey Candidate", Email = "casey-sq3@example.com", Role = UserRole.Candidate };
        db.Users.Add(candidateUser);
        await db.SaveChangesAsync();
        var profile = new CandidateProfile { UserId = candidateUser.Id };
        db.CandidateProfiles.Add(profile);
        await db.SaveChangesAsync();
        var application = new JobApplication { JobPostingId = job.Id, CandidateProfileId = profile.Id };
        db.JobApplications.Add(application);
        await db.SaveChangesAsync();
        db.ScreeningAnswers.Add(new ScreeningAnswer { JobApplicationId = application.Id, JobScreeningQuestionId = question.Id, TextValue = "Yes" });
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var update = new UpdateJobPostingRequest(job.Title, job.Description, null, null, null, null, null, "Bengaluru", "Karnataka", null, false, JobType.FullTime,
            Array.Empty<UpsertScreeningQuestionRequest>());

        await Assert.ThrowsAsync<ConflictException>(() => sut.UpdateAsync(recruiter.Id, job.Id, update));
    }

    [Fact]
    public async Task UpdateAsync_DeletingUnansweredQuestion_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, job, _) = await SeedJobWithQuestionAsync(db);
        var sut = CreateSut(db);

        var update = new UpdateJobPostingRequest(job.Title, job.Description, null, null, null, null, null, "Bengaluru", "Karnataka", null, false, JobType.FullTime,
            Array.Empty<UpsertScreeningQuestionRequest>());

        var dto = await sut.UpdateAsync(recruiter.Id, job.Id, update);

        Assert.Empty(dto.ScreeningQuestions!);
    }

    [Fact]
    public async Task UpdateAsync_NonOwningRecruiter_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (_, job, _) = await SeedJobWithQuestionAsync(db);
        var otherRecruiterUser = new User { FullName = "Other Recruiter", Email = "other-sq@example.com", Role = UserRole.Recruiter };
        db.Users.Add(otherRecruiterUser);
        await db.SaveChangesAsync();
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = otherRecruiterUser.Id, Company = new Company { Name = "Other Co" } });
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var update = new UpdateJobPostingRequest(job.Title, job.Description, null, null, null, null, null, "Bengaluru", "Karnataka", null, false, JobType.FullTime,
            Array.Empty<UpsertScreeningQuestionRequest>());

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.UpdateAsync(otherRecruiterUser.Id, job.Id, update));
    }

    [Fact]
    public async Task GetByIdAsync_AnonymousViewer_NeverIncludesPreferredAnswer()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, job, question) = await SeedJobWithQuestionAsync(db);
        question.PreferredAnswer = "Yes";
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var dto = await sut.GetByIdAsync(job.Id, viewerKey: "anon-1", viewerUserId: null);

        Assert.Null(dto!.ScreeningQuestions![0].PreferredAnswer);
    }

    [Fact]
    public async Task GetByIdAsync_OwningRecruiter_IncludesPreferredAnswer()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, job, question) = await SeedJobWithQuestionAsync(db);
        question.PreferredAnswer = "Yes";
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var dto = await sut.GetByIdAsync(job.Id, viewerKey: null, viewerUserId: recruiter.Id);

        Assert.Equal("Yes", dto!.ScreeningQuestions![0].PreferredAnswer);
    }
}
