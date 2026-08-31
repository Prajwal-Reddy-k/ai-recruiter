using AIRecruiter.Infrastructure.Services;

namespace AIRecruiter.UnitTests.Services;

public class InMemoryIpRateLimiterTests
{
    [Fact]
    public void IsAllowed_UnderLimit_ReturnsTrue()
    {
        var sut = new InMemoryIpRateLimiter();

        for (var i = 0; i < 5; i++)
        {
            Assert.True(sut.IsAllowed("key-a", maxRequests: 5, TimeSpan.FromMinutes(1)));
        }
    }

    [Fact]
    public void IsAllowed_OverLimit_ReturnsFalse()
    {
        var sut = new InMemoryIpRateLimiter();

        for (var i = 0; i < 3; i++)
        {
            sut.IsAllowed("key-b", maxRequests: 3, TimeSpan.FromMinutes(1));
        }

        Assert.False(sut.IsAllowed("key-b", maxRequests: 3, TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void IsAllowed_DifferentKeys_AreIndependent()
    {
        var sut = new InMemoryIpRateLimiter();

        for (var i = 0; i < 3; i++)
        {
            sut.IsAllowed("key-c", maxRequests: 3, TimeSpan.FromMinutes(1));
        }

        Assert.True(sut.IsAllowed("key-d", maxRequests: 3, TimeSpan.FromMinutes(1)));
    }
}
