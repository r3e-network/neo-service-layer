using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.Services.Monitoring.Models;

namespace NeoServiceLayer.Services.Monitoring;

/// <summary>
/// Persistent storage extensions for MonitoringService.
/// </summary>
public partial class MonitoringService
{
    private readonly IPersistentStorageProvider? _persistentStorage;
    private const string HEALTH_PREFIX = "monitoring:health:";
    private const string METRICS_PREFIX = "monitoring:metrics:";
    private const string ALERT_PREFIX = "monitoring:alert:";
    private const string SESSION_PREFIX = "monitoring:session:";
    private const string AGGREGATE_PREFIX = "monitoring:aggregate:";

    /// <summary>
    /// Loads persistent monitoring data from storage.
    /// </summary>
    private async Task LoadPersistentMonitoringDataAsync()
    {
        if (_persistentStorage == null)
        {
            Logger.LogWarning("Persistent storage not available for monitoring service");
            return;
        }

        try
        {
            Logger.LogInformation("Loading persistent monitoring data...");

            // Load active alerts
            var alertKeys = await _persistentStorage.ListKeysAsync(ALERT_PREFIX);
            foreach (var key in alertKeys)
            {
                var data = await _persistentStorage.RetrieveAsync(key);
                if (data != null)
                {
                    var alert = JsonSerializer.Deserialize<Alert>(data);
                    if (alert != null && alert.IsActive)
                    {
                        _activeAlerts[alert.Id] = alert;
                    }
                }
            }
            Logger.LogInformation("Loaded {Count} active alerts from persistent storage", _activeAlerts.Count);

            // Load recent health status (last hour)
            var healthKeys = await _persistentStorage.ListKeysAsync(HEALTH_PREFIX);
            var recentHealthKeys = healthKeys.Where(k => IsRecentKey(k, TimeSpan.FromHours(1)));

            foreach (var key in recentHealthKeys)
            {
                var data = await _persistentStorage.RetrieveAsync(key);
                if (data != null)
                {
                    var healthStatus = JsonSerializer.Deserialize<ServiceHealthStatus>(data);
                    if (healthStatus != null)
                    {
                        _serviceHealthCache[healthStatus.ServiceName] = healthStatus;
                    }
                }
            }
            Logger.LogInformation("Loaded {Count} service health statuses from persistent storage", _serviceHealthCache.Count);

            // Load monitoring sessions
            var sessionKeys = await _persistentStorage.ListKeysAsync(SESSION_PREFIX);
            foreach (var key in sessionKeys)
            {
                var data = await _persistentStorage.RetrieveAsync(key);
                if (data != null)
                {
                    var session = JsonSerializer.Deserialize<MonitoringSession>(data);
                    if (session != null && session.IsActive)
                    {
                        _monitoringSessions[session.SessionId] = session;
                    }
                }
            }
            Logger.LogInformation("Loaded {Count} monitoring sessions from persistent storage", _monitoringSessions.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading persistent monitoring data");
        }
    }

    /// <summary>
    /// Persists metrics to storage.
    /// </summary>
    private async Task PersistMetricsAsync(string serviceName, ServiceMetric metric)
    {
        if (_persistentStorage == null) return;

        try
        {
            // Use timestamp in key for time-series data
            var key = $"{METRICS_PREFIX}{serviceName}:{metric.Timestamp.Ticks}";
            var data = JsonSerializer.SerializeToUtf8Bytes(metric);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(7), // Keep metrics for 7 days
                Metadata = new Dictionary<string, string>
                {
                    ["Type"] = "Metric",
                    ["Service"] = serviceName,
                    ["MetricName"] = metric.Name,
                    ["Timestamp"] = metric.Timestamp.ToString("O")
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting metric for service {Service}", serviceName);
        }
    }

    /// <summary>
    /// Persists health status to storage.
    /// </summary>
    private async Task PersistHealthStatusAsync(ServiceHealthStatus healthStatus)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{HEALTH_PREFIX}{healthStatus.ServiceName}:{DateTime.UtcNow.Ticks}";
            var data = JsonSerializer.SerializeToUtf8Bytes(healthStatus);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true,
                TimeToLive = TimeSpan.FromHours(24), // Keep health history for 24 hours
                Metadata = new Dictionary<string, string>
                {
                    ["Type"] = "HealthStatus",
                    ["Service"] = healthStatus.ServiceName,
                    ["Status"] = healthStatus.Status.ToString(),
                    ["Timestamp"] = healthStatus.LastCheckTime.ToString("O")
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting health status for service {Service}", healthStatus.ServiceName);
        }
    }

    /// <summary>
    /// Persists alert to storage.
    /// </summary>
    private async Task PersistAlertAsync(Alert alert)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{ALERT_PREFIX}{alert.Id}";
            var data = JsonSerializer.SerializeToUtf8Bytes(alert);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                TimeToLive = alert.IsActive ? null : TimeSpan.FromDays(30), // Keep resolved alerts for 30 days
                Metadata = new Dictionary<string, string>
                {
                    ["Type"] = "Alert",
                    ["Severity"] = alert.Severity.ToString(),
                    ["Service"] = alert.ServiceName,
                    ["Active"] = alert.IsActive.ToString(),
                    ["CreatedAt"] = alert.CreatedAt.ToString("O")
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting alert {Id}", alert.Id);
        }
    }

    /// <summary>
    /// Removes resolved alert from persistent storage.
    /// </summary>
    private async Task UpdatePersistedAlertAsync(string alertId, bool isActive)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{ALERT_PREFIX}{alertId}";
            var data = await _persistentStorage.RetrieveAsync(key);
            if (data != null)
            {
                var alert = JsonSerializer.Deserialize<Alert>(data);
                if (alert != null)
                {
                    alert.IsActive = isActive;
                    alert.ResolvedAt = isActive ? null : DateTime.UtcNow;
                    await PersistAlertAsync(alert);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating persisted alert {Id}", alertId);
        }
    }

    /// <summary>
    /// Persists monitoring session to storage.
    /// </summary>
    private async Task PersistSessionAsync(MonitoringSession session)
    {
        if (_persistentStorage == null) return;

        try
        {
            var key = $"{SESSION_PREFIX}{session.SessionId}";
            var data = JsonSerializer.SerializeToUtf8Bytes(session);

            await _persistentStorage.StoreAsync(key, data, new StorageOptions
            {
                Encrypt = true,
                Compress = true,
                TimeToLive = session.IsActive ? null : TimeSpan.FromDays(7), // Keep ended sessions for 7 days
                Metadata = new Dictionary<string, string>
                {
                    ["Type"] = "Session",
                    ["Active"] = session.IsActive.ToString(),
                    ["StartedAt"] = session.StartedAt.ToString("O")
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting monitoring session {Id}", session.SessionId);
        }
    }

    /// <summary>
    /// Persists aggregated metrics periodically.
    /// </summary>
    private async Task PersistAggregatedMetricsAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            var now = DateTime.UtcNow;
            var hourlyKey = $"{AGGREGATE_PREFIX}hourly:{now:yyyyMMddHH}";

            var aggregateData = new AggregatedMetrics
            {
                Period = "hourly",
                Timestamp = now,
                ServiceMetrics = _metricsCache.ToDictionary(
                    kvp => kvp.Key,
                    kvp => CalculateAggregates(kvp.Value)
                ),
                TotalAlerts = _activeAlerts.Count,
                HealthyServices = _serviceHealthCache.Count(kvp => kvp.Value.Status == HealthStatus.Healthy),
                UnhealthyServices = _serviceHealthCache.Count(kvp => kvp.Value.Status != HealthStatus.Healthy)
            };

            var data = JsonSerializer.SerializeToUtf8Bytes(aggregateData);

            await _persistentStorage.StoreAsync(hourlyKey, data, new StorageOptions
            {
                Encrypt = false,
                Compress = true,
                TimeToLive = TimeSpan.FromDays(90), // Keep aggregated data for 90 days
                Metadata = new Dictionary<string, string>
                {
                    ["Type"] = "AggregatedMetrics",
                    ["Period"] = "hourly",
                    ["Timestamp"] = now.ToString("O")
                }
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error persisting aggregated metrics");
        }
    }

    /// <summary>
    /// Retrieves historical metrics from persistent storage.
    /// </summary>
    private async Task<List<ServiceMetric>> GetHistoricalMetricsAsync(string serviceName, DateTime startTime, DateTime endTime)
    {
        if (_persistentStorage == null) return new List<ServiceMetric>();

        var metrics = new List<ServiceMetric>();

        try
        {
            var prefix = $"{METRICS_PREFIX}{serviceName}:";
            var keys = await _persistentStorage.ListKeysAsync(prefix);

            foreach (var key in keys)
            {
                // Extract timestamp from key
                var parts = key.Split(':');
                if (parts.Length >= 2 && long.TryParse(parts[^1], out var ticks))
                {
                    var timestamp = new DateTime(ticks);
                    if (timestamp >= startTime && timestamp <= endTime)
                    {
                        var data = await _persistentStorage.RetrieveAsync(key);
                        if (data != null)
                        {
                            var metric = JsonSerializer.Deserialize<ServiceMetric>(data);
                            if (metric != null)
                            {
                                metrics.Add(metric);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving historical metrics for service {Service}", serviceName);
        }

        return metrics.OrderBy(m => m.Timestamp).ToList();
    }

    /// <summary>
    /// Cleans up old monitoring data periodically.
    /// </summary>
    private async Task CleanupOldMonitoringDataAsync()
    {
        if (_persistentStorage == null) return;

        try
        {
            // Clean up old metrics (older than 7 days)
            await CleanupOldKeysAsync(METRICS_PREFIX, TimeSpan.FromDays(7));

            // Clean up old health status (older than 24 hours)
            await CleanupOldKeysAsync(HEALTH_PREFIX, TimeSpan.FromHours(24));

            // Clean up resolved alerts (older than 30 days)
            var alertKeys = await _persistentStorage.ListKeysAsync(ALERT_PREFIX);
            foreach (var key in alertKeys)
            {
                var metadata = await _persistentStorage.GetMetadataAsync(key);
                if (metadata != null && metadata.CustomMetadata.TryGetValue("Active", out var active))
                {
                    if (active == "False" && metadata.CreatedAt < DateTime.UtcNow.AddDays(-30))
                    {
                        await _persistentStorage.DeleteAsync(key);
                    }
                }
            }

            Logger.LogInformation("Completed cleanup of old monitoring data");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during monitoring data cleanup");
        }
    }

    private async Task CleanupOldKeysAsync(string prefix, TimeSpan maxAge)
    {
        var keys = await _persistentStorage!.ListKeysAsync(prefix);
        var cutoffTime = DateTime.UtcNow - maxAge;

        foreach (var key in keys)
        {
            var metadata = await _persistentStorage.GetMetadataAsync(key);
            if (metadata != null && metadata.CreatedAt < cutoffTime)
            {
                await _persistentStorage.DeleteAsync(key);
            }
        }
    }

    private bool IsRecentKey(string key, TimeSpan maxAge)
    {
        var parts = key.Split(':');
        if (parts.Length >= 2 && long.TryParse(parts[^1], out var ticks))
        {
            var timestamp = new DateTime(ticks);
            return DateTime.UtcNow - timestamp <= maxAge;
        }
        return false;
    }

    private MetricAggregates CalculateAggregates(List<ServiceMetric> metrics)
    {
        if (!metrics.Any()) return new MetricAggregates();

        var values = metrics.Select(m => m.Value).ToList();
        return new MetricAggregates
        {
            Min = values.Min(),
            Max = values.Max(),
            Average = values.Average(),
            Count = values.Count,
            Sum = values.Sum()
        };
    }
}

/// <summary>
/// Aggregated metrics for persistence.
/// </summary>
internal class AggregatedMetrics
{
    public string Period { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, MetricAggregates> ServiceMetrics { get; set; } = new();
    public int TotalAlerts { get; set; }
    public int HealthyServices { get; set; }
    public int UnhealthyServices { get; set; }
}

/// <summary>
/// Metric aggregates.
/// </summary>
internal class MetricAggregates
{
    public double Min { get; set; }
    public double Max { get; set; }
    public double Average { get; set; }
    public int Count { get; set; }
    public double Sum { get; set; }
}
