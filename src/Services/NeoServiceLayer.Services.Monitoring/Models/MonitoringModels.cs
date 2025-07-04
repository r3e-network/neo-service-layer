using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.Monitoring.Models;

/// <summary>
/// Performance statistics request.
/// </summary>
public class PerformanceStatisticsRequest
{
    /// <summary>
    /// Gets or sets the time range for statistics.
    /// </summary>
    public TimeSpan TimeRange { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets or sets the services to include.
    /// </summary>
    public string[] ServiceNames { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Performance statistics result.
/// </summary>
public class PerformanceStatisticsResult
{
    /// <summary>
    /// Gets or sets the system performance data.
    /// </summary>
    public SystemPerformance SystemPerformance { get; set; } = new();

    /// <summary>
    /// Gets or sets the individual service performance data.
    /// </summary>
    public ServicePerformance[] ServicePerformances { get; set; } = Array.Empty<ServicePerformance>();

    /// <summary>
    /// Gets or sets the time range for the statistics.
    /// </summary>
    public TimeSpan TimeRange { get; set; }

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
/// System performance statistics.
/// </summary>
public class SystemPerformance
{
    /// <summary>
    /// Gets or sets the CPU usage percentage.
    /// </summary>
    public double CpuUsagePercent { get; set; }

    /// <summary>
    /// Gets or sets the memory usage percentage.
    /// </summary>
    public double MemoryUsagePercent { get; set; }

    /// <summary>
    /// Gets or sets the total requests per second.
    /// </summary>
    public double RequestsPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the average response time in milliseconds.
    /// </summary>
    public double AverageResponseTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the error rate percentage.
    /// </summary>
    public double ErrorRatePercent { get; set; }

    /// <summary>
    /// Gets or sets additional performance metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Individual service performance statistics.
/// </summary>
public class ServicePerformance
{
    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the requests per second.
    /// </summary>
    public double RequestsPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the average response time in milliseconds.
    /// </summary>
    public double AverageResponseTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the error rate percentage.
    /// </summary>
    public double ErrorRatePercent { get; set; }

    /// <summary>
    /// Gets or sets the success rate percentage.
    /// </summary>
    public double SuccessRatePercent { get; set; }

    /// <summary>
    /// Gets or sets the total requests processed.
    /// </summary>
    public long TotalRequests { get; set; }

    /// <summary>
    /// Gets or sets additional service-specific metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Create alert rule request.
/// </summary>
public class CreateAlertRuleRequest
{
    /// <summary>
    /// Gets or sets the alert rule name.
    /// </summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the service name to monitor.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the metric name to monitor.
    /// </summary>
    public string MetricName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the alert condition.
    /// </summary>
    public AlertCondition Condition { get; set; }

    /// <summary>
    /// Gets or sets the threshold value.
    /// </summary>
    public double Threshold { get; set; }

    /// <summary>
    /// Gets or sets the alert severity.
    /// </summary>
    public AlertSeverity Severity { get; set; }

    /// <summary>
    /// Gets or sets the notification channels.
    /// </summary>
    public string[] NotificationChannels { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Alert condition enumeration.
/// </summary>
public enum AlertCondition
{
    /// <summary>
    /// Alert when value is greater than threshold.
    /// </summary>
    GreaterThan,

    /// <summary>
    /// Alert when value is less than threshold.
    /// </summary>
    LessThan,

    /// <summary>
    /// Alert when value equals threshold.
    /// </summary>
    Equals,

    /// <summary>
    /// Alert when value is not equal to threshold.
    /// </summary>
    NotEquals,

    /// <summary>
    /// Alert when value is greater than or equal to threshold.
    /// </summary>
    GreaterThanOrEqual,

    /// <summary>
    /// Alert when value is less than or equal to threshold.
    /// </summary>
    LessThanOrEqual
}

/// <summary>
/// Alert severity enumeration.
/// </summary>
public enum AlertSeverity
{
    /// <summary>
    /// Informational alert.
    /// </summary>
    Info,

    /// <summary>
    /// Warning alert.
    /// </summary>
    Warning,

    /// <summary>
    /// Error alert.
    /// </summary>
    Error,

    /// <summary>
    /// Critical alert.
    /// </summary>
    Critical
}

/// <summary>
/// Alert rule result.
/// </summary>
public class AlertRuleResult
{
    /// <summary>
    /// Gets or sets the alert rule ID.
    /// </summary>
    public string RuleId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Get alerts request.
/// </summary>
public class GetAlertsRequest
{
    /// <summary>
    /// Gets or sets the service name filter.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the severity filter.
    /// </summary>
    public AlertSeverity? Severity { get; set; }

    /// <summary>
    /// Gets or sets the start time filter.
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time filter.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of alerts to return.
    /// </summary>
    public int Limit { get; set; } = 100;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Alerts result.
/// </summary>
public class AlertsResult
{
    /// <summary>
    /// Gets or sets the active alerts.
    /// </summary>
    public Alert[] Alerts { get; set; } = Array.Empty<Alert>();

    /// <summary>
    /// Gets or sets the total number of alerts.
    /// </summary>
    public int TotalCount { get; set; }

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
/// Individual alert.
/// </summary>
public class Alert
{
    /// <summary>
    /// Gets or sets the alert ID.
    /// </summary>
    public string AlertId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the alert ID (alias for AlertId).
    /// </summary>
    public string Id { get => AlertId; set => AlertId = value; }

    /// <summary>
    /// Gets or sets the rule ID that triggered this alert.
    /// </summary>
    public string RuleId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the metric name.
    /// </summary>
    public string MetricName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the alert severity.
    /// </summary>
    public AlertSeverity Severity { get; set; }

    /// <summary>
    /// Gets or sets the alert message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current value that triggered the alert.
    /// </summary>
    public double CurrentValue { get; set; }

    /// <summary>
    /// Gets or sets the threshold value.
    /// </summary>
    public double ThresholdValue { get; set; }

    /// <summary>
    /// Gets or sets when the alert was triggered.
    /// </summary>
    public DateTime TriggeredAt { get; set; }

    /// <summary>
    /// Gets or sets whether the alert is acknowledged.
    /// </summary>
    public bool IsAcknowledged { get; set; }

    /// <summary>
    /// Gets or sets additional alert metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the alert is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets when the alert was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the alert was resolved.
    /// </summary>
    public DateTime? ResolvedAt { get; set; }
}

/// <summary>
/// Get logs request.
/// </summary>
public class GetLogsRequest
{
    /// <summary>
    /// Gets or sets the service name filter.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the log level filter.
    /// </summary>
    public LogLevel? LogLevel { get; set; }

    /// <summary>
    /// Gets or sets the start time filter.
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time filter.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Gets or sets the search query.
    /// </summary>
    public string? SearchQuery { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of logs to return.
    /// </summary>
    public int Limit { get; set; } = 1000;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Log level enumeration.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Trace level.
    /// </summary>
    Trace,

    /// <summary>
    /// Debug level.
    /// </summary>
    Debug,

    /// <summary>
    /// Information level.
    /// </summary>
    Information,

    /// <summary>
    /// Warning level.
    /// </summary>
    Warning,

    /// <summary>
    /// Error level.
    /// </summary>
    Error,

    /// <summary>
    /// Critical level.
    /// </summary>
    Critical
}

/// <summary>
/// Logs result.
/// </summary>
public class LogsResult
{
    /// <summary>
    /// Gets or sets the log entries.
    /// </summary>
    public LogEntry[] LogEntries { get; set; } = Array.Empty<LogEntry>();

    /// <summary>
    /// Gets or sets the total number of logs.
    /// </summary>
    public int TotalCount { get; set; }

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
/// Individual log entry.
/// </summary>
public class LogEntry
{
    /// <summary>
    /// Gets or sets the log ID.
    /// </summary>
    public string LogId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the log level.
    /// </summary>
    public LogLevel Level { get; set; }

    /// <summary>
    /// Gets or sets the log message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the exception information if any.
    /// </summary>
    public string? Exception { get; set; }

    /// <summary>
    /// Gets or sets additional log metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Start monitoring request.
/// </summary>
public class StartMonitoringRequest
{
    /// <summary>
    /// Gets or sets the service name to monitor.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the monitoring interval.
    /// </summary>
    public TimeSpan MonitoringInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets the metrics to monitor.
    /// </summary>
    public string[] MetricsToMonitor { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Stop monitoring request.
/// </summary>
public class StopMonitoringRequest
{
    /// <summary>
    /// Gets or sets the service name to stop monitoring.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Monitoring result.
/// </summary>
public class MonitoringResult
{
    /// <summary>
    /// Gets or sets the monitoring session ID.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the operation timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Performance trend information.
/// </summary>
public class PerformanceTrend
{
    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the time range for the trend analysis.
    /// </summary>
    public TimeSpan TimeRange { get; set; }

    /// <summary>
    /// Gets or sets the metric name.
    /// </summary>
    public string MetricName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the trend direction.
    /// </summary>
    public TrendDirection TrendDirection { get; set; }

    /// <summary>
    /// Gets or sets the response time trend.
    /// </summary>
    public TrendDirection ResponseTimeTrend { get; set; }

    /// <summary>
    /// Gets or sets the error rate trend.
    /// </summary>
    public TrendDirection ErrorRateTrend { get; set; }

    /// <summary>
    /// Gets or sets the request rate trend.
    /// </summary>
    public TrendDirection RequestRateTrend { get; set; }

    /// <summary>
    /// Gets or sets the trend direction.
    /// </summary>
    public TrendDirection Direction { get; set; }

    /// <summary>
    /// Gets or sets the percentage change.
    /// </summary>
    public double PercentageChange { get; set; }

    /// <summary>
    /// Gets or sets the time period for the trend.
    /// </summary>
    public TimeSpan TimePeriod { get; set; }

    /// <summary>
    /// Gets or sets the confidence level of the trend.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets the confidence level of the trend.
    /// </summary>
    public double ConfidenceLevel { get; set; }

    /// <summary>
    /// Gets or sets the number of data points analyzed.
    /// </summary>
    public int DataPoints { get; set; }

    /// <summary>
    /// Gets or sets when the analysis was performed.
    /// </summary>
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets any error message.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional trend metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Trend direction enumeration.
/// </summary>
public enum TrendDirection
{
    /// <summary>
    /// Trend is stable.
    /// </summary>
    Stable,

    /// <summary>
    /// Trend is increasing.
    /// </summary>
    Increasing,

    /// <summary>
    /// Trend is decreasing.
    /// </summary>
    Decreasing,

    /// <summary>
    /// Trend is improving.
    /// </summary>
    Improving,

    /// <summary>
    /// Trend is degrading.
    /// </summary>
    Degrading,

    /// <summary>
    /// Trend is volatile.
    /// </summary>
    Volatile,

    /// <summary>
    /// Trend is unknown.
    /// </summary>
    Unknown
}

/// <summary>
/// Performance summary information.
/// </summary>
public class PerformanceSummary
{
    /// <summary>
    /// Gets or sets the total number of services.
    /// </summary>
    public int TotalServices { get; set; }

    /// <summary>
    /// Gets or sets the total requests per second.
    /// </summary>
    public double TotalRequestsPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the average response time in milliseconds.
    /// </summary>
    public double AverageResponseTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the average error rate percentage.
    /// </summary>
    public double AverageErrorRatePercent { get; set; }

    /// <summary>
    /// Gets or sets the overall health percentage.
    /// </summary>
    public double OverallHealthPercentage { get; set; }

    /// <summary>
    /// Gets or sets when the summary was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets any error message.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the overall health score.
    /// </summary>
    public double OverallHealthScore { get; set; }

    /// <summary>
    /// Gets or sets the total services monitored.
    /// </summary>
    public int TotalServicesMonitored { get; set; }

    /// <summary>
    /// Gets or sets the number of healthy services.
    /// </summary>
    public int HealthyServices { get; set; }

    /// <summary>
    /// Gets or sets the number of unhealthy services.
    /// </summary>
    public int UnhealthyServices { get; set; }

    /// <summary>
    /// Gets or sets the average response time across all services.
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// Gets or sets the total requests processed.
    /// </summary>
    public long TotalRequestsProcessed { get; set; }

    /// <summary>
    /// Gets or sets the overall error rate.
    /// </summary>
    public double OverallErrorRate { get; set; }

    /// <summary>
    /// Gets or sets the performance trends.
    /// </summary>
    public PerformanceTrend[] Trends { get; set; } = Array.Empty<PerformanceTrend>();

    /// <summary>
    /// Gets or sets the summary timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional summary metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Monitoring session information.
/// </summary>
public class MonitoringSession
{
    /// <summary>
    /// Gets or sets the session ID.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the service name being monitored.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the blockchain type.
    /// </summary>
    public BlockchainType BlockchainType { get; set; }

    /// <summary>
    /// Gets or sets when the session started.
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets when the session ended.
    /// </summary>
    public DateTime? EndedAt { get; set; }

    /// <summary>
    /// Gets or sets whether the session is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the monitoring interval.
    /// </summary>
    public TimeSpan MonitoringInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets the metrics being monitored.
    /// </summary>
    public string[] MetricsMonitored { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional session metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Alert rule definition.
/// </summary>
public class AlertRule
{
    /// <summary>
    /// Gets or sets the rule ID.
    /// </summary>
    public string RuleId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rule name.
    /// </summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the service name to monitor.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the metric name to monitor.
    /// </summary>
    public string MetricName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the alert condition.
    /// </summary>
    public AlertCondition Condition { get; set; }

    /// <summary>
    /// Gets or sets the threshold value.
    /// </summary>
    public double Threshold { get; set; }

    /// <summary>
    /// Gets or sets the alert severity.
    /// </summary>
    public AlertSeverity Severity { get; set; }

    /// <summary>
    /// Gets or sets the notification channels.
    /// </summary>
    public string[] NotificationChannels { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets whether the rule is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets when the rule was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional rule metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
