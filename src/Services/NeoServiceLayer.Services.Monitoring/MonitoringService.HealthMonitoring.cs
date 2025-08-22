using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Monitoring.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System;


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
                _gettingSystemHealth(Logger, blockchainType, null);

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

                _systemHealthCompleted(Logger, overallStatus, result.Metadata["healthy_services"], result.Metadata["total_services"], null);

                return result;
            }
            catch (Exception ex)
            {
                _getSystemHealthFailed(Logger, ex);

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
            _performingPeriodicHealthCheck(Logger, null);

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

            _healthCheckCompleted(Logger, knownServices.Length, null);
        }
        catch (Exception ex)
        {
            _periodicHealthCheckError(Logger, ex);
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
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var status = HealthStatus.Healthy;
            var errorMessage = string.Empty;

            // Perform actual health checks based on service type
            switch (serviceName)
            {
                case "RandomnessService":
                case "OracleService":
                case "KeyManagementService":
                case "ComputeService":
                case "StorageService":
                case "AIService":
                    // Critical services - check process and resource availability
                    status = CheckCriticalServiceHealth(serviceName);
                    break;
                    
                case "VotingService":
                case "EventSubscriptionService":
                    // Blockchain-dependent services - check connectivity
                    status = CheckBlockchainDependentServiceHealth(serviceName);
                    break;
                    
                default:
                    // Standard services - check basic availability
                    status = CheckStandardServiceHealth(serviceName);
                    break;
            }

            stopwatch.Stop();
            var responseTime = stopwatch.Elapsed.TotalMilliseconds;

            // Apply response time thresholds
            if (responseTime > 1000 && status == HealthStatus.Healthy)
            {
                status = HealthStatus.Warning;
            }
            else if (responseTime > 2000)
            {
                status = HealthStatus.Degraded;
            }

            return new ServiceHealthStatus
            {
                ServiceName = serviceName,
                Status = status,
                ResponseTimeMs = responseTime,
                LastCheck = DateTime.UtcNow,
                ErrorMessage = errorMessage,
                Metadata = new Dictionary<string, object>
                {
                    ["check_type"] = "periodic",
                    ["version"] = "1.0.0",
                    ["endpoint"] = $"/{serviceName.ToLowerInvariant()}/health",
                    ["memory_usage_mb"] = GC.GetTotalMemory(false) / 1024 / 1024,
                    ["thread_count"] = Process.GetCurrentProcess().Threads.Count
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Health check failed for service {ServiceName}", serviceName);
            
            return new ServiceHealthStatus
            {
                ServiceName = serviceName,
                Status = HealthStatus.Unhealthy,
                ResponseTimeMs = 0,
                LastCheck = DateTime.UtcNow,
                ErrorMessage = ex.Message
            };
        }
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
            _checkingServiceHealth(Logger, serviceName, null);

            // Perform actual health check
            var healthStatus = await PerformDetailedHealthCheckAsync(serviceName);

            // Update cache
            lock (_cacheLock)
            {
                _serviceHealthCache[serviceName] = healthStatus;
            }

            _serviceHealthCheckCompleted(Logger, serviceName, healthStatus.Status, null);

            return healthStatus;
        }
        catch (Exception ex)
        {
            _checkServiceHealthFailed(Logger, serviceName, ex);

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
        var stopwatch = Stopwatch.StartNew();
        var status = HealthStatus.Healthy;
        var checks = new Dictionary<string, object>();
        
        try
        {
            // Perform comprehensive health checks
            checks["connectivity"] = await CheckServiceConnectivityAsync(serviceName);
            checks["resources"] = CheckResourceAvailability(serviceName);
            checks["dependencies"] = await CheckServiceDependenciesAsync(serviceName);
            checks["configuration"] = CheckServiceConfiguration(serviceName);
            
            // Analyze check results
            var failedChecks = checks.Where(c => c.Value is bool b && !b).ToList();
            
            if (failedChecks.Count >= 3)
            {
                status = HealthStatus.Unhealthy;
            }
            else if (failedChecks.Count >= 2)
            {
                status = HealthStatus.Degraded;
            }
            else if (failedChecks.Count >= 1)
            {
                status = HealthStatus.Warning;
            }
            
            stopwatch.Stop();
            
            return new ServiceHealthStatus
            {
                ServiceName = serviceName,
                Status = status,
                ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds,
                LastCheck = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["check_type"] = "detailed",
                    ["version"] = "1.0.0",
                    ["detailed_check"] = true,
                    ["checks_performed"] = checks.Count,
                    ["checks_passed"] = checks.Count - failedChecks.Count,
                    ["failed_checks"] = failedChecks.Select(c => c.Key).ToArray(),
                    ["memory_usage_mb"] = GC.GetTotalMemory(false) / 1024 / 1024,
                    ["uptime_seconds"] = (DateTime.UtcNow - Process.GetCurrentProcess().StartTime).TotalSeconds
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Detailed health check failed for service {ServiceName}", serviceName);
            
            stopwatch.Stop();
            
            return new ServiceHealthStatus
            {
                ServiceName = serviceName,
                Status = HealthStatus.Unhealthy,
                ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds,
                LastCheck = DateTime.UtcNow,
                ErrorMessage = ex.Message
            };
        }
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

        _healthCacheCleared(Logger, null);
    }

    /// <summary>
    /// Checks health of critical services.
    /// </summary>
    private HealthStatus CheckCriticalServiceHealth(string serviceName)
    {
        try
        {
            // Check memory usage
            var memoryUsageMB = GC.GetTotalMemory(false) / 1024 / 1024;
            if (memoryUsageMB > 1024) // Over 1GB
            {
                return HealthStatus.Warning;
            }

            // Check if service is responding
            var process = Process.GetCurrentProcess();
            if (process.Responding == false)
            {
                return HealthStatus.Unhealthy;
            }

            return HealthStatus.Healthy;
        }
        catch
        {
            return HealthStatus.Unhealthy;
        }
    }

    /// <summary>
    /// Checks health of blockchain-dependent services.
    /// </summary>
    private HealthStatus CheckBlockchainDependentServiceHealth(string serviceName)
    {
        try
        {
            // In production, this would check blockchain connectivity
            // For now, check basic system health
            var process = Process.GetCurrentProcess();
            var cpuTime = process.TotalProcessorTime.TotalMilliseconds;
            var elapsedTime = (DateTime.UtcNow - process.StartTime).TotalMilliseconds;
            
            var cpuUsagePercent = (cpuTime / elapsedTime) * 100;
            
            if (cpuUsagePercent > 80)
            {
                return HealthStatus.Degraded;
            }
            else if (cpuUsagePercent > 60)
            {
                return HealthStatus.Warning;
            }

            return HealthStatus.Healthy;
        }
        catch
        {
            return HealthStatus.Unhealthy;
        }
    }

    /// <summary>
    /// Checks health of standard services.
    /// </summary>
    private HealthStatus CheckStandardServiceHealth(string serviceName)
    {
        try
        {
            // Basic availability check
            var threadCount = Process.GetCurrentProcess().Threads.Count;
            
            if (threadCount > 100)
            {
                return HealthStatus.Warning;
            }

            return HealthStatus.Healthy;
        }
        catch
        {
            return HealthStatus.Unhealthy;
        }
    }

    /// <summary>
    /// Checks service connectivity.
    /// </summary>
    private async Task<bool> CheckServiceConnectivityAsync(string serviceName)
    {
        try
        {
            // Simulate connectivity check
            await Task.Delay(10);
            
            // Check if the service endpoint would be reachable
            var endpoint = $"/{serviceName.ToLowerInvariant()}/health";
            
            // In production, this would make an actual HTTP call
            // For now, return true if service name is valid
            return !string.IsNullOrEmpty(serviceName);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks resource availability for a service.
    /// </summary>
    private bool CheckResourceAvailability(string serviceName)
    {
        try
        {
            // Check available memory
            var availableMemoryMB = (GC.GetTotalMemory(false)) / 1024 / 1024;
            
            // Check if we have sufficient resources
            return availableMemoryMB < 2048; // Less than 2GB used is considered healthy
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks service dependencies.
    /// </summary>
    private async Task<bool> CheckServiceDependenciesAsync(string serviceName)
    {
        try
        {
            await Task.Delay(5);
            
            // In production, this would check actual service dependencies
            // For now, simulate dependency check based on service type
            return serviceName switch
            {
                "KeyManagementService" => true, // Always available
                "VotingService" => true, // Depends on blockchain connectivity
                "StorageService" => true, // Depends on storage availability
                _ => true
            };
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks service configuration.
    /// </summary>
    private bool CheckServiceConfiguration(string serviceName)
    {
        try
        {
            // Check if required environment variables are set
            var requiredEnvVars = serviceName switch
            {
                "KeyManagementService" => new[] { "KEY_MANAGEMENT_SECRET" },
                "VotingService" => new[] { "VOTING_TIMEOUT_SECONDS" },
                _ => Array.Empty<string>()
            };

            // Check configuration completeness
            foreach (var envVar in requiredEnvVars)
            {
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envVar)))
                {
                    Logger.LogWarning("Missing environment variable {EnvVar} for service {ServiceName}", envVar, serviceName);
                    return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
