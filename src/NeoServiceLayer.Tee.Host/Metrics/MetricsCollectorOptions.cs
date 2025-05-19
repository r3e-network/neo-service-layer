namespace NeoServiceLayer.Tee.Host.Metrics
{
    /// <summary>
    /// Options for the metrics collector.
    /// </summary>
    public class MetricsCollectorOptions
    {
        /// <summary>
        /// Gets or sets the directory where the metrics files are stored.
        /// </summary>
        public string MetricsDirectory { get; set; } = "metrics";

        /// <summary>
        /// Gets or sets the event type for metrics events.
        /// </summary>
        public string MetricsEventType { get; set; } = "Metrics";

        /// <summary>
        /// Gets or sets whether to enable periodic reporting of metrics.
        /// </summary>
        public bool EnablePeriodicReporting { get; set; } = true;

        /// <summary>
        /// Gets or sets the interval for periodic reporting in milliseconds.
        /// </summary>
        public int ReportingIntervalMs { get; set; } = 60000; // 1 minute

        /// <summary>
        /// Gets or sets whether to enable reporting metrics to a file.
        /// </summary>
        public bool EnableFileReporting { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable reporting metrics as events.
        /// </summary>
        public bool EnableEventReporting { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable reporting metrics to the host.
        /// </summary>
        public bool EnableHostReporting { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of metrics to keep.
        /// </summary>
        public int MaxMetrics { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the maximum number of metric files to keep.
        /// </summary>
        public int MaxMetricFiles { get; set; } = 10;

        /// <summary>
        /// Gets or sets whether to include tags in metrics.
        /// </summary>
        public bool IncludeTags { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include timestamps in metrics.
        /// </summary>
        public bool IncludeTimestamps { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include histogram statistics in metrics.
        /// </summary>
        public bool IncludeHistogramStats { get; set; } = true;
    }
}
