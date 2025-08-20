using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using System.Linq;
using System.Threading;


namespace NeoServiceLayer.Infrastructure.Monitoring;

/// <summary>
/// Comprehensive observability service providing metrics, tracing, and monitoring.
/// Addresses monitoring and observability gaps identified in the code review.
/// </summary>
public class ObservabilityService : ServiceBase, IObservabilityService
{
    private readonly Meter _meter;
    private readonly ActivitySource _activitySource;
    private readonly ConcurrentDictionary<string, Counter<long>> _counters = new();
    private readonly ConcurrentDictionary<string, Histogram<double>> _histograms = new();
    private readonly ConcurrentDictionary<string, ObservableGauge<double>> _gauges = new();
    private readonly ConcurrentDictionary<string, MetricValue> _metricValues = new();
    private readonly object _lockObject = new();

    // Performance tracking
    private readonly ConcurrentDictionary<string, PerformanceMetrics> _performanceMetrics = new();
    private readonly ConcurrentDictionary<string, HealthStatus> _healthStatuses = new();

    public ObservabilityService(ILogger<ObservabilityService> logger)
        : base("ObservabilityService", "Comprehensive monitoring and observability", "1.0.0", logger)
    {
        _meter = new Meter("NeoServiceLayer.Observability", "1.0.0");
        _activitySource = new ActivitySource("NeoServiceLayer.Tracing", "1.0.0");

        // Add observability capability
        AddCapability<IObservabilityService>();

        // Set metadata
        SetMetadata("MeterName", "NeoServiceLayer.Observability");
        SetMetadata("TracingEnabled", true);
        SetMetadata("MetricsEnabled", true);
        SetMetadata("HealthChecksEnabled", true);

        // Initialize core metrics
        InitializeCoreMetrics();
    }

    /// <inheritdoc/>
    public void RecordMetric(string name, double value, Dictionary<string, string>? tags = null)
    {
        try
        {
            // Update metric value
            _metricValues.AddOrUpdate(name,
                new MetricValue { Value = value, Timestamp = DateTime.UtcNow, Tags = tags ?? new Dictionary<string, string>() },
                (key, existing) =>
                {
                    existing.Value = value;
                    existing.Timestamp = DateTime.UtcNow;
                    existing.Count++;
                    if (tags != null) existing.Tags = tags;
                    return existing;
                });

            // Record to histogram for analysis
            var histogram = _histograms.GetOrAdd(name, _ =>
                _meter.CreateHistogram<double>(name, "value", $"Histogram for {name}"));

            if (tags != null && tags.Count > 0)
            {
                var tagPairs = new KeyValuePair<string, object?>[tags.Count];
                int i = 0;
                foreach (var tag in tags)
                {
                    tagPairs[i++] = new KeyValuePair<string, object?>(tag.Key, tag.Value);
                }
                histogram.Record(value, tagPairs);
            }
            else
            {
                histogram.Record(value);
            }

            Logger.LogTrace("Recorded metric {MetricName} = {Value}", name, value);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to record metric {MetricName}", name);
        }
    }

    /// <inheritdoc/>
    public void IncrementCounter(string name, long increment = 1, Dictionary<string, string>? tags = null)
    {
        try
        {
            var counter = _counters.GetOrAdd(name, _ =>
                _meter.CreateCounter<long>(name, "count", $"Counter for {name}"));

            if (tags != null && tags.Count > 0)
            {
                var tagPairs = new KeyValuePair<string, object?>[tags.Count];
                int i = 0;
                foreach (var tag in tags)
                {
                    tagPairs[i++] = new KeyValuePair<string, object?>(tag.Key, tag.Value);
                }
                counter.Add(increment, tagPairs);
            }
            else
            {
                counter.Add(increment);
            }

            // Update internal tracking
            _metricValues.AddOrUpdate($"{name}_counter",
                new MetricValue { Value = increment, Count = 1, Timestamp = DateTime.UtcNow, Tags = tags ?? new Dictionary<string, string>() },
                (key, existing) =>
                {
                    existing.Value += increment;
                    existing.Count++;
                    existing.Timestamp = DateTime.UtcNow;
                    return existing;
                });

            Logger.LogTrace("Incremented counter {CounterName} by {Increment}", name, increment);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to increment counter {CounterName}", name);
        }
    }

