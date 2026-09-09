using AIRecruiter.Application.Interfaces;
using AIRecruiter.Infrastructure.Email;
using AIRecruiter.Infrastructure.ExternalJobs;
using AIRecruiter.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace AIRecruiter.UnitTests.Integration;

/// <summary>Exercises the real ValidateOnStart() pipeline wired in Program.cs — these must boot
/// the real host (not a mocked service) since that's the only way to prove startup actually
/// fails fast, with a specific message, rather than failing later on first use.</summary>
[Collection("WebHost")]
public class ConfigurationValidationTests
{
    [Fact]
    public void Production_WithShortJwtSecret_ThrowsSpecificErrorOnStartup()
    {
        var factory = new ConfigurableWebApplicationFactory("Production", new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "too-short",
            ["Jwt:Issuer"] = "AIRecruiter.API",
            ["Jwt:Audience"] = "AIRecruiter.Client",
            ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=AIRecruiterDb;Trusted_Connection=True;TrustServerCertificate=True;",
        });

        var ex = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("Jwt:Secret", ex.ToString());

        factory.Dispose();
    }

    [Fact]
    public void Production_WithMissingConnectionString_ThrowsSpecificErrorOnStartup()
    {
        var factory = new ConfigurableWebApplicationFactory("Production", new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "REPLACE_WITH_A_LONG_RANDOM_SECRET_KEY_AT_LEAST_32_CHARS",
            ["Jwt:Issuer"] = "AIRecruiter.API",
            ["Jwt:Audience"] = "AIRecruiter.Client",
            ["ConnectionStrings:DefaultConnection"] = "",
            ["UseInMemoryDatabase"] = "false",
        });

        var ex = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("ConnectionStrings:DefaultConnection", ex.ToString());

        factory.Dispose();
    }

    [Fact]
    public void Production_WithValidRequiredConfig_StartsCleanly()
    {
        using var factory = new ConfigurableWebApplicationFactory("Production", new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "REPLACE_WITH_A_LONG_RANDOM_SECRET_KEY_AT_LEAST_32_CHARS",
            ["Jwt:Issuer"] = "AIRecruiter.API",
            ["Jwt:Audience"] = "AIRecruiter.Client",
            ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=AIRecruiterDb;Trusted_Connection=True;TrustServerCertificate=True;",
        });

        using var client = factory.CreateClient();
        Assert.NotNull(client);
    }

    [Fact]
    public void Development_WithNoOptionalIntegrationsConfigured_StartsCleanlyAndFallsBackToDevImplementations()
    {
        using var factory = new ConfigurableWebApplicationFactory("Development", new Dictionary<string, string?>
        {
            ["UseInMemoryDatabase"] = "true",
            ["InMemoryDatabaseName"] = Guid.NewGuid().ToString(),
            ["Smtp:Host"] = "",
            ["Smtp:Username"] = "",
            ["Smtp:Password"] = "",
            ["Smtp:SenderEmail"] = "",
            ["Cloudinary:CloudName"] = "",
            ["Cloudinary:ApiKey"] = "",
            ["Cloudinary:ApiSecret"] = "",
            ["Adzuna:AppId"] = "",
            ["Adzuna:AppKey"] = "",
        });

        using var scope = factory.Services.CreateScope();
        Assert.IsType<DevEmailSender>(scope.ServiceProvider.GetRequiredService<IEmailSender>());
        Assert.IsType<LocalResumeStorage>(scope.ServiceProvider.GetRequiredService<IResumeStorage>());
        Assert.IsType<DisabledExternalJobSearchService>(scope.ServiceProvider.GetRequiredService<IExternalJobSearchService>());
    }
}
