using AIRecruiter.Application.Validation;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;

namespace AIRecruiter.UnitTests.Validation;

public class JobAlertMatcherTests
{
    private static JobPosting Job(string title = "Backend Engineer", string? description = "role", string? skills = "C#,SQL", string? city = "Bengaluru", string? state = "Karnataka", bool isRemote = false, JobType jobType = JobType.FullTime, int? minExp = 2, decimal? minSalary = null, decimal? maxSalary = null) =>
        new() { Title = title, Description = description ?? "", RequiredSkillsCsv = skills, City = city, State = state, IsRemote = isRemote, JobType = jobType, MinExperienceYears = minExp, MinSalary = minSalary, MaxSalary = maxSalary };

    private static JobAlert Alert() => new();

    [Fact]
    public void Matches_EmptyAlert_MatchesAnyJob()
    {
        Assert.True(JobAlertMatcher.Matches(Alert(), Job()));
    }

    [Fact]
    public void Matches_CityMismatch_ReturnsFalse()
    {
        var alert = Alert();
        alert.City = "Mumbai";
        Assert.False(JobAlertMatcher.Matches(alert, Job(city: "Bengaluru")));
    }

    [Fact]
    public void Matches_RemoteOnlyAlert_ExcludesOnSiteJob()
    {
        var alert = Alert();
        alert.IsRemote = true;
        Assert.False(JobAlertMatcher.Matches(alert, Job(isRemote: false)));
    }

    [Fact]
    public void Matches_KeywordInTitle_ReturnsTrue()
    {
        var alert = Alert();
        alert.Keyword = "backend";
        Assert.True(JobAlertMatcher.Matches(alert, Job(title: "Senior Backend Engineer")));
    }

    [Fact]
    public void Matches_KeywordNotPresent_ReturnsFalse()
    {
        var alert = Alert();
        alert.Keyword = "frontend";
        Assert.False(JobAlertMatcher.Matches(alert, Job(title: "Senior Backend Engineer", description: "APIs and databases")));
    }

    [Fact]
    public void Matches_SalaryRangesOverlap_ReturnsTrue()
    {
        var alert = Alert();
        alert.MinSalary = 1000000;
        alert.MaxSalary = 2000000;
        Assert.True(JobAlertMatcher.Matches(alert, Job(minSalary: 1500000, maxSalary: 2500000)));
    }

    [Fact]
    public void Matches_SalaryRangesDoNotOverlap_ReturnsFalse()
    {
        var alert = Alert();
        alert.MinSalary = 3000000;
        alert.MaxSalary = 4000000;
        Assert.False(JobAlertMatcher.Matches(alert, Job(minSalary: 800000, maxSalary: 1200000)));
    }

    [Fact]
    public void Matches_SalaryOnlyOnOneSide_IsNotEnforced()
    {
        // Salary comparison is only applied when both sides disclose a range.
        var alert = Alert();
        alert.MinSalary = 3000000;
        Assert.True(JobAlertMatcher.Matches(alert, Job(minSalary: null, maxSalary: null)));
    }

    [Fact]
    public void Matches_SkillsCsvSubstring_ReturnsTrue()
    {
        var alert = Alert();
        alert.SkillsCsv = "React";
        Assert.True(JobAlertMatcher.Matches(alert, Job(skills: "C#,React,SQL")));
    }
}
