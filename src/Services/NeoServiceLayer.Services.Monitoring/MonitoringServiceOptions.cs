using System;
using System.Collections.Generic;

namespace NeoServiceLayer.Services.Monitoring
{
    /// <summary>
    /// Configuration options for the monitoring service.
    /// </summary>
    public class MonitoringServiceOptions
    {
        /// <summary>
        /// Gets or sets whether monitoring is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the metrics collection interval.
        /// </summary>
        public TimeSpan MetricsInterval { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets the health check interval.
        /// </summary>
        public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the alert threshold settings.
        /// </summary>
        public AlertThresholds Thresholds { get; set; } = new();

        /// <summary>
        /// Gets or sets the retention period for metrics.
        /// </summary>
        public TimeSpan MetricsRetention { get; set; } = TimeSpan.FromDays(7);

        /// <summary>
        /// Gets or sets the retention period for alerts.
        /// </summary>
        public TimeSpan AlertsRetention { get; set; } = TimeSpan.FromDays(30);

        /// <summary>
        /// Gets or sets whether to enable performance monitoring.
        /// </summary>
        public bool EnablePerformanceMonitoring { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable distributed tracing.
        /// </summary>
        public bool EnableDistributedTracing { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of concurrent health checks.
        /// </summary>
        public int MaxConcurrentHealthChecks { get; set; } = 10;

        /// <summary>
        /// Gets or sets custom tags to apply to all metrics.
        /// </summary>
        public Dictionary<string, string> GlobalTags { get; set; } = new();
    }

    /// <summary>
    /// Alert threshold settings.
    /// </summary>
    public class AlertThresholds
    {
        /// <summary>
        /// Gets or sets the CPU usage threshold percentage.
        /// </summary>
        public double CpuThreshold { get; set; } = 80.0;

        /// <summary>
        /// Gets or sets the memory usage threshold percentage.
        /// </summary>
        public double MemoryThreshold { get; set; } = 85.0;

        /// <summary>
        /// Gets or sets the disk usage threshold percentage.
        /// </summary>
        public double DiskThreshold { get; set; } = 90.0;

        /// <summary>
        /// Gets or sets the error rate threshold percentage.
        /// </summary>
        public double ErrorRateThreshold { get; set; } = 5.0;

        /// <summary>
        /// Gets or sets the response time threshold in milliseconds.
        /// </summary>
        public int ResponseTimeThreshold { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the minimum duration before triggering an alert.
        /// </summary>
        public TimeSpan MinAlertDuration { get; set; } = TimeSpan.FromMinutes(2);
    }
}