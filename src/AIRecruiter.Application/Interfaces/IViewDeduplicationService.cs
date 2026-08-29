namespace AIRecruiter.Application.Interfaces;

/// <summary>Prevents a single visitor from inflating a job's view count by refreshing the
/// page repeatedly. No personal data is persisted — callers pass an opaque, already-hashed
/// key and this only ever keeps that key (never raw IP/user-agent/user id) in memory.</summary>
public interface IViewDeduplicationService
{
    /// <summary>Returns true (and records the visit) if this key hasn't been seen for the
    /// given job within the dedup window; returns false without recording anything otherwise.</summary>
    bool ShouldCountView(string visitorKey, int jobId);
}
