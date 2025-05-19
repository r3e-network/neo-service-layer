using Microsoft.Extensions.Logging;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NeoServiceLayer.Infrastructure.Monitoring
{
    /// <summary>
    /// Service for collecting and exposing metrics for monitoring.
    /// </summary>
    public class MetricsService : IMetricsService
    {
        private readonly ILogger<MetricsService> _logger;
        private readonly Dictionary<string, Counter> _counters = new Dictionary<string, Counter>();
        private readonly Dictionary<string, Gauge> _gauges = new Dictionary<string, Gauge>();
        private readonly Dictionary<string, Histogram> _histograms = new Dictionary<string, Histogram>();
        private readonly Dictionary<string, Summary> _summaries = new Dictionary<string, Summary>();

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsService"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public MetricsService(ILogger<MetricsService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize default metrics
            InitializeDefaultMetrics();

            _logger.LogInformation("Metrics service initialized");
        }

        /// <summary>
        /// Increments a counter metric.
        /// </summary>
        /// <param name="name">The name of the counter.</param>
        /// <param name="labelValues">The label values.</param>
        public void IncrementCounter(string name, params string[] labelValues)
        {
            try
            {
                if (_counters.TryGetValue(name, out var counter))
                {
                    counter.WithLabels(labelValues).Inc();
                }
                else
                {
                    _logger.LogWarning("Counter {Name} not found", name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing counter {Name}", name);
            }
        }

        /// <summary>
        /// Increments a counter metric by a specified amount.
        /// </summary>
        /// <param name="name">The name of the counter.</param>
        /// <param name="value">The value to increment by.</param>
        /// <param name="labelValues">The label values.</param>
        public void IncrementCounter(string name, double value, params string[] labelValues)
        {
            try
            {
                if (_counters.TryGetValue(name, out var counter))
                {
                    counter.WithLabels(labelValues).Inc(value);
                }
                else
                {
                    _logger.LogWarning("Counter {Name} not found", name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing counter {Name} by {Value}", name, value);
            }
        }

        /// <summary>
        /// Sets a gauge metric.
        /// </summary>
        /// <param name="name">The name of the gauge.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="labelValues">The label values.</param>
        public void SetGauge(string name, double value, params string[] labelValues)
        {
            try
            {
                if (_gauges.TryGetValue(name, out var gauge))
                {
                    gauge.WithLabels(labelValues).Set(value);
                }
                else
                {
                    _logger.LogWarning("Gauge {Name} not found", name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting gauge {Name} to {Value}", name, value);
            }
        }

        /// <summary>
        /// Observes a histogram metric.
        /// </summary>
        /// <param name="name">The name of the histogram.</param>
        /// <param name="value">The value to observe.</param>
        /// <param name="labelValues">The label values.</param>
        public void ObserveHistogram(string name, double value, params string[] labelValues)
        {
            try
            {
                if (_histograms.TryGetValue(name, out var histogram))
                {
                    histogram.WithLabels(labelValues).Observe(value);
                }
                else
                {
                    _logger.LogWarning("Histogram {Name} not found", name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error observing histogram {Name} with {Value}", name, value);
            }
        }

        /// <summary>
        /// Observes a summary metric.
        /// </summary>
        /// <param name="name">The name of the summary.</param>
        /// <param name="value">The value to observe.</param>
        /// <param name="labelValues">The label values.</param>
        public void ObserveSummary(string name, double value, params string[] labelValues)
        {
            try
            {
                if (_summaries.TryGetValue(name, out var summary))
                {
                    summary.WithLabels(labelValues).Observe(value);
                }
                else
                {
                    _logger.LogWarning("Summary {Name} not found", name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error observing summary {Name} with {Value}", name, value);
            }
        }

        /// <summary>
        /// Creates a timer that will record the execution time of the specified action.
        /// </summary>
        /// <param name="name">The name of the histogram or summary.</param>
        /// <param name="useHistogram">Whether to use a histogram or summary.</param>
        /// <param name="labelValues">The label values.</param>
        /// <returns>A disposable timer that will record the execution time when disposed.</returns>
        public IDisposable CreateTimer(string name, bool useHistogram = true, params string[] labelValues)
        {
            try
            {
                if (useHistogram && _histograms.TryGetValue(name, out var histogram))
                {
                    return histogram.WithLabels(labelValues).NewTimer();
                }
                else if (!useHistogram && _summaries.TryGetValue(name, out var summary))
                {
                    return summary.WithLabels(labelValues).NewTimer();
                }
                else
                {
                    _logger.LogWarning("{Type} {Name} not found", useHistogram ? "Histogram" : "Summary", name);
                    return new NoOpTimer();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating timer for {Type} {Name}", useHistogram ? "histogram" : "summary", name);
                return new NoOpTimer();
            }
        }

        /// <summary>
        /// Measures the execution time of the specified action.
        /// </summary>
        /// <param name="name">The name of the histogram or summary.</param>
        /// <param name="action">The action to measure.</param>
        /// <param name="useHistogram">Whether to use a histogram or summary.</param>
        /// <param name="labelValues">The label values.</param>
        public void MeasureExecutionTime(string name, Action action, bool useHistogram = true, params string[] labelValues)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                action();
            }
            finally
            {
                stopwatch.Stop();
                if (useHistogram)
                {
                    ObserveHistogram(name, stopwatch.Elapsed.TotalSeconds, labelValues);
                }
                else
                {
                    ObserveSummary(name, stopwatch.Elapsed.TotalSeconds, labelValues);
                }
            }
        }

        /// <summary>
        /// Measures the execution time of the specified function.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="name">The name of the histogram or summary.</param>
        /// <param name="func">The function to measure.</param>
        /// <param name="useHistogram">Whether to use a histogram or summary.</param>
        /// <param name="labelValues">The label values.</param>
        /// <returns>The result of the function.</returns>
        public T MeasureExecutionTime<T>(string name, Func<T> func, bool useHistogram = true, params string[] labelValues)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                return func();
            }
            finally
            {
                stopwatch.Stop();
                if (useHistogram)
                {
                    ObserveHistogram(name, stopwatch.Elapsed.TotalSeconds, labelValues);
                }
                else
                {
                    ObserveSummary(name, stopwatch.Elapsed.TotalSeconds, labelValues);
                }
            }
        }

        /// <summary>
        /// Registers a counter metric.
        /// </summary>
        /// <param name="name">The name of the counter.</param>
        /// <param name="help">The help text for the counter.</param>
        /// <param name="labelNames">The label names.</param>
        public void RegisterCounter(string name, string help, params string[] labelNames)
        {
            try
            {
                if (!_counters.ContainsKey(name))
                {
                    var counter = Prometheus.Metrics.CreateCounter(name, help, labelNames);
                    _counters[name] = counter;
                    _logger.LogInformation("Registered counter {Name}", name);
                }
                else
                {
                    _logger.LogWarning("Counter {Name} already registered", name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering counter {Name}", name);
            }
        }

        /// <summary>
        /// Registers a gauge metric.
        /// </summary>
        /// <param name="name">The name of the gauge.</param>
        /// <param name="help">The help text for the gauge.</param>
        /// <param name="labelNames">The label names.</param>
        public void RegisterGauge(string name, string help, params string[] labelNames)
        {
            try
            {
                if (!_gauges.ContainsKey(name))
                {
                    var gauge = Prometheus.Metrics.CreateGauge(name, help, labelNames);
                    _gauges[name] = gauge;
                    _logger.LogInformation("Registered gauge {Name}", name);
                }
                else
                {
                    _logger.LogWarning("Gauge {Name} already registered", name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering gauge {Name}", name);
            }
        }

        /// <summary>
        /// Registers a histogram metric.
        /// </summary>
        /// <param name="name">The name of the histogram.</param>
        /// <param name="help">The help text for the histogram.</param>
        /// <param name="labelNames">The label names.</param>
        /// <param name="buckets">The histogram buckets.</param>
        public void RegisterHistogram(string name, string help, string[] labelNames, double[]? buckets = null)
        {
            try
            {
                if (!_histograms.ContainsKey(name))
                {
                    var histogram = buckets == null
                        ? Prometheus.Metrics.CreateHistogram(name, help, labelNames)
                        : Prometheus.Metrics.CreateHistogram(name, help, new HistogramConfiguration
                        {
                            LabelNames = labelNames,
                            Buckets = buckets
                        });
                    _histograms[name] = histogram;
                    _logger.LogInformation("Registered histogram {Name}", name);
                }
                else
                {
                    _logger.LogWarning("Histogram {Name} already registered", name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering histogram {Name}", name);
            }
        }

        /// <summary>
        /// Registers a summary metric.
        /// </summary>
        /// <param name="name">The name of the summary.</param>
        /// <param name="help">The help text for the summary.</param>
        /// <param name="labelNames">The label names.</param>
        public void RegisterSummary(string name, string help, params string[] labelNames)
        {
            try
            {
                if (!_summaries.ContainsKey(name))
                {
                    var summary = Prometheus.Metrics.CreateSummary(name, help, labelNames);
                    _summaries[name] = summary;
                    _logger.LogInformation("Registered summary {Name}", name);
                }
                else
                {
                    _logger.LogWarning("Summary {Name} already registered", name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering summary {Name}", name);
            }
        }

        private void InitializeDefaultMetrics()
        {
            // Register default counters
            RegisterCounter("nsl_api_requests_total", "Total number of API requests", "method", "path", "status_code");
            RegisterCounter("nsl_blockchain_requests_total", "Total number of blockchain requests", "method", "status");
            RegisterCounter("nsl_tee_requests_total", "Total number of TEE requests", "type", "status");
            RegisterCounter("nsl_events_total", "Total number of events", "type", "status");
            RegisterCounter("nsl_errors_total", "Total number of errors", "service", "type");

            // Register default gauges
            RegisterGauge("nsl_active_connections", "Number of active connections");
            RegisterGauge("nsl_active_tasks", "Number of active tasks", "type");
            RegisterGauge("nsl_blockchain_height", "Current blockchain height");
            RegisterGauge("nsl_memory_usage_bytes", "Memory usage in bytes");
            RegisterGauge("nsl_cpu_usage_percent", "CPU usage percentage");

            // Register default histograms
            RegisterHistogram("nsl_request_duration_seconds", "Request duration in seconds", new[] { "method", "path" });
            RegisterHistogram("nsl_blockchain_request_duration_seconds", "Blockchain request duration in seconds", new[] { "method" });
            RegisterHistogram("nsl_tee_request_duration_seconds", "TEE request duration in seconds", new[] { "type" });
            RegisterHistogram("nsl_event_processing_duration_seconds", "Event processing duration in seconds", new[] { "type" });

            // Register default summaries
            RegisterSummary("nsl_response_size_bytes", "Response size in bytes", "method", "path");
        }

        /// <summary>
        /// A no-op timer that does nothing when disposed.
        /// </summary>
        private class NoOpTimer : IDisposable
        {
            public void Dispose()
            {
                // Do nothing
            }
        }
    }
}
