using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Monitoring.Models;

namespace NeoServiceLayer.Services.Monitoring;

/// <summary>
/// Health monitoring operations for the Monitoring Service.
/// </summary>
public partial class MonitoringService
{
    /// <inheritdoc/>
    public async Task<SystemHealthResult> GetSystemHealthAsync(BlockchainType blockchainType)
    {
        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteAsync(async () =>
        {
            try
            {
                Logger.LogDebug("Getting system health status for {Blockchain}", blockchainType);

                var serviceStatuses = new List<ServiceHealthStatus>();

                lock (_cacheLock)
                {
                    serviceStatuses.AddRange(_serviceHealthCache.Values);
                }

                // Determine overall system health
                var overallStatus = DetermineOverallHealth(serviceStatuses);

                var result = new SystemHealthResult
                {
                    OverallStatus = overallStatus,
                    ServiceStatuses = serviceStatuses.ToArray(),
                    SystemUptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime,
                    LastHealthCheck = DateTime.UtcNow,
                    Success = true,
                    Metadata = new Dictionary<string, object>
                    {
                        ["total_services"] = serviceStatuses.Count,
                        ["healthy_services"] = serviceStatuses.Count(s => s.Status == HealthStatus.Healthy),
                        ["blockchain"] = blockchainType.ToString()
                    }
                };

                Logger.LogInformation("System health check completed: {OverallStatus} ({HealthyCount}/{TotalCount} services healthy)",
                    overallStatus, result.Metadata["healthy_services"], result.Metadata["total_services"]);

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get system health status");

                return new SystemHealthResult
                {
                    OverallStatus = HealthStatus.Unknown,
                    Success = false,
                    ErrorMessage = ex.Message,
                    LastHealthCheck = DateTime.UtcNow
                };
            }
        });
    }

    /// <summary>
    /// Performs periodic health checks on all services.
    /// </summary>
    /// <param name="state">Timer state (unused).</param>
    private void PerformHealthCheck(object? state)
    {
        try
        {
            Logger.LogDebug("Performing periodic health check");

            // Get list of known services to monitor
            var knownServices = GetKnownServices();

            foreach (var serviceName in knownServices)
            {
                var healthStatus = PerformServiceHealthCheck(serviceName);

                lock (_cacheLock)
                {
                    _serviceHealthCache[serviceName] = healthStatus;
                }
            }

            Logger.LogDebug("Health check completed for {ServiceCount} services", knownServices.Length);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during periodic health check");
        }
    }

    /// <summary>
    /// Gets the list of known services to monitor.
    /// </summary>
    /// <returns>Array of service names.</returns>
    private string[] GetKnownServices()
    {
        return new[]
        {
            "RandomnessService",
            "OracleService",
            "KeyManagementService",
            "ComputeService",
            "StorageService",
            "AIService",
            "AbstractAccountService",
            "NotificationService",
            "ConfigurationService",
            "BackupService",
            "HealthService",
            "VotingService",
            "EventSubscriptionService"
        };
    }

    /// <summary>
    /// Performs health check for a specific service.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <returns>The health status.</returns>
    private ServiceHealthStatus PerformServiceHealthCheck(string serviceName)
    {
        // In production, this would perform actual health checks
        // For now, simulate health status with some variability
        var random = Random.Shared;
        var healthProbability = random.NextDouble();

        var status = healthProbability switch
        {
            > 0.95 => HealthStatus.Unhealthy,
            > 0.90 => HealthStatus.Degraded,
            > 0.85 => HealthStatus.Warning,
            _ => HealthStatus.Healthy
        };

        var responseTime = status switch
        {
            HealthStatus.Healthy => random.NextDouble() * 50,
            HealthStatus.Warning => random.NextDouble() * 100 + 50,
            HealthStatus.Degraded => random.NextDouble() * 200 + 100,
            HealthStatus.Unhealthy => random.NextDouble() * 500 + 200,
            _ => random.NextDouble() * 100
        };

        return new ServiceHealthStatus
        {
            ServiceName = serviceName,
            Status = status,
            ResponseTimeMs = responseTime,
            LastCheck = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["check_type"] = "periodic",
                ["version"] = "1.0.0",
                ["endpoint"] = $"/{serviceName.ToLowerInvariant()}/health"
            }
        };
    }

    /// <summary>
    /// Checks the health of a specific service.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <returns>The service health status.</returns>
    public async Task<ServiceHealthStatus> CheckServiceHealthAsync(string serviceName)
    {
        ArgumentException.ThrowIfNullOrEmpty(serviceName);

        try
        {
            Logger.LogDebug("Checking health for service {ServiceName}", serviceName);

            // Perform actual health check
            var healthStatus = await PerformDetailedHealthCheckAsync(serviceName);

            // Update cache
            lock (_cacheLock)
            {
                _serviceHealthCache[serviceName] = healthStatus;
            }

            Logger.LogDebug("Health check completed for service {ServiceName}: {Status}",
                serviceName, healthStatus.Status);

            return healthStatus;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to check health for service {ServiceName}", serviceName);

            return new ServiceHealthStatus
            {
                ServiceName = serviceName,
                Status = HealthStatus.Unknown,
                LastCheck = DateTime.UtcNow,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Performs detailed health check for a service.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <returns>The detailed health status.</returns>
    private async Task<ServiceHealthStatus> PerformDetailedHealthCheckAsync(string serviceName)
    {
        await Task.Delay(Random.Shared.Next(10, 100)); // Simulate health check time

        // In production, this would make actual HTTP calls or service checks
        var isHealthy = Random.Shared.NextDouble() > 0.1; // 90% healthy
        var responseTime = Random.Shared.NextDouble() * (isHealthy ? 100 : 500);

        return new ServiceHealthStatus
        {
            ServiceName = serviceName,
            Status = isHealthy ? HealthStatus.Healthy : HealthStatus.Degraded,
            ResponseTimeMs = responseTime,
            LastCheck = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["check_type"] = "detailed",
                ["version"] = "1.0.0",
                ["detailed_check"] = true
            }
        };
    }

    /// <summary>
    /// Gets cached health status for all services.
    /// </summary>
    /// <returns>Dictionary of service health statuses.</returns>
    public Dictionary<string, ServiceHealthStatus> GetCachedHealthStatuses()
    {
        lock (_cacheLock)
        {
            return new Dictionary<string, ServiceHealthStatus>(_serviceHealthCache);
        }
    }

    /// <summary>
    /// Clears health status cache.
    /// </summary>
    public void ClearHealthCache()
    {
        lock (_cacheLock)
        {
            _serviceHealthCache.Clear();
        }

        Logger.LogInformation("Health status cache cleared");
    }
}
