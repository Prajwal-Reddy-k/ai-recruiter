using AIRecruiter.Application.Interfaces;
using AIRecruiter.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AIRecruiter.UnitTests.Integration;

/// <summary>Confirms DependencyInjection.AddInfrastructure picks the distributed (Redis)
/// view-dedup implementation only when Redis:ConnectionString is present, and the existing
/// in-memory implementation otherwise — without requiring a reachable Redis server just to
/// verify which type got registered.</summary>
[Collection("WebHost")]
public class RedisProviderSelectionTests
{
    [Fact]
    public void RedisNotConfigured_ResolvesInMemoryViewDeduplicationService()
    {
        using var factory = new ConfigurableWebApplicationFactory("Development", new Dictionary<string, string?>
        {
            ["UseInMemoryDatabase"] = "true",
            ["InMemoryDatabaseName"] = Guid.NewGuid().ToString(),
        });

        using var scope = factory.Services.CreateScope();
        Assert.IsType<InMemoryViewDeduplicationService>(scope.ServiceProvider.GetRequiredService<IViewDeduplicationService>());
    }

    [Fact]
    public void RedisConfigured_ResolvesRedisViewDeduplicationService()
    {
        // An unreachable host is fine here — this only verifies which type DI selects at
        // registration time, not that a connection can actually be established.
        using var factory = new ConfigurableWebApplicationFactory("Development", new Dictionary<string, string?>
        {
            ["UseInMemoryDatabase"] = "true",
            ["InMemoryDatabaseName"] = Guid.NewGuid().ToString(),
            ["Redis:ConnectionString"] = "localhost:6399,abortConnect=false,connectTimeout=200",
        });

        using var scope = factory.Services.CreateScope();
        var redisOptions = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AIRecruiter.Infrastructure.Options.RedisOptions>>().Value;
        var resolved = scope.ServiceProvider.GetRequiredService<IViewDeduplicationService>();
        Assert.True(resolved is RedisViewDeduplicationService, $"ConnectionString='{redisOptions.ConnectionString}' IsConfigured={redisOptions.IsConfigured} ResolvedType={resolved.GetType().Name}");
    }
}
