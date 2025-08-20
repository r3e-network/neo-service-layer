using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.ServiceFramework;
using System.Linq;


namespace NeoServiceLayer.Services.Authentication
{
    /// <summary>
    /// Health check for authentication service components
    /// </summary>
    public class AuthenticationHealthCheck : IHealthCheck
    {
        private readonly ILogger<AuthenticationHealthCheck> Logger;
        private readonly IDistributedCache _cache;
        private readonly IConfiguration _configuration;
        private readonly ITokenService _tokenService;
        private readonly IUserRepository _userRepository;
        private readonly IAuthenticationMetricsCollector _metricsCollector;

        public AuthenticationHealthCheck(
            ILogger<AuthenticationHealthCheck> logger,
            IDistributedCache cache,
            IConfiguration configuration,
            ITokenService tokenService,
            IUserRepository userRepository,
            IAuthenticationMetricsCollector metricsCollector)
        {
            Logger = logger;
            _cache = cache;
            _configuration = configuration;
            _tokenService = tokenService;
            _userRepository = userRepository;
            _metricsCollector = metricsCollector;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var data = new Dictionary<string, object>();
            var unhealthyReasons = new List<string>();
            var degradedReasons = new List<string>();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Check JWT configuration
                var jwtHealthy = CheckJwtConfiguration(data);
                if (!jwtHealthy)
                {
                    unhealthyReasons.Add("JWT configuration is invalid");
                }

                // Check cache connectivity
                var cacheHealthy = await CheckCacheAsync(data, cancellationToken);
                if (!cacheHealthy)
                {
                    degradedReasons.Add("Cache service is unavailable");
                }

                // Check token service
                var tokenServiceHealthy = await CheckTokenServiceAsync(data, cancellationToken);
                if (!tokenServiceHealthy)
                {
                    unhealthyReasons.Add("Token service is not operational");
                }

                // Check user repository
                var userRepoHealthy = await CheckUserRepositoryAsync(data, cancellationToken);
                if (!userRepoHealthy)
                {
                    degradedReasons.Add("User repository is not accessible");
                }

                // Check metrics
                var metricsHealthy = await CheckMetricsAsync(data, cancellationToken);
                if (!metricsHealthy)
                {
                    degradedReasons.Add("Metrics collection has issues");
                }

                // Check rate limiting
                var rateLimitHealthy = await CheckRateLimitingAsync(data, cancellationToken);
                if (!rateLimitHealthy)
                {
                    degradedReasons.Add("Rate limiting service has issues");
                }

                stopwatch.Stop();
                data["ResponseTime"] = $"{stopwatch.ElapsedMilliseconds}ms";
                data["Timestamp"] = DateTime.UtcNow;

                // Determine overall health status
                if (unhealthyReasons.Count > 0)
                {
                    var message = $"Authentication service is unhealthy: {string.Join("; ", unhealthyReasons)}";
                    Logger.LogError(message);
                    return HealthCheckResult.Unhealthy(message, null, data);
                }

                if (degradedReasons.Count > 0)
                {
                    var message = $"Authentication service is degraded: {string.Join("; ", degradedReasons)}";
                    Logger.LogWarning(message);
                    return HealthCheckResult.Degraded(message, null, data);
                }

                return HealthCheckResult.Healthy("Authentication service is healthy", data);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Health check failed with exception");
                stopwatch.Stop();
                data["ResponseTime"] = $"{stopwatch.ElapsedMilliseconds}ms";
                data["Error"] = ex.Message;

                return HealthCheckResult.Unhealthy(
                    "Authentication service health check failed",
                    ex,
                    data);
            }
        }

