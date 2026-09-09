using AIRecruiter.Application.Interfaces;
using AIRecruiter.Infrastructure.Services;
using StackExchange.Redis;

namespace AIRecruiter.UnitTests.Services;

/// <summary>Runs the same behavioral contract against both view-dedup implementations, so
/// switching providers (in-memory vs. Redis) never changes observable app behavior. The Redis
/// case is skipped gracefully when no local Redis is reachable — most dev machines and CI don't
/// have one running, and the existing xUnit suite must stay green without requiring Docker.</summary>
public class ViewDeduplicationServiceContractTests
{
    [Fact]
    public void InMemory_FirstCall_ReturnsTrue()
    {
        var sut = new InMemoryViewDeduplicationService();
        Assert.True(sut.ShouldCountView("visitor-1", jobId: 1));
    }

    [Fact]
    public void InMemory_SecondCallWithinWindow_ReturnsFalse()
    {
        var sut = new InMemoryViewDeduplicationService();
        Assert.True(sut.ShouldCountView("visitor-1", jobId: 1));
        Assert.False(sut.ShouldCountView("visitor-1", jobId: 1));
    }

    [Fact]
    public void InMemory_DifferentJobOrVisitor_IsIndependent()
    {
        var sut = new InMemoryViewDeduplicationService();
        Assert.True(sut.ShouldCountView("visitor-1", jobId: 1));
        Assert.True(sut.ShouldCountView("visitor-2", jobId: 1));
        Assert.True(sut.ShouldCountView("visitor-1", jobId: 2));
    }

    [Fact]
    public void Redis_FirstAndSecondCall_MatchesInMemoryContract()
    {
        if (!TryConnectLocalRedis(out var redis))
        {
            return; // No local Redis reachable — skip gracefully rather than fail CI/dev runs.
        }

        using (redis)
        {
            var sut = new RedisViewDeduplicationService(redis!, Microsoft.Extensions.Logging.Abstractions.NullLogger<RedisViewDeduplicationService>.Instance);
            var jobId = new Random().Next(1_000_000, 2_000_000); // avoid collisions with prior runs
            var visitorKey = Guid.NewGuid().ToString("N");

            Assert.True(sut.ShouldCountView(visitorKey, jobId));
            Assert.False(sut.ShouldCountView(visitorKey, jobId));
        }
    }

    private static bool TryConnectLocalRedis(out IConnectionMultiplexer? redis)
    {
        try
        {
            var options = ConfigurationOptions.Parse("localhost:6379");
            options.ConnectTimeout = 300;
            options.AbortOnConnectFail = true;
            redis = ConnectionMultiplexer.Connect(options);
            return true;
        }
        catch
        {
            redis = null;
            return false;
        }
    }
}
