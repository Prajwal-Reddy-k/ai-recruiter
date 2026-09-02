using AIRecruiter.Application.DTOs.Candidates;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Application.Validation;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Options;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;
using Microsoft.Extensions.Options;
using Moq;

namespace AIRecruiter.UnitTests.Services;

public class CandidateSearchServiceTests
{
    private static CandidateSearchService CreateSut(AppDbContext db)
    {
        var resumeStorage = new Mock<IResumeStorage>();
        var extractorFactory = new Mock<IResumeTextExtractorFactory>();
        var validator = new ResumeFileValidator();
        var candidateProfileService = TestServiceFactory.CreateCandidateProfileService(
            db, resumeStorage.Object, extractorFactory.Object, validator);

        return new CandidateSearchService(db, candidateProfileService, TestServiceFactory.CreateAuditLog(db));
    }

    private static async Task<(User RecruiterA, User RecruiterB, CandidateProfile Candidate, JobApplication Application)> SeedAsync(AppDbContext db)
    {
        var recruiterAUser = new User { FullName = "Rita RecruiterA", Email = "rita@companya.example", Role = UserRole.Recruiter };
        var recruiterBUser = new User { FullName = "Bob RecruiterB", Email = "bob@companyb.example", Role = UserRole.Recruiter };
        var candidateUser = new User { FullName = "Casey Candidate", Email = "casey@example.com", Role = UserRole.Candidate };
        db.Users.AddRange(recruiterAUser, recruiterBUser, candidateUser);
        await db.SaveChangesAsync();

        var companyA = new Company { Name = "Company A" };
        var companyB = new Company { Name = "Company B" };
        var recruiterAProfile = new RecruiterProfile { UserId = recruiterAUser.Id, Company = companyA };
        var recruiterBProfile = new RecruiterProfile { UserId = recruiterBUser.Id, Company = companyB };
        db.RecruiterProfiles.AddRange(recruiterAProfile, recruiterBProfile);

        var candidateProfile = new CandidateProfile
        {
            UserId = candidateUser.Id,
            Headline = "Backend Engineer",
            SkillsCsv = "C#, SQL Server",
            TotalExperienceYears = 5,
            City = "Bengaluru",
            State = "Karnataka",
        };
        db.CandidateProfiles.Add(candidateProfile);
        await db.SaveChangesAsync();

        var jobA = new JobPosting
        {
            Title = "Backend Engineer",
            Description = "role",
            Status = JobStatus.Open,
            CompanyId = companyA.Id,
            RecruiterProfileId = recruiterAProfile.Id,
        };
        db.JobPostings.Add(jobA);
        await db.SaveChangesAsync();

        var application = new JobApplication
        {
            JobPostingId = jobA.Id,
            CandidateProfileId = candidateProfile.Id,
            Status = ApplicationStatus.Applied,
            MatchScore = 82,
        };
        db.JobApplications.Add(application);
        await db.SaveChangesAsync();

        return (recruiterAUser, recruiterBUser, candidateProfile, application);
    }

