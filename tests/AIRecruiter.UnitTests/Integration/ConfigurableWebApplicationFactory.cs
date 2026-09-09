using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AIRecruiter.UnitTests.Integration;

/// <summary>Like RateLimitedWebApplicationFactory, but lets each test pick its own
/// ASPNETCORE_ENVIRONMENT and override arbitrary configuration keys — used to exercise
/// startup-time options validation (JwtOptionsValidator/ConnectionStringsOptionsValidator)
/// under Production vs. Development, and with/without required settings present.
///
/// Uses IWebHostBuilder.UseSetting per key rather than ConfigureAppConfiguration +
/// AddInMemoryCollection — the latter's source only lands in the final built
/// IConfigurationRoot, invisible to any config read that happens synchronously in Program.cs's
/// top-level code *before* WebApplicationFactory's internal Build() call (e.g.
/// DependencyInjection.AddInfrastructure's eager reads). UseSetting writes into the same
/// settings dictionary that backs IConfiguration from the very start, so both eager
/// Program.cs-time reads and later IOptions<T>/ValidateOnStart reads see it consistently.</summary>
public class ConfigurableWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _environment;
    private readonly IReadOnlyDictionary<string, string?> _overrides;

    public ConfigurableWebApplicationFactory(string environment, IReadOnlyDictionary<string, string?> overrides)
    {
        _environment = environment;
        _overrides = overrides;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);
        foreach (var (key, value) in _overrides)
        {
            builder.UseSetting(key, value);
        }
    }
}
