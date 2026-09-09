using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AIRecruiter.Infrastructure.Options;

/// <summary>Requires a non-empty DefaultConnection only in Production, and only when the app
/// isn't running against the in-memory provider (the existing UseInMemoryDatabase switch that
/// tests/CI already rely on — see DependencyInjection.cs — bypasses SQL Server entirely, so a
/// missing connection string there is expected, not a misconfiguration).</summary>
public class ConnectionStringsOptionsValidator : IValidateOptions<ConnectionStringsOptions>
{
    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public ConnectionStringsOptionsValidator(IHostEnvironment environment, IConfiguration configuration)
    {
        _environment = environment;
        _configuration = configuration;
    }

    public ValidateOptionsResult Validate(string? name, ConnectionStringsOptions options)
    {
        if (!_environment.IsProduction())
        {
            return ValidateOptionsResult.Success;
        }

        if (_configuration.GetValue<bool>("UseInMemoryDatabase"))
        {
            return ValidateOptionsResult.Success;
        }

        return string.IsNullOrWhiteSpace(options.DefaultConnection)
            ? ValidateOptionsResult.Fail("ConnectionStrings:DefaultConnection is required in Production.")
            : ValidateOptionsResult.Success;
    }
}
