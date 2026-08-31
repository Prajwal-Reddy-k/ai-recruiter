namespace AIRecruiter.Application.Interfaces;

/// <summary>Simple sliding-window request counter, keyed by an arbitrary string (typically
/// "<action>:<ip>" or "<action>:<email>"). No personal data is required — callers choose
/// the key.</summary>
public interface IIpRateLimiter
{
    /// <summary>Returns true (and records the attempt) if fewer than <paramref name="maxRequests"/>
    /// have been recorded for this key within <paramref name="window"/>; returns false without
    /// recording anything otherwise.</summary>
    bool IsAllowed(string key, int maxRequests, TimeSpan window);
}