    [Fact]
    public async Task SearchAsync_OwningCompanyRecruiter_SeesApplicant()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiterA, _, candidate, _) = await SeedAsync(db);
        var sut = CreateSut(db);

        var results = await sut.SearchAsync(recruiterA.Id, new CandidateSearchQuery());

        Assert.Single(results);
        Assert.Equal(candidate.Id, results[0].CandidateProfileId);
    }

    [Fact]
    public async Task SearchAsync_DifferentCompanyRecruiter_SeesNoApplicants()
    {
        using var db = TestDbContextFactory.Create();
        var (_, recruiterB, _, _) = await SeedAsync(db);
        var sut = CreateSut(db);

        var results = await sut.SearchAsync(recruiterB.Id, new CandidateSearchQuery());

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetDetailAsync_DifferentCompanyRecruiter_ThrowsNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var (_, recruiterB, candidate, _) = await SeedAsync(db);
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetDetailAsync(recruiterB.Id, candidate.Id));
    }

    [Fact]
    public async Task DownloadApplicantResumeAsync_DifferentCompanyRecruiter_ThrowsForbidden()
    {
        using var db = TestDbContextFactory.Create();
        var (_, recruiterB, _, application) = await SeedAsync(db);
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.DownloadApplicantResumeAsync(recruiterB.Id, application.Id));
    }

    [Fact]
    public async Task ExportCsvAsync_ContainsOnlyNonSensitiveHeadersAndOwnCompanyData()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiterA, _, _, _) = await SeedAsync(db);
        var sut = CreateSut(db);

        var csv = await sut.ExportCsvAsync(recruiterA.Id, new CandidateSearchQuery());

        Assert.StartsWith("Name,Headline,Skills,City,State,ExperienceYears,Education,ApplicationStatus,AppliedDate,MatchScore,JobTitle", csv);
        Assert.Contains("Casey Candidate", csv);
        Assert.DoesNotContain("PasswordHash", csv, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", csv, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ResumeStorageKey", csv, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExportCsvAsync_DifferentCompanyRecruiter_ProducesEmptyExport()
    {
        using var db = TestDbContextFactory.Create();
        var (_, recruiterB, _, _) = await SeedAsync(db);
        var sut = CreateSut(db);

        var csv = await sut.ExportCsvAsync(recruiterB.Id, new CandidateSearchQuery());
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        Assert.Single(lines); // header row only, no data rows
    }

    [Fact]
    public async Task SearchAsync_NotOnboardedRecruiter_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var user = new User { FullName = "No Company Recruiter", Email = "nocompany@example.com", Role = UserRole.Recruiter };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<ConflictException>(() => sut.SearchAsync(user.Id, new CandidateSearchQuery()));
    }

    [Fact]
    public async Task SearchAsync_FiltersByCity()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiterA, _, _, _) = await SeedAsync(db);
        var sut = CreateSut(db);

        var matching = await sut.SearchAsync(recruiterA.Id, new CandidateSearchQuery(City: "Bengaluru"));
        var nonMatching = await sut.SearchAsync(recruiterA.Id, new CandidateSearchQuery(City: "Mumbai"));

        Assert.Single(matching);
        Assert.Empty(nonMatching);
    }

    [Fact]
    public async Task SearchAsync_FiltersBySkillSubstringMatch()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiterA, _, _, _) = await SeedAsync(db);
        var sut = CreateSut(db);

        var matching = await sut.SearchAsync(recruiterA.Id, new CandidateSearchQuery(Skills: "SQL Server"));
        var nonMatching = await sut.SearchAsync(recruiterA.Id, new CandidateSearchQuery(Skills: "Python"));

        Assert.Single(matching);
        Assert.Empty(nonMatching);
    }

    [Fact]
    public async Task GetDiscoverableCandidatesAsync_OnlyReturnsVisibleToRecruiters()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiterA, _, _, _) = await SeedAsync(db);

        var discoverableUser = new User { FullName = "Dana Discoverable", Email = "dana@example.com", Role = UserRole.Candidate };
        var privateUser = new User { FullName = "Priya Private", Email = "priya@example.com", Role = UserRole.Candidate };
        var afterApplyingUser = new User { FullName = "Alex AfterApplying", Email = "alex@example.com", Role = UserRole.Candidate };
        db.Users.AddRange(discoverableUser, privateUser, afterApplyingUser);
        await db.SaveChangesAsync();

        db.CandidateProfiles.AddRange(
            new CandidateProfile { UserId = discoverableUser.Id, ProfileVisibility = ProfileVisibility.VisibleToRecruiters },
            new CandidateProfile { UserId = privateUser.Id, ProfileVisibility = ProfileVisibility.Private },
            new CandidateProfile { UserId = afterApplyingUser.Id, ProfileVisibility = ProfileVisibility.VisibleAfterApplying });
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var results = await sut.GetDiscoverableCandidatesAsync(new DiscoverCandidatesQuery(null, null, null, null, null));

        Assert.Single(results);
        Assert.Equal("Dana Discoverable", results[0].FullName);
    }

    [Fact]
    public async Task GetDiscoverableCandidatesAsync_IncludesPublicShareableProfiles()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiterA, _, _, _) = await SeedAsync(db);

        var publicUser = new User { FullName = "Priya PublicShareable", Email = "priya-public@example.com", Role = UserRole.Candidate };
        db.Users.Add(publicUser);
        await db.SaveChangesAsync();
        db.CandidateProfiles.Add(new CandidateProfile { UserId = publicUser.Id, ProfileVisibility = ProfileVisibility.PublicShareable });
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var results = await sut.GetDiscoverableCandidatesAsync(new DiscoverCandidatesQuery(null, null, null, null, null));

        Assert.Contains(results, r => r.FullName == "Priya PublicShareable");
    }

    [Fact]
    public async Task GetDiscoverableCandidatesAsync_FiltersByAvailabilityStatus()
    {
        using var db = TestDbContextFactory.Create();
        var activeUser = new User { FullName = "Active Ana", Email = "ana@example.com", Role = UserRole.Candidate };
        var openUser = new User { FullName = "Open Omar", Email = "omar@example.com", Role = UserRole.Candidate };
        db.Users.AddRange(activeUser, openUser);
        await db.SaveChangesAsync();

        db.CandidateProfiles.AddRange(
            new CandidateProfile { UserId = activeUser.Id, ProfileVisibility = ProfileVisibility.VisibleToRecruiters, AvailabilityStatus = AvailabilityStatus.ActivelyLooking },
            new CandidateProfile { UserId = openUser.Id, ProfileVisibility = ProfileVisibility.VisibleToRecruiters, AvailabilityStatus = AvailabilityStatus.OpenToOpportunities });
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var results = await sut.GetDiscoverableCandidatesAsync(new DiscoverCandidatesQuery(null, null, null, null, AvailabilityStatus.ActivelyLooking));

        Assert.Single(results);
        Assert.Equal("Active Ana", results[0].FullName);
    }

    [Fact]
    public async Task GetDetailAsync_IncludesResumeBuilderSectionsForApplicant()
    {
        using var db = TestDbContextFactory.Create();
        var (recruiterA, _, candidate, _) = await SeedAsync(db);
        db.CandidateWorkExperiences.Add(new CandidateWorkExperience { CandidateProfileId = candidate.Id, Title = "Engineer", Company = "Acme", StartDate = DateTime.UtcNow.AddYears(-2) });
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var detail = await sut.GetDetailAsync(recruiterA.Id, candidate.Id);

        Assert.Single(detail.WorkExperiences!);
    }

    [Fact]
    public async Task GetDetailAsync_DifferentCompanyRecruiter_CannotSeeResumeSections()
    {
        using var db = TestDbContextFactory.Create();
        var (_, recruiterB, candidate, _) = await SeedAsync(db);
        db.CandidateWorkExperiences.Add(new CandidateWorkExperience { CandidateProfileId = candidate.Id, Title = "Engineer", Company = "Acme", StartDate = DateTime.UtcNow.AddYears(-2) });
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetDetailAsync(recruiterB.Id, candidate.Id));
    }

    [Fact]
    public async Task GetDiscoverableCandidatesAsync_PrivateCandidate_NeverAppears()
    {
        using var db = TestDbContextFactory.Create();
        var privateUser = new User { FullName = "Private Pat", Email = "pat@example.com", Role = UserRole.Candidate };
        db.Users.Add(privateUser);
        await db.SaveChangesAsync();
        db.CandidateProfiles.Add(new CandidateProfile { UserId = privateUser.Id, ProfileVisibility = ProfileVisibility.Private });
        await db.SaveChangesAsync();
        var sut = CreateSut(db);

        var results = await sut.GetDiscoverableCandidatesAsync(new DiscoverCandidatesQuery(null, null, null, null, null));

        Assert.Empty(results);
    }
}
