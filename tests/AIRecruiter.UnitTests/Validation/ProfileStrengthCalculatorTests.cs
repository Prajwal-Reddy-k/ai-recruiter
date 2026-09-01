using AIRecruiter.Application.Validation;

namespace AIRecruiter.UnitTests.Validation;

public class ProfileStrengthCalculatorTests
{
    private static ProfileStrengthInput AllFalse() => new(false, false, false, false, false, false, false, false, false);
    private static ProfileStrengthInput AllTrue() => new(true, true, true, true, true, true, true, true, true);

    [Fact]
    public void Calculate_NothingFilled_ScoreIsZero()
    {
        var result = ProfileStrengthCalculator.Calculate(AllFalse());

        Assert.Equal(0, result.Score);
        Assert.Equal(9, result.MissingItems.Count);
    }

    [Fact]
    public void Calculate_EverythingFilled_ScoreIsHundred()
    {
        var result = ProfileStrengthCalculator.Calculate(AllTrue());

        Assert.Equal(100, result.Score);
        Assert.Empty(result.MissingItems);
    }

    [Fact]
    public void Calculate_MissingItems_IncludeLinkPathForNavigation()
    {
        var result = ProfileStrengthCalculator.Calculate(AllFalse());

        Assert.All(result.MissingItems, item => Assert.False(string.IsNullOrWhiteSpace(item.LinkPath)));
    }

    [Fact]
    public void Calculate_PartiallyFilled_ScoreReflectsFraction()
    {
        var input = AllFalse() with { HasHeadline = true, HasSummary = true, HasSkills = true };

        var result = ProfileStrengthCalculator.Calculate(input);

        // 3 of 9 checks filled
        Assert.Equal((int)System.Math.Round(3 / 9.0 * 100), result.Score);
        Assert.Equal(6, result.MissingItems.Count);
    }
}
