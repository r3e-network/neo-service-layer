using System;

namespace NeoServiceLayer.Infrastructure.Monitoring
{
    /// <summary>
    /// Interface for a service that collects and exposes metrics for monitoring.
    /// </summary>
    public interface IMetricsService
    {
        /// <summary>
        /// Increments a counter metric.
        /// </summary>
        /// <param name="name">The name of the counter.</param>
        /// <param name="labelValues">The label values.</param>
        void IncrementCounter(string name, params string[] labelValues);

        /// <summary>
        /// Increments a counter metric by a specified amount.
        /// </summary>
        /// <param name="name">The name of the counter.</param>
        /// <param name="value">The value to increment by.</param>
        /// <param name="labelValues">The label values.</param>
        void IncrementCounter(string name, double value, params string[] labelValues);

        /// <summary>
        /// Sets a gauge metric.
        /// </summary>
        /// <param name="name">The name of the gauge.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="labelValues">The label values.</param>
        void SetGauge(string name, double value, params string[] labelValues);

        /// <summary>
        /// Observes a histogram metric.
        /// </summary>
        /// <param name="name">The name of the histogram.</param>
        /// <param name="value">The value to observe.</param>
        /// <param name="labelValues">The label values.</param>
        void ObserveHistogram(string name, double value, params string[] labelValues);

        /// <summary>
        /// Observes a summary metric.
        /// </summary>
        /// <param name="name">The name of the summary.</param>
        /// <param name="value">The value to observe.</param>
        /// <param name="labelValues">The label values.</param>
        void ObserveSummary(string name, double value, params string[] labelValues);

        /// <summary>
        /// Creates a timer that will record the execution time of the specified action.
        /// </summary>
        /// <param name="name">The name of the histogram or summary.</param>
        /// <param name="useHistogram">Whether to use a histogram or summary.</param>
        /// <param name="labelValues">The label values.</param>
        /// <returns>A disposable timer that will record the execution time when disposed.</returns>
        IDisposable CreateTimer(string name, bool useHistogram = true, params string[] labelValues);

        /// <summary>
        /// Measures the execution time of the specified action.
        /// </summary>
        /// <param name="name">The name of the histogram or summary.</param>
        /// <param name="action">The action to measure.</param>
        /// <param name="useHistogram">Whether to use a histogram or summary.</param>
        /// <param name="labelValues">The label values.</param>
        void MeasureExecutionTime(string name, Action action, bool useHistogram = true, params string[] labelValues);

        /// <summary>
        /// Measures the execution time of the specified function.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="name">The name of the histogram or summary.</param>
        /// <param name="func">The function to measure.</param>
        /// <param name="useHistogram">Whether to use a histogram or summary.</param>
        /// <param name="labelValues">The label values.</param>
        /// <returns>The result of the function.</returns>
        T MeasureExecutionTime<T>(string name, Func<T> func, bool useHistogram = true, params string[] labelValues);

        /// <summary>
        /// Registers a counter metric.
        /// </summary>
        /// <param name="name">The name of the counter.</param>
        /// <param name="help">The help text for the counter.</param>
        /// <param name="labelNames">The label names.</param>
        void RegisterCounter(string name, string help, params string[] labelNames);

        /// <summary>
        /// Registers a gauge metric.
        /// </summary>
        /// <param name="name">The name of the gauge.</param>
        /// <param name="help">The help text for the gauge.</param>
        /// <param name="labelNames">The label names.</param>
        void RegisterGauge(string name, string help, params string[] labelNames);

        /// <summary>
        /// Registers a histogram metric.
        /// </summary>
        /// <param name="name">The name of the histogram.</param>
        /// <param name="help">The help text for the histogram.</param>
        /// <param name="labelNames">The label names.</param>
        /// <param name="buckets">The histogram buckets.</param>
        void RegisterHistogram(string name, string help, string[] labelNames, double[]? buckets = null);

        /// <summary>
        /// Registers a summary metric.
        /// </summary>
        /// <param name="name">The name of the summary.</param>
        /// <param name="help">The help text for the summary.</param>
        /// <param name="labelNames">The label names.</param>
        void RegisterSummary(string name, string help, params string[] labelNames);
    }
}
