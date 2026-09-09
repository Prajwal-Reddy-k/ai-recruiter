using AIRecruiter.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AIRecruiter.Infrastructure.Services;

/// <summary>Same key format ("{jobId}:{visitorKey}") and same 30-minute window as
/// InMemoryViewDeduplicationService, so switching providers is behavior-preserving. Uses
/// Redis's atomic SET-if-not-exists as the distributed equivalent of the in-memory
/// check-and-set. A Redis outage fails open — "should count" — exactly like a never-seen key,
/// rather than throwing and taking down the request.</summary>
public class RedisViewDeduplicationService : IViewDeduplicationService
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(30);

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisViewDeduplicationService> _logger;

    public RedisViewDeduplicationService(IConnectionMultiplexer redis, ILogger<RedisViewDeduplicationService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public bool ShouldCountView(string visitorKey, int jobId)
    {
        var key = $"viewdedup:{jobId}:{visitorKey}";

        try
        {
            return _redis.GetDatabase().StringSet(key, "1", Window, When.NotExists);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for view-dedup check; failing open (counting this view).");
            return true;
        }
    }
}
