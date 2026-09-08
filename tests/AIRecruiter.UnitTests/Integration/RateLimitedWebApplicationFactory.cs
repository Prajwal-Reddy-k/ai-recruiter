using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AIRecruiter.UnitTests.Integration;

/// <summary>Hosts the real API in-process (real middleware pipeline, including the rate
/// limiter — this is the one thing that genuinely can't be verified via a mocked-service unit
/// test) against an EF Core InMemory database instead of the real SQL Server connection string,
/// so these tests need no external SQL Server instance. Switches providers via configuration
/// (see DependencyInjection.AddInfrastructure's "UseInMemoryDatabase" switch) rather than
/// post-hoc DI-descriptor surgery, since Program.cs's own AddDbContext call already runs by the
/// time a WebApplicationFactory ConfigureServices override would otherwise apply.</summary>
public class RateLimitedWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseInMemoryDatabase"] = "true",
                ["InMemoryDatabaseName"] = _dbName,
            });
        });
    }
}
