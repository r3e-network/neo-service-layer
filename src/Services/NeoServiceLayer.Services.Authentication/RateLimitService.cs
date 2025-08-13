using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Api.Middleware;

namespace NeoServiceLayer.Services.Authentication
{
    /// <summary>
    /// Distributed rate limiting service using sliding window algorithm
    /// </summary>
    public class RateLimitService : IRateLimitService
    {
        private readonly ILogger<RateLimitService> _logger;
        private readonly IDistributedCache _cache;
        private readonly IConfiguration _configuration;
        private readonly RateLimitConfiguration _defaultConfig;

        public RateLimitService(
            ILogger<RateLimitService> logger,
            IDistributedCache cache,
            IConfiguration configuration)
        {
            _logger = logger;
            _cache = cache;
            _configuration = configuration;
            
            _defaultConfig = new RateLimitConfiguration
            {
                RequestsPerMinute = configuration.GetValue<int>("RateLimit:DefaultRequestsPerMinute", 60),
                RequestsPerHour = configuration.GetValue<int>("RateLimit:DefaultRequestsPerHour", 1000),
                BurstSize = configuration.GetValue<int>("RateLimit:DefaultBurstSize", 10)
            };
        }

        public async Task<RateLimitResult> CheckRateLimitAsync(string identifier, string resource)
        {
            var config = GetRateLimitConfiguration(resource);
            var now = DateTimeOffset.UtcNow;
            
            // Check minute window
            var minuteKey = $"rate:{identifier}:{resource}:minute:{now.Minute}";
            var minuteCount = await GetCountAsync(minuteKey);
            
            if (minuteCount >= config.RequestsPerMinute)
            {
                var resetAt = now.AddMinutes(1).AddSeconds(-now.Second);
                return new RateLimitResult
                {
                    IsAllowed = false,
                    Limit = config.RequestsPerMinute,
                    Remaining = 0,
                    ResetAt = resetAt,
                    RetryAfter = (int)(resetAt - now).TotalSeconds
                };
            }
            
            // Check hour window
            var hourKey = $"rate:{identifier}:{resource}:hour:{now.Hour}";
            var hourCount = await GetCountAsync(hourKey);
            
            if (hourCount >= config.RequestsPerHour)
            {
                var resetAt = now.AddHours(1).AddMinutes(-now.Minute).AddSeconds(-now.Second);
                return new RateLimitResult
                {
                    IsAllowed = false,
                    Limit = config.RequestsPerHour,
                    Remaining = 0,
                    ResetAt = resetAt,
                    RetryAfter = (int)(resetAt - now).TotalSeconds
                };
            }
            
            // Check burst protection
            var burstKey = $"rate:{identifier}:{resource}:burst";
            var burstCount = await GetCountAsync(burstKey);
            
            if (burstCount >= config.BurstSize)
            {
                return new RateLimitResult
                {
                    IsAllowed = false,
                    Limit = config.BurstSize,
                    Remaining = 0,
                    ResetAt = now.AddSeconds(1),
                    RetryAfter = 1
                };
            }
            
            // Request is allowed
            await RecordRequestAsync(identifier, resource);
            
            return new RateLimitResult
            {
                IsAllowed = true,
                Limit = config.RequestsPerMinute,
                Remaining = Math.Max(0, config.RequestsPerMinute - minuteCount - 1),
                ResetAt = now.AddMinutes(1).AddSeconds(-now.Second),
                RetryAfter = 0
            };
        }

        public async Task RecordRequestAsync(string identifier, string resource)
        {
            var now = DateTimeOffset.UtcNow;
            var tasks = new[]
            {
                IncrementCountAsync($"rate:{identifier}:{resource}:minute:{now.Minute}", TimeSpan.FromMinutes(2)),
                IncrementCountAsync($"rate:{identifier}:{resource}:hour:{now.Hour}", TimeSpan.FromHours(2)),
                IncrementCountAsync($"rate:{identifier}:{resource}:burst", TimeSpan.FromSeconds(1))
            };
            
            await Task.WhenAll(tasks);
            
            _logger.LogDebug("Recorded request for {Identifier} on {Resource}", identifier, resource);
        }

        public async Task ResetLimitAsync(string identifier, string resource)
        {
            var now = DateTimeOffset.UtcNow;
            var keys = new[]
            {
                $"rate:{identifier}:{resource}:minute:{now.Minute}",
                $"rate:{identifier}:{resource}:hour:{now.Hour}",
                $"rate:{identifier}:{resource}:burst"
            };
            
            foreach (var key in keys)
            {
                await _cache.RemoveAsync(key);
            }
            
            _logger.LogInformation("Reset rate limit for {Identifier} on {Resource}", identifier, resource);
        }

        private async Task<int> GetCountAsync(string key)
        {
            var value = await _cache.GetStringAsync(key);
            return int.TryParse(value, out var count) ? count : 0;
        }

        private async Task IncrementCountAsync(string key, TimeSpan expiration)
        {
            try
            {
                var value = await _cache.GetStringAsync(key);
                var count = int.TryParse(value, out var current) ? current : 0;
                count++;
                
                await _cache.SetStringAsync(key, count.ToString(),
                    new DistributedCacheEntryOptions
                    {
                        SlidingExpiration = expiration
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to increment rate limit counter for {Key}", key);
            }
        }

        private RateLimitConfiguration GetRateLimitConfiguration(string resource)
        {
            // Different limits for different endpoints
            return resource switch
            {
                "/api/v1/authentication/login" => new RateLimitConfiguration
                {
                    RequestsPerMinute = 5,
                    RequestsPerHour = 20,
                    BurstSize = 3
                },
                "/api/v1/authentication/register" => new RateLimitConfiguration
                {
                    RequestsPerMinute = 2,
                    RequestsPerHour = 10,
                    BurstSize = 1
                },
                "/api/v1/authentication/password/reset" => new RateLimitConfiguration
                {
                    RequestsPerMinute = 2,
                    RequestsPerHour = 5,
                    BurstSize = 1
                },
                _ => _defaultConfig
            };
        }

        private class RateLimitConfiguration
        {
            public int RequestsPerMinute { get; set; }
            public int RequestsPerHour { get; set; }
            public int BurstSize { get; set; }
        }
    }
}