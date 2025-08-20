using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


namespace NeoServiceLayer.Services.Authentication.Services
{
    /// <summary>
    /// Distributed rate limiting service using Redis
    /// </summary>
    public class RateLimitingService : IRateLimitingService
    {
        private readonly ILogger<RateLimitingService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;

        // Configuration
        private readonly int _defaultRequestsPerMinute;
        private readonly int _defaultRequestsPerHour;
        private readonly int _defaultRequestsPerDay;
        private readonly int _burstSize;
        private readonly bool _enableDistributed;

        public RateLimitingService(
            ILogger<RateLimitingService> logger,
            IConfiguration configuration,
            IConnectionMultiplexer redis)
        {
            _logger = logger;
            _configuration = configuration;
            _redis = redis;
            _db = redis.GetDatabase();

            // Load configuration
            _defaultRequestsPerMinute = configuration.GetValue<int>("RateLimiting:RequestsPerMinute", 60);
            _defaultRequestsPerHour = configuration.GetValue<int>("RateLimiting:RequestsPerHour", 1000);
            _defaultRequestsPerDay = configuration.GetValue<int>("RateLimiting:RequestsPerDay", 10000);
            _burstSize = configuration.GetValue<int>("RateLimiting:BurstSize", 10);
            _enableDistributed = configuration.GetValue<bool>("RateLimiting:EnableDistributed", true);
        }

        /// <summary>
        /// Check if a request is allowed using sliding window algorithm
        /// </summary>
        public async Task<RateLimitResult> CheckRateLimitAsync(
            string key,
            RateLimitPolicy policy = null)
        {
            try
            {
                policy ??= GetDefaultPolicy();

                var result = new RateLimitResult();
                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                // Check multiple time windows
                var minuteCheck = await CheckTimeWindowAsync(
                    $"{key}:minute",
                    now,
                    60000, // 1 minute in ms
                    policy.RequestsPerMinute);

                if (!minuteCheck.IsAllowed)
                {
                    result.IsAllowed = false;
                    result.RetryAfter = minuteCheck.RetryAfter;
                    result.Limit = policy.RequestsPerMinute;
                    result.Remaining = 0;
                    result.ResetsAt = DateTime.UtcNow.AddMilliseconds(minuteCheck.RetryAfter);
                    return result;
                }

                // Check hourly limit if minute limit passed
                if (policy.RequestsPerHour > 0)
                {
                    var hourCheck = await CheckTimeWindowAsync(
                        $"{key}:hour",
                        now,
                        3600000, // 1 hour in ms
                        policy.RequestsPerHour);

                    if (!hourCheck.IsAllowed)
                    {
                        result.IsAllowed = false;
                        result.RetryAfter = hourCheck.RetryAfter;
                        result.Limit = policy.RequestsPerHour;
                        result.Remaining = 0;
                        result.ResetsAt = DateTime.UtcNow.AddMilliseconds(hourCheck.RetryAfter);
                        return result;
                    }
                }

                // Check daily limit if hour limit passed
                if (policy.RequestsPerDay > 0)
                {
                    var dayCheck = await CheckTimeWindowAsync(
                        $"{key}:day",
                        now,
                        86400000, // 1 day in ms
                        policy.RequestsPerDay);

                    if (!dayCheck.IsAllowed)
                    {
                        result.IsAllowed = false;
                        result.RetryAfter = dayCheck.RetryAfter;
                        result.Limit = policy.RequestsPerDay;
                        result.Remaining = 0;
                        result.ResetsAt = DateTime.UtcNow.AddMilliseconds(dayCheck.RetryAfter);
                        return result;
                    }
                }

                // All checks passed
                result.IsAllowed = true;
                result.Limit = policy.RequestsPerMinute;
                result.Remaining = minuteCheck.Remaining;
                result.ResetsAt = DateTime.UtcNow.AddSeconds(60);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking rate limit for key {Key}", key);

                // On error, allow the request but log the issue
                return new RateLimitResult
                {
                    IsAllowed = true,
                    Limit = policy?.RequestsPerMinute ?? _defaultRequestsPerMinute,
                    Remaining = -1,
                    ResetsAt = DateTime.UtcNow.AddMinutes(1)
                };
            }
        }

