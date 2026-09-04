using AIRecruiter.Application.Validation;

namespace AIRecruiter.UnitTests.Validation;

public class JobQualityScorerTests
{
    private static JobQualityInput AllFalse() => new(false, false, false, false, false, false, false, false);
    private static JobQualityInput AllTrue() => new(true, true, true, true, true, true, true, true);

    [Fact]
    public void Calculate_NothingFilled_ScoreIsZero()
    {
        var result = JobQualityScorer.Calculate(AllFalse());

        Assert.Equal(0, result.Score);
        Assert.Equal(8, result.Suggestions.Count);
    }

    [Fact]
    public void Calculate_EverythingFilled_ScoreIsHundred()
    {
        var result = JobQualityScorer.Calculate(AllTrue());

        Assert.Equal(100, result.Score);
        Assert.Empty(result.Suggestions);
    }

    [Fact]
    public void Calculate_PartiallyFilled_ScoreReflectsFraction()
    {
        var input = AllFalse() with { HasClearTitle = true, HasCompleteDescription = true, HasEnoughRequiredSkills = true, HasApplicationDeadline = true };

        var result = JobQualityScorer.Calculate(input);

        // 4 of 8 checks filled
        Assert.Equal((int)System.Math.Round(4 / 8.0 * 100), result.Score);
        Assert.Equal(4, result.Suggestions.Count);
    }

    [Fact]
    public void Calculate_MissingSalaryInfo_NeverBlocksScoreFromReachingHigh()
    {
        // Salary info is optional per spec — its absence should lower, not zero out, the score.
        var input = AllTrue() with { HasSalaryInfo = false };

        var result = JobQualityScorer.Calculate(input);

        Assert.True(result.Score >= 87);
        Assert.Single(result.Suggestions);
    }

    [Fact]
    public void Calculate_EachMissingCheck_ProducesALabelAndTip()
    {
        var result = JobQualityScorer.Calculate(AllFalse());

        Assert.All(result.Suggestions, s =>
        {
            Assert.False(string.IsNullOrWhiteSpace(s.Label));
            Assert.False(string.IsNullOrWhiteSpace(s.Tip));
        });
    }
}
