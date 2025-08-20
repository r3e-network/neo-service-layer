using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NeoServiceLayer.Services.Monitoring
{
    /// <summary>
    /// Interface for metrics collection and reporting service.
    /// </summary>
    public interface IMetricsService
    {
        /// <summary>
        /// Records a metric value.
        /// </summary>
        Task RecordMetricAsync(string metricName, double value, Dictionary<string, string>? tags = null);

        /// <summary>
        /// Increments a counter metric.
        /// </summary>
        Task IncrementCounterAsync(string counterName, long increment = 1, Dictionary<string, string>? tags = null);

        /// <summary>
        /// Records a gauge value.
        /// </summary>
        Task RecordGaugeAsync(string gaugeName, double value, Dictionary<string, string>? tags = null);

        /// <summary>
        /// Records a histogram value.
        /// </summary>
        Task RecordHistogramAsync(string histogramName, double value, Dictionary<string, string>? tags = null);

        /// <summary>
        /// Gets current metric values.
        /// </summary>
        Task<Dictionary<string, double>> GetMetricsAsync();

        /// <summary>
        /// Resets all metrics.
        /// </summary>
        Task ResetMetricsAsync();

        /// <summary>
        /// Gets the current CPU usage percentage.
        /// </summary>
        Task<double> GetCPUUsageAsync();

        /// <summary>
        /// Gets the current memory usage percentage.
        /// </summary>
        Task<double> GetMemoryUsageAsync();

        /// <summary>
        /// Gets the average response time in milliseconds.
        /// </summary>
        Task<double> GetAverageResponseTimeAsync();
    }
}