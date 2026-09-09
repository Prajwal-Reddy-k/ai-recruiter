namespace AIRecruiter.UnitTests.Integration;

/// <summary>Confirms UseSerilogRequestLogging (wired in Program.cs) actually emits one
/// structured log line per request with the fields the observability feature promises —
/// verified against the real rolling-file sink and a real in-process request, not a mocked
/// logger. The file sink is shared with every other WebApplicationFactory-hosted test running
/// concurrently in the same process (a per-test log-file override was attempted and dropped —
/// Serilog's own UseSerilog(context.Configuration) callback does not reliably observe
/// WebApplicationFactory config overrides the way IOptions<T>/eager IConfiguration reads
/// elsewhere in this codebase do), so this snapshots each candidate file's length before the
/// request and only inspects the newly-appended suffix, with a generous retry budget for
/// contention under full-suite parallel test execution.</summary>
[Collection("WebHost")]
public class RequestLoggingTests
{
    [Fact]
    public async Task SampleRequest_EmitsStructuredLogLineWithRequestPathStatusCodeAndElapsed()
    {
        using var factory = new ConfigurableWebApplicationFactory("Development", new Dictionary<string, string?>
        {
            ["UseInMemoryDatabase"] = "true",
            ["InMemoryDatabaseName"] = Guid.NewGuid().ToString(),
        });

        var logsDir = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
        var beforeLengths = Directory.Exists(logsDir)
            ? Directory.GetFiles(logsDir, "log-*.txt").ToDictionary(f => f, f => new FileInfo(f).Length)
            : new Dictionary<string, long>();

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/health");
        Assert.True(response.IsSuccessStatusCode);

        string? newContent = null;
        for (var i = 0; i < 100 && newContent is null; i++)
        {
            if (Directory.Exists(logsDir))
            {
                foreach (var logFile in Directory.GetFiles(logsDir, "log-*.txt"))
                {
                    try
                    {
                        using var stream = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        using var reader = new StreamReader(stream);
                        var text = await reader.ReadToEndAsync();
                        var startOffset = beforeLengths.TryGetValue(logFile, out var len) ? (int)Math.Min(len, text.Length) : 0;
                        var appended = text[startOffset..];
                        if (appended.Contains("/health") && appended.Contains("responded"))
                        {
                            newContent = appended;
                            break;
                        }
                    }
                    catch (IOException)
                    {
                        // File still being written by another handle — retry.
                    }
                }
            }

            if (newContent is null)
            {
                await Task.Delay(150);
            }
        }

        Assert.NotNull(newContent);
        Assert.Contains("/health", newContent);
        Assert.Contains("responded", newContent);
    }
}
