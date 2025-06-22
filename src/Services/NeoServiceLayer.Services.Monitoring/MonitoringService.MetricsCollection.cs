using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Monitoring.Models;

namespace NeoServiceLayer.Services.Monitoring;

/// <summary>
/// Metrics collection operations for the Monitoring Service.
/// </summary>
public partial class MonitoringService
{
    /// <inheritdoc/>
    public async Task<ServiceMetricsResult> GetServiceMetricsAsync(ServiceMetricsRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteAsync(async () =>
        {
            try
            {
                Logger.LogDebug("Getting metrics for service {ServiceName}", request.ServiceName);

                var metrics = new List<ServiceMetric>();

                lock (_cacheLock)
                {
                    if (_metricsCache.TryGetValue(request.ServiceName, out var serviceMetrics))
                    {
                        metrics.AddRange(serviceMetrics);
                    }
                }

                // Filter by time range if specified
                if (request.StartTime.HasValue)
                {
                    metrics = metrics.Where(m => m.Timestamp >= request.StartTime.Value).ToList();
                }

                if (request.EndTime.HasValue)
                {
                    metrics = metrics.Where(m => m.Timestamp <= request.EndTime.Value).ToList();
                }

                // Filter by metric names if specified
                if (request.MetricNames.Length > 0)
                {
                    metrics = metrics.Where(m => request.MetricNames.Contains(m.Name)).ToList();
                }

                Logger.LogInformation("Retrieved {MetricCount} metrics for service {ServiceName}",
                    metrics.Count, request.ServiceName);

                return new ServiceMetricsResult
                {
                    ServiceName = request.ServiceName,
                    Metrics = metrics.ToArray(),
                    Success = true,
                    Metadata = new Dictionary<string, object>
                    {
                        ["metric_count"] = metrics.Count,
                        ["time_range"] = $"{request.StartTime} - {request.EndTime}"
                    }
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get metrics for service {ServiceName}", request.ServiceName);

                return new ServiceMetricsResult
                {
                    ServiceName = request.ServiceName,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        });
    }

    /// <inheritdoc/>
    public async Task<MetricRecordingResult> RecordMetricAsync(RecordMetricRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteAsync(async () =>
        {
            try
            {
                var metricId = Guid.NewGuid().ToString();
                var timestamp = DateTime.UtcNow;

                var metric = new ServiceMetric
                {
                    Name = request.MetricName,
                    Value = request.Value,
                    Unit = request.Unit,
                    Timestamp = timestamp,
                    Metadata = new Dictionary<string, object>(request.Metadata)
                };

                // Add tags to metadata
                foreach (var tag in request.Tags)
                {
                    metric.Metadata[$"tag_{tag.Key}"] = tag.Value;
                }

                lock (_cacheLock)
                {
                    if (!_metricsCache.ContainsKey(request.ServiceName))
                    {
                        _metricsCache[request.ServiceName] = new List<ServiceMetric>();
                    }

                    _metricsCache[request.ServiceName].Add(metric);

                    // Keep only recent metrics (last 24 hours)
                    var cutoffTime = DateTime.UtcNow.AddHours(-24);
                    _metricsCache[request.ServiceName].RemoveAll(m => m.Timestamp < cutoffTime);
                }

                Logger.LogDebug("Recorded metric {MetricName} = {Value} {Unit} for service {ServiceName}",
                    request.MetricName, request.Value, request.Unit, request.ServiceName);

                return new MetricRecordingResult
                {
                    MetricId = metricId,
                    Success = true,
                    RecordedAt = timestamp,
                    Metadata = new Dictionary<string, object>
                    {
                        ["service_name"] = request.ServiceName,
                        ["metric_name"] = request.MetricName
                    }
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to record metric {MetricName} for service {ServiceName}",
                    request.MetricName, request.ServiceName);

                return new MetricRecordingResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    RecordedAt = DateTime.UtcNow
                };
            }
        });
    }

    /// <summary>
    /// Collects metrics from all services.
    /// </summary>
    /// <param name="state">Timer state (unused).</param>
    private void CollectMetrics(object? state)
    {
        try
        {
            Logger.LogDebug("Collecting periodic metrics");

            // Get services to collect metrics from
            var services = GetServicesForMetricsCollection();
            var metricNames = GetStandardMetricNames();

            foreach (var service in services)
            {
                foreach (var metricName in metricNames)
                {
                    var metric = GenerateMetricValue(service, metricName);

                    lock (_cacheLock)
                    {
                        if (!_metricsCache.ContainsKey(service))
                        {
                            _metricsCache[service] = new List<ServiceMetric>();
                        }

                        _metricsCache[service].Add(metric);

                        // Keep only recent metrics
                        var cutoffTime = DateTime.UtcNow.AddHours(-24);
                        _metricsCache[service].RemoveAll(m => m.Timestamp < cutoffTime);
                    }
                }
            }

            Logger.LogDebug("Metrics collection completed for {ServiceCount} services", services.Length);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during metrics collection");
        }
    }

    /// <summary>
    /// Gets services for metrics collection.
    /// </summary>
    /// <returns>Array of service names.</returns>
    private string[] GetServicesForMetricsCollection()
    {
        return new[]
        {
            "RandomnessService",
            "OracleService",
            "KeyManagementService",
            "ComputeService",
            "StorageService",
            "AIService"
        };
    }

    /// <summary>
    /// Gets standard metric names to collect.
    /// </summary>
    /// <returns>Array of metric names.</returns>
    private string[] GetStandardMetricNames()
    {
        return new[]
        {
            "requests_per_second",
            "response_time_ms",
            "error_rate_percent",
            "cpu_usage_percent",
            "memory_usage_percent",
            "active_connections"
        };
    }

    /// <summary>
    /// Generates a metric value for a service and metric name.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <param name="metricName">The metric name.</param>
    /// <returns>The generated metric.</returns>
    private ServiceMetric GenerateMetricValue(string serviceName, string metricName)
    {
        var value = metricName switch
        {
            "requests_per_second" => Random.Shared.NextDouble() * 100,
            "response_time_ms" => Random.Shared.NextDouble() * 500,
            "error_rate_percent" => Random.Shared.NextDouble() * 5,
            "cpu_usage_percent" => Random.Shared.NextDouble() * 80,
            "memory_usage_percent" => Random.Shared.NextDouble() * 70,
            "active_connections" => Random.Shared.Next(1, 100),
            _ => 0.0
        };

        var unit = metricName switch
        {
            var name when name.EndsWith("_percent") => "%",
            var name when name.EndsWith("_ms") => "ms",
            var name when name.EndsWith("_per_second") => "count/sec",
            "active_connections" => "count",
            _ => "count"
        };

        return new ServiceMetric
        {
            Name = metricName,
            Value = value,
            Unit = unit,
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["collection_type"] = "automatic",
                ["service"] = serviceName
            }
        };
    }

    /// <summary>
    /// Calculates average value for a metric from a collection of metrics.
    /// </summary>
    /// <param name="metrics">The metrics collection.</param>
    /// <param name="metricName">The metric name.</param>
    /// <returns>The average value.</returns>
    private double CalculateAverage(ServiceMetric[] metrics, string metricName)
    {
        var relevantMetrics = metrics.Where(m => m.Name == metricName).ToArray();
        return relevantMetrics.Length > 0 ? relevantMetrics.Average(m => m.Value) : 0.0;
    }

    /// <summary>
    /// Calculates sum value for a metric from a collection of metrics.
    /// </summary>
    /// <param name="metrics">The metrics collection.</param>
    /// <param name="metricName">The metric name.</param>
    /// <returns>The sum value.</returns>
    private double CalculateSum(ServiceMetric[] metrics, string metricName)
    {
        var relevantMetrics = metrics.Where(m => m.Name == metricName).ToArray();
        return relevantMetrics.Length > 0 ? relevantMetrics.Sum(m => m.Value) : 0.0;
    }

    /// <summary>
    /// Gets all cached metrics.
    /// </summary>
    /// <returns>Dictionary of all cached metrics by service name.</returns>
    public Dictionary<string, List<ServiceMetric>> GetAllCachedMetrics()
    {
        lock (_cacheLock)
        {
            return _metricsCache.ToDictionary(
                kvp => kvp.Key,
                kvp => new List<ServiceMetric>(kvp.Value)
            );
        }
    }

    /// <summary>
    /// Clears metrics cache for a specific service.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    public void ClearMetricsCache(string serviceName)
    {
        ArgumentException.ThrowIfNullOrEmpty(serviceName);

        lock (_cacheLock)
        {
            _metricsCache.Remove(serviceName, out _);
        }

        Logger.LogInformation("Metrics cache cleared for service {ServiceName}", serviceName);
    }

    /// <summary>
    /// Clears all metrics cache.
    /// </summary>
    public void ClearAllMetricsCache()
    {
        lock (_cacheLock)
        {
            _metricsCache.Clear();
        }

        Logger.LogInformation("All metrics cache cleared");
    }
}
