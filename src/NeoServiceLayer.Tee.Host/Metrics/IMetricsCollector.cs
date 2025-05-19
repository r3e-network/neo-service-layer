using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NeoServiceLayer.Shared.Models.Metrics;

namespace NeoServiceLayer.Tee.Host.Metrics
{
    /// <summary>
    /// Interface for collecting and reporting metrics.
    /// </summary>
    public interface IMetricsCollector : IDisposable
    {
        /// <summary>
        /// Records a counter metric.
        /// </summary>
        /// <param name="name">The name of the metric.</param>
        /// <param name="value">The value to add to the counter.</param>
        /// <param name="tags">The tags for the metric.</param>
        void RecordCounter(string name, double value = 1, Dictionary<string, string> tags = null);

        /// <summary>
        /// Records a gauge metric.
        /// </summary>
        /// <param name="name">The name of the metric.</param>
        /// <param name="value">The value of the gauge.</param>
        /// <param name="tags">The tags for the metric.</param>
        void RecordGauge(string name, double value, Dictionary<string, string> tags = null);

        /// <summary>
        /// Records a histogram metric.
        /// </summary>
        /// <param name="name">The name of the metric.</param>
        /// <param name="value">The value to add to the histogram.</param>
        /// <param name="tags">The tags for the metric.</param>
        void RecordHistogram(string name, double value, Dictionary<string, string> tags = null);

        /// <summary>
        /// Records a timer metric.
        /// </summary>
        /// <param name="name">The name of the metric.</param>
        /// <param name="milliseconds">The time in milliseconds.</param>
        /// <param name="tags">The tags for the metric.</param>
        void RecordTimer(string name, double milliseconds, Dictionary<string, string> tags = null);

        /// <summary>
        /// Gets a metric by name.
        /// </summary>
        /// <param name="name">The name of the metric.</param>
        /// <returns>The metric, or null if the metric does not exist.</returns>
        MetricValue GetMetric(string name);

        /// <summary>
        /// Gets all metrics.
        /// </summary>
        /// <returns>A list of all metrics.</returns>
        IReadOnlyList<MetricValue> GetAllMetrics();

        /// <summary>
        /// Reports metrics to the configured outputs.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task ReportMetricsAsync();

        /// <summary>
        /// Resets all metrics.
        /// </summary>
        void ResetMetrics();
    }
}
