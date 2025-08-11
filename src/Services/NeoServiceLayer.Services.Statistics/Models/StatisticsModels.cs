using System;
using System.Collections.Generic;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Services.Statistics.Models;

/// <summary>
/// Overall system statistics.
/// </summary>
public class SystemStatistics
{
    /// <summary>
    /// Total number of active services.
    /// </summary>
    public int ActiveServices { get; set; }

    /// <summary>
    /// Total number of healthy services.
    /// </summary>
    public int HealthyServices { get; set; }

    /// <summary>
    /// System uptime in seconds.
    /// </summary>
    public long UptimeSeconds { get; set; }

    /// <summary>
    /// Total operations processed.
    /// </summary>
    public long TotalOperations { get; set; }

    /// <summary>
    /// Total operations succeeded.
    /// </summary>
    public long SuccessfulOperations { get; set; }

    /// <summary>
    /// Overall success rate percentage.
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Average response time in milliseconds.
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// Current memory usage in MB.
    /// </summary>
    public long MemoryUsageMB { get; set; }

    /// <summary>
    /// Current CPU usage percentage.
    /// </summary>
    public double CpuUsagePercent { get; set; }

    /// <summary>
    /// Number of active SGX enclaves.
    /// </summary>
    public int ActiveEnclaves { get; set; }

    /// <summary>
    /// Total SGX operations.
    /// </summary>
    public long TotalSGXOperations { get; set; }

    /// <summary>
    /// Timestamp of the statistics.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Statistics for a specific service.
/// </summary>
public class ServiceStatistics
{
    /// <summary>
    /// Service name.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Service status.
    /// </summary>
    public ServiceStatus Status { get; set; }

    /// <summary>
    /// Service health.
    /// </summary>
    public ServiceHealth Health { get; set; }

    /// <summary>
    /// Service uptime in seconds.
    /// </summary>
    public long UptimeSeconds { get; set; }

    /// <summary>
    /// Total operations processed.
    /// </summary>
    public long TotalOperations { get; set; }

    /// <summary>
    /// Successful operations.
    /// </summary>
    public long SuccessfulOperations { get; set; }

    /// <summary>
    /// Failed operations.
    /// </summary>
    public long FailedOperations { get; set; }

    /// <summary>
    /// Success rate percentage.
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Average response time in milliseconds.
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// P99 response time in milliseconds.
    /// </summary>
    public double P99ResponseTime { get; set; }

    /// <summary>
    /// Operations per second.
    /// </summary>
    public double OperationsPerSecond { get; set; }

    /// <summary>
    /// Memory usage in MB.
    /// </summary>
    public long MemoryUsageMB { get; set; }

    /// <summary>
    /// Number of active connections.
    /// </summary>
    public int ActiveConnections { get; set; }

    /// <summary>
    /// SGX enclave usage statistics.
    /// </summary>
    public EnclaveStatistics? EnclaveStats { get; set; }

    /// <summary>
    /// Last updated timestamp.
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Operation breakdown by type.
    /// </summary>
    public Dictionary<string, OperationMetrics> OperationBreakdown { get; set; } = new();
}

/// <summary>
/// Service status enumeration.
/// </summary>
public enum ServiceStatus
{
    /// <summary>Service is stopped.</summary>
    Stopped,
    /// <summary>Service is starting.</summary>
    Starting,
    /// <summary>Service is running.</summary>
    Running,
    /// <summary>Service is stopping.</summary>
    Stopping,
    /// <summary>Service has failed.</summary>
    Failed
}


/// <summary>
/// Metrics for a specific operation type.
/// </summary>
public class OperationMetrics
{
    /// <summary>
    /// Operation name.
    /// </summary>
    public string OperationName { get; set; } = string.Empty;

    /// <summary>
    /// Total count.
    /// </summary>
    public long Count { get; set; }

    /// <summary>
    /// Success count.
    /// </summary>
    public long SuccessCount { get; set; }

    /// <summary>
    /// Average duration in milliseconds.
    /// </summary>
    public double AverageDuration { get; set; }

    /// <summary>
    /// Maximum duration in milliseconds.
    /// </summary>
    public double MaxDuration { get; set; }

    /// <summary>
    /// Minimum duration in milliseconds.
    /// </summary>
    public double MinDuration { get; set; }
}

/// <summary>
/// Blockchain-specific statistics.
/// </summary>
public class BlockchainStatistics
{
    /// <summary>
    /// Blockchain type.
    /// </summary>
    public BlockchainType BlockchainType { get; set; }

    /// <summary>
    /// Total transactions processed.
    /// </summary>
    public long TotalTransactions { get; set; }

