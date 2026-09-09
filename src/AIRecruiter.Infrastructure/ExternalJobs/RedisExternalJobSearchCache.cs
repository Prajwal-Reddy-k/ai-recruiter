using System.Text.Json;
using AIRecruiter.Application.DTOs.ExternalJobs;
using AIRecruiter.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AIRecruiter.Infrastructure.ExternalJobs;

/// <summary>Same key format and TTL as MemoryExternalJobSearchCache — switching providers is
/// behavior-preserving. A Redis outage degrades to "treat as a cache miss, call Adzuna" rather
/// than throwing, so a dead cache never turns into a 500 for the caller.</summary>
public class RedisExternalJobSearchCache : IExternalJobSearchCache
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisExternalJobSearchCache> _logger;

    public RedisExternalJobSearchCache(IConnectionMultiplexer redis, ILogger<RedisExternalJobSearchCache> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public bool TryGet(string key, out ExternalJobSearchResult? result)
    {
        try
        {
            var value = _redis.GetDatabase().StringGet(RedisKey(key));
            if (value.HasValue)
            {
                result = JsonSerializer.Deserialize<ExternalJobSearchResult>((string)value!);
                return result is not null;
            }
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for external-job-search cache read; treating as a cache miss.");
        }

        result = null;
        return false;
    }

    public void Set(string key, ExternalJobSearchResult result, TimeSpan ttl)
    {
        try
        {
            _redis.GetDatabase().StringSet(RedisKey(key), JsonSerializer.Serialize(result), ttl);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for external-job-search cache write; result will simply not be cached.");
        }
    }

    private static string RedisKey(string key) => $"externaljobs:{key}";
}
