using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AIRecruiter.UnitTests.Integration;

// Each test gets its own factory/server/database — the rate-limiter policies partition by IP
// (loopback, for an in-process TestServer) with a real time window, so sharing one server
// across tests would let an earlier test's requests exhaust a later test's budget.
[Collection("WebHost")]
public class RateLimitingTests
{
    [Fact]
    public async Task ExceedingLoginLimit_Returns429WithRetryAfter()
    {
        using var factory = new RateLimitedWebApplicationFactory();
        var client = factory.CreateClient();
        HttpResponseMessage? last = null;

        // The "auth" policy permits 10/60s per IP (appsettings.json) — the 11th request in the
        // same window must be rejected before the login logic even runs.
        for (var i = 0; i < 11; i++)
        {
            last = await client.PostAsJsonAsync("/api/auth/login", new { email = "nobody@example.com", password = "wrong" });
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
        Assert.True(last.Headers.Contains("Retry-After"), "Expected a Retry-After header on the 429 response.");

        var body = await last.Content.ReadAsStringAsync();
        Assert.Contains("RATE_LIMITED", body);
    }

    [Fact]
    public async Task NormalTrafficUnderLimit_IsUnaffected()
    {
        using var factory = new RateLimitedWebApplicationFactory();
        var client = factory.CreateClient();

        for (var i = 0; i < 3; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "nobody@example.com", password = "wrong" });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [Fact]
    public async Task GeneralApiPolicy_DoesNotBlockNormalPublicJobBrowsing()
    {
        using var factory = new RateLimitedWebApplicationFactory();
        var client = factory.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            var response = await client.GetAsync("/api/jobs");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task ExportEndpoint_ExceedingLimit_Returns429()
    {
        using var factory = new RateLimitedWebApplicationFactory();
        var client = factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Rita RateLimitTest",
            email = $"ratelimit-recruiter-{Guid.NewGuid():N}@example.com",
            password = "Passw0rd!",
            role = 2, // Recruiter
        });
        registerResponse.EnsureSuccessStatusCode();
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponseShape>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.Token);

        HttpResponseMessage? last = null;
        // The "export" policy permits 10/60s — this recruiter has no company on file, so every
        // call fails with 409 (NOT_ONBOARDED) until the policy itself starts rejecting at 429.
        for (var i = 0; i < 11; i++)
        {
            last = await client.GetAsync("/api/recruiters/reports/export/jobs");
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
    }

    private record AuthResponseShape(string Token);
}