        private bool CheckJwtConfiguration(Dictionary<string, object> data)
        {
            try
            {
                var jwtSecret = _configuration["Authentication:JwtSecret"];
                var issuer = _configuration["Authentication:Issuer"];
                var audience = _configuration["Authentication:Audience"];

                var isValid = !string.IsNullOrEmpty(jwtSecret) &&
                             jwtSecret.Length >= 32 &&
                             !string.IsNullOrEmpty(issuer) &&
                             !string.IsNullOrEmpty(audience);

                data["JwtConfiguration"] = isValid ? "Valid" : "Invalid";
                data["JwtIssuer"] = issuer ?? "Not configured";
                data["JwtAudience"] = audience ?? "Not configured";
                data["JwtSecretConfigured"] = !string.IsNullOrEmpty(jwtSecret);

                return isValid;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to check JWT configuration");
                data["JwtConfiguration"] = "Error";
                return false;
            }
        }

        private async Task<bool> CheckCacheAsync(Dictionary<string, object> data, CancellationToken cancellationToken)
        {
            try
            {
                var testKey = $"health_check:{Guid.NewGuid()}";
                var testValue = DateTime.UtcNow.ToString();

                var stopwatch = Stopwatch.StartNew();

                // Test write
                await _cache.SetStringAsync(testKey, testValue,
                    new DistributedCacheEntryOptions
                    {
                        SlidingExpiration = TimeSpan.FromSeconds(10)
                    },
                    cancellationToken);

                // Test read
                var retrievedValue = await _cache.GetStringAsync(testKey, cancellationToken);

                // Test delete
                await _cache.RemoveAsync(testKey, cancellationToken);

                stopwatch.Stop();

                var isHealthy = retrievedValue == testValue;
                data["CacheStatus"] = isHealthy ? "Connected" : "Connection Failed";
                data["CacheResponseTime"] = $"{stopwatch.ElapsedMilliseconds}ms";

                return isHealthy;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Cache health check failed");
                data["CacheStatus"] = "Error";
                data["CacheError"] = ex.Message;
                return false;
            }
        }

        private async Task<bool> CheckTokenServiceAsync(Dictionary<string, object> data, CancellationToken cancellationToken)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();

                // Test token generation
                var tokenPair = await _tokenService.GenerateTokenPairAsync(
                    "health_check_user",
                    new[] { "test" },
                    new Dictionary<string, string> { ["test"] = "health_check" });

                // Test token validation
                var isValid = await _tokenService.ValidateTokenAsync(tokenPair.AccessToken);

                stopwatch.Stop();

                data["TokenServiceStatus"] = isValid ? "Operational" : "Failed";
                data["TokenGenerationTime"] = $"{stopwatch.ElapsedMilliseconds}ms";
                data["TokenValidation"] = isValid;