    /// <inheritdoc/>
    public Activity? StartActivity(string operationName, Dictionary<string, string>? tags = null)
    {
        try
        {
            var activity = _activitySource.StartActivity(operationName);

            if (activity != null && tags != null)
            {
                foreach (var tag in tags)
                {
                    activity.SetTag(tag.Key, tag.Value);
                }
            }

            // Set common tags
            activity?.SetTag("service.name", Name);
            activity?.SetTag("service.version", Version);
            activity?.SetTag("operation.start_time", DateTime.UtcNow.ToString("O"));

            Logger.LogTrace("Started activity {OperationName} with ID {ActivityId}", operationName, activity?.Id);
            return activity;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to start activity {OperationName}", operationName);
            return null;
        }
    }

    /// <inheritdoc/>
    public void CompleteActivity(Activity? activity, bool success = true, string? error = null)
    {
        if (activity == null) return;

        try
        {
            activity.SetTag("operation.success", success.ToString());
            activity.SetTag("operation.end_time", DateTime.UtcNow.ToString("O"));

            if (!success && !string.IsNullOrEmpty(error))
            {
                activity.SetTag("error.message", error);
                activity.SetStatus(ActivityStatusCode.Error, error);
            }
            else if (success)
            {
                activity.SetStatus(ActivityStatusCode.Ok);
            }

            // Calculate duration
            var duration = DateTime.UtcNow - activity.StartTimeUtc;
            activity.SetTag("operation.duration_ms", duration.TotalMilliseconds.ToString());

            // Record operation metrics
            IncrementCounter($"operations.{activity.OperationName}", 1, new Dictionary<string, string>
            {
                ["success"] = success.ToString(),
                ["service"] = Name
            });

            RecordMetric($"operation_duration.{activity.OperationName}", duration.TotalMilliseconds, new Dictionary<string, string>
            {
                ["service"] = Name
            });

            Logger.LogTrace("Completed activity {OperationName} with ID {ActivityId}, success: {Success}, duration: {Duration}ms",
                activity.OperationName, activity.Id, success, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to complete activity {OperationName}", activity.OperationName);
        }
        finally
        {
            activity.Dispose();
        }
    }

    /// <inheritdoc/>
    public async Task<T> TraceOperationAsync<T>(string operationName, Func<Task<T>> operation, Dictionary<string, string>? tags = null)
    {

        try
        {
            var startTime = DateTime.UtcNow;
            var result = await operation();
            var duration = DateTime.UtcNow - startTime;

            // Track performance metrics
            TrackPerformanceMetric(operationName, duration, true);

            CompleteActivity(activity, success: true);
            return result;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - (activity?.StartTimeUtc ?? DateTime.UtcNow);
            TrackPerformanceMetric(operationName, duration, false);

            CompleteActivity(activity, success: false, error: ex.Message);
            throw;
        }
    }

    /// <inheritdoc/>
    public void LogStructuredEvent(string eventName, object data, LogLevel logLevel = LogLevel.Information)
    {
        try
        {
            var eventData = new
            {
                EventName = eventName,
                Timestamp = DateTime.UtcNow,
                Service = Name,
                Data = data,
                TraceId = Activity.Current?.TraceId.ToString(),
                SpanId = Activity.Current?.SpanId.ToString()
            };

            var jsonData = JsonSerializer.Serialize(eventData);

            Logger.Log(logLevel, "StructuredEvent: {JsonData}", jsonData);

            // Increment event counter
            IncrementCounter($"events.{eventName}", 1, new Dictionary<string, string>
            {
                ["level"] = logLevel.ToString(),
                ["service"] = Name
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to log structured event {EventName}", eventName);
        }
    }

    /// <inheritdoc/>
    public Dictionary<string, object> GetMetrics(string? namePrefix = null)
    {
        var result = new Dictionary<string, object>();

        try
        {
            foreach (var kvp in _metricValues)
            {
                if (string.IsNullOrEmpty(namePrefix) || kvp.Key.StartsWith(namePrefix))
                {
                    result[kvp.Key] = new
                    {
                        kvp.Value.Value,
                        kvp.Value.Count,
                        kvp.Value.Timestamp,
                        kvp.Value.Tags
                    };
                }
            }

            // Add performance metrics
            foreach (var kvp in _performanceMetrics)
            {
                if (string.IsNullOrEmpty(namePrefix) || kvp.Key.StartsWith(namePrefix))
                {
                    result[$"performance.{kvp.Key}"] = new
                    {
                        kvp.Value.AverageLatency,
                        kvp.Value.MinLatency,
                        kvp.Value.MaxLatency,
                        kvp.Value.TotalCalls,
                        kvp.Value.SuccessfulCalls,
                        kvp.Value.FailedCalls,
                        SuccessRate = kvp.Value.TotalCalls > 0 ? (double)kvp.Value.SuccessfulCalls / kvp.Value.TotalCalls : 0.0,
                        kvp.Value.LastUpdated
                    };
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get metrics");
        }

        return result;
    }

    /// <inheritdoc/>
    public void SetHealthStatus(string component, bool healthy, string? message = null, Dictionary<string, object>? details = null)
    {
        try
        {
            var status = new HealthStatus
            {
                Component = component,
                IsHealthy = healthy,
                Message = message,
                Details = details ?? new Dictionary<string, object>(),
                LastChecked = DateTime.UtcNow
            };

            _healthStatuses.AddOrUpdate(component, status, (key, existing) => status);

            // Record health metric
            RecordMetric($"health.{component}", healthy ? 1 : 0, new Dictionary<string, string>
            {
                ["component"] = component,
                ["status"] = healthy ? "healthy" : "unhealthy"
            });

            Logger.LogDebug("Health status updated for {Component}: {Status}", component, healthy ? "Healthy" : "Unhealthy");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to set health status for {Component}", component);
        }
    }

    /// <inheritdoc/>
    public Dictionary<string, HealthStatus> GetHealthStatuses()
    {
        return new Dictionary<string, HealthStatus>(_healthStatuses);
    }

    /// <inheritdoc/>
    public AlertResult CheckAlerts()
    {
        var alerts = new List<Alert>();

        try
        {
            // Check performance alerts
            foreach (var kvp in _performanceMetrics)
            {
                var metrics = kvp.Value;

                // High latency alert
                if (metrics.AverageLatency > 5000) // 5 seconds
                {
                    alerts.Add(new Alert
                    {
                        Severity = AlertSeverity.Warning,
                        Component = kvp.Key,
                        Message = $"High average latency: {metrics.AverageLatency:F2}ms",
                        Timestamp = DateTime.UtcNow,
                        Details = new Dictionary<string, object>
                        {
                            ["avg_latency"] = metrics.AverageLatency,
                            ["max_latency"] = metrics.MaxLatency,
                            ["total_calls"] = metrics.TotalCalls
                        }
                    });
                }

                // High error rate alert
                if (metrics.TotalCalls > 10 && (double)metrics.FailedCalls / metrics.TotalCalls > 0.1) // 10% error rate
                {
                    alerts.Add(new Alert
                    {
                        Severity = AlertSeverity.Critical,
                        Component = kvp.Key,
                        Message = $"High error rate: {((double)metrics.FailedCalls / metrics.TotalCalls * 100):F1}%",
                        Timestamp = DateTime.UtcNow,
                        Details = new Dictionary<string, object>
                        {
                            ["error_rate"] = (double)metrics.FailedCalls / metrics.TotalCalls,
                            ["failed_calls"] = metrics.FailedCalls,
                            ["total_calls"] = metrics.TotalCalls
                        }
                    });
                }
            }

            // Check health alerts
            foreach (var kvp in _healthStatuses)
            {
                if (!kvp.Value.IsHealthy)
                {
                    alerts.Add(new Alert
                    {
                        Severity = AlertSeverity.Critical,
                        Component = kvp.Key,
                        Message = kvp.Value.Message ?? "Component is unhealthy",
                        Timestamp = DateTime.UtcNow,
                        Details = kvp.Value.Details
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to check alerts");
        }

        return new AlertResult
        {
            Alerts = alerts,
            Timestamp = DateTime.UtcNow,
            TotalAlerts = alerts.Count,
            CriticalAlerts = alerts.Count(a => a.Severity == AlertSeverity.Critical),
            WarningAlerts = alerts.Count(a => a.Severity == AlertSeverity.Warning)
        };
    }

    #region Service Lifecycle

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        try
        {
            Logger.LogInformation("Initializing observability service...");

            // Set initial health status
            SetHealthStatus(Name, true, "Service initialized");

            Logger.LogInformation("Observability service initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize observability service");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        Logger.LogInformation("Observability service started");
        SetHealthStatus(Name, true, "Service running");
        await Task.CompletedTask;
        return true;
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStopAsync()
    {
        Logger.LogInformation("Observability service stopping...");
        SetHealthStatus(Name, false, "Service stopping");
        await Task.CompletedTask;
        return true;
    }

    /// <inheritdoc/>
    protected override async Task<ServiceHealth> OnGetHealthAsync()
    {
        try
        {
            // Check internal health
            var unhealthyComponents = _healthStatuses.Values.Count(h => !h.IsHealthy);

            if (unhealthyComponents > _healthStatuses.Count / 2)
            {
                return ServiceHealth.Unhealthy;
            }

            if (unhealthyComponents > 0)
            {
                return ServiceHealth.Degraded;
            }

            await Task.CompletedTask;
            return ServiceHealth.Healthy;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Health check failed");
            return ServiceHealth.Unhealthy;
        }
    }

    #endregion

    #region Private Methods

    private void InitializeCoreMetrics()
    {
        // Create observable gauges for system metrics
        _gauges.TryAdd("system.memory", _meter.CreateObservableGauge("system_memory_bytes", "bytes",
            "System memory usage", () => GC.GetTotalMemory(false)));

        _gauges.TryAdd("system.gc_collections", _meter.CreateObservableGauge("system_gc_collections", "count",
            "GC collection count", () => GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2)));
    }

    private void TrackPerformanceMetric(string operationName, TimeSpan duration, bool success)
    {
        _performanceMetrics.AddOrUpdate(operationName,
            new PerformanceMetrics
            {
                TotalCalls = 1,
                SuccessfulCalls = success ? 1 : 0,
                FailedCalls = success ? 0 : 1,
                TotalLatency = duration.TotalMilliseconds,
                MinLatency = duration.TotalMilliseconds,
                MaxLatency = duration.TotalMilliseconds,
                LastUpdated = DateTime.UtcNow
            },
            (key, existing) =>
            {
                existing.TotalCalls++;
                existing.TotalLatency += duration.TotalMilliseconds;
                existing.MinLatency = Math.Min(existing.MinLatency, duration.TotalMilliseconds);
                existing.MaxLatency = Math.Max(existing.MaxLatency, duration.TotalMilliseconds);
                existing.LastUpdated = DateTime.UtcNow;

                if (success)
                    existing.SuccessfulCalls++;
                else
                    existing.FailedCalls++;

                return existing;
            });
    }

    #endregion

    /// <summary>
    /// Disposes resources.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _meter?.Dispose();
            _activitySource?.Dispose();
        }

        base.Dispose(disposing);
    }
}

/// <summary>
/// Internal metric value tracking.
/// </summary>
internal class MetricValue
{
    public double Value { get; set; }
    public long Count { get; set; } = 1;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
}

/// <summary>
/// Internal performance metrics tracking.
/// </summary>
internal class PerformanceMetrics
{
    public long TotalCalls { get; set; }
    public long SuccessfulCalls { get; set; }
    public long FailedCalls { get; set; }
    public double TotalLatency { get; set; }
    public double MinLatency { get; set; }
    public double MaxLatency { get; set; }
    public DateTime LastUpdated { get; set; }

    public double AverageLatency => TotalCalls > 0 ? TotalLatency / TotalCalls : 0;
}