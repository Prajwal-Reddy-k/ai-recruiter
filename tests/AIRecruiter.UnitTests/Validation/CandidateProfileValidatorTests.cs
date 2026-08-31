using AIRecruiter.Application.DTOs.Candidates;
using AIRecruiter.Application.Validation;

namespace AIRecruiter.UnitTests.Validation;

public class CandidateProfileValidatorTests
{
    private readonly CandidateProfileValidator _sut = new();

    private static UpsertCandidateProfileRequest ValidRequest() => new(
        Headline: "Senior Backend Engineer",
        Summary: "Experienced engineer.",
        Education: "B.Tech Computer Science",
        GraduationYear: 2015,
        ExperienceSummary: "8 years building APIs.",
        TotalExperienceYears: 8,
        City: "Bengaluru",
        State: "Karnataka",
        Locality: null,
        CurrentSalary: null,
        ExpectedSalary: null,
        SkillsCsv: "C#, SQL Server",
        Phone: "9876543210",
        LinkedInUrl: "https://linkedin.com/in/someone",
        GithubUrl: "https://github.com/someone",
        PortfolioUrl: "https://someone.dev");

    [Fact]
    public void Validate_ValidRequest_Succeeds()
    {
        var result = _sut.Validate(ValidRequest());
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal("9876543210", result.NormalizedPhone);
    }

    [Fact]
    public void Validate_MissingHeadline_Fails()
    {
        var result = _sut.Validate(ValidRequest() with { Headline = "" });
        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("headline"));
    }

    [Fact]
    public void Validate_TooShortHeadline_Fails()
    {
        var result = _sut.Validate(ValidRequest() with { Headline = "Dev" });
        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("headline"));
    }

    [Fact]
    public void Validate_NoSkills_Fails()
    {
        var result = _sut.Validate(ValidRequest() with { SkillsCsv = "" });
        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("skills"));
    }

    [Fact]
    public void Validate_DuplicateSkills_Fails()
    {
        var result = _sut.Validate(ValidRequest() with { SkillsCsv = "C#, c#, SQL" });
        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("skills"));
    }

    [Fact]
    public void Validate_TooManySkills_Fails()
    {
        var skills = string.Join(", ", Enumerable.Range(0, 31).Select(i => $"Skill{i}"));
        var result = _sut.Validate(ValidRequest() with { SkillsCsv = skills });
        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("skills"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(61)]
    public void Validate_ExperienceYearsOutOfRange_Fails(int years)
    {
        var result = _sut.Validate(ValidRequest() with { TotalExperienceYears = years });
        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("totalExperienceYears"));
    }

    [Fact]
    public void Validate_GraduationYearTooOld_Fails()
    {
        var result = _sut.Validate(ValidRequest() with { GraduationYear = 1900 });
        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("graduationYear"));
    }

    [Fact]
    public void Validate_GraduationYearFarFuture_Fails()
    {
        var result = _sut.Validate(ValidRequest() with { GraduationYear = DateTime.UtcNow.Year + 10 });
        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("graduationYear"));
    }

    [Fact]
    public void Validate_GraduationYearWithoutEducation_Fails()
    {
        var result = _sut.Validate(ValidRequest() with { Education = "", GraduationYear = 2015 });
        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("education"));
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("5876543210")]
    [InlineData("not-a-phone")]
    public void Validate_InvalidPhone_Fails(string phone)
    {
        var result = _sut.Validate(ValidRequest() with { Phone = phone });
        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("phone"));
    }

    [Theory]
    [InlineData("+91 98765 43210")]
    [InlineData("09876543210")]
    [InlineData("919876543210")]
    public void Validate_PhoneWithCountryCodeOrTrunkPrefix_NormalizesAndSucceeds(string phone)
    {
        var result = _sut.Validate(ValidRequest() with { Phone = phone });
        Assert.True(result.IsValid);
        Assert.Equal("9876543210", result.NormalizedPhone);
    }

    [Fact]
    public void Validate_LinkedInUrlWrongDomain_Fails()
    {
        var result = _sut.Validate(ValidRequest() with { LinkedInUrl = "https://example.com/in/someone" });
        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("linkedInUrl"));
    }

    [Fact]
    public void Validate_GithubUrlWrongDomain_Fails()
    {
        var result = _sut.Validate(ValidRequest() with { GithubUrl = "https://notgithub.com/someone" });
        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("githubUrl"));
    }

    [Fact]
    public void Validate_PortfolioUrlNotAbsolute_Fails()
    {
        var result = _sut.Validate(ValidRequest() with { PortfolioUrl = "not-a-url" });
        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("portfolioUrl"));
    }

    [Fact]
    public void Validate_NegativeSalary_Fails()
    {
        var result = _sut.Validate(ValidRequest() with { CurrentSalary = -1 });
        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("currentSalary"));
    }

    [Theory]
    [InlineData("John Doe")]
    [InlineData("Priya S. Reddy")]
    [InlineData("O'Neil")]
    public void IsValidFullName_ValidNames_ReturnsTrue(string name)
    {
        Assert.True(CandidateProfileValidator.IsValidFullName(name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("John123")]
    public void IsValidFullName_InvalidNames_ReturnsFalse(string name)
    {
        Assert.False(CandidateProfileValidator.IsValidFullName(name));
    }
}
