using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Core.Health;

/// <summary>
/// Base health check service for all Neo Service Layer components.
/// </summary>
public abstract class HealthCheckService : IHealthCheck
{
    protected readonly ILogger Logger;
    private readonly string _serviceName;
    private readonly List<Func<CancellationToken, Task<HealthCheckResult>>> _checks = new();

    protected HealthCheckService(ILogger logger, string serviceName)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
    }

    /// <summary>
    /// Add a custom health check.
    /// </summary>
    protected void AddCheck(string name, Func<CancellationToken, Task<HealthCheckResult>> check)
    {
        _checks.Add(check);
    }

    /// <summary>
    /// Add a dependency health check.
    /// </summary>
    protected void AddDependencyCheck(string name, Func<Task<bool>> checkFunc)
    {
        _checks.Add(async (ct) =>
        {
            try
            {
                var isHealthy = await checkFunc().ConfigureAwait(false);
                return isHealthy
                    ? HealthCheckResult.Healthy($"{name} is reachable")
                    : HealthCheckResult.Unhealthy($"{name} is not reachable");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"{name} check failed", ex);
            }
        });
    }

    /// <summary>
    /// Check service health.
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var results = new List<HealthCheckResult>();
            var data = new Dictionary<string, object>
            {
                ["service"] = _serviceName,
                ["timestamp"] = DateTime.UtcNow,
                ["checks"] = new List<object>()
            };

            // Run all checks
            foreach (var check in _checks)
            {
                var result = await check(cancellationToken).ConfigureAwait(false);
                results.Add(result);
            }

            // Add service-specific checks
            var specificResult = await CheckServiceSpecificHealthAsync(cancellationToken).ConfigureAwait(false);
            results.Add(specificResult);

            // Aggregate results
            var unhealthyChecks = results.Where(r => r.Status == HealthStatus.Unhealthy).ToList();
            var degradedChecks = results.Where(r => r.Status == HealthStatus.Degraded).ToList();

            if (unhealthyChecks.Any())
            {
                var errors = string.Join("; ", unhealthyChecks.Select(c => c.Description));
                return HealthCheckResult.Unhealthy($"{_serviceName} has unhealthy dependencies: {errors}", data: data);
            }

            if (degradedChecks.Any())
            {
                var warnings = string.Join("; ", degradedChecks.Select(c => c.Description));
                return HealthCheckResult.Degraded($"{_serviceName} has degraded dependencies: {warnings}", data: data);
            }

            data["status"] = "healthy";
            data["uptime"] = GetUptime();
            data["version"] = GetVersion();
            
            return HealthCheckResult.Healthy($"{_serviceName} is healthy", data);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Health check failed for {ServiceName}", _serviceName);
            return HealthCheckResult.Unhealthy($"{_serviceName} health check failed", ex);
        }
    }

    /// <summary>
    /// Service-specific health checks to be implemented by derived classes.
    /// </summary>
    protected abstract Task<HealthCheckResult> CheckServiceSpecificHealthAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Get service uptime.
    /// </summary>
    protected virtual string GetUptime()
    {
        var uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();
        return $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m";
    }

    /// <summary>
    /// Get service version.
    /// </summary>
    protected virtual string GetVersion()
    {
        return GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0";
    }
}

/// <summary>
/// Database health check.
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly Func<Task<bool>> _checkDatabase;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(Func<Task<bool>> checkDatabase, ILogger<DatabaseHealthCheck> logger)
    {
        _checkDatabase = checkDatabase ?? throw new ArgumentNullException(nameof(checkDatabase));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var isHealthy = await _checkDatabase().ConfigureAwait(false);
            return isHealthy
                ? HealthCheckResult.Healthy("Database connection is healthy")
                : HealthCheckResult.Unhealthy("Database connection failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy("Database health check failed", ex);
        }
    }
}

/// <summary>
/// Redis health check.
/// </summary>
public class RedisHealthCheck : IHealthCheck
{
    private readonly Func<Task<bool>> _checkRedis;
    private readonly ILogger<RedisHealthCheck> _logger;

    public RedisHealthCheck(Func<Task<bool>> checkRedis, ILogger<RedisHealthCheck> logger)
    {
        _checkRedis = checkRedis ?? throw new ArgumentNullException(nameof(checkRedis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var isHealthy = await _checkRedis().ConfigureAwait(false);
            return isHealthy
                ? HealthCheckResult.Healthy("Redis connection is healthy")
                : HealthCheckResult.Unhealthy("Redis connection failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            return HealthCheckResult.Unhealthy("Redis health check failed", ex);
        }
    }
}

/// <summary>
/// Message queue health check.
/// </summary>
public class MessageQueueHealthCheck : IHealthCheck
{
    private readonly Func<Task<bool>> _checkQueue;
    private readonly ILogger<MessageQueueHealthCheck> _logger;

    public MessageQueueHealthCheck(Func<Task<bool>> checkQueue, ILogger<MessageQueueHealthCheck> logger)
    {
        _checkQueue = checkQueue ?? throw new ArgumentNullException(nameof(checkQueue));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var isHealthy = await _checkQueue().ConfigureAwait(false);
            return isHealthy
                ? HealthCheckResult.Healthy("Message queue connection is healthy")
                : HealthCheckResult.Unhealthy("Message queue connection failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Message queue health check failed");
            return HealthCheckResult.Unhealthy("Message queue health check failed", ex);
        }
    }
}