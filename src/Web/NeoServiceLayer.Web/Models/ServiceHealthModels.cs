using System.ComponentModel.DataAnnotations;

namespace NeoServiceLayer.Web.Models;

/// <summary>
/// Represents the health status of a service.
/// </summary>
public class ServiceHealthStatus
{
    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the health status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp of the health check.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the service version.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the service uptime.
    /// </summary>
    public TimeSpan Uptime { get; set; }

    /// <summary>
    /// Gets or sets the dependency status.
    /// </summary>
    public Dictionary<string, string> Dependencies { get; set; } = new();

    /// <summary>
    /// Gets or sets any health check errors.
    /// </summary>
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Represents detailed status information for a service.
/// </summary>
public class ServiceStatus
{
    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the service is healthy.
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Gets or sets the current status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the service version.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the service uptime.
    /// </summary>
    public TimeSpan Uptime { get; set; }

    /// <summary>
    /// Gets or sets the number of requests today.
    /// </summary>
    public int RequestsToday { get; set; }

    /// <summary>
    /// Gets or sets the success rate percentage.
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Gets or sets the average response time.
    /// </summary>
    public TimeSpan AverageResponseTime { get; set; }

    /// <summary>
    /// Gets or sets the number of active connections.
    /// </summary>
    public int ActiveConnections { get; set; }

    /// <summary>
    /// Gets or sets the memory usage percentage.
    /// </summary>
    public double MemoryUsage { get; set; }

    /// <summary>
    /// Gets or sets the CPU usage percentage.
    /// </summary>
    public double CpuUsage { get; set; }

    /// <summary>
    /// Gets or sets the error rate percentage.
    /// </summary>
    public double ErrorRate { get; set; }

    /// <summary>
    /// Gets or sets the last error message.
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Gets or sets the service configuration.
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Represents performance metrics for a service.
/// </summary>
public class ServiceMetrics
{
    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the requests per second.
    /// </summary>
    public double RequestsPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the average response time in milliseconds.
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// Gets or sets the error rate percentage.
    /// </summary>
    public double ErrorRate { get; set; }

    /// <summary>
    /// Gets or sets the throughput in Mbps.
    /// </summary>
    public double ThroughputMbps { get; set; }

    /// <summary>
    /// Gets or sets the number of concurrent users.
    /// </summary>
    public int ConcurrentUsers { get; set; }

    /// <summary>
    /// Gets or sets the memory usage in MB.
    /// </summary>
    public double MemoryUsageMB { get; set; }

    /// <summary>
    /// Gets or sets the CPU usage percentage.
    /// </summary>
    public double CpuUsagePercent { get; set; }

    /// <summary>
    /// Gets or sets the disk usage percentage.
    /// </summary>
    public double DiskUsagePercent { get; set; }

    /// <summary>
    /// Gets or sets the network latency in milliseconds.
    /// </summary>
    public double NetworkLatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the cache hit rate percentage.
    /// </summary>
    public double CacheHitRate { get; set; }

    /// <summary>
    /// Gets or sets the number of database connections.
    /// </summary>
    public int DatabaseConnections { get; set; }

    /// <summary>
    /// Gets or sets the queue length.
    /// </summary>
    public int QueueLength { get; set; }

    /// <summary>
    /// Gets or sets additional performance counters.
    /// </summary>
    public Dictionary<string, double> PerformanceCounters { get; set; } = new();
}

/// <summary>
/// Represents diagnostic test results for a service.
/// </summary>
public class DiagnosticResults
{
    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the overall health status.
    /// </summary>
    public string OverallHealth { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the individual test results.
    /// </summary>
    public List<DiagnosticTest> TestResults { get; set; } = new();

    /// <summary>
    /// Gets or sets recommendations for improvement.
    /// </summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// Gets or sets the total test duration.
    /// </summary>
    public TimeSpan TotalDuration => TimeSpan.FromMilliseconds(TestResults.Sum(t => t.Duration.TotalMilliseconds));

    /// <summary>
    /// Gets the number of passed tests.
    /// </summary>
    public int PassedTests => TestResults.Count(t => t.Status == "Passed");

    /// <summary>
    /// Gets the number of failed tests.
    /// </summary>
    public int FailedTests => TestResults.Count(t => t.Status == "Failed");
}

/// <summary>
/// Represents an individual diagnostic test.
/// </summary>
public class DiagnosticTest
{
    /// <summary>
    /// Gets or sets the test name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the test status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the test duration.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the test details.
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets any error message.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional test data.
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Represents a service alert.
/// </summary>
public class ServiceAlert
{
    /// <summary>
    /// Gets or sets the alert ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the alert level.
    /// </summary>
    public AlertLevel Level { get; set; }

    /// <summary>
    /// Gets or sets the alert message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the alert timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets whether the alert is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the alert source.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional alert data.
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Alert severity levels.
/// </summary>
public enum AlertLevel
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
/// Represents a service log entry.
/// </summary>
public class ServiceLogEntry
{
    /// <summary>
    /// Gets or sets the log entry ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

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
    /// Gets or sets the log source.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets any exception information.
    /// </summary>
    public string? Exception { get; set; }

    /// <summary>
    /// Gets or sets additional log data.
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Log levels for service logging.
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