                return isValid;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Token service health check failed");
                data["TokenServiceStatus"] = "Error";
                data["TokenServiceError"] = ex.Message;
                return false;
            }
        }

        private async Task<bool> CheckUserRepositoryAsync(Dictionary<string, object> data, CancellationToken cancellationToken)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();

                // Test user retrieval (using a known test user)
                var user = await _userRepository.GetByIdAsync("admin-001");

                stopwatch.Stop();

                var isHealthy = user != null;
                data["UserRepositoryStatus"] = isHealthy ? "Connected" : "Connection Failed";
                data["UserRepositoryResponseTime"] = $"{stopwatch.ElapsedMilliseconds}ms";
                data["TestUserFound"] = isHealthy;

                return isHealthy;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "User repository health check failed");
                data["UserRepositoryStatus"] = "Error";
                data["UserRepositoryError"] = ex.Message;
                return false;
            }
        }

        private async Task<bool> CheckMetricsAsync(Dictionary<string, object> data, CancellationToken cancellationToken)
        {
            try
            {
                var metrics = await _metricsCollector.GetMetricsAsync();

                data["MetricsStatus"] = "Operational";
                data["TotalLogins"] = metrics.LoginMetrics.TotalAttempts;
                data["TotalTokensGenerated"] = metrics.TokenMetrics.AccessTokensGenerated;
                data["SecurityEvents"] = metrics.SecurityMetrics.SecurityEventsByType.Count;

                // Check for anomalies
                var failureRate = metrics.LoginMetrics.TotalAttempts > 0
                    ? (double)metrics.LoginMetrics.FailedLogins / metrics.LoginMetrics.TotalAttempts
                    : 0;

                if (failureRate > 0.5) // More than 50% failure rate
                {
                    data["LoginFailureRate"] = $"{failureRate:P}";
                    data["MetricsWarning"] = "High login failure rate detected";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Metrics health check failed");
                data["MetricsStatus"] = "Error";
                data["MetricsError"] = ex.Message;
                return false;
            }
        }

        private async Task<bool> CheckRateLimitingAsync(Dictionary<string, object> data, CancellationToken cancellationToken)
        {
            try
            {
                // Check if rate limiting configuration is present
                var rateLimitEnabled = _configuration.GetValue<bool>("RateLimit:Enabled", true);
                var defaultLimit = _configuration.GetValue<int>("RateLimit:DefaultRequestsPerMinute", 60);

                data["RateLimitingEnabled"] = rateLimitEnabled;
                data["DefaultRateLimit"] = $"{defaultLimit} requests/minute";

                if (!rateLimitEnabled)
                {
                    data["RateLimitingStatus"] = "Disabled";
                    return true; // Not unhealthy if disabled by configuration
                }

                // Test rate limit storage
                var testKey = $"rate_limit_health_check:{DateTime.UtcNow.Ticks}";
                await _cache.SetStringAsync(testKey, "1",
                    new DistributedCacheEntryOptions
                    {
                        SlidingExpiration = TimeSpan.FromSeconds(5)
                    },
                    cancellationToken);

                var value = await _cache.GetStringAsync(testKey, cancellationToken);
                await _cache.RemoveAsync(testKey, cancellationToken);

                var isHealthy = value == "1";
                data["RateLimitingStatus"] = isHealthy ? "Operational" : "Failed";

                return isHealthy;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Rate limiting health check failed");
                data["RateLimitingStatus"] = "Error";
                data["RateLimitingError"] = ex.Message;
                return false;
            }
        }
    }

    /// <summary>
    /// Detailed authentication health check for comprehensive monitoring
    /// </summary>
    public class DetailedAuthenticationHealthCheck : IHealthCheck
    {
        private readonly IEnumerable<IHealthCheck> _healthChecks;
        private readonly ILogger<DetailedAuthenticationHealthCheck> Logger;

        public DetailedAuthenticationHealthCheck(
            IEnumerable<IHealthCheck> healthChecks,
            ILogger<DetailedAuthenticationHealthCheck> logger)
        {
            _healthChecks = healthChecks;
            Logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var results = new Dictionary<string, HealthCheckResult>();
            var overallData = new Dictionary<string, object>();
            var worstStatus = HealthStatus.Healthy;

            foreach (var healthCheck in _healthChecks)
            {
                try
                {
                    var result = await healthCheck.CheckHealthAsync(context, cancellationToken);
                    var checkName = healthCheck.GetType().Name;
                    results[checkName] = result;

                    if (result.Status < worstStatus)
                    {
                        worstStatus = result.Status;
                    }

                    overallData[checkName] = new
                    {
                        Status = result.Status.ToString(),
                        Description = result.Description,
                        Data = result.Data
                    };
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Health check failed for {HealthCheck}", healthCheck.GetType().Name);
                    worstStatus = HealthStatus.Unhealthy;
                }
            }

            overallData["CheckCount"] = results.Count;
            overallData["Timestamp"] = DateTime.UtcNow;

            var description = worstStatus switch
            {
                HealthStatus.Healthy => "All authentication components are healthy",
                HealthStatus.Degraded => "Some authentication components are degraded",
                HealthStatus.Unhealthy => "Authentication service has critical issues",
                _ => "Unknown status"
            };

            return new HealthCheckResult(worstStatus, description, data: overallData);
        }
    }
}