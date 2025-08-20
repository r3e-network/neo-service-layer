using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Web.Models
{
    /// <summary>
    /// View model for the dashboard page.
    /// </summary>
    public class DashboardViewModel
    {
        public int TotalServices { get; set; }
        public int HealthyServices { get; set; }
        public int UnhealthyServices { get; set; }
        public double AverageResponseTime { get; set; }
        public double RequestsPerSecond { get; set; }
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public int ActiveConnections { get; set; }
        public DateTime LastUpdated { get; set; }
        public List<ServiceCategory> ServiceCategories { get; set; }
        public PerformanceTrend PerformanceTrend { get; set; }
    }

    /// <summary>
    /// Represents the status of a single service.
    /// </summary>
    public class ServiceStatus
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public string Status { get; set; }
        public bool IsHealthy { get; set; }
        public double ResponseTime { get; set; }
        public double ErrorRate { get; set; }
        public DateTime LastCheck { get; set; }
        public string Endpoint { get; set; }
        public string Version { get; set; }
        public int Uptime { get; set; } // in minutes
        public Dictionary<string, object> Metadata { get; set; }
        public List<HealthCheckResult> HealthChecks { get; set; }
    }

    /// <summary>
    /// Health check result for a service component.
    /// </summary>
    public class HealthCheckResult
    {
        public string Component { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public TimeSpan Duration { get; set; }
        public Dictionary<string, object> Data { get; set; }
    }

    /// <summary>
    /// System-wide metrics.
    /// </summary>
    public class SystemMetrics
    {
        public double RequestsPerSecond { get; set; }
        public double AverageResponseTime { get; set; }
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public long MemoryUsedBytes { get; set; }
        public long MemoryTotalBytes { get; set; }
        public int ActiveConnections { get; set; }
        public int TotalRequests { get; set; }
        public int FailedRequests { get; set; }
        public double NetworkInMbps { get; set; }
        public double NetworkOutMbps { get; set; }
        public Dictionary<string, double> ServiceMetrics { get; set; }
        public List<MetricDataPoint> HistoricalData { get; set; }
    }

    /// <summary>
    /// Data point for time-series metrics.
    /// </summary>
    public class MetricDataPoint
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
        public string Label { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    /// <summary>
    /// Represents an alert in the system.
    /// </summary>
    public class Alert
    {
        public string Id { get; set; }
        public string ServiceName { get; set; }
        public string Message { get; set; }
        public AlertSeverity Severity { get; set; }
        public AlertType Type { get; set; }
        public DateTime TriggeredAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public bool IsActive { get; set; }
        public bool IsAcknowledged { get; set; }
        public string AcknowledgedBy { get; set; }
        public Dictionary<string, object> Details { get; set; }
        public List<string> AffectedServices { get; set; }
    }

    /// <summary>
    /// Alert severity levels.
    /// </summary>
    public enum AlertSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
        Critical = 3
    }

    /// <summary>
    /// Types of alerts.
    /// </summary>
    public enum AlertType
    {
        ServiceDown,
        HighResponseTime,
        HighErrorRate,
        ResourceExhaustion,
        SecurityThreat,
        ConfigurationChange,
        Maintenance,
        Custom
    }

    /// <summary>
    /// Activity log entry.
    /// </summary>
    public class ActivityLog
    {
        public string Id { get; set; }
        public string ServiceName { get; set; }
        public string Action { get; set; }
        public string Message { get; set; }
        public string User { get; set; }
        public string Source { get; set; }
        public ActivityType Type { get; set; }
        public DateTime Timestamp { get; set; }
        public TimeSpan? Duration { get; set; }
        public bool Success { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    /// <summary>
    /// Types of activities.
    /// </summary>
    public enum ActivityType
    {
        ServiceStart,
        ServiceStop,
        ServiceRestart,
        ConfigurationUpdate,
        HealthCheck,
        UserAction,
        SystemEvent,
        SecurityEvent,
        DataOperation,
        ApiCall
    }

    /// <summary>
    /// Service category with aggregated metrics.
    /// </summary>
    public class ServiceCategory
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int TotalServices { get; set; }
        public int HealthyServices { get; set; }
        public double AverageResponseTime { get; set; }
        public double TotalRequestsPerSecond { get; set; }
        public List<ServiceStatus> Services { get; set; }
    }

    /// <summary>
    /// Performance trend data.
    /// </summary>
    public class PerformanceTrend
    {
        public double ResponseTimeTrend { get; set; } // percentage change
        public double ErrorRateTrend { get; set; } // percentage change
        public double ThroughputTrend { get; set; } // percentage change
        public string Period { get; set; } // e.g., "last hour", "last 24 hours"
        public List<MetricDataPoint> ResponseTimeHistory { get; set; }
        public List<MetricDataPoint> ErrorRateHistory { get; set; }
        public List<MetricDataPoint> ThroughputHistory { get; set; }
    }

    /// <summary>
    /// Service restart result.
    /// </summary>
    public class ServiceRestartResult
    {
        public bool Success { get; set; }
        public string ServiceName { get; set; }
        public string Message { get; set; }
        public DateTime RestartedAt { get; set; }
        public TimeSpan DownTime { get; set; }
        public Dictionary<string, object> Details { get; set; }
    }

    /// <summary>
    /// Export configuration.
    /// </summary>
    public class ExportConfiguration
    {
        public string Format { get; set; } // json, csv, pdf, excel
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<string> IncludeServices { get; set; }
        public List<string> IncludeMetrics { get; set; }
        public bool IncludeAlerts { get; set; }
        public bool IncludeActivity { get; set; }
        public bool IncludeHealthChecks { get; set; }
    }
}
