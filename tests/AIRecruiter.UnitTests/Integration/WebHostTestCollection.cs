namespace AIRecruiter.UnitTests.Integration;

/// <summary>Every test class that hosts the real API via WebApplicationFactory shares this
/// collection, which xUnit runs sequentially (never in parallel with other tests in the same
/// collection) — these tests all write to the same Serilog rolling file
/// (Logs/log-.txt) under the shared process working directory, and running them concurrently
/// made RequestLoggingTests' before/after file-content check flaky. Tests outside this
/// collection are unaffected and still run in parallel as usual.</summary>
[CollectionDefinition("WebHost", DisableParallelization = true)]
public class WebHostTestCollection
{
}