        /// <summary>
        /// Check rate limit using token bucket algorithm
        /// </summary>
        public async Task<RateLimitResult> CheckTokenBucketAsync(
            string key,
            int capacity,
            int refillRate,
            int tokensRequested = 1)
        {
            try
            {
                var bucketKey = $"bucket:{key}";
                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                // Lua script for atomic token bucket operation
                var script = @"
                    local key = KEYS[1]
                    local capacity = tonumber(ARGV[1])
                    local refill_rate = tonumber(ARGV[2])
                    local now = tonumber(ARGV[3])
                    local tokens_requested = tonumber(ARGV[4])

                    local bucket = redis.call('HMGET', key, 'tokens', 'last_refill')
                    local tokens = tonumber(bucket[1]) or capacity
                    local last_refill = tonumber(bucket[2]) or now

                    -- Calculate tokens to add based on time passed
                    local time_passed = math.max(0, now - last_refill)
                    local tokens_to_add = math.floor(time_passed * refill_rate / 1000)
                    tokens = math.min(capacity, tokens + tokens_to_add)

                    if tokens >= tokens_requested then
                        tokens = tokens - tokens_requested
                        redis.call('HMSET', key, 'tokens', tokens, 'last_refill', now)
                        redis.call('EXPIRE', key, 3600)
                        return {1, tokens, capacity}
                    else
                        redis.call('HMSET', key, 'tokens', tokens, 'last_refill', now)
                        redis.call('EXPIRE', key, 3600)
                        local wait_time = math.ceil((tokens_requested - tokens) * 1000 / refill_rate)
                        return {0, tokens, wait_time}
                    end
                ";

                var result = await _db.ScriptEvaluateAsync(
                    script,
                    new RedisKey[] { bucketKey },
                    new RedisValue[] { capacity, refillRate, now, tokensRequested });

                var resultArray = (RedisValue[])result;
                var allowed = (int)resultArray[0] == 1;
                var remainingTokens = (int)resultArray[1];
                var waitTime = allowed ? 0 : (int)resultArray[2];

                return new RateLimitResult
                {
                    IsAllowed = allowed,
                    Limit = capacity,
                    Remaining = remainingTokens,
                    RetryAfter = waitTime,
                    ResetsAt = DateTime.UtcNow.AddMilliseconds(waitTime)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking token bucket for key {Key}", key);

                return new RateLimitResult
                {
                    IsAllowed = true,
                    Limit = capacity,
                    Remaining = -1,
                    ResetsAt = DateTime.UtcNow.AddMinutes(1)
                };
            }
        }

        /// <summary>
        /// Check rate limit for specific time window
        /// </summary>
        private async Task<WindowCheckResult> CheckTimeWindowAsync(
            string key,
            long now,
            long windowSize,
            int limit)
        {
            // Lua script for sliding window rate limiting
            var script = @"
                local key = KEYS[1]
                local now = tonumber(ARGV[1])
                local window = tonumber(ARGV[2])
                local limit = tonumber(ARGV[3])

                local clearBefore = now - window

                -- Remove old entries
                redis.call('ZREMRANGEBYSCORE', key, 0, clearBefore)

                -- Count requests in current window
                local current = redis.call('ZCARD', key)

                if current < limit then
                    -- Add current request
                    redis.call('ZADD', key, now, now)
                    redis.call('EXPIRE', key, window / 1000 + 1)
                    return {1, limit - current - 1, 0}
                else
                    -- Get oldest request in window
                    local oldest = redis.call('ZRANGE', key, 0, 0, 'WITHSCORES')
                    if oldest[2] then
                        local oldestTime = tonumber(oldest[2])
                        local retryAfter = oldestTime + window - now
                        return {0, 0, retryAfter}
                    else
                        return {0, 0, window}
                    end
                end
            ";

            var result = await _db.ScriptEvaluateAsync(
                script,
                new RedisKey[] { key },
                new RedisValue[] { now, windowSize, limit });

            var resultArray = (RedisValue[])result;

            return new WindowCheckResult
            {
                IsAllowed = (int)resultArray[0] == 1,
                Remaining = (int)resultArray[1],
                RetryAfter = (long)resultArray[2]
            };
        }

        /// <summary>
        /// Apply IP-based rate limiting
        /// </summary>
        public async Task<RateLimitResult> CheckIpRateLimitAsync(
            string ipAddress,
            string action = "global")
        {
            var key = $"ip:{ipAddress}:{action}";
            var policy = GetIpPolicy(action);
            return await CheckRateLimitAsync(key, policy);
        }

        /// <summary>
        /// Apply user-based rate limiting
        /// </summary>
        public async Task<RateLimitResult> CheckUserRateLimitAsync(
            Guid userId,
            string action = "global")
        {
            var key = $"user:{userId}:{action}";
            var policy = GetUserPolicy(action);
            return await CheckRateLimitAsync(key, policy);
        }

        /// <summary>
        /// Apply combined IP and user rate limiting
        /// </summary>
        public async Task<RateLimitResult> CheckCombinedRateLimitAsync(
            string ipAddress,
            Guid? userId,
            string action = "global")
        {
            // Check IP limit first
            var ipResult = await CheckIpRateLimitAsync(ipAddress, action);
            if (!ipResult.IsAllowed)
            {
                return ipResult;
            }

            // If user is authenticated, check user limit
            if (userId.HasValue)
            {
                var userResult = await CheckUserRateLimitAsync(userId.Value, action);
                if (!userResult.IsAllowed)
                {
                    return userResult;
                }
            }

            return ipResult;
        }

        /// <summary>
        /// Reset rate limit for a key
        /// </summary>
        public async Task ResetRateLimitAsync(string key)
        {
            try
            {
                var keys = new[]
                {
                    $"{key}:minute",
                    $"{key}:hour",
                    $"{key}:day",
                    $"bucket:{key}"
                };

                foreach (var k in keys)
                {
                    await _db.KeyDeleteAsync(k);
                }

                _logger.LogInformation("Rate limit reset for key {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting rate limit for key {Key}", key);
            }
        }

        /// <summary>
        /// Block a key for specified duration
        /// </summary>
        public async Task BlockKeyAsync(string key, TimeSpan duration, string reason)
        {
            try
            {
                var blockKey = $"blocked:{key}";
                var blockInfo = new
                {
                    Reason = reason,
                    BlockedAt = DateTime.UtcNow,
                    BlockedUntil = DateTime.UtcNow.Add(duration)
                };

                await _db.StringSetAsync(
                    blockKey,
                    JsonSerializer.Serialize(blockInfo),
                    duration);

                _logger.LogWarning("Key {Key} blocked for {Duration}: {Reason}",
                    key, duration, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error blocking key {Key}", key);
            }
        }

        /// <summary>
        /// Check if a key is blocked
        /// </summary>
        public async Task<bool> IsBlockedAsync(string key)
        {
            try
            {
                var blockKey = $"blocked:{key}";
                return await _db.KeyExistsAsync(blockKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if key {Key} is blocked", key);
                return false;
            }
        }

        /// <summary>
        /// Get rate limit statistics
        /// </summary>
        public async Task<RateLimitStatistics> GetStatisticsAsync(string key)
        {
            try
            {
                var stats = new RateLimitStatistics
                {
                    Key = key,
                    Timestamp = DateTime.UtcNow
                };

                // Get request counts for different windows
                var minuteKey = $"{key}:minute";
                var hourKey = $"{key}:hour";
                var dayKey = $"{key}:day";

                stats.RequestsLastMinute = (int)await _db.SortedSetLengthAsync(minuteKey);
                stats.RequestsLastHour = (int)await _db.SortedSetLengthAsync(hourKey);
                stats.RequestsLastDay = (int)await _db.SortedSetLengthAsync(dayKey);

                // Check if blocked
                stats.IsBlocked = await IsBlockedAsync(key);

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statistics for key {Key}", key);
                return new RateLimitStatistics { Key = key };
            }
        }

        private RateLimitPolicy GetDefaultPolicy()
        {
            return new RateLimitPolicy
            {
                RequestsPerMinute = _defaultRequestsPerMinute,
                RequestsPerHour = _defaultRequestsPerHour,
                RequestsPerDay = _defaultRequestsPerDay,
                BurstSize = _burstSize
            };
        }

        private RateLimitPolicy GetIpPolicy(string action)
        {
            var section = _configuration.GetSection($"RateLimiting:IpPolicies:{action}");
            if (!section.Exists())
            {
                section = _configuration.GetSection("RateLimiting:IpPolicies:default");
            }

            return new RateLimitPolicy
            {
                RequestsPerMinute = section.GetValue<int>("RequestsPerMinute", 30),
                RequestsPerHour = section.GetValue<int>("RequestsPerHour", 500),
                RequestsPerDay = section.GetValue<int>("RequestsPerDay", 5000),
                BurstSize = section.GetValue<int>("BurstSize", 5)
            };
        }

        private RateLimitPolicy GetUserPolicy(string action)
        {
            var section = _configuration.GetSection($"RateLimiting:UserPolicies:{action}");
            if (!section.Exists())
            {
                section = _configuration.GetSection("RateLimiting:UserPolicies:default");
            }

            return new RateLimitPolicy
            {
                RequestsPerMinute = section.GetValue<int>("RequestsPerMinute", 60),
                RequestsPerHour = section.GetValue<int>("RequestsPerHour", 1000),
                RequestsPerDay = section.GetValue<int>("RequestsPerDay", 10000),
                BurstSize = section.GetValue<int>("BurstSize", 10)
            };
        }

        private class WindowCheckResult
        {
            public bool IsAllowed { get; set; }
            public int Remaining { get; set; }
            public long RetryAfter { get; set; }
        }
    }

    /// <summary>
    /// Rate limiting service interface
    /// </summary>
    public interface IRateLimitingService
    {
        Task<RateLimitResult> CheckRateLimitAsync(string key, RateLimitPolicy policy = null);
        Task<RateLimitResult> CheckTokenBucketAsync(string key, int capacity, int refillRate, int tokensRequested = 1);
        Task<RateLimitResult> CheckIpRateLimitAsync(string ipAddress, string action = "global");
        Task<RateLimitResult> CheckUserRateLimitAsync(Guid userId, string action = "global");
        Task<RateLimitResult> CheckCombinedRateLimitAsync(string ipAddress, Guid? userId, string action = "global");
        Task ResetRateLimitAsync(string key);
        Task BlockKeyAsync(string key, TimeSpan duration, string reason);
        Task<bool> IsBlockedAsync(string key);
        Task<RateLimitStatistics> GetStatisticsAsync(string key);
    }

    /// <summary>
    /// Rate limit check result
    /// </summary>
    public class RateLimitResult
    {
        public bool IsAllowed { get; set; }
        public int Limit { get; set; }
        public int Remaining { get; set; }
        public DateTime ResetsAt { get; set; }
        public long RetryAfter { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// Rate limit policy configuration
    /// </summary>
    public class RateLimitPolicy
    {
        public int RequestsPerMinute { get; set; }
        public int RequestsPerHour { get; set; }
        public int RequestsPerDay { get; set; }
        public int BurstSize { get; set; }
        public bool AllowBurst { get; set; } = true;
        public string PolicyName { get; set; }
    }

    /// <summary>
    /// Rate limit statistics
    /// </summary>
    public class RateLimitStatistics
    {
        public string Key { get; set; }
        public int RequestsLastMinute { get; set; }
        public int RequestsLastHour { get; set; }
        public int RequestsLastDay { get; set; }
        public bool IsBlocked { get; set; }
        public DateTime Timestamp { get; set; }
    }
}