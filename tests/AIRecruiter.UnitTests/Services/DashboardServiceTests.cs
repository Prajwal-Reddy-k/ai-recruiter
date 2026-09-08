using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class DashboardServiceTests
{
    private static DashboardService CreateSut(AppDbContext db)
    {
        var onboarding = new RecruiterOnboardingService(db, TestServiceFactory.CreateLocationValidator(), TestServiceFactory.CreateAuditLog(db));
        var notifications = TestServiceFactory.CreateNotifications(db);
        var auditLog = TestServiceFactory.CreateAuditLog(db);
        var savedJobs = TestServiceFactory.CreateSavedJobs(db);
        var interviews = new InterviewService(db, notifications, auditLog);
        var jobAlerts = TestServiceFactory.CreateJobAlerts(db);
        var careerGoals = new CareerGoalService(db, TestServiceFactory.CreateLocationValidator());
        var jobViews = new JobViewService(db);
        return new DashboardService(db, onboarding, savedJobs, interviews, jobAlerts, careerGoals, jobViews);
    }

    [Fact]
    public async Task GetCandidateDashboardAsync_EmptyProfile_ZeroCompletion()
    {
        using var db = TestDbContextFactory.Create();
        var user = new User { FullName = "Casey Candidate", Email = "casey@example.com", Role = UserRole.Candidate };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        db.CandidateProfiles.Add(new CandidateProfile { UserId = user.Id });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var dashboard = await sut.GetCandidateDashboardAsync(user.Id);

        Assert.Equal(0, dashboard.ProfileCompletionPercent);
    }

    [Fact]
    public async Task GetCandidateDashboardAsync_IncludesRecentAssessmentResultsAndCareerGoalsSummary()
    {
        using var db = TestDbContextFactory.Create();
        var user = new User { FullName = "Casey Candidate", Email = "casey-goals@example.com", Role = UserRole.Candidate };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var profile = new CandidateProfile { UserId = user.Id };
        db.CandidateProfiles.Add(profile);
        await db.SaveChangesAsync();

        db.SkillAssessmentAttempts.Add(new SkillAssessmentAttempt
        {
            CandidateProfileId = profile.Id,
            Category = AssessmentCategory.Java,
            Status = AssessmentAttemptStatus.Completed,
            ExpiresAt = DateTime.UtcNow,
            SubmittedAt = DateTime.UtcNow,
            ScoreCorrectCount = 10,
            TotalQuestionCount = 15,
            PercentageScore = 66.7m,
        });
        db.CareerGoals.Add(new CareerGoal
        {
            CandidateProfileId = profile.Id,
            TargetRole = "Senior Backend Engineer",
            Status = CareerGoalStatus.InProgress,
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var dashboard = await sut.GetCandidateDashboardAsync(user.Id);

        Assert.Single(dashboard.RecentAssessmentResults);
        Assert.Equal("Java", dashboard.RecentAssessmentResults[0].Category);
        Assert.Single(dashboard.CareerGoalsSummary.Goals);
        Assert.Equal("Senior Backend Engineer", dashboard.CareerGoalsSummary.Goals[0].TargetRole);
    }

    [Fact]
    public async Task GetCandidateDashboardAsync_FullyFilledProfile_HundredPercentCompletion()
    {
        using var db = TestDbContextFactory.Create();
        var user = new User { FullName = "Casey Candidate", Email = "casey@example.com", Role = UserRole.Candidate };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var profile = new CandidateProfile
        {
            UserId = user.Id,
            Headline = "Backend Engineer",
            Summary = "Summary",
            Education = "B.Sc.",
            ExperienceSummary = "5 years",
            TotalExperienceYears = 5,
            City = "Bengaluru",
            SkillsCsv = "C#, SQL Server, Docker",
            ResumeStorageKey = "abc123.pdf",
            AvatarStorageKey = "avatar123.jpg",
            LinkedInUrl = "https://linkedin.com/in/casey",
            PreferredJobTypesCsv = "FullTime",
        };
        db.CandidateProfiles.Add(profile);
        await db.SaveChangesAsync();
        db.CandidateWorkExperiences.Add(new CandidateWorkExperience { CandidateProfileId = profile.Id, Title = "Engineer", Company = "Acme", StartDate = DateTime.UtcNow.AddYears(-2) });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var dashboard = await sut.GetCandidateDashboardAsync(user.Id);

        Assert.Equal(100, dashboard.ProfileCompletionPercent);
    }

    [Fact]
    public async Task GetCandidateDashboardAsync_ApplicationSummary_BucketsCorrectly()
    {
        using var db = TestDbContextFactory.Create();
        var candidateUser = new User { FullName = "Casey Candidate", Email = "casey@example.com", Role = UserRole.Candidate };
        var recruiterUser = new User { FullName = "Rita Recruiter", Email = "rita@example.com", Role = UserRole.Recruiter };
        db.Users.AddRange(candidateUser, recruiterUser);
        await db.SaveChangesAsync();

        var company = new Company { Name = "Acme" };
        var recruiterProfile = new RecruiterProfile { UserId = recruiterUser.Id, Company = company };
        var candidateProfile = new CandidateProfile { UserId = candidateUser.Id };
        db.RecruiterProfiles.Add(recruiterProfile);
        db.CandidateProfiles.Add(candidateProfile);
        await db.SaveChangesAsync();

        var job1 = new JobPosting { Title = "A", Description = "d", Status = JobStatus.Open, CompanyId = company.Id, RecruiterProfileId = recruiterProfile.Id };
        var job2 = new JobPosting { Title = "B", Description = "d", Status = JobStatus.Open, CompanyId = company.Id, RecruiterProfileId = recruiterProfile.Id };
        var job3 = new JobPosting { Title = "C", Description = "d", Status = JobStatus.Open, CompanyId = company.Id, RecruiterProfileId = recruiterProfile.Id };
        var job4 = new JobPosting { Title = "D", Description = "d", Status = JobStatus.Open, CompanyId = company.Id, RecruiterProfileId = recruiterProfile.Id };
        db.JobPostings.AddRange(job1, job2, job3, job4);
        await db.SaveChangesAsync();

        db.JobApplications.AddRange(
            new JobApplication { JobPostingId = job1.Id, CandidateProfileId = candidateProfile.Id, Status = ApplicationStatus.Applied },
            new JobApplication { JobPostingId = job2.Id, CandidateProfileId = candidateProfile.Id, Status = ApplicationStatus.InterviewScheduled },
            new JobApplication { JobPostingId = job3.Id, CandidateProfileId = candidateProfile.Id, Status = ApplicationStatus.Shortlisted },
            new JobApplication { JobPostingId = job4.Id, CandidateProfileId = candidateProfile.Id, Status = ApplicationStatus.Rejected });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var dashboard = await sut.GetCandidateDashboardAsync(candidateUser.Id);

        Assert.Equal(1, dashboard.ApplicationSummary.Applied);
        Assert.Equal(1, dashboard.ApplicationSummary.UnderReview);
        Assert.Equal(1, dashboard.ApplicationSummary.Shortlisted);
        Assert.Equal(1, dashboard.ApplicationSummary.Rejected);
    }

    [Fact]
    public async Task GetCandidateDashboardAsync_RecommendsJobsMatchingSkillsFirst()
    {
        using var db = TestDbContextFactory.Create();
        var candidateUser = new User { FullName = "Casey Candidate", Email = "casey@example.com", Role = UserRole.Candidate };
        var recruiterUser = new User { FullName = "Rita Recruiter", Email = "rita@example.com", Role = UserRole.Recruiter };
        db.Users.AddRange(candidateUser, recruiterUser);
        await db.SaveChangesAsync();

        var company = new Company { Name = "Acme" };
        var recruiterProfile = new RecruiterProfile { UserId = recruiterUser.Id, Company = company };
        var candidateProfile = new CandidateProfile { UserId = candidateUser.Id, SkillsCsv = "C#, SQL Server" };
        db.RecruiterProfiles.Add(recruiterProfile);
        db.CandidateProfiles.Add(candidateProfile);
        await db.SaveChangesAsync();

        var matchingJob = new JobPosting { Title = "Backend role", Description = "d", RequiredSkillsCsv = "C#, SQL Server", Status = JobStatus.Open, CompanyId = company.Id, RecruiterProfileId = recruiterProfile.Id };
        var nonMatchingJob = new JobPosting { Title = "Design role", Description = "d", RequiredSkillsCsv = "Figma", Status = JobStatus.Open, CompanyId = company.Id, RecruiterProfileId = recruiterProfile.Id };
        db.JobPostings.AddRange(nonMatchingJob, matchingJob);
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var dashboard = await sut.GetCandidateDashboardAsync(candidateUser.Id);

        Assert.Equal("Backend role", dashboard.RecommendedJobs.First().Title);
    }

    [Fact]
    public async Task GetRecruiterDashboardAsync_ScopesCountsToOwnJobsOnly()
    {
        using var db = TestDbContextFactory.Create();
        var recruiter1User = new User { FullName = "Rita Recruiter", Email = "rita@example.com", Role = UserRole.Recruiter };
        var recruiter2User = new User { FullName = "Rick Recruiter", Email = "rick@example.com", Role = UserRole.Recruiter };
        var candidateUser = new User { FullName = "Casey Candidate", Email = "casey@example.com", Role = UserRole.Candidate };
        db.Users.AddRange(recruiter1User, recruiter2User, candidateUser);
        await db.SaveChangesAsync();

        var company1 = new Company { Name = "Acme" };
        var company2 = new Company { Name = "Other Co" };
        var recruiter1Profile = new RecruiterProfile { UserId = recruiter1User.Id, Company = company1 };
        var recruiter2Profile = new RecruiterProfile { UserId = recruiter2User.Id, Company = company2 };
        var candidateProfile = new CandidateProfile { UserId = candidateUser.Id };
        db.RecruiterProfiles.AddRange(recruiter1Profile, recruiter2Profile);
        db.CandidateProfiles.Add(candidateProfile);
        await db.SaveChangesAsync();

        var recruiter1Job = new JobPosting { Title = "R1 Job", Description = "d", Status = JobStatus.Open, CompanyId = company1.Id, RecruiterProfileId = recruiter1Profile.Id };
        var recruiter2Job = new JobPosting { Title = "R2 Job", Description = "d", Status = JobStatus.Open, CompanyId = company2.Id, RecruiterProfileId = recruiter2Profile.Id };
        db.JobPostings.AddRange(recruiter1Job, recruiter2Job);
        await db.SaveChangesAsync();

        db.JobApplications.AddRange(
            new JobApplication { JobPostingId = recruiter1Job.Id, CandidateProfileId = candidateProfile.Id, Status = ApplicationStatus.Applied },
            new JobApplication { JobPostingId = recruiter2Job.Id, CandidateProfileId = candidateProfile.Id, Status = ApplicationStatus.Applied });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var dashboard = await sut.GetRecruiterDashboardAsync(recruiter1User.Id);

        Assert.Equal(1, dashboard.ActiveJobPostingCount);
        Assert.Equal(1, dashboard.TotalApplicantCount);
        Assert.Single(dashboard.JobPerformance);
        Assert.Equal("R1 Job", dashboard.JobPerformance.Single().Title);
    }

    [Fact]
    public async Task GetCandidateDashboardAsync_IncompleteProfile_SuggestsCompletingProfileFirst()
    {
        using var db = TestDbContextFactory.Create();
        var user = new User { FullName = "Casey Candidate", Email = "casey@example.com", Role = UserRole.Candidate };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        db.CandidateProfiles.Add(new CandidateProfile { UserId = user.Id });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var dashboard = await sut.GetCandidateDashboardAsync(user.Id);

        Assert.NotEmpty(dashboard.NextBestActions);
    }

    [Fact]
    public async Task GetCandidateDashboardAsync_PendingInvitation_AppearsInNextBestActions()
    {
        using var db = TestDbContextFactory.Create();
        var candidateUser = new User { FullName = "Casey Candidate", Email = "casey@example.com", Role = UserRole.Candidate };
        var recruiterUser = new User { FullName = "Rita Recruiter", Email = "rita@example.com", Role = UserRole.Recruiter };
        db.Users.AddRange(candidateUser, recruiterUser);
        await db.SaveChangesAsync();
        var company = new Company { Name = "Acme" };
        var recruiterProfile = new RecruiterProfile { UserId = recruiterUser.Id, Company = company };
        var candidateProfile = new CandidateProfile { UserId = candidateUser.Id };
        db.RecruiterProfiles.Add(recruiterProfile);
        db.CandidateProfiles.Add(candidateProfile);
        await db.SaveChangesAsync();
        var job = new JobPosting { Title = "Role", Description = "d", Status = JobStatus.Open, CompanyId = company.Id, RecruiterProfileId = recruiterProfile.Id };
        db.JobPostings.Add(job);
        await db.SaveChangesAsync();
        db.Invitations.Add(new Invitation { JobPostingId = job.Id, CandidateProfileId = candidateProfile.Id, InvitedByUserId = recruiterUser.Id, Status = InvitationStatus.Sent, ExpiresAtUtc = DateTime.UtcNow.AddDays(14) });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var dashboard = await sut.GetCandidateDashboardAsync(candidateUser.Id);

        Assert.Contains(dashboard.NextBestActions, a => a.Label.Contains("invitation", StringComparison.OrdinalIgnoreCase));
    }
}
