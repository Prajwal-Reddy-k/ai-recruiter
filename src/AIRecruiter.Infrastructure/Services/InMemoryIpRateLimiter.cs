using System.Collections.Concurrent;
using AIRecruiter.Application.Interfaces;

namespace AIRecruiter.Infrastructure.Services;

/// <summary>Process-lifetime, in-memory sliding-window counter — no database table, no
/// distributed cache dependency. Adequate for a single-instance portfolio deployment; a
/// multi-instance production deployment would swap this for a Redis-backed implementation
/// behind the same interface.</summary>
public class InMemoryIpRateLimiter : IIpRateLimiter
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> _hits = new();
    private int _checkCount;

    public bool IsAllowed(string key, int maxRequests, TimeSpan window)
    {
        var now = DateTime.UtcNow;
        var queue = _hits.GetOrAdd(key, _ => new ConcurrentQueue<DateTime>());

        while (queue.TryPeek(out var oldest) && now - oldest > window)
        {
            queue.TryDequeue(out _);
        }

        if (queue.Count >= maxRequests)
        {
            return false;
        }

        queue.Enqueue(now);

        if (Interlocked.Increment(ref _checkCount) % 500 == 0)
        {
            Sweep(now, window);
        }

        return true;
    }

    private void Sweep(DateTime now, TimeSpan window)
    {
        foreach (var (key, queue) in _hits)
        {
            while (queue.TryPeek(out var oldest) && now - oldest > window)
            {
                queue.TryDequeue(out _);
            }
            if (queue.IsEmpty)
            {
                _hits.TryRemove(key, out _);
            }
        }
    }
}
