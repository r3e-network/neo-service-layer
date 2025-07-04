using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Monitoring.Models;

namespace NeoServiceLayer.Services.Monitoring;

/// <summary>
/// Interface for the Monitoring Service that provides system health monitoring and metrics collection.
/// </summary>
public interface IMonitoringService : IService
{
    /// <summary>
    /// Gets the current health status of all services.
    /// </summary>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The system health status.</returns>
    Task<SystemHealthResult> GetSystemHealthAsync(BlockchainType blockchainType);

    /// <summary>
    /// Gets performance metrics for a specific service.
    /// </summary>
    /// <param name="request">The metrics request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The service metrics.</returns>
    Task<ServiceMetricsResult> GetServiceMetricsAsync(ServiceMetricsRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Records a custom metric.
    /// </summary>
    /// <param name="request">The metric recording request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The recording result.</returns>
    Task<MetricRecordingResult> RecordMetricAsync(RecordMetricRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Gets system performance statistics.
    /// </summary>
    /// <param name="request">The performance statistics request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The performance statistics.</returns>
    Task<PerformanceStatisticsResult> GetPerformanceStatisticsAsync(PerformanceStatisticsRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Creates an alert rule for monitoring.
    /// </summary>
    /// <param name="request">The alert rule creation request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The alert rule creation result.</returns>
    Task<AlertRuleResult> CreateAlertRuleAsync(CreateAlertRuleRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Gets active alerts.
    /// </summary>
    /// <param name="request">The alerts request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The active alerts.</returns>
    Task<AlertsResult> GetActiveAlertsAsync(GetAlertsRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Gets system logs.
    /// </summary>
    /// <param name="request">The logs request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The system logs.</returns>
    Task<LogsResult> GetLogsAsync(GetLogsRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Starts monitoring a service.
    /// </summary>
    /// <param name="request">The monitoring request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The monitoring result.</returns>
    Task<MonitoringResult> StartMonitoringAsync(StartMonitoringRequest request, BlockchainType blockchainType);

    /// <summary>
    /// Stops monitoring a service.
    /// </summary>
    /// <param name="request">The stop monitoring request.</param>
    /// <param name="blockchainType">The target blockchain type.</param>
    /// <returns>The monitoring result.</returns>
    Task<MonitoringResult> StopMonitoringAsync(StopMonitoringRequest request, BlockchainType blockchainType);
}

/// <summary>
/// System health status result.
/// </summary>
public class SystemHealthResult
{
    /// <summary>
    /// Gets or sets the overall system health status.
    /// </summary>
    public HealthStatus OverallStatus { get; set; }

    /// <summary>
    /// Gets or sets the individual service health statuses.
    /// </summary>
    public ServiceHealthStatus[] ServiceStatuses { get; set; } = Array.Empty<ServiceHealthStatus>();

    /// <summary>
    /// Gets or sets the system uptime.
    /// </summary>
    public TimeSpan SystemUptime { get; set; }

    /// <summary>
    /// Gets or sets the last health check timestamp.
    /// </summary>
    public DateTime LastHealthCheck { get; set; }

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Health status enumeration.
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// System is healthy.
    /// </summary>
    Healthy,

    /// <summary>
    /// System has warnings but is operational.
    /// </summary>
    Warning,

    /// <summary>
    /// System is degraded but partially operational.
    /// </summary>
    Degraded,

    /// <summary>
    /// System is unhealthy.
    /// </summary>
    Unhealthy,

    /// <summary>
    /// System status is unknown.
    /// </summary>
    Unknown
}

/// <summary>
/// Individual service health status.
/// </summary>
public class ServiceHealthStatus
{
    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the service health status.
    /// </summary>
    public HealthStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the response time in milliseconds.
    /// </summary>
    public double ResponseTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the last check timestamp.
    /// </summary>
    public DateTime LastCheck { get; set; }

    /// <summary>
    /// Gets or sets the last check time (alias for LastCheck).
    /// </summary>
    public DateTime LastCheckTime { get => LastCheck; set => LastCheck = value; }

    /// <summary>
    /// Gets or sets the error message if unhealthy.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional service-specific metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Service metrics request.
/// </summary>
public class ServiceMetricsRequest
{
    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the start time for metrics.
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time for metrics.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Gets or sets the metric names to retrieve.
    /// </summary>
    public string[] MetricNames { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Service metrics result.
/// </summary>
public class ServiceMetricsResult
{
    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the metrics.
    /// </summary>
    public ServiceMetric[] Metrics { get; set; } = Array.Empty<ServiceMetric>();

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Individual service metric.
/// </summary>
public class ServiceMetric
{
    /// <summary>
    /// Gets or sets the metric name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the metric value.
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// Gets or sets the metric unit.
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets additional metric metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Record metric request.
/// </summary>
public class RecordMetricRequest
{
    /// <summary>
    /// Gets or sets the metric name.
    /// </summary>
    public string MetricName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the metric value.
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// Gets or sets the metric unit.
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional tags.
    /// </summary>
    public Dictionary<string, string> Tags { get; set; } = new();

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Metric recording result.
/// </summary>
public class MetricRecordingResult
{
    /// <summary>
    /// Gets or sets the metric ID.
    /// </summary>
    public string MetricId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the recording timestamp.
    /// </summary>
    public DateTime RecordedAt { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
