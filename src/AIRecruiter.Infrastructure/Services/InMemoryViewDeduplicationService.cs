using System.Collections.Concurrent;
using AIRecruiter.Application.Interfaces;

namespace AIRecruiter.Infrastructure.Services;

/// <summary>Process-lifetime, in-memory sliding window — no database table, no personal
/// data at rest. Adequate for a single-instance portfolio deployment; a multi-instance
/// production deployment would swap this for a distributed cache behind the same interface.</summary>
public class InMemoryViewDeduplicationService : IViewDeduplicationService
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(30);
    private readonly ConcurrentDictionary<string, DateTime> _seen = new();
    private int _checkCount;

    public bool ShouldCountView(string visitorKey, int jobId)
    {
        var key = $"{jobId}:{visitorKey}";
        var now = DateTime.UtcNow;

        if (_seen.TryGetValue(key, out var lastSeen) && now - lastSeen < Window)
        {
            return false;
        }

        _seen[key] = now;

        if (Interlocked.Increment(ref _checkCount) % 500 == 0)
        {
            Sweep(now);
        }

        return true;
    }

    private void Sweep(DateTime now)
    {
        foreach (var (key, seenAt) in _seen)
        {
            if (now - seenAt >= Window)
            {
                _seen.TryRemove(key, out _);
            }
        }
    }
}
