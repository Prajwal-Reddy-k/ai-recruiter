using AIRecruiter.Application.DTOs.Applications;
using AIRecruiter.Application.DTOs.Matching;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Application.Validation;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Options;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace AIRecruiter.UnitTests.Services;

public class JobApplicationServiceTests
{
    private static JobApplicationService CreateSut(AppDbContext db, IResumeMatchingService? matcher = null)
    {
        matcher ??= Mock.Of<IResumeMatchingService>(m =>
            m.CalculateMatch(It.IsAny<string>(), It.IsAny<JobMatchInput>(), It.IsAny<CandidateMatchInput>())
            == new ResumeMatchResult(70, new List<string> { "C#" }, new List<string>(), new List<string>(), "explanation"));

        var resumeStorage = new Mock<IResumeStorage>();
        var extractorFactory = new Mock<IResumeTextExtractorFactory>();
        var validator = new ResumeFileValidator();
        var candidateProfileService = TestServiceFactory.CreateCandidateProfileService(
            db, resumeStorage.Object, extractorFactory.Object, validator);

        return new JobApplicationService(
            db, matcher, candidateProfileService, TestServiceFactory.CreateNotifications(db), TestServiceFactory.CreateAuditLog(db));
    }

    private static async Task<(User Candidate, User Recruiter, JobPosting Job, CandidateProfile Profile)> SeedAsync(AppDbContext db, string? resumeText = "C# developer")
    {
        var candidateUser = new User { FullName = "Casey Candidate", Email = "casey@example.com", Role = UserRole.Candidate };
        var recruiterUser = new User { FullName = "Rita Recruiter", Email = "rita@example.com", Role = UserRole.Recruiter };
        db.Users.AddRange(candidateUser, recruiterUser);
        await db.SaveChangesAsync();

        var company = new Company { Name = "Acme Corp" };
        var recruiterProfile = new RecruiterProfile { UserId = recruiterUser.Id, Company = company };
        db.RecruiterProfiles.Add(recruiterProfile);

        var candidateProfile = new CandidateProfile
        {
            UserId = candidateUser.Id,
            ResumeExtractedText = resumeText,
        };
        db.CandidateProfiles.Add(candidateProfile);
        await db.SaveChangesAsync();

        var job = new JobPosting
        {
            Title = "Backend Engineer",
            Description = "C# role",
            RequiredSkillsCsv = "C#",
            Status = JobStatus.Open,
            CompanyId = company.Id,
            RecruiterProfileId = recruiterProfile.Id,
        };
        db.JobPostings.Add(job);
        await db.SaveChangesAsync();

        return (candidateUser, recruiterUser, job, candidateProfile);
    }

    [Fact]
    public async Task ApplyAsync_WithResumeOnFile_ComputesAndStoresMatchScore()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var sut = CreateSut(db);

        var result = await sut.ApplyAsync(candidate.Id, job.Id, "Excited to apply");

