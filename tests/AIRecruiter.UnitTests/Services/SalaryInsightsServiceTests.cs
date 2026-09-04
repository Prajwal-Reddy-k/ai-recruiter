using AIRecruiter.Application.DTOs.SalaryInsights;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.UnitTests.TestHelpers;

namespace AIRecruiter.UnitTests.Services;

public class SalaryInsightsServiceTests
{
    private static async Task<(Company Company, RecruiterProfile RecruiterProfile)> SeedCompanyAsync(AppDbContext db)
    {
        var recruiterUser = new User { FullName = "Rita Recruiter", Email = $"rita{Guid.NewGuid():N}@example.com", Role = UserRole.Recruiter };
        db.Users.Add(recruiterUser);
        await db.SaveChangesAsync();
        var company = new Company { Name = "Acme Corp" };
        var recruiterProfile = new RecruiterProfile { UserId = recruiterUser.Id, Company = company };
        db.RecruiterProfiles.Add(recruiterProfile);
        await db.SaveChangesAsync();
        return (company, recruiterProfile);
    }

    private static JobPosting MakeJob(Company company, RecruiterProfile recruiterProfile, string title, decimal minSalary, decimal maxSalary, int minExp = 3) =>
        new()
        {
            Title = title,
            Description = "role",
            CompanyId = company.Id,
            RecruiterProfileId = recruiterProfile.Id,
            Status = JobStatus.Open,
            City = "Bengaluru",
            State = "Karnataka",
            MinSalary = minSalary,
            MaxSalary = maxSalary,
            MinExperienceYears = minExp,
        };

    [Fact]
    public async Task GetInsightsAsync_BelowSampleThreshold_ReturnsNoNumbers()
    {
        using var db = TestDbContextFactory.Create();
        var (company, recruiterProfile) = await SeedCompanyAsync(db);
        db.JobPostings.Add(MakeJob(company, recruiterProfile, "Rare Role", 1000000, 1200000));
        await db.SaveChangesAsync();
        var sut = TestServiceFactory.CreateSalaryInsightsService(db);

        var insights = await sut.GetInsightsAsync(new SalaryInsightsQuery("Rare Role", null, null, null, null));

        var bucket = Assert.Single(insights);
        Assert.False(bucket.HasEnoughData);
        Assert.Null(bucket.Min);
        Assert.Null(bucket.Median);
        Assert.Null(bucket.Max);
        Assert.Equal(1, bucket.SampleCount);
    }

    [Fact]
    public async Task GetInsightsAsync_AtOrAboveSampleThreshold_ReturnsRealNumbers()
    {
        using var db = TestDbContextFactory.Create();
        var (company, recruiterProfile) = await SeedCompanyAsync(db);
        for (var i = 0; i < 5; i++)
        {
            db.JobPostings.Add(MakeJob(company, recruiterProfile, "Common Role", 1000000 + i * 100000, 1400000 + i * 100000));
        }
        await db.SaveChangesAsync();
        var sut = TestServiceFactory.CreateSalaryInsightsService(db);

        var insights = await sut.GetInsightsAsync(new SalaryInsightsQuery("Common Role", null, null, null, null));

        var bucket = Assert.Single(insights);
        Assert.True(bucket.HasEnoughData);
        Assert.Equal(5, bucket.SampleCount);
        Assert.NotNull(bucket.Min);
        Assert.NotNull(bucket.Median);
        Assert.NotNull(bucket.Max);
    }

    [Fact]
    public async Task GetGuidanceForJobAsync_BelowThreshold_ReturnsNull()
    {
        using var db = TestDbContextFactory.Create();
        var (company, recruiterProfile) = await SeedCompanyAsync(db);
        db.JobPostings.Add(MakeJob(company, recruiterProfile, "Rare Role", 1000000, 1200000));
        await db.SaveChangesAsync();
        var sut = TestServiceFactory.CreateSalaryInsightsService(db);

        var guidance = await sut.GetGuidanceForJobAsync("Rare Role", "Bengaluru", "Karnataka", false, 3, 200000, 300000);

        Assert.Null(guidance);
    }

    [Fact]
    public async Task GetGuidanceForJobAsync_ProposedRangeNotablyBelowMedian_ReturnsAdvisory()
    {
        using var db = TestDbContextFactory.Create();
        var (company, recruiterProfile) = await SeedCompanyAsync(db);
        for (var i = 0; i < 5; i++)
        {
            db.JobPostings.Add(MakeJob(company, recruiterProfile, "Common Role", 1800000, 2200000));
        }
        await db.SaveChangesAsync();
        var sut = TestServiceFactory.CreateSalaryInsightsService(db);

        var guidance = await sut.GetGuidanceForJobAsync("Common Role", "Bengaluru", "Karnataka", false, 3, 500000, 700000);

        Assert.NotNull(guidance);
    }

    [Fact]
    public async Task GetGuidanceForJobAsync_ProposedRangeWithinNormalRange_ReturnsNull()
    {
        using var db = TestDbContextFactory.Create();
        var (company, recruiterProfile) = await SeedCompanyAsync(db);
        for (var i = 0; i < 5; i++)
        {
            db.JobPostings.Add(MakeJob(company, recruiterProfile, "Common Role", 1800000, 2200000));
        }
        await db.SaveChangesAsync();
        var sut = TestServiceFactory.CreateSalaryInsightsService(db);

        var guidance = await sut.GetGuidanceForJobAsync("Common Role", "Bengaluru", "Karnataka", false, 3, 1900000, 2100000);

        Assert.Null(guidance);
    }
}
