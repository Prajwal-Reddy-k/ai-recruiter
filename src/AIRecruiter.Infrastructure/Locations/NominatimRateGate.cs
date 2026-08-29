namespace AIRecruiter.Infrastructure.Locations;

/// <summary>
/// Process-wide gate enforcing Nominatim's usage-policy limit of at most one request per
/// second, regardless of how many concurrent callers there are. Registered as a singleton.
/// </summary>
public class NominatimRateGate
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private DateTime _lastRequestUtc = DateTime.MinValue;
    private static readonly TimeSpan MinInterval = TimeSpan.FromMilliseconds(1000);

    public async Task WaitAsync(CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            var elapsed = DateTime.UtcNow - _lastRequestUtc;
            if (elapsed < MinInterval)
            {
                await Task.Delay(MinInterval - elapsed, ct);
            }
            _lastRequestUtc = DateTime.UtcNow;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
