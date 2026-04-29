using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Simcag.NotificationService.Application.Abstractions;
using StackExchange.Redis;

namespace Simcag.NotificationService.Infrastructure.Redis;

public sealed class RedisNotificationSendOptions
{
    public int DedupTtlHours { get; set; } = 24;
    public int MaxSendsPerUserPerHour { get; set; } = 30;
    public string KeyPrefix { get; set; } = "notif";
}

public sealed class RedisNotificationSendPolicy : INotificationSendPolicy
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly RedisNotificationSendOptions _opt;
    private readonly ILogger<RedisNotificationSendPolicy> _logger;

    public RedisNotificationSendPolicy(
        IOptions<RedisNotificationSendOptions> options,
        IConnectionMultiplexer? redis,
        ILogger<RedisNotificationSendPolicy> logger)
    {
        _opt = options.Value;
        _redis = redis;
        _logger = logger;
    }

    public async Task<bool> TryAcquireAsync(
        string deduplicationKey,
        string rateLimitKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rateLimitKey))
        {
            _logger.LogWarning("Rate limit key is empty; allowing send (no throttling)");
            return true;
        }

        if (_redis == null || !_redis.IsConnected)
        {
            _logger.LogWarning("Redis not available; allowing send (no deduplication or rate limit)");
            return true;
        }

        var db = _redis.GetDatabase();
        var prefix = string.IsNullOrWhiteSpace(_opt.KeyPrefix) ? "notif" : _opt.KeyPrefix;
        var rateRedisKey = $"{prefix}:rl:{SanitizeKey(rateLimitKey)}";
        var dedupRedisKey = string.IsNullOrWhiteSpace(deduplicationKey)
            ? null
            : $"{prefix}:dedup:{SanitizeKey(deduplicationKey)}";

        if (dedupRedisKey is not null && !await SetDedupIfAbsentAsync(db, dedupRedisKey, cancellationToken))
        {
            _logger.LogDebug("Deduplication hit for {Key}", deduplicationKey);
            return false;
        }

        if (!await IsWithinRateLimitAsync(db, rateRedisKey, cancellationToken))
        {
            _logger.LogInformation("Rate limit hit for {Key}", rateLimitKey);
            if (dedupRedisKey is not null)
            {
                try { await db.KeyDeleteAsync(dedupRedisKey); } catch { /* ignore */ }
            }
            return false;
        }

        return true;
    }

    private static string SanitizeKey(string k)
    {
        var s = k.Trim();
        return s.Length > 200 ? s[..200] : s;
    }

    private async Task<bool> SetDedupIfAbsentAsync(IDatabase db, string key, CancellationToken ct)
    {
        var ttl = TimeSpan.FromHours(Math.Max(1, _opt.DedupTtlHours));
        try
        {
            return await db.StringSetAsync(key, "1", ttl, when: When.NotExists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis deduplication check failed; allowing send");
            return true;
        }
    }

    private async Task<bool> IsWithinRateLimitAsync(IDatabase db, string key, CancellationToken ct)
    {
        var max = Math.Max(1, _opt.MaxSendsPerUserPerHour);
        try
        {
            var n = await db.StringIncrementAsync(key);
            if (n == 1)
                await db.KeyExpireAsync(key, TimeSpan.FromHours(1));
            return n <= max;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis rate limit check failed; allowing send");
            return true;
        }
    }
}

public sealed class NullNotificationSendPolicy : INotificationSendPolicy
{
    public Task<bool> TryAcquireAsync(string deduplicationKey, string rateLimitKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}
