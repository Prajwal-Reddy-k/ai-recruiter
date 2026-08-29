using AIRecruiter.Application.DTOs.Matching;
using AIRecruiter.Application.Matching;

namespace AIRecruiter.UnitTests.Matching;

public class ResumeMatchingServiceTests
{
    private readonly ResumeMatchingService _sut = new();

    [Fact]
    public void CalculateMatch_ScoreIsAlwaysWithinZeroToOneHundred()
    {
        var job = new JobMatchInput("Backend Engineer", "We need C# and SQL Server experience.", "C#,SQL Server", 3);
        var candidate = new CandidateMatchInput(5, "Bachelor's degree");

        var result = _sut.CalculateMatch("I have 5 years of C# and SQL Server experience.", job, candidate);

        Assert.InRange(result.OverallScore, 0, 100);
    }

    [Fact]
    public void CalculateMatch_EmptyResumeText_ProducesLowButValidScore()
    {
        var job = new JobMatchInput("Backend Engineer", "We need C# and SQL Server experience.", "C#,SQL Server", 3);
        var candidate = new CandidateMatchInput(null, null);

        var result = _sut.CalculateMatch(string.Empty, job, candidate);

        Assert.InRange(result.OverallScore, 0, 100);
        Assert.Equal(2, result.MissingSkills.Count);
        Assert.Empty(result.MatchedSkills);
    }

    [Fact]
    public void CalculateMatch_FullSkillCoverage_AllSkillsMatched()
    {
        var job = new JobMatchInput("Backend Engineer", "Looking for a strong backend developer.", "C#,SQL Server,Docker", 2);
        var candidate = new CandidateMatchInput(4, null);
        var resumeText = "Experienced backend developer skilled in C#, SQL Server, and Docker for containerized deployments.";

        var result = _sut.CalculateMatch(resumeText, job, candidate);

        Assert.Equal(3, result.MatchedSkills.Count);
        Assert.Empty(result.MissingSkills);
        Assert.True(result.OverallScore > 50);
    }

    [Fact]
    public void CalculateMatch_DuplicateSkillsInJobList_AreDeduped()
    {
        var job = new JobMatchInput("Developer", "Role description.", "C#, C#, c#, SQL Server", 1);
        var candidate = new CandidateMatchInput(2, null);
        var resumeText = "Proficient in C# and SQL Server development.";

        var result = _sut.CalculateMatch(resumeText, job, candidate);

        Assert.Equal(2, result.MatchedSkills.Count);
    }

    [Fact]
    public void CalculateMatch_NoRequiredSkillsOnJob_FallsBackToDescriptionExtraction()
    {
        var job = new JobMatchInput("Developer", "We use Python and Django extensively.", null, null);
        var candidate = new CandidateMatchInput(null, null);
        var resumeText = "I have built several applications with Python and Django.";

        var result = _sut.CalculateMatch(resumeText, job, candidate);

        Assert.Contains("Python", result.MatchedSkills);
        Assert.Contains("Django", result.MatchedSkills);
    }

    [Fact]
    public void CalculateMatch_MissingExperienceData_DoesNotThrowAndScoresLowerOnExperience()
    {
        var jobWithReq = new JobMatchInput("Developer", "Role.", "C#", 5);
        var candidateNoExperience = new CandidateMatchInput(null, null);

        var withData = _sut.CalculateMatch("C# developer", jobWithReq, new CandidateMatchInput(5, null));
        var withoutData = _sut.CalculateMatch("C# developer", jobWithReq, candidateNoExperience);

        Assert.True(withData.OverallScore >= withoutData.OverallScore);
    }

    [Fact]
    public void CalculateMatch_Explanation_MentionsAllFourFactors()
    {
        var job = new JobMatchInput("Developer", "Bachelor degree required. C# development.", "C#", 2);
        var candidate = new CandidateMatchInput(3, "Bachelor of Science");

        var result = _sut.CalculateMatch("C# developer with a bachelor degree.", job, candidate);

        Assert.Contains("Skill match", result.Explanation);
        Assert.Contains("Text similarity", result.Explanation);
        Assert.Contains("Experience", result.Explanation);
        Assert.Contains("Education", result.Explanation);
    }

    [Fact]
    public void CalculateMatch_SuggestsMissingSkillsAsImprovements()
    {
        var job = new JobMatchInput("Developer", "Role.", "C#,Kubernetes", 0);
        var candidate = new CandidateMatchInput(1, null);

        var result = _sut.CalculateMatch("I know C# well.", job, candidate);

        Assert.Contains(result.SuggestedImprovements, s => s.Contains("Kubernetes"));
    }
}