    /// <summary>
    /// Successful transactions.
    /// </summary>
    public long SuccessfulTransactions { get; set; }

    /// <summary>
    /// Failed transactions.
    /// </summary>
    public long FailedTransactions { get; set; }

    /// <summary>
    /// Average gas used.
    /// </summary>
    public double AverageGasUsed { get; set; }

    /// <summary>
    /// Total gas consumed.
    /// </summary>
    public long TotalGasConsumed { get; set; }

    /// <summary>
    /// Average confirmation time in seconds.
    /// </summary>
    public double AverageConfirmationTime { get; set; }

    /// <summary>
    /// Transaction breakdown by type.
    /// </summary>
    public Dictionary<string, long> TransactionsByType { get; set; } = new();

    /// <summary>
    /// Current block height.
    /// </summary>
    public long CurrentBlockHeight { get; set; }

    /// <summary>
    /// Network status.
    /// </summary>
    public string NetworkStatus { get; set; } = "Connected";

    /// <summary>
    /// Last updated timestamp.
    /// </summary>
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Performance metrics for a time range.
/// </summary>
public class PerformanceMetrics
{
    /// <summary>
    /// Start time of the metrics period.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// End time of the metrics period.
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Average CPU usage percentage.
    /// </summary>
    public double AverageCpuUsage { get; set; }

    /// <summary>
    /// Peak CPU usage percentage.
    /// </summary>
    public double PeakCpuUsage { get; set; }

    /// <summary>
    /// Average memory usage in MB.
    /// </summary>
    public double AverageMemoryUsage { get; set; }

    /// <summary>
    /// Peak memory usage in MB.
    /// </summary>
    public double PeakMemoryUsage { get; set; }

    /// <summary>
    /// Total requests processed.
    /// </summary>
    public long TotalRequests { get; set; }

    /// <summary>
    /// Requests per second.
    /// </summary>
    public double RequestsPerSecond { get; set; }

    /// <summary>
    /// Average latency in milliseconds.
    /// </summary>
    public double AverageLatency { get; set; }

    /// <summary>
    /// P50 latency in milliseconds.
    /// </summary>
    public double P50Latency { get; set; }

    /// <summary>
    /// P95 latency in milliseconds.
    /// </summary>
    public double P95Latency { get; set; }

    /// <summary>
    /// P99 latency in milliseconds.
    /// </summary>
    public double P99Latency { get; set; }

    /// <summary>
    /// Error rate percentage.
    /// </summary>
    public double ErrorRate { get; set; }

    /// <summary>
    /// Service availability percentage.
    /// </summary>
    public double AvailabilityPercent { get; set; }
}

/// <summary>
/// SGX enclave statistics.
/// </summary>
public class EnclaveStatistics
{
    /// <summary>
    /// Number of active enclaves.
    /// </summary>
    public int ActiveEnclaves { get; set; }

    /// <summary>
    /// Total enclave operations.
    /// </summary>
    public long TotalOperations { get; set; }

    /// <summary>
    /// Average enclave call duration in microseconds.
    /// </summary>
    public double AverageCallDuration { get; set; }

    /// <summary>
    /// Enclave memory usage in MB.
    /// </summary>
    public long MemoryUsageMB { get; set; }

    /// <summary>
    /// Number of sealed data items.
    /// </summary>
    public long SealedDataCount { get; set; }

    /// <summary>
    /// Total sealed data size in MB.
    /// </summary>
    public long SealedDataSizeMB { get; set; }

    /// <summary>
    /// Remote attestation count.
    /// </summary>
    public long RemoteAttestationCount { get; set; }

    /// <summary>
    /// JavaScript execution count.
    /// </summary>
    public long JavaScriptExecutions { get; set; }
}

/// <summary>
/// Real-time statistics update.
/// </summary>
public class StatisticsUpdate
{
    /// <summary>
    /// Update type.
    /// </summary>
    public UpdateType Type { get; set; }

    /// <summary>
    /// Service name (if service-specific).
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Metric name.
    /// </summary>
    public string MetricName { get; set; } = string.Empty;

    /// <summary>
    /// Metric value.
    /// </summary>
    public object Value { get; set; } = new();

    /// <summary>
    /// Timestamp of the update.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Statistics update type.
/// </summary>
public enum UpdateType
{
    /// <summary>System-wide update.</summary>
    System,
    /// <summary>Service-specific update.</summary>
    Service,
    /// <summary>Blockchain-specific update.</summary>
    Blockchain,
    /// <summary>Performance update.</summary>
    Performance,
    /// <summary>Alert or warning.</summary>
    Alert
}