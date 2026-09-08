using AIRecruiter.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace AIRecruiter.API.Controllers;

/// <summary>Test-only reset endpoint for the Playwright E2E suite — gives every spec a known,
/// deterministic seeded state to start from. Strictly gated behind the "Testing" ASP.NET Core
/// environment (never Development or Production) so it is never reachable in a real deployment,
/// and requires no authentication check beyond that gate since it exists purely to reset a
/// disposable CI/local test database.</summary>
[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public TestController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpPost("reset-seed")]
    public async Task<IActionResult> ResetSeed(CancellationToken ct)
    {
        if (!_env.IsEnvironment("Testing"))
        {
            return NotFound();
        }

        await DataSeeder.ResetForTestingAsync(_db, ct);
        return NoContent();
    }
}
