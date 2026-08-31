using AIRecruiter.Application.DTOs.Interviews;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class InterviewServiceTests
{
    private static InterviewService CreateSut(AppDbContext db) =>
        new(db, TestServiceFactory.CreateNotifications(db), TestServiceFactory.CreateAuditLog(db));

    private static async Task<(User Recruiter, User Candidate, JobApplication Application)> SeedAsync(AppDbContext db)
    {
        var recruiterUser = new User { FullName = "Rita Recruiter", Email = "rita@example.com", Role = UserRole.Recruiter };
        var candidateUser = new User { FullName = "Casey Candidate", Email = "casey@example.com", Role = UserRole.Candidate };
        db.Users.AddRange(recruiterUser, candidateUser);
        await db.SaveChangesAsync();

        var company = new Company { Name = "Acme Corp" };
        var recruiterProfile = new RecruiterProfile { UserId = recruiterUser.Id, Company = company };
        var candidateProfile = new CandidateProfile { UserId = candidateUser.Id };
        db.RecruiterProfiles.Add(recruiterProfile);
        db.CandidateProfiles.Add(candidateProfile);
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

        var application = new JobApplication
        {
            JobPostingId = job.Id,
            CandidateProfileId = candidateProfile.Id,
            Status = ApplicationStatus.Applied,
        };
        db.JobApplications.Add(application);
        await db.SaveChangesAsync();

        return (recruiterUser, candidateUser, application);
    }

    private static ScheduleInterviewRequest ValidRequest(int hoursFromNow = 24) => new(
        DateTime.UtcNow.AddHours(hoursFromNow),
        DateTime.UtcNow.AddHours(hoursFromNow).AddMinutes(30),
        InterviewType.Online,
        "https://meet.example.com/abc",
        "Looking forward to it.");

    [Fact]
    public async Task ScheduleAsync_OwningRecruiter_CreatesProposedInterview()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, _, application) = await SeedAsync(db);
        var sut = CreateSut(db);

        var result = await sut.ScheduleAsync(recruiter.Id, application.Id, ValidRequest());

        Assert.Equal("Proposed", result.Status);
        Assert.True(result.CanManage);
    }

    [Fact]
    public async Task ScheduleAsync_NonOwningRecruiter_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (_, _, application) = await SeedAsync(db);
        var sut = CreateSut(db);

        var otherRecruiter = new User { FullName = "Other Recruiter", Email = "other@example.com", Role = UserRole.Recruiter };
        db.Users.Add(otherRecruiter);
        await db.SaveChangesAsync();
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = otherRecruiter.Id, Company = new Company { Name = "Other Co" } });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.ScheduleAsync(otherRecruiter.Id, application.Id, ValidRequest()));
    }

    [Fact]
    public async Task ScheduleAsync_StartInThePast_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, _, application) = await SeedAsync(db);
        var sut = CreateSut(db);

        var pastRequest = new ScheduleInterviewRequest(
            DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(-1).AddMinutes(30),
            InterviewType.Online, "https://meet.example.com", null);

        await Assert.ThrowsAsync<ValidationException>(() => sut.ScheduleAsync(recruiter.Id, application.Id, pastRequest));
    }

    [Fact]
    public async Task ScheduleAsync_EndBeforeStart_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, _, application) = await SeedAsync(db);
        var sut = CreateSut(db);

        var start = DateTime.UtcNow.AddHours(24);
        var invalidRequest = new ScheduleInterviewRequest(start, start.AddMinutes(-10), InterviewType.Online, "https://meet.example.com", null);

        await Assert.ThrowsAsync<ValidationException>(() => sut.ScheduleAsync(recruiter.Id, application.Id, invalidRequest));
    }

    [Fact]
    public async Task ScheduleAsync_OverlappingForSameCandidate_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, candidate, application) = await SeedAsync(db);
        var sut = CreateSut(db);
        await sut.ScheduleAsync(recruiter.Id, application.Id, ValidRequest());

        // A second job/application for the SAME candidate, overlapping the same time window.
        var company2 = new Company { Name = "Second Co" };
        var recruiter2User = new User { FullName = "Second Recruiter", Email = "second@example.com", Role = UserRole.Recruiter };
        db.Users.Add(recruiter2User);
        await db.SaveChangesAsync();
        var recruiter2Profile = new RecruiterProfile { UserId = recruiter2User.Id, Company = company2 };
        db.RecruiterProfiles.Add(recruiter2Profile);
        await db.SaveChangesAsync();
        var job2 = new JobPosting { Title = "Other Role", Description = "d", Status = JobStatus.Open, CompanyId = company2.Id, RecruiterProfileId = recruiter2Profile.Id };
        db.JobPostings.Add(job2);
        await db.SaveChangesAsync();
        var application2 = new JobApplication { JobPostingId = job2.Id, CandidateProfileId = application.CandidateProfileId, Status = ApplicationStatus.Applied };
        db.JobApplications.Add(application2);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() => sut.ScheduleAsync(recruiter2User.Id, application2.Id, ValidRequest()));
    }

    [Fact]
    public async Task ScheduleAsync_OverlappingForSameRecruiter_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, _, application) = await SeedAsync(db);
        var sut = CreateSut(db);
        await sut.ScheduleAsync(recruiter.Id, application.Id, ValidRequest());

        // Same recruiter, a different candidate applying to the same job, same time window.
        var candidate2User = new User { FullName = "Other Candidate", Email = "other-cand@example.com", Role = UserRole.Candidate };
        db.Users.Add(candidate2User);
        await db.SaveChangesAsync();
        var candidate2Profile = new CandidateProfile { UserId = candidate2User.Id };
        db.CandidateProfiles.Add(candidate2Profile);
        await db.SaveChangesAsync();
        var application2 = new JobApplication { JobPostingId = application.JobPostingId, CandidateProfileId = candidate2Profile.Id, Status = ApplicationStatus.Applied };
        db.JobApplications.Add(application2);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() => sut.ScheduleAsync(recruiter.Id, application2.Id, ValidRequest()));
    }

    [Fact]
    public async Task ScheduleAsync_NonOverlappingTime_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, _, application) = await SeedAsync(db);
        var sut = CreateSut(db);
        await sut.ScheduleAsync(recruiter.Id, application.Id, ValidRequest(24));

        var candidate2User = new User { FullName = "Other Candidate", Email = "other-cand2@example.com", Role = UserRole.Candidate };
        db.Users.Add(candidate2User);
        await db.SaveChangesAsync();
        db.CandidateProfiles.Add(new CandidateProfile { UserId = candidate2User.Id });
        await db.SaveChangesAsync();
        var candidate2Profile = db.CandidateProfiles.First(c => c.UserId == candidate2User.Id);
        var application2 = new JobApplication { JobPostingId = application.JobPostingId, CandidateProfileId = candidate2Profile.Id, Status = ApplicationStatus.Applied };
        db.JobApplications.Add(application2);
        await db.SaveChangesAsync();

        var result = await sut.ScheduleAsync(recruiter.Id, application2.Id, ValidRequest(48));

        Assert.Equal("Proposed", result.Status);
    }

    [Fact]
    public async Task AcceptAsync_OwningCandidate_SetsScheduledAndAdvancesApplicationStatus()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, candidate, application) = await SeedAsync(db);
        var sut = CreateSut(db);
        var interview = await sut.ScheduleAsync(recruiter.Id, application.Id, ValidRequest());

        var result = await sut.AcceptAsync(candidate.Id, interview.Id, new RespondInterviewRequest("Looking forward to it"));

        Assert.Equal("Scheduled", result.Status);
        var updatedApplication = db.JobApplications.First(a => a.Id == application.Id);
        Assert.Equal(ApplicationStatus.InterviewScheduled, updatedApplication.Status);
    }

    [Fact]
    public async Task AcceptAsync_NonOwningCandidate_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, _, application) = await SeedAsync(db);
        var sut = CreateSut(db);
        var interview = await sut.ScheduleAsync(recruiter.Id, application.Id, ValidRequest());

        var otherCandidate = new User { FullName = "Other Candidate", Email = "othercand@example.com", Role = UserRole.Candidate };
        db.Users.Add(otherCandidate);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.AcceptAsync(otherCandidate.Id, interview.Id, new RespondInterviewRequest(null)));
    }

    [Fact]
    public async Task DeclineAsync_OwningCandidate_SetsDeclinedWithNote()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, candidate, application) = await SeedAsync(db);
        var sut = CreateSut(db);
        var interview = await sut.ScheduleAsync(recruiter.Id, application.Id, ValidRequest());

        var result = await sut.DeclineAsync(candidate.Id, interview.Id, new RespondInterviewRequest("Not available at this time"));

        Assert.Equal("Declined", result.Status);
        Assert.Equal("Not available at this time", result.CandidateResponseNote);
    }

    [Fact]
    public async Task AcceptAsync_AlreadyDeclined_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, candidate, application) = await SeedAsync(db);
        var sut = CreateSut(db);
        var interview = await sut.ScheduleAsync(recruiter.Id, application.Id, ValidRequest());
        await sut.DeclineAsync(candidate.Id, interview.Id, new RespondInterviewRequest(null));

        await Assert.ThrowsAsync<ConflictException>(() => sut.AcceptAsync(candidate.Id, interview.Id, new RespondInterviewRequest(null)));
    }

    [Fact]
    public async Task RescheduleAsync_OwningRecruiter_ResetsToProposed()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, candidate, application) = await SeedAsync(db);
        var sut = CreateSut(db);
        var interview = await sut.ScheduleAsync(recruiter.Id, application.Id, ValidRequest());
        await sut.AcceptAsync(candidate.Id, interview.Id, new RespondInterviewRequest(null));

        var newStart = DateTime.UtcNow.AddHours(72);
        var result = await sut.RescheduleAsync(recruiter.Id, interview.Id,
            new RescheduleInterviewRequest(newStart, newStart.AddMinutes(30), null, null, null));

        Assert.Equal("Proposed", result.Status);
        Assert.Equal(newStart, result.ScheduledStartUtc);
    }

    [Fact]
    public async Task RescheduleAsync_NonOwningRecruiter_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, _, application) = await SeedAsync(db);
        var sut = CreateSut(db);
        var interview = await sut.ScheduleAsync(recruiter.Id, application.Id, ValidRequest());

        var otherRecruiter = new User { FullName = "Other Recruiter", Email = "other-r@example.com", Role = UserRole.Recruiter };
        db.Users.Add(otherRecruiter);
        await db.SaveChangesAsync();
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = otherRecruiter.Id, Company = new Company { Name = "Other Co" } });
        await db.SaveChangesAsync();

        var newStart = DateTime.UtcNow.AddHours(72);
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.RescheduleAsync(
            otherRecruiter.Id, interview.Id, new RescheduleInterviewRequest(newStart, newStart.AddMinutes(30), null, null, null)));
    }

    [Fact]
    public async Task CancelAsync_OwningRecruiter_SetsCancelled()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, _, application) = await SeedAsync(db);
        var sut = CreateSut(db);
        var interview = await sut.ScheduleAsync(recruiter.Id, application.Id, ValidRequest());

        var result = await sut.CancelAsync(recruiter.Id, interview.Id);

        Assert.Equal("Cancelled", result.Status);
    }

    [Fact]
    public async Task CancelAsync_AlreadyCancelled_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, _, application) = await SeedAsync(db);
        var sut = CreateSut(db);
        var interview = await sut.ScheduleAsync(recruiter.Id, application.Id, ValidRequest());
        await sut.CancelAsync(recruiter.Id, interview.Id);

        await Assert.ThrowsAsync<ConflictException>(() => sut.CancelAsync(recruiter.Id, interview.Id));
    }

    [Fact]
    public async Task CompleteAsync_ScheduledInterview_MarksCompletedAndAdvancesApplication()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, candidate, application) = await SeedAsync(db);
        var sut = CreateSut(db);
        var interview = await sut.ScheduleAsync(recruiter.Id, application.Id, ValidRequest());
        await sut.AcceptAsync(candidate.Id, interview.Id, new RespondInterviewRequest(null));

        var result = await sut.CompleteAsync(recruiter.Id, interview.Id);

        Assert.Equal("Completed", result.Status);
        var updatedApplication = db.JobApplications.First(a => a.Id == application.Id);
        Assert.Equal(ApplicationStatus.InterviewCompleted, updatedApplication.Status);
    }

    [Fact]
    public async Task CompleteAsync_StillProposed_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, _, application) = await SeedAsync(db);
        var sut = CreateSut(db);
        var interview = await sut.ScheduleAsync(recruiter.Id, application.Id, ValidRequest());

        await Assert.ThrowsAsync<ConflictException>(() => sut.CompleteAsync(recruiter.Id, interview.Id));
    }

    [Fact]
    public async Task GetMyInterviewsAsync_Recruiter_IsScopedToOwnCompanyOnly()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, _, application) = await SeedAsync(db);
        var sut = CreateSut(db);
        await sut.ScheduleAsync(recruiter.Id, application.Id, ValidRequest());

        var otherRecruiter = new User { FullName = "Other Recruiter", Email = "other-r2@example.com", Role = UserRole.Recruiter };
        db.Users.Add(otherRecruiter);
        await db.SaveChangesAsync();
        db.RecruiterProfiles.Add(new RecruiterProfile { UserId = otherRecruiter.Id, Company = new Company { Name = "Other Co" } });
        await db.SaveChangesAsync();

        var ownResults = await sut.GetMyInterviewsAsync(recruiter.Id, "Recruiter", null);
        var otherResults = await sut.GetMyInterviewsAsync(otherRecruiter.Id, "Recruiter", null);

        Assert.Single(ownResults);
        Assert.Empty(otherResults);
    }

    [Fact]
    public async Task GetMyInterviewsAsync_StatusFilter_FiltersCorrectly()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, candidate, application) = await SeedAsync(db);
        var sut = CreateSut(db);
        var interview = await sut.ScheduleAsync(recruiter.Id, application.Id, ValidRequest());
        await sut.AcceptAsync(candidate.Id, interview.Id, new RespondInterviewRequest(null));

        var scheduled = await sut.GetMyInterviewsAsync(candidate.Id, "Candidate", "Scheduled");
        var declined = await sut.GetMyInterviewsAsync(candidate.Id, "Candidate", "Declined");

        Assert.Single(scheduled);
        Assert.Empty(declined);
    }

    [Fact]
    public async Task GetIcsAsync_ScheduledInterview_ContainsCorrectUtcTimesAndDetails()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, candidate, application) = await SeedAsync(db);
        var sut = CreateSut(db);
        var interview = await sut.ScheduleAsync(recruiter.Id, application.Id, ValidRequest());
        await sut.AcceptAsync(candidate.Id, interview.Id, new RespondInterviewRequest(null));

        var (ics, fileName) = await sut.GetIcsAsync(candidate.Id, "Candidate", interview.Id);

        Assert.Contains("BEGIN:VCALENDAR", ics);
        Assert.Contains("BEGIN:VEVENT", ics);
        Assert.Contains("Backend Engineer", ics);
        Assert.Contains("Acme Corp", ics);
        Assert.Contains("https://meet.example.com/abc", ics);
        Assert.EndsWith(".ics", fileName);
    }

    [Fact]
    public async Task GetIcsAsync_ProposedNotYetAccepted_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, candidate, application) = await SeedAsync(db);
        var sut = CreateSut(db);
        var interview = await sut.ScheduleAsync(recruiter.Id, application.Id, ValidRequest());

        await Assert.ThrowsAsync<ValidationException>(() => sut.GetIcsAsync(candidate.Id, "Candidate", interview.Id));
    }

    [Fact]
    public async Task GetForApplicationAsync_UnrelatedCandidate_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiter, _, application) = await SeedAsync(db);
        var sut = CreateSut(db);
        await sut.ScheduleAsync(recruiter.Id, application.Id, ValidRequest());

        var otherCandidate = new User { FullName = "Other Candidate", Email = "unrelated@example.com", Role = UserRole.Candidate };
        db.Users.Add(otherCandidate);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.GetForApplicationAsync(otherCandidate.Id, "Candidate", application.Id));
    }
}