        Assert.Equal(70, result.MatchScore);
    }

    [Fact]
    public async Task ApplyAsync_CoverNoteExceedsMaxLength_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var sut = CreateSut(db);

        var tooLong = new string('a', 4001);
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            sut.ApplyAsync(candidate.Id, job.Id, tooLong));

        Assert.True(ex.FieldErrors!.ContainsKey("coverNote"));
    }

    [Fact]
    public async Task ApplyAsync_AfterRegistrationViaReferral_AutoLinksAndAdvancesStatus()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, referrer, job, _) = await SeedAsync(db);
        // referrer stands in as the person who created the referral; the app under test
        // never distinguishes referrer role, only that Referral.RegisteredUserId matches.
        db.Referrals.Add(new Referral
        {
            ReferrerUserId = referrer.Id,
            JobPostingId = job.Id,
            ReferredName = "Casey Candidate",
            ReferredEmail = "casey@example.com",
            TokenHash = "irrelevant-for-this-test",
            TokenExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            Status = ReferralStatus.Registered,
            RegisteredUserId = candidate.Id,
            RegisteredAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        await sut.ApplyAsync(candidate.Id, job.Id, null);

        var referral = db.Referrals.Single();
        Assert.Equal(ReferralStatus.Applied, referral.Status);
        Assert.NotNull(referral.JobApplicationId);
    }

    [Fact]
    public async Task ApplyAsync_NoResumeOnFile_AppliesWithoutScore()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db, resumeText: null);
        var sut = CreateSut(db);

        var result = await sut.ApplyAsync(candidate.Id, job.Id, null);

        Assert.Null(result.MatchScore);
    }

    [Fact]
    public async Task ApplyAsync_SecondApplicationToSameJob_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var sut = CreateSut(db);

        await sut.ApplyAsync(candidate.Id, job.Id, null);

        await Assert.ThrowsAsync<ConflictException>(() => sut.ApplyAsync(candidate.Id, job.Id, null));
    }

    [Fact]
    public async Task GetApplicationDetailAsync_OwningCandidate_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var sut = CreateSut(db);
        var application = await sut.ApplyAsync(candidate.Id, job.Id, null);

        var detail = await sut.GetApplicationDetailAsync(candidate.Id, "Candidate", application.Id);

        Assert.Equal(application.Id, detail.Id);
    }

    [Fact]
    public async Task GetApplicationDetailAsync_OwningRecruiter_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, recruiter, job, _) = await SeedAsync(db);
        var sut = CreateSut(db);
        var application = await sut.ApplyAsync(candidate.Id, job.Id, null);

        var detail = await sut.GetApplicationDetailAsync(recruiter.Id, "Recruiter", application.Id);

        Assert.Equal(application.Id, detail.Id);
    }

    [Fact]
    public async Task GetApplicationDetailAsync_WrongCandidate_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var sut = CreateSut(db);
        var application = await sut.ApplyAsync(candidate.Id, job.Id, null);

        var otherCandidate = new User { FullName = "Other Candidate", Email = "other@example.com", Role = UserRole.Candidate };
        db.Users.Add(otherCandidate);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(
            () => sut.GetApplicationDetailAsync(otherCandidate.Id, "Candidate", application.Id));
    }

    [Fact]
    public async Task GetApplicationDetailAsync_NonOwningRecruiter_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var sut = CreateSut(db);
        var application = await sut.ApplyAsync(candidate.Id, job.Id, null);

        var otherRecruiterUser = new User { FullName = "Other Recruiter", Email = "otherrec@example.com", Role = UserRole.Recruiter };
        db.Users.Add(otherRecruiterUser);
        await db.SaveChangesAsync();
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = otherRecruiterUser.Id, Company = new Company { Name = "Other Co" } });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(
            () => sut.GetApplicationDetailAsync(otherRecruiterUser.Id, "Recruiter", application.Id));
    }

    [Fact]
    public async Task ApplyAsync_NoCandidateProfile_ThrowsNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var (_, recruiterUser, job, _) = await SeedAsync(db);
        var sut = CreateSut(db);

        var strangerUser = new User { FullName = "Stranger", Email = "stranger@example.com", Role = UserRole.Candidate };
        db.Users.Add(strangerUser);
        await db.SaveChangesAsync();
        // Note: no CandidateProfile created for strangerUser.

        await Assert.ThrowsAsync<NotFoundException>(() => sut.ApplyAsync(strangerUser.Id, job.Id, null));
    }

    [Fact]
    public async Task UpdateStatusAsync_OwningRecruiter_UpdatesStatus()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, recruiter, job, _) = await SeedAsync(db);
        var sut = CreateSut(db);
        var application = await sut.ApplyAsync(candidate.Id, job.Id, null);

        var updated = await sut.UpdateStatusAsync(recruiter.Id, application.Id, ApplicationStatus.Shortlisted);

        Assert.Equal("Shortlisted", updated.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_NonOwningRecruiter_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var sut = CreateSut(db);
        var application = await sut.ApplyAsync(candidate.Id, job.Id, null);

        var otherRecruiterUser = new User { FullName = "Other Recruiter", Email = "other-rec2@example.com", Role = UserRole.Recruiter };
        db.Users.Add(otherRecruiterUser);
        await db.SaveChangesAsync();
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = otherRecruiterUser.Id, Company = new Company { Name = "Other Co 2" } });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(
            () => sut.UpdateStatusAsync(otherRecruiterUser.Id, application.Id, ApplicationStatus.Rejected));
    }

    [Fact]
    public async Task WithdrawAsync_OwningCandidate_SetsStatusToWithdrawn()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var sut = CreateSut(db);
        var application = await sut.ApplyAsync(candidate.Id, job.Id, null);

        var updated = await sut.WithdrawAsync(candidate.Id, application.Id);

        Assert.Equal("Withdrawn", updated.Status);
    }

    [Fact]
    public async Task WithdrawAsync_NonOwningCandidate_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var sut = CreateSut(db);
        var application = await sut.ApplyAsync(candidate.Id, job.Id, null);

        var otherCandidate = new User { FullName = "Other Candidate 2", Email = "other-cand2@example.com", Role = UserRole.Candidate };
        db.Users.Add(otherCandidate);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.WithdrawAsync(otherCandidate.Id, application.Id));
    }

    [Theory]
    [InlineData(ApplicationStatus.Hired)]
    [InlineData(ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.Withdrawn)]
    public async Task WithdrawAsync_FinalStatus_ThrowsConflict(ApplicationStatus finalStatus)
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var sut = CreateSut(db);
        var application = await sut.ApplyAsync(candidate.Id, job.Id, null);
        var entity = db.JobApplications.First(a => a.Id == application.Id);
        entity.Status = finalStatus;
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ConflictException>(() => sut.WithdrawAsync(candidate.Id, application.Id));
        Assert.Equal("APPLICATION_FINAL", ex.ErrorCode);
    }

    [Fact]
    public async Task WithdrawAsync_EarlyStatus_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var sut = CreateSut(db);
        var application = await sut.ApplyAsync(candidate.Id, job.Id, null);
        var entity = db.JobApplications.First(a => a.Id == application.Id);
        entity.Status = ApplicationStatus.Shortlisted;
        await db.SaveChangesAsync();

        var updated = await sut.WithdrawAsync(candidate.Id, application.Id);

        Assert.Equal("Withdrawn", updated.Status);
    }

    [Theory]
    [InlineData(JobStatus.Closed)]
    [InlineData(JobStatus.Archived)]
    [InlineData(JobStatus.Draft)]
    public async Task ApplyAsync_JobNotOpen_ThrowsConflict(JobStatus status)
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        job.Status = status;
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => sut.ApplyAsync(candidate.Id, job.Id, null));
        Assert.Equal("JOB_CLOSED", ex.ErrorCode);
    }

    [Fact]
    public async Task ApplyAsync_JobHidden_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        job.ModerationStatus = ModerationStatus.Hidden;
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => sut.ApplyAsync(candidate.Id, job.Id, null));
        Assert.Equal("JOB_CLOSED", ex.ErrorCode);
    }

    [Fact]
    public async Task ApplyAsync_PastDeadline_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        job.ApplicationDeadlineUtc = DateTime.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => sut.ApplyAsync(candidate.Id, job.Id, null));
        Assert.Equal("JOB_EXPIRED", ex.ErrorCode);
    }

    [Fact]
    public async Task ApplyAsync_FutureDeadline_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        job.ApplicationDeadlineUtc = DateTime.UtcNow.AddDays(7);
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var result = await sut.ApplyAsync(candidate.Id, job.Id, null);

        Assert.NotNull(result);
    }

    private static async Task<JobScreeningQuestion> AddQuestionAsync(AppDbContext db, JobPosting job, ScreeningQuestionType type, bool required = true, IReadOnlyList<string>? options = null, string? preferredAnswer = null, int order = 0)
    {
        var question = new JobScreeningQuestion
        {
            JobPostingId = job.Id,
            QuestionText = $"Question ({type})",
            QuestionType = type,
            IsRequired = required,
            DisplayOrder = order,
            PreferredAnswer = preferredAnswer,
        };
        if (options is not null)
        {
            question.Options = options.Select((o, i) => new ScreeningQuestionOption { OptionText = o, DisplayOrder = i }).ToList();
        }
        db.JobScreeningQuestions.Add(question);
        await db.SaveChangesAsync();
        return question;
    }

    [Fact]
    public async Task ApplyAsync_ShortTextAnswer_Persists()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var question = await AddQuestionAsync(db, job, ScreeningQuestionType.ShortText);
        var sut = CreateSut(db);

        await sut.ApplyAsync(candidate.Id, job.Id, null, new[] { new SubmitScreeningAnswerRequest(question.Id, "Bengaluru", null, null) });

        var saved = db.ScreeningAnswers.Single();
        Assert.Equal("Bengaluru", saved.TextValue);
    }

    [Fact]
    public async Task ApplyAsync_LongTextAnswer_Persists()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var question = await AddQuestionAsync(db, job, ScreeningQuestionType.LongText);
        var sut = CreateSut(db);

        await sut.ApplyAsync(candidate.Id, job.Id, null, new[] { new SubmitScreeningAnswerRequest(question.Id, "A long detailed answer about my experience.", null, null) });

        Assert.Equal("A long detailed answer about my experience.", db.ScreeningAnswers.Single().TextValue);
    }

    [Fact]
    public async Task ApplyAsync_YesNoAnswer_Persists()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var question = await AddQuestionAsync(db, job, ScreeningQuestionType.YesNo);
        var sut = CreateSut(db);

        await sut.ApplyAsync(candidate.Id, job.Id, null, new[] { new SubmitScreeningAnswerRequest(question.Id, "Yes", null, null) });

        Assert.Equal("Yes", db.ScreeningAnswers.Single().TextValue);
    }

    [Fact]
    public async Task ApplyAsync_NumberAnswer_PersistsNumericValue()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var question = await AddQuestionAsync(db, job, ScreeningQuestionType.Number);
        var sut = CreateSut(db);

        await sut.ApplyAsync(candidate.Id, job.Id, null, new[] { new SubmitScreeningAnswerRequest(question.Id, null, 5, null) });

        Assert.Equal(5, db.ScreeningAnswers.Single().NumberValue);
    }

    [Fact]
    public async Task ApplyAsync_UrlAnswer_Persists()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var question = await AddQuestionAsync(db, job, ScreeningQuestionType.Url);
        var sut = CreateSut(db);

        await sut.ApplyAsync(candidate.Id, job.Id, null, new[] { new SubmitScreeningAnswerRequest(question.Id, "https://github.com/casey", null, null) });

        Assert.Equal("https://github.com/casey", db.ScreeningAnswers.Single().TextValue);
    }

    [Fact]
    public async Task ApplyAsync_SingleChoiceAnswer_PersistsSelectedOption()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var question = await AddQuestionAsync(db, job, ScreeningQuestionType.SingleChoice, options: new[] { "Remote", "On-site" });
        var optionId = question.Options.First(o => o.OptionText == "Remote").Id;
        var sut = CreateSut(db);

        await sut.ApplyAsync(candidate.Id, job.Id, null, new[] { new SubmitScreeningAnswerRequest(question.Id, null, null, new[] { optionId }) });

        var saved = db.ScreeningAnswers.Include(a => a.SelectedOptions).Single();
        Assert.Single(saved.SelectedOptions);
    }

    [Fact]
    public async Task ApplyAsync_MultipleChoiceAnswer_PersistsAllSelectedOptions()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var question = await AddQuestionAsync(db, job, ScreeningQuestionType.MultipleChoice, options: new[] { "C#", "Java", "Python" });
        var selectedIds = question.Options.Where(o => o.OptionText != "Java").Select(o => o.Id).ToList();
        var sut = CreateSut(db);

        await sut.ApplyAsync(candidate.Id, job.Id, null, new[] { new SubmitScreeningAnswerRequest(question.Id, null, null, selectedIds) });

        var saved = db.ScreeningAnswers.Include(a => a.SelectedOptions).Single();
        Assert.Equal(2, saved.SelectedOptions.Count);
    }

    [Fact]
    public async Task ApplyAsync_MissingRequiredAnswer_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        await AddQuestionAsync(db, job, ScreeningQuestionType.ShortText, required: true);
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<ValidationException>(() => sut.ApplyAsync(candidate.Id, job.Id, null));
    }

    [Fact]
    public async Task ApplyAsync_InvalidOptionId_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var question = await AddQuestionAsync(db, job, ScreeningQuestionType.SingleChoice, options: new[] { "Remote", "On-site" });
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<ValidationException>(() =>
            sut.ApplyAsync(candidate.Id, job.Id, null, new[] { new SubmitScreeningAnswerRequest(question.Id, null, null, new[] { 999999 }) }));
    }

    [Fact]
    public async Task ApplyAsync_UnparseableNumber_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var question = await AddQuestionAsync(db, job, ScreeningQuestionType.Number);
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<ValidationException>(() =>
            sut.ApplyAsync(candidate.Id, job.Id, null, new[] { new SubmitScreeningAnswerRequest(question.Id, "not-a-number", null, null) }));
    }

    [Fact]
    public async Task ApplyAsync_MalformedUrl_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var question = await AddQuestionAsync(db, job, ScreeningQuestionType.Url);
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<ValidationException>(() =>
            sut.ApplyAsync(candidate.Id, job.Id, null, new[] { new SubmitScreeningAnswerRequest(question.Id, "not a url", null, null) }));
    }

    [Fact]
    public async Task GetApplicationDetailAsync_Candidate_NeverIncludesPreferredAnswer()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var question = await AddQuestionAsync(db, job, ScreeningQuestionType.YesNo, preferredAnswer: "Yes");
        var sut = CreateSut(db);
        var application = await sut.ApplyAsync(candidate.Id, job.Id, null, new[] { new SubmitScreeningAnswerRequest(question.Id, "Yes", null, null) });

        var detail = await sut.GetApplicationDetailAsync(candidate.Id, "Candidate", application.Id);

        Assert.Null(detail.ScreeningAnswers!.Single().PreferredAnswer);
    }

    [Fact]
    public async Task GetApplicationDetailAsync_OwningRecruiter_IncludesPreferredAnswer()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, recruiter, job, _) = await SeedAsync(db);
        var question = await AddQuestionAsync(db, job, ScreeningQuestionType.YesNo, preferredAnswer: "Yes");
        var sut = CreateSut(db);
        var application = await sut.ApplyAsync(candidate.Id, job.Id, null, new[] { new SubmitScreeningAnswerRequest(question.Id, "Yes", null, null) });

        var detail = await sut.GetApplicationDetailAsync(recruiter.Id, "Recruiter", application.Id);

        Assert.Equal("Yes", detail.ScreeningAnswers!.Single().PreferredAnswer);
    }

    [Fact]
    public async Task GetApplicationDetailAsync_DifferentCompanyRecruiter_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, _, job, _) = await SeedAsync(db);
        var question = await AddQuestionAsync(db, job, ScreeningQuestionType.YesNo);
        var sut = CreateSut(db);
        var application = await sut.ApplyAsync(candidate.Id, job.Id, null, new[] { new SubmitScreeningAnswerRequest(question.Id, "Yes", null, null) });

        var otherRecruiterUser = new User { FullName = "Other Recruiter", Email = "other-app@example.com", Role = UserRole.Recruiter };
        db.Users.Add(otherRecruiterUser);
        await db.SaveChangesAsync();
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = otherRecruiterUser.Id, Company = new Company { Name = "Other Co" } });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.GetApplicationDetailAsync(otherRecruiterUser.Id, "Recruiter", application.Id));
    }

    [Fact]
    public async Task GetApplicationsForJobAsync_DifferentCompanyRecruiter_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (_, _, job, _) = await SeedAsync(db);
        var otherRecruiterUser = new User { FullName = "Other Recruiter", Email = "other-app2@example.com", Role = UserRole.Recruiter };
        db.Users.Add(otherRecruiterUser);
        await db.SaveChangesAsync();
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = otherRecruiterUser.Id, Company = new Company { Name = "Other Co" } });
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.GetApplicationsForJobAsync(otherRecruiterUser.Id, job.Id));
    }

    [Fact]
    public async Task GetApplicationsForJobAsync_FilterByYesNo_ReturnsOnlyMatching()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, recruiter, job, _) = await SeedAsync(db);
        var question = await AddQuestionAsync(db, job, ScreeningQuestionType.YesNo, required: false);
        var sut = CreateSut(db);
        await sut.ApplyAsync(candidate.Id, job.Id, null, new[] { new SubmitScreeningAnswerRequest(question.Id, "Yes", null, null) });

        var candidate2User = new User { FullName = "Other Candidate", Email = "other-cand@example.com", Role = UserRole.Candidate };
        db.Users.Add(candidate2User);
        await db.SaveChangesAsync();
        db.CandidateProfiles.Add(new CandidateProfile { UserId = candidate2User.Id });
        await db.SaveChangesAsync();
        await sut.ApplyAsync(candidate2User.Id, job.Id, null, new[] { new SubmitScreeningAnswerRequest(question.Id, "No", null, null) });

        var results = await sut.GetApplicationsForJobAsync(recruiter.Id, job.Id, new ApplicantScreeningFilterQuery(question.Id, "Yes", null, null, null, null));

        Assert.Single(results);
    }

    [Fact]
    public async Task GetApplicationsForJobAsync_FilterBySelectedOption_ReturnsOnlyMatching()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, recruiter, job, _) = await SeedAsync(db);
        var question = await AddQuestionAsync(db, job, ScreeningQuestionType.SingleChoice, required: false, options: new[] { "Remote", "On-site" });
        var remoteId = question.Options.First(o => o.OptionText == "Remote").Id;
        var onSiteId = question.Options.First(o => o.OptionText == "On-site").Id;
        var sut = CreateSut(db);
        await sut.ApplyAsync(candidate.Id, job.Id, null, new[] { new SubmitScreeningAnswerRequest(question.Id, null, null, new[] { remoteId }) });

        var candidate2User = new User { FullName = "Other Candidate", Email = "other-cand2@example.com", Role = UserRole.Candidate };
        db.Users.Add(candidate2User);
        await db.SaveChangesAsync();
        db.CandidateProfiles.Add(new CandidateProfile { UserId = candidate2User.Id });
        await db.SaveChangesAsync();
        await sut.ApplyAsync(candidate2User.Id, job.Id, null, new[] { new SubmitScreeningAnswerRequest(question.Id, null, null, new[] { onSiteId }) });

        var results = await sut.GetApplicationsForJobAsync(recruiter.Id, job.Id, new ApplicantScreeningFilterQuery(null, null, remoteId, null, null, null));

        Assert.Single(results);
    }

    [Fact]
    public async Task GetApplicationsForJobAsync_FilterByNumericRange_ReturnsOnlyMatching()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, recruiter, job, _) = await SeedAsync(db);
        var question = await AddQuestionAsync(db, job, ScreeningQuestionType.Number, required: false);
        var sut = CreateSut(db);
        await sut.ApplyAsync(candidate.Id, job.Id, null, new[] { new SubmitScreeningAnswerRequest(question.Id, null, 3, null) });

        var candidate2User = new User { FullName = "Other Candidate", Email = "other-cand3@example.com", Role = UserRole.Candidate };
        db.Users.Add(candidate2User);
        await db.SaveChangesAsync();
        db.CandidateProfiles.Add(new CandidateProfile { UserId = candidate2User.Id });
        await db.SaveChangesAsync();
        await sut.ApplyAsync(candidate2User.Id, job.Id, null, new[] { new SubmitScreeningAnswerRequest(question.Id, null, 8, null) });

        var results = await sut.GetApplicationsForJobAsync(recruiter.Id, job.Id, new ApplicantScreeningFilterQuery(question.Id, null, null, 0, 5, null));

        Assert.Single(results);
    }

    [Fact]
    public async Task GetApplicationsForJobAsync_FilterAnsweredRequiredOnly_ExcludesIncomplete()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, recruiter, job, _) = await SeedAsync(db);
        var required1 = await AddQuestionAsync(db, job, ScreeningQuestionType.ShortText, required: true, order: 0);
        var required2 = await AddQuestionAsync(db, job, ScreeningQuestionType.ShortText, required: true, order: 1);
        var sut = CreateSut(db);
        await sut.ApplyAsync(candidate.Id, job.Id, null, new[]
        {
            new SubmitScreeningAnswerRequest(required1.Id, "answer1", null, null),
            new SubmitScreeningAnswerRequest(required2.Id, "answer2", null, null),
        });

        var results = await sut.GetApplicationsForJobAsync(recruiter.Id, job.Id, new ApplicantScreeningFilterQuery(null, null, null, null, null, true));

        Assert.Single(results);
        Assert.Equal(2, results[0].RequiredQuestionsAnsweredCount);
        Assert.Equal(2, results[0].RequiredQuestionsTotalCount);
    }

    [Fact]
    public async Task UpdateAsync_EditingJobAfterAnswer_PreservesOriginalAnswerAndQuestion()
    {
        using var db = TestDbContextFactory.Create();
        var (candidate, recruiter, job, _) = await SeedAsync(db);
        var answeredQuestion = await AddQuestionAsync(db, job, ScreeningQuestionType.ShortText, order: 0);
        var unansweredQuestion = await AddQuestionAsync(db, job, ScreeningQuestionType.ShortText, required: false, order: 1);
        var sut = CreateSut(db);
        var application = await sut.ApplyAsync(candidate.Id, job.Id, null, new[] { new SubmitScreeningAnswerRequest(answeredQuestion.Id, "Bengaluru", null, null) });

        var jobPostingSut = new JobPostingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db), TestServiceFactory.CreateViewDedup(), TestServiceFactory.CreateNotifications(db), TestServiceFactory.CreateSalaryInsightsService(db));
        var update = new AIRecruiter.Application.DTOs.Jobs.UpdateJobPostingRequest(job.Title, job.Description, null, null, null, null, null, "Bengaluru", "Karnataka", null, false, job.JobType,
            new[] { new AIRecruiter.Application.DTOs.Jobs.UpsertScreeningQuestionRequest(answeredQuestion.Id, answeredQuestion.QuestionText, "ShortText", true, null, null, 0, null) });
        await jobPostingSut.UpdateAsync(recruiter.Id, job.Id, update);

        var detail = await sut.GetApplicationDetailAsync(candidate.Id, "Candidate", application.Id);

        Assert.Single(detail.ScreeningAnswers!);
        Assert.Equal("Bengaluru", detail.ScreeningAnswers![0].TextValue);
        Assert.Empty(db.JobScreeningQuestions.Where(q => q.Id == unansweredQuestion.Id));
    }
}
