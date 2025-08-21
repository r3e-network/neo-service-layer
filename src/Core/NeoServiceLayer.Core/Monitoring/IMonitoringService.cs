using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Core.Monitoring
{
    /// <summary>
    /// Comprehensive monitoring service for Neo Service Layer
    /// Provides metrics collection, alerting, and observability features
    /// </summary>
    public interface IMonitoringService
    {
        /// <summary>
        /// Records a metric value
        /// </summary>
        /// <param name="metricName">Metric name</param>
        /// <param name="value">Metric value</param>
        /// <param name="tags">Metric tags</param>
        /// <param name="timestamp">Optional timestamp (defaults to now)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Metric recording result</returns>
        Task<MetricResult> RecordMetricAsync(
            string metricName,
            double value,
            Dictionary<string, string>? tags = null,
            DateTime? timestamp = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Records multiple metrics in batch
        /// </summary>
        /// <param name="metrics">Metrics to record</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Batch metric recording result</returns>
        Task<BatchMetricResult> RecordMetricsBatchAsync(
            IEnumerable<MetricData> metrics,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Increments a counter metric
        /// </summary>
        /// <param name="counterName">Counter name</param>
        /// <param name="increment">Increment value</param>
        /// <param name="tags">Counter tags</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Counter increment result</returns>
        Task<MetricResult> IncrementCounterAsync(
            string counterName,
            double increment = 1.0,
            Dictionary<string, string>? tags = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Records a histogram/distribution value
        /// </summary>
        /// <param name="histogramName">Histogram name</param>
        /// <param name="value">Value to record</param>
        /// <param name="tags">Histogram tags</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Histogram recording result</returns>
        Task<MetricResult> RecordHistogramAsync(
            string histogramName,
            double value,
            Dictionary<string, string>? tags = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets a gauge metric value
        /// </summary>
        /// <param name="gaugeName">Gauge name</param>
        /// <param name="value">Gauge value</param>
        /// <param name="tags">Gauge tags</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Gauge setting result</returns>
        Task<MetricResult> SetGaugeAsync(
            string gaugeName,
            double value,
            Dictionary<string, string>? tags = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Times an operation and records the duration
        /// </summary>
        /// <param name="timerName">Timer name</param>
        /// <param name="operation">Operation to time</param>
        /// <param name="tags">Timer tags</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Timed operation result</returns>
        Task<TimedOperationResult<T>> TimeOperationAsync<T>(
            string timerName,
            Func<CancellationToken, Task<T>> operation,
            Dictionary<string, string>? tags = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a custom timer for manual timing
        /// </summary>
        /// <param name="timerName">Timer name</param>
        /// <param name="tags">Timer tags</param>
        /// <returns>Custom timer instance</returns>
        ICustomTimer CreateTimer(string timerName, Dictionary<string, string>? tags = null);

        /// <summary>
        /// Records an event
        /// </summary>
        /// <param name="eventName">Event name</param>
        /// <param name="eventData">Event data</param>
        /// <param name="severity">Event severity</param>
        /// <param name="tags">Event tags</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Event recording result</returns>
        Task<EventResult> RecordEventAsync(
            string eventName,
            object? eventData = null,
            EventSeverity severity = EventSeverity.Info,
            Dictionary<string, string>? tags = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Records an exception event
        /// </summary>
        /// <param name="exception">Exception to record</param>
        /// <param name="context">Additional context</param>
        /// <param name="tags">Event tags</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Exception recording result</returns>
        Task<EventResult> RecordExceptionAsync(
            Exception exception,
            string? context = null,
            Dictionary<string, string>? tags = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates an alert rule
        /// </summary>
        /// <param name="alertRule">Alert rule definition</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Alert rule creation result</returns>
        Task<AlertRuleResult> CreateAlertRuleAsync(
            AlertRule alertRule,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing alert rule
        /// </summary>
        /// <param name="ruleId">Alert rule ID</param>
        /// <param name="alertRule">Updated alert rule</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Alert rule update result</returns>
        Task<AlertRuleResult> UpdateAlertRuleAsync(
            string ruleId,
            AlertRule alertRule,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an alert rule
        /// </summary>
        /// <param name="ruleId">Alert rule ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Alert rule deletion result</returns>
        Task<AlertRuleResult> DeleteAlertRuleAsync(
            string ruleId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets active alerts
        /// </summary>
        /// <param name="filters">Optional filters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Active alerts</returns>
        Task<AlertsResult> GetActiveAlertsAsync(
            AlertFilters? filters = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Acknowledges an alert
        /// </summary>
        /// <param name="alertId">Alert ID</param>
        /// <param name="acknowledgedBy">Who acknowledged the alert</param>
        /// <param name="notes">Acknowledgment notes</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Alert acknowledgment result</returns>
        Task<AlertAcknowledgmentResult> AcknowledgeAlertAsync(
            string alertId,
            string acknowledgedBy,
            string? notes = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Queries metrics data
        /// </summary>
        /// <param name="query">Metric query</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Query result</returns>
        Task<MetricQueryResult> QueryMetricsAsync(
            MetricQuery query,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets dashboard data
        /// </summary>
        /// <param name="dashboardId">Dashboard ID</param>
        /// <param name="timeRange">Time range for data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dashboard data</returns>
        Task<DashboardData> GetDashboardDataAsync(
            string dashboardId,
            TimeRange timeRange,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a custom dashboard
        /// </summary>
        /// <param name="dashboard">Dashboard definition</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dashboard creation result</returns>
        Task<DashboardResult> CreateDashboardAsync(
            Dashboard dashboard,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets service health metrics
        /// </summary>
        /// <param name="serviceNames">Optional service names filter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Service health information</returns>
        Task<ServiceHealthResult> GetServiceHealthAsync(
            IEnumerable<string>? serviceNames = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets monitoring service statistics
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Service statistics</returns>
        Task<MonitoringStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets monitoring service health
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Service health</returns>
        Task<MonitoringHealth> GetHealthAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Exports metrics data
        /// </summary>
        /// <param name="exportRequest">Export request parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Export result</returns>
        Task<MetricExportResult> ExportMetricsAsync(
            MetricExportRequest exportRequest,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a synthetic monitor
        /// </summary>
        /// <param name="monitor">Synthetic monitor definition</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Monitor creation result</returns>
        Task<SyntheticMonitorResult> CreateSyntheticMonitorAsync(
            SyntheticMonitor monitor,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets synthetic monitor results
        /// </summary>
        /// <param name="monitorId">Monitor ID</param>
        /// <param name="timeRange">Time range</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Monitor results</returns>
        Task<SyntheticMonitorResults> GetSyntheticMonitorResultsAsync(
            string monitorId,
            TimeRange timeRange,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Custom timer for manual timing operations
    /// </summary>
    public interface ICustomTimer : IDisposable
    {
        /// <summary>
        /// Timer name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Timer tags
        /// </summary>
        Dictionary<string, string> Tags { get; }

        /// <summary>
        /// Whether the timer is running
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Elapsed time since start
        /// </summary>
        TimeSpan Elapsed { get; }

        /// <summary>
        /// Starts the timer
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the timer and records the duration
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Timer result</returns>
        Task<TimerResult> StopAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Records an intermediate measurement without stopping
        /// </summary>
        /// <param name="label">Measurement label</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Intermediate measurement result</returns>
        Task<TimerResult> RecordIntermediateAsync(
            string label,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Metric data for batch operations
    /// </summary>
    public class MetricData
    {
        /// <summary>
        /// Metric name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Metric type
        /// </summary>
        public MetricType Type { get; set; }

        /// <summary>
        /// Metric value
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Metric tags
        /// </summary>
        public Dictionary<string, string> Tags { get; set; } = new();

        /// <summary>
        /// Metric timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Alert rule definition
    /// </summary>
    public class AlertRule
    {
        /// <summary>
        /// Alert rule ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Alert rule name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Alert description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Metric query for the alert
        /// </summary>
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// Alert condition
        /// </summary>
        public AlertCondition Condition { get; set; } = new();

        /// <summary>
        /// Alert severity
        /// </summary>
        public AlertSeverity Severity { get; set; } = AlertSeverity.Medium;

        /// <summary>
        /// Alert evaluation frequency
        /// </summary>
        public TimeSpan EvaluationFrequency { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Time window for evaluation
        /// </summary>
        public TimeSpan EvaluationWindow { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Whether the alert is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Alert tags
        /// </summary>
        public Dictionary<string, string> Tags { get; set; } = new();

        /// <summary>
        /// Notification channels
        /// </summary>
        public List<string> NotificationChannels { get; set; } = new();

        /// <summary>
        /// Alert actions
        /// </summary>
        public List<AlertAction> Actions { get; set; } = new();
    }

    /// <summary>
    /// Alert condition definition
    /// </summary>
    public class AlertCondition
    {
        /// <summary>
        /// Condition operator
        /// </summary>
        public AlertOperator Operator { get; set; } = AlertOperator.GreaterThan;

        /// <summary>
        /// Threshold value
        /// </summary>
        public double Threshold { get; set; }

        /// <summary>
        /// Duration the condition must be true
        /// </summary>
        public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Aggregation function
        /// </summary>
        public AggregationFunction Aggregation { get; set; } = AggregationFunction.Average;
    }

    /// <summary>
    /// Alert action definition
    /// </summary>
    public class AlertAction
    {
        /// <summary>
        /// Action type
        /// </summary>
        public AlertActionType Type { get; set; }

        /// <summary>
        /// Action configuration
        /// </summary>
        public Dictionary<string, object> Configuration { get; set; } = new();

        /// <summary>
        /// Whether the action is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// Metric query definition
    /// </summary>
    public class MetricQuery
    {
        /// <summary>
        /// Query expression
        /// </summary>
        public string Expression { get; set; } = string.Empty;

        /// <summary>
        /// Time range for the query
        /// </summary>
        public TimeRange TimeRange { get; set; } = new();

        /// <summary>
        /// Aggregation function
        /// </summary>
        public AggregationFunction? Aggregation { get; set; }

        /// <summary>
        /// Group by fields
        /// </summary>
        public List<string> GroupBy { get; set; } = new();

        /// <summary>
        /// Filters to apply
        /// </summary>
        public Dictionary<string, string> Filters { get; set; } = new();

        /// <summary>
        /// Maximum number of results
        /// </summary>
        public int? Limit { get; set; }

        /// <summary>
        /// Results offset
        /// </summary>
        public int? Offset { get; set; }
    }

    /// <summary>
    /// Time range specification
    /// </summary>
    public class TimeRange
    {
        /// <summary>
        /// Start time
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// End time
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Creates a time range from now back by specified duration
        /// </summary>
        /// <param name="duration">Duration to go back</param>
        /// <returns>Time range</returns>
        public static TimeRange FromDuration(TimeSpan duration)
        {
            var now = DateTime.UtcNow;
            return new TimeRange { StartTime = now - duration, EndTime = now };
        }

        /// <summary>
        /// Creates a time range for the last N minutes
        /// </summary>
        /// <param name="minutes">Number of minutes</param>
        /// <returns>Time range</returns>
        public static TimeRange LastMinutes(int minutes) => FromDuration(TimeSpan.FromMinutes(minutes));

        /// <summary>
        /// Creates a time range for the last N hours
        /// </summary>
        /// <param name="hours">Number of hours</param>
        /// <returns>Time range</returns>
        public static TimeRange LastHours(int hours) => FromDuration(TimeSpan.FromHours(hours));

        /// <summary>
        /// Creates a time range for the last N days
        /// </summary>
        /// <param name="days">Number of days</param>
        /// <returns>Time range</returns>
        public static TimeRange LastDays(int days) => FromDuration(TimeSpan.FromDays(days));
    }

    /// <summary>
    /// Dashboard definition
    /// </summary>
    public class Dashboard
    {
        /// <summary>
        /// Dashboard ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Dashboard name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Dashboard description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Dashboard panels
        /// </summary>
        public List<DashboardPanel> Panels { get; set; } = new();

        /// <summary>
        /// Dashboard tags
        /// </summary>
        public Dictionary<string, string> Tags { get; set; } = new();

        /// <summary>
        /// Dashboard variables
        /// </summary>
        public List<DashboardVariable> Variables { get; set; } = new();

        /// <summary>
        /// Refresh interval
        /// </summary>
        public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromMinutes(1);
    }

    /// <summary>
    /// Dashboard panel definition
    /// </summary>
    public class DashboardPanel
    {
        /// <summary>
        /// Panel ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Panel title
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Panel type
        /// </summary>
        public PanelType Type { get; set; }

        /// <summary>
        /// Panel queries
        /// </summary>
        public List<MetricQuery> Queries { get; set; } = new();

        /// <summary>
        /// Panel position and size
        /// </summary>
        public PanelLayout Layout { get; set; } = new();

        /// <summary>
        /// Panel configuration
        /// </summary>
        public Dictionary<string, object> Configuration { get; set; } = new();
    }

    /// <summary>
    /// Synthetic monitor definition
    /// </summary>
    public class SyntheticMonitor
    {
        /// <summary>
        /// Monitor ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Monitor name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Monitor type
        /// </summary>
        public SyntheticMonitorType Type { get; set; }

        /// <summary>
        /// Monitor configuration
        /// </summary>
        public SyntheticMonitorConfig Configuration { get; set; } = new();

        /// <summary>
        /// Monitoring frequency
        /// </summary>
        public TimeSpan Frequency { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Monitor locations
        /// </summary>
        public List<string> Locations { get; set; } = new();

        /// <summary>
        /// Whether the monitor is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Monitor tags
        /// </summary>
        public Dictionary<string, string> Tags { get; set; } = new();
    }

    /// <summary>
    /// Various enums for monitoring
    /// </summary>
    public enum MetricType
    {
        Counter,
        Gauge,
        Histogram,
        Timer
    }

    public enum EventSeverity
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    public enum AlertSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum AlertOperator
    {
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Equal,
        NotEqual
    }

    public enum AggregationFunction
    {
        Sum,
        Average,
        Min,
        Max,
        Count,
        Percentile95,
        Percentile99
    }

    public enum AlertActionType
    {
        Email,
        Webhook,
        PagerDuty,
        Slack,
        SMS
    }

    public enum PanelType
    {
        LineChart,
        BarChart,
        Gauge,
        SingleValue,
        Table,
        Heatmap
    }

    public enum SyntheticMonitorType
    {
        Http,
        Tcp,
        Dns,
        Icmp,
        Script
    }

    public enum HealthStatus
    {
        Healthy,
        Degraded,
        Unhealthy,
        Unknown
    }

    /// <summary>
    /// Result classes
    /// </summary>
    public class MetricResult
    {
        public bool Success { get; set; }
        public string MetricName { get; set; } = string.Empty;
        public DateTime RecordedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class BatchMetricResult
    {
        public bool Success { get; set; }
        public int TotalMetrics { get; set; }
        public int SuccessfulMetrics { get; set; }
        public int FailedMetrics { get; set; }
        public List<string> Errors { get; set; } = new();
        public DateTime RecordedAt { get; set; }
    }

    public class TimedOperationResult<T>
    {
        public bool Success { get; set; }
        public T? Result { get; set; }
        public TimeSpan Duration { get; set; }
        public string? ErrorMessage { get; set; }
        public Exception? Exception { get; set; }
    }

    public class EventResult
    {
        public bool Success { get; set; }
        public string EventId { get; set; } = string.Empty;
        public DateTime RecordedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class AlertRuleResult
    {
        public bool Success { get; set; }
        public string RuleId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class AlertsResult
    {
        public bool Success { get; set; }
        public List<ActiveAlert> Alerts { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public class AlertAcknowledgmentResult
    {
        public bool Success { get; set; }
        public string AlertId { get; set; } = string.Empty;
        public DateTime AcknowledgedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class MetricQueryResult
    {
        public bool Success { get; set; }
        public List<MetricDataPoint> DataPoints { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public class TimerResult
    {
        public bool Success { get; set; }
        public string TimerName { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public DateTime RecordedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Supporting classes
    /// </summary>
    public class ActiveAlert
    {
        public string Id { get; set; } = string.Empty;
        public string RuleId { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public AlertSeverity Severity { get; set; }
        public DateTime TriggeredAt { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public string? AcknowledgedBy { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, string> Tags { get; set; } = new();
    }

    public class MetricDataPoint
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
        public Dictionary<string, string> Tags { get; set; } = new();
    }

    public class DashboardData
    {
        public string DashboardId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<PanelData> Panels { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    public class PanelData
    {
        public string PanelId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public List<MetricQueryResult> QueryResults { get; set; } = new();
    }

    public class DashboardResult
    {
        public bool Success { get; set; }
        public string DashboardId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class ServiceHealthResult
    {
        public bool Success { get; set; }
        public List<ServiceHealthInfo> Services { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public class ServiceHealthInfo
    {
        public string ServiceName { get; set; } = string.Empty;
        public HealthStatus Status { get; set; }
        public double ResponseTime { get; set; }
        public double ErrorRate { get; set; }
        public double Availability { get; set; }
        public DateTime LastChecked { get; set; }
        public Dictionary<string, object> Metrics { get; set; } = new();
    }

    public class MonitoringStatistics
    {
        public long TotalMetricsRecorded { get; set; }
        public long TotalEventsRecorded { get; set; }
        public long ActiveAlertRules { get; set; }
        public long ActiveAlerts { get; set; }
        public long TotalDashboards { get; set; }
        public TimeSpan Uptime { get; set; }
        public double MetricsPerSecond { get; set; }
        public DateTime CollectedAt { get; set; }
    }

    public class MonitoringHealth
    {
        public HealthStatus Status { get; set; }
        public bool MetricsIngestionHealthy { get; set; }
        public bool AlertingHealthy { get; set; }
        public bool StorageHealthy { get; set; }
        public double MemoryUsagePercent { get; set; }
        public double CpuUsagePercent { get; set; }
        public TimeSpan Uptime { get; set; }
        public DateTime LastHealthCheck { get; set; }
        public List<string> Issues { get; set; } = new();
        public Dictionary<string, object> Details { get; set; } = new();
    }

    // Additional supporting classes and options
    public class AlertFilters
    {
        public AlertSeverity? Severity { get; set; }
        public string? RuleName { get; set; }
        public Dictionary<string, string>? Tags { get; set; }
        public DateTime? Since { get; set; }
    }

    public class DashboardVariable
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public object DefaultValue { get; set; } = null!;
        public List<object> Options { get; set; } = new();
    }

    public class PanelLayout
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class SyntheticMonitorConfig
    {
        public string Url { get; set; } = string.Empty;
        public int TimeoutMs { get; set; } = 30000;
        public Dictionary<string, string> Headers { get; set; } = new();
        public string? Body { get; set; }
        public List<SyntheticAssertion> Assertions { get; set; } = new();
    }

    public class SyntheticAssertion
    {
        public string Type { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public object Value { get; set; } = null!;
    }

    public class SyntheticMonitorResult
    {
        public bool Success { get; set; }
        public string MonitorId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class SyntheticMonitorResults
    {
        public bool Success { get; set; }
        public string MonitorId { get; set; } = string.Empty;
        public List<SyntheticTestResult> Results { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public class SyntheticTestResult
    {
        public DateTime Timestamp { get; set; }
        public bool Success { get; set; }
        public double ResponseTime { get; set; }
        public string Location { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> Metrics { get; set; } = new();
    }

    public class MetricExportRequest
    {
        public List<string> MetricNames { get; set; } = new();
        public TimeRange TimeRange { get; set; } = new();
        public ExportFormat Format { get; set; } = ExportFormat.Json;
        public Dictionary<string, string> Filters { get; set; } = new();
        public bool IncludeMetadata { get; set; } = true;
    }

    public class MetricExportResult
    {
        public bool Success { get; set; }
        public string ExportId { get; set; } = string.Empty;
        public string? DownloadUrl { get; set; }
        public long RecordCount { get; set; }
        public long FileSizeBytes { get; set; }
        public DateTime ExportedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public enum ExportFormat
    {
        Json,
        Csv,
        Parquet,
        Avro
    }
}