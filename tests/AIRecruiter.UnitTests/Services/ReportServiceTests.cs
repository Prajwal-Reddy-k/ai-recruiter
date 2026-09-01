using AIRecruiter.Application.DTOs.Reports;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class ReportServiceTests
{
    private static ReportService CreateSut(AppDbContext db) => new(db, TestServiceFactory.CreateAuditLog(db));

    private static async Task<(User RecruiterA, User RecruiterB, JobApplication ApplicationA)> SeedTwoCompaniesAsync(AppDbContext db)
    {
        var recruiterA = new User { FullName = "Rita A", Email = "rita@a.example", Role = UserRole.Recruiter };
        var recruiterB = new User { FullName = "Bob B", Email = "bob@b.example", Role = UserRole.Recruiter };
        var candidateUser = new User { FullName = "Casey Candidate", Email = "casey@example.com", Role = UserRole.Candidate };
        db.Users.AddRange(recruiterA, recruiterB, candidateUser);
        await db.SaveChangesAsync();

        var companyA = new Company { Name = "Company A" };
        var companyB = new Company { Name = "Company B" };
        var profileA = new RecruiterProfile { UserId = recruiterA.Id, Company = companyA };
        var profileB = new RecruiterProfile { UserId = recruiterB.Id, Company = companyB };
        var candidateProfile = new CandidateProfile { UserId = candidateUser.Id, SkillsCsv = "C#, SQL" };
        db.RecruiterProfiles.AddRange(profileA, profileB);
        db.CandidateProfiles.Add(candidateProfile);
        await db.SaveChangesAsync();

        var jobA = new JobPosting { Title = "Backend Engineer A", Description = "role", Status = JobStatus.Open, CompanyId = companyA.Id, RecruiterProfileId = profileA.Id, PublishedAt = DateTime.UtcNow };
        var jobB = new JobPosting { Title = "Backend Engineer B", Description = "role", Status = JobStatus.Open, CompanyId = companyB.Id, RecruiterProfileId = profileB.Id, PublishedAt = DateTime.UtcNow };
        db.JobPostings.AddRange(jobA, jobB);
        await db.SaveChangesAsync();

        var applicationA = new JobApplication { JobPostingId = jobA.Id, CandidateProfileId = candidateProfile.Id, Status = ApplicationStatus.Shortlisted };
        var applicationB = new JobApplication { JobPostingId = jobB.Id, CandidateProfileId = candidateProfile.Id, Status = ApplicationStatus.Applied };
        db.JobApplications.AddRange(applicationA, applicationB);
        await db.SaveChangesAsync();

        return (recruiterA, recruiterB, applicationA);
    }

    [Fact]
    public async Task GetReportAsync_OnlyIncludesCallersOwnCompanyData()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiterA, _, _) = await SeedTwoCompaniesAsync(db);
        var sut = CreateSut(db);

        var report = await sut.GetReportAsync(recruiterA.Id, new ReportFilterRequest(null, null));

        Assert.Equal(1, report.JobsCreated);
        Assert.Equal(1, report.TotalApplications);
    }

    [Fact]
    public async Task ExportJobsCsvAsync_DoesNotIncludeOtherCompanyJobs()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiterA, _, _) = await SeedTwoCompaniesAsync(db);
        var sut = CreateSut(db);

        var csv = await sut.ExportJobsCsvAsync(recruiterA.Id, new ReportFilterRequest(null, null));

        Assert.Contains("Backend Engineer A", csv);
        Assert.DoesNotContain("Backend Engineer B", csv);
    }

    [Fact]
    public async Task ExportApplicantsCsvAsync_ScopedToCallersCompanyOnly()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiterA, recruiterB, _) = await SeedTwoCompaniesAsync(db);
        var sut = CreateSut(db);

        var csvA = await sut.ExportApplicantsCsvAsync(recruiterA.Id, new ReportFilterRequest(null, null));
        var csvB = await sut.ExportApplicantsCsvAsync(recruiterB.Id, new ReportFilterRequest(null, null));

        Assert.Contains("Backend Engineer A", csvA);
        Assert.DoesNotContain("Backend Engineer B", csvA);
        Assert.Contains("Backend Engineer B", csvB);
        Assert.DoesNotContain("Backend Engineer A", csvB);
    }

    [Fact]
    public async Task ExportApplicantsCsvAsync_NeverIncludesResumeOrPasswordColumns()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiterA, _, _) = await SeedTwoCompaniesAsync(db);
        var sut = CreateSut(db);

        var csv = await sut.ExportApplicantsCsvAsync(recruiterA.Id, new ReportFilterRequest(null, null));

        Assert.DoesNotContain("PasswordHash", csv, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ResumeStorageKey", csv, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SecurityStamp", csv, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetReportAsync_DateRangeFilter_ExcludesJobsOutsideRange()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiterA, _, _) = await SeedTwoCompaniesAsync(db);
        var sut = CreateSut(db);

        var futureFilter = new ReportFilterRequest(DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2));
        var report = await sut.GetReportAsync(recruiterA.Id, futureFilter);

        Assert.Equal(0, report.JobsCreated);
    }

    [Fact]
    public async Task GetReportAsync_StatusFunnel_IncludesAllApplicationStatuses()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiterA, _, _) = await SeedTwoCompaniesAsync(db);
        var sut = CreateSut(db);

        var report = await sut.GetReportAsync(recruiterA.Id, new ReportFilterRequest(null, null));

        Assert.Equal(Enum.GetValues<ApplicationStatus>().Length, report.StatusFunnel.Count);
    }
}
