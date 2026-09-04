using AIRecruiter.Application.Validation;

namespace AIRecruiter.UnitTests.Validation;

public class SalaryExperienceBandTests
{
    [Theory]
    [InlineData(null, SalaryExperienceBand.Mid)]
    [InlineData(0, SalaryExperienceBand.Junior)]
    [InlineData(2, SalaryExperienceBand.Junior)]
    [InlineData(3, SalaryExperienceBand.Mid)]
    [InlineData(5, SalaryExperienceBand.Mid)]
    [InlineData(6, SalaryExperienceBand.Senior)]
    [InlineData(10, SalaryExperienceBand.Senior)]
    [InlineData(11, SalaryExperienceBand.Lead)]
    public void From_ReturnsExpectedBand(int? years, string expected)
    {
        Assert.Equal(expected, SalaryExperienceBand.From(years));
    }
}
