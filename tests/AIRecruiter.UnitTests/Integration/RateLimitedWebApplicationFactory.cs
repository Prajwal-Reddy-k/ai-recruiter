using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AIRecruiter.UnitTests.Integration;

/// <summary>Hosts the real API in-process (real middleware pipeline, including the rate
/// limiter — this is the one thing that genuinely can't be verified via a mocked-service unit
/// test) against an EF Core InMemory database instead of the real SQL Server connection string,
/// so these tests need no external SQL Server instance. Switches providers via configuration
/// (see DependencyInjection.AddInfrastructure's "UseInMemoryDatabase" switch).
///
/// Uses IWebHostBuilder.UseSetting (not ConfigureAppConfiguration/AddInMemoryCollection) —
/// AddInfrastructure reads "UseInMemoryDatabase" synchronously from Program.cs's top-level
/// code, which runs *before* WebApplicationFactory's internal host actually calls Build().
/// ConfigureAppConfiguration-added sources only land in the final built IConfigurationRoot
/// (visible to later IOptions<T> resolution), not to that earlier eager read — UseSetting
/// writes directly into WebHostBuilder's settings dictionary, which pre-existing entry-point
/// code reads through the very same IConfiguration reference, so it's visible immediately.
/// (Verified the hard way: without this, these tests were silently writing real rows into the
/// shared local AIRecruiterDb via LocalDB — appsettings.Development.json's real connection
/// string — because the intended in-memory-database override never actually took effect.)</summary>
public class RateLimitedWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("UseInMemoryDatabase", "true");
        builder.UseSetting("InMemoryDatabaseName", _dbName);
    }
}
