using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Services.Health;

/// <summary>
/// Node statistics summary.
/// </summary>
public class NodeStatistics
{
    public int TotalNodes { get; set; }
    public int OnlineNodes { get; set; }
    public int OfflineNodes { get; set; }
    public int ConsensusNodes { get; set; }
    public int OnlineConsensusNodes { get; set; }
    public double AverageUptimePercentage { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public double NetworkHealthScore { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Consensus health report for the network.
/// </summary>
public class ConsensusHealthReport
{
    /// <summary>
    /// Gets or sets the total number of consensus nodes.
    /// </summary>
    public int TotalConsensusNodes { get; set; }
    
    /// <summary>
    /// Gets or sets the number of online consensus nodes.
    /// </summary>
    public int OnlineConsensusNodes { get; set; }
    
    /// <summary>
    /// Gets or sets the consensus participation rate.
    /// </summary>
    public double ParticipationRate { get; set; }
    
    /// <summary>
    /// Gets or sets the average block time.
    /// </summary>
    public TimeSpan AverageBlockTime { get; set; }
    
    /// <summary>
    /// Gets or sets the last block height.
    /// </summary>
    public long LastBlockHeight { get; set; }
    
    /// <summary>
    /// Gets or sets the last block timestamp.
    /// </summary>
    public DateTime LastBlockTimestamp { get; set; }
    
    /// <summary>
    /// Gets or sets whether consensus is healthy.
    /// </summary>
    public bool IsHealthy { get; set; }
    
    /// <summary>
    /// Gets or sets the consensus health score (0.0 to 1.0).
    /// </summary>
    public double HealthScore { get; set; }
    
    // Additional properties for compatibility
    /// <summary>
    /// Gets or sets the list of consensus nodes.
    /// </summary>
    public List<string> ConsensusNodes { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the number of active consensus nodes.
    /// </summary>
    public int ActiveConsensusNodes { get; set; }
    
    /// <summary>
    /// Gets or sets the number of healthy consensus nodes.
    /// </summary>
    public int HealthyConsensusNodes { get; set; }
    
    /// <summary>
    /// Gets or sets the current block height.
    /// </summary>
    public long CurrentBlockHeight { get; set; }
    
    /// <summary>
    /// Gets or sets the last block time.
    /// </summary>
    public DateTime LastBlockTime { get; set; }
    
    /// <summary>
    /// Gets or sets the consensus efficiency percentage.
    /// </summary>
    public double ConsensusEfficiency { get; set; }
    
    /// <summary>
    /// Gets or sets the network metrics.
    /// </summary>
    public Dictionary<string, object> NetworkMetrics { get; set; } = new();
}

/// <summary>
/// Health summary for the entire network.
/// </summary>
public class HealthSummary
{
    public NodeStatistics NodeStatistics { get; set; } = new();
    public ConsensusHealthReport ConsensusHealth { get; set; } = new();
    public HealthMetrics NetworkMetrics { get; set; } = new();
    public double NetworkHealthScore { get; set; }
    public int ActiveAlertCount { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Alert statistics summary.
/// </summary>
public class AlertStatistics
{
    public int TotalAlerts { get; set; }
    public int ActiveAlerts { get; set; }
    public int ResolvedAlerts { get; set; }
    public int CriticalAlerts { get; set; }
    public int ErrorAlerts { get; set; }
    public int WarningAlerts { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Represents a comprehensive health report for a network node.
/// </summary>
public class NodeHealthReport
{
    /// <summary>
    /// Gets or sets the unique identifier for this health report.
    /// </summary>
    public string ReportId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the node identifier.
    /// </summary>
    public string NodeId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the node address.
    /// </summary>
    public string NodeAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the overall health status.
    /// </summary>
    public HealthStatus Status { get; set; }
    
    /// <summary>
    /// Gets or sets the overall health score (0.0 to 1.0).
    /// </summary>
    public double HealthScore { get; set; }
    
    /// <summary>
    /// Gets or sets the uptime percentage.
    /// </summary>
    public double UptimePercentage { get; set; }
    
    /// <summary>
    /// Gets or sets the current response time.
    /// </summary>
    public TimeSpan ResponseTime { get; set; }
    
    /// <summary>
    /// Gets or sets the CPU usage percentage.
    /// </summary>
    public double CpuUsage { get; set; }
    
    /// <summary>
    /// Gets or sets the memory usage percentage.
    /// </summary>
    public double MemoryUsage { get; set; }
    
    /// <summary>
    /// Gets or sets the disk usage percentage.
    /// </summary>
    public double DiskUsage { get; set; }
    
    /// <summary>
    /// Gets or sets the network connectivity status.
    /// </summary>
    public ConnectivityStatus NetworkConnectivity { get; set; }
    
    /// <summary>
    /// Gets or sets the list of health metrics.
    /// </summary>
    public List<HealthMetrics> Metrics { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the list of health alerts.
    /// </summary>
    public List<HealthAlert> Alerts { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the health thresholds configuration.
    /// </summary>
    public HealthThreshold Thresholds { get; set; } = new();
    
    /// <summary>
    /// Gets or sets validation checks performed.
    /// </summary>
    public List<ValidationCheck> ValidationChecks { get; set; } = new();
    
    /// <summary>
    /// Gets or sets when this report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets when the node was last seen active.
    /// </summary>
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets the last health check timestamp.
    /// </summary>
    public DateTime LastHealthCheck { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets additional metadata for the report.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    // Additional properties for compatibility
    /// <summary>
    /// Gets or sets the node's public key.
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets whether this is a consensus node.
    /// </summary>
    public bool IsConsensusNode { get; set; }
    
    /// <summary>
    /// Gets or sets the current block height.
    /// </summary>
    public long BlockHeight { get; set; }
    
    /// <summary>
    /// Gets or sets additional data dictionary.
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the consensus rank.
    /// </summary>
    public int ConsensusRank { get; set; }
}

/// <summary>
/// Represents health metrics for monitoring.
/// </summary>
public class HealthMetrics
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
    /// Gets or sets the metric type.
    /// </summary>
    public MetricType Type { get; set; }
    
    /// <summary>
    /// Gets or sets when this metric was recorded.
    /// </summary>
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets the metric tags for categorization.
    /// </summary>
    public Dictionary<string, string> Tags { get; set; } = new();
    
    // Additional properties for compatibility
    /// <summary>
    /// Gets or sets the total requests count.
    /// </summary>
    public long TotalRequests { get; set; }
    
    /// <summary>
    /// Gets or sets the successful requests count.
    /// </summary>
    public long SuccessfulRequests { get; set; }
    
    /// <summary>
    /// Gets or sets the failed requests count.
    /// </summary>
    public long FailedRequests { get; set; }
    
    /// <summary>
    /// Gets or sets the success rate percentage.
    /// </summary>
    public double SuccessRate { get; set; }
    
    /// <summary>
    /// Gets or sets the average response time in milliseconds.
    /// </summary>
    public double AverageResponseTime { get; set; }
    
    /// <summary>
    /// Gets or sets the memory usage in bytes.
    /// </summary>
    public long MemoryUsage { get; set; }
    
    /// <summary>
    /// Gets or sets custom metrics dictionary.
    /// </summary>
    public Dictionary<string, object> CustomMetrics { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the CPU usage percentage.
    /// </summary>
    public double CpuUsage { get; set; }
    
    /// <summary>
    /// Gets or sets the network bytes sent.
    /// </summary>
    public long NetworkBytesSent { get; set; }
    
    /// <summary>
    /// Gets or sets the network bytes received.
    /// </summary>
    public long NetworkBytesReceived { get; set; }
}

/// <summary>
/// Represents a health alert for monitoring issues.
/// </summary>
public class HealthAlert
{
    /// <summary>
    /// Gets or sets the unique identifier for this alert.
    /// </summary>
    public string AlertId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the alert severity level.
    /// </summary>
    public AlertSeverity Severity { get; set; }
    
    /// <summary>
    /// Gets or sets the alert type.
    /// </summary>
    public AlertType Type { get; set; }
    
    /// <summary>
    /// Gets or sets the alert message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the alert description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the source that triggered this alert.
    /// </summary>
    public string Source { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets when this alert was triggered.
    /// </summary>
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets when this alert was acknowledged.
    /// </summary>
    public DateTime? AcknowledgedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the ID alias for compatibility.
    /// </summary>
    public string Id 
    { 
        get => AlertId; 
        set => AlertId = value; 
    }
    
    /// <summary>
    /// Gets or sets the alert type alias for compatibility.
    /// </summary>
    public AlertType AlertType 
    { 
        get => Type; 
        set => Type = value; 
    }
    
    /// <summary>
    /// Gets or sets the node address associated with this alert.
    /// </summary>
    public string NodeAddress { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets additional details about the alert.
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();
    
    /// <summary>
    /// Gets or sets when this alert was resolved.
    /// </summary>
    public DateTime? ResolvedAt { get; set; }
    
    /// <summary>
    /// Gets or sets additional alert data.
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();
    
    /// <summary>
    /// Gets or sets whether the alert is resolved.
    /// </summary>
    public bool IsResolved => ResolvedAt.HasValue;
    
    /// <summary>
    /// Gets or sets when the alert was created.
    /// </summary>
    public DateTime CreatedAt => TriggeredAt;
}

/// <summary>
/// Represents health threshold configuration.
/// </summary>
public class HealthThreshold
{
    /// <summary>
    /// Gets or sets the CPU usage warning threshold.
    /// </summary>
    public double CpuWarningThreshold { get; set; } = 0.7;
    
    /// <summary>
    /// Gets or sets the CPU usage critical threshold.
    /// </summary>
    public double CpuCriticalThreshold { get; set; } = 0.9;
    
    /// <summary>
    /// Gets or sets the memory usage warning threshold.
    /// </summary>
    public double MemoryWarningThreshold { get; set; } = 0.8;
    
    /// <summary>
    /// Gets or sets the memory usage critical threshold.
    /// </summary>
    public double MemoryCriticalThreshold { get; set; } = 0.95;
    
    /// <summary>
    /// Gets or sets the disk usage warning threshold.
    /// </summary>
    public double DiskWarningThreshold { get; set; } = 0.8;
    
    /// <summary>
    /// Gets or sets the disk usage critical threshold.
    /// </summary>
    public double DiskCriticalThreshold { get; set; } = 0.95;
    
    /// <summary>
    /// Gets or sets the response time warning threshold in milliseconds.
    /// </summary>
    public int ResponseTimeWarningMs { get; set; } = 1000;
    
    /// <summary>
    /// Gets or sets the response time critical threshold in milliseconds.
    /// </summary>
    public int ResponseTimeCriticalMs { get; set; } = 5000;
    
    /// <summary>
    /// Gets or sets the minimum uptime percentage.
    /// </summary>
    public double MinUptimePercentage { get; set; } = 0.99;
    
    /// <summary>
    /// Gets or sets the maximum response time in milliseconds.
    /// </summary>
    public int MaxResponseTime { get; set; } = 10000;
    
    /// <summary>
    /// Gets or sets custom threshold values.
    /// </summary>
    public Dictionary<string, double> CustomThresholds { get; set; } = new();
}

/// <summary>
/// Represents a validation check performed on the system.
/// </summary>
public class ValidationCheck
{
    /// <summary>
    /// Gets or sets the unique identifier for this validation check.
    /// </summary>
    public string CheckId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the name of the validation check.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the check type.
    /// </summary>
    public ValidationCheckType Type { get; set; }
    
    /// <summary>
    /// Gets or sets the check status.
    /// </summary>
    public CheckStatus Status { get; set; }
    
    /// <summary>
    /// Gets or sets the check result message.
    /// </summary>
    public string ResultMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets when this check was performed.
    /// </summary>
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets or sets the duration of the check.
    /// </summary>
    public TimeSpan Duration { get; set; }
    
    /// <summary>
    /// Gets or sets additional check data.
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Represents health status levels.
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// System is healthy and operating normally.
    /// </summary>
    Healthy,
    
    /// <summary>
    /// System has minor issues but is still operational.
    /// </summary>
    Warning,
    
    /// <summary>
    /// System has significant issues affecting performance.
    /// </summary>
    Critical,
    
    /// <summary>
    /// System is not operational.
    /// </summary>
    Unhealthy,
    
    /// <summary>
    /// Health status cannot be determined.
    /// </summary>
    Unknown
}

/// <summary>
/// Represents network connectivity status.
/// </summary>
public enum ConnectivityStatus
{
    /// <summary>
    /// Full connectivity to all peers.
    /// </summary>
    Connected,
    
    /// <summary>
    /// Partial connectivity to some peers.
    /// </summary>
    PartiallyConnected,
    
    /// <summary>
    /// No connectivity to any peers.
    /// </summary>
    Disconnected,
    
    /// <summary>
    /// Connectivity status is unknown.
    /// </summary>
    Unknown
}

/// <summary>
/// Represents metric types for categorization.
/// </summary>
public enum MetricType
{
    /// <summary>
    /// Counter metric that only increases.
    /// </summary>
    Counter,
    
    /// <summary>
    /// Gauge metric that can increase or decrease.
    /// </summary>
    Gauge,
    
    /// <summary>
    /// Histogram metric for distribution tracking.
    /// </summary>
    Histogram,
    
    /// <summary>
    /// Summary metric for quantile tracking.
    /// </summary>
    Summary
}

/// <summary>
/// Represents alert severity levels.
/// </summary>
public enum AlertSeverity
{
    /// <summary>
    /// Information alert.
    /// </summary>
    Info,
    
    /// <summary>
    /// Warning alert.
    /// </summary>
    Warning,
    
    /// <summary>
    /// Critical alert requiring immediate attention.
    /// </summary>
    Critical,
    
    /// <summary>
    /// Emergency alert indicating system failure.
    /// </summary>
    Emergency
}

/// <summary>
/// Represents alert types.
/// </summary>
public enum AlertType
{
    /// <summary>
    /// Performance-related alert.
    /// </summary>
    Performance,
    
    /// <summary>
    /// Security-related alert.
    /// </summary>
    Security,
    
    /// <summary>
    /// Connectivity-related alert.
    /// </summary>
    Connectivity,
    
    /// <summary>
    /// Resource usage alert.
    /// </summary>
    Resource,
    
    /// <summary>
    /// Configuration-related alert.
    /// </summary>
    Configuration,
    
    /// <summary>
    /// Data integrity alert.
    /// </summary>
    DataIntegrity
}

/// <summary>
/// Represents validation check types.
/// </summary>
public enum ValidationCheckType
{
    /// <summary>
    /// Configuration validation.
    /// </summary>
    Configuration,
    
    /// <summary>
    /// Connectivity validation.
    /// </summary>
    Connectivity,
    
    /// <summary>
    /// Security validation.
    /// </summary>
    Security,
    
    /// <summary>
    /// Performance validation.
    /// </summary>
    Performance,
    
    /// <summary>
    /// Data integrity validation.
    /// </summary>
    DataIntegrity,
    
    /// <summary>
    /// Service availability validation.
    /// </summary>
    ServiceAvailability
}

/// <summary>
/// Represents check status results.
/// </summary>
public enum CheckStatus
{
    /// <summary>
    /// Check passed successfully.
    /// </summary>
    Passed,
    
    /// <summary>
    /// Check failed.
    /// </summary>
    Failed,
    
    /// <summary>
    /// Check was skipped.
    /// </summary>
    Skipped,
    
    /// <summary>
    /// Check is currently running.
    /// </summary>
    Running,
    
    /// <summary>
    /// Check status is unknown.
    /// </summary>
    Unknown
}
