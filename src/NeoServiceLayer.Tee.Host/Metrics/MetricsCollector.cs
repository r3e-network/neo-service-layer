using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Shared.Models.Metrics;
using NeoServiceLayer.Tee.Host.Events;
using NeoServiceLayer.Tee.Host.Exceptions;
using NeoServiceLayer.Tee.Shared.Interfaces;

namespace NeoServiceLayer.Tee.Host.Metrics
{
    /// <summary>
    /// Collects and reports metrics for enclaves.
    /// </summary>
    public class MetricsCollector : IMetricsCollector, IDisposable
    {
        private readonly ILogger<MetricsCollector> _logger;
        private readonly IOcclumInterface _occlumInterface;
        private readonly IEnclaveEventSystem _eventSystem;
        private readonly MetricsCollectorOptions _options;
        private readonly ConcurrentDictionary<string, MetricValue> _metrics;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _reportingTask;
        private readonly SemaphoreSlim _fileSemaphore;
        private readonly Stopwatch _uptime;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsCollector"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for logging information and errors.</param>
        /// <param name="occlumInterface">The Occlum interface to use for secure operations.</param>
        /// <param name="eventSystem">The event system to use for publishing metric events.</param>
        /// <param name="options">The options for the metrics collector.</param>
        public MetricsCollector(
            ILogger<MetricsCollector> logger,
            IOcclumInterface occlumInterface,
            IEnclaveEventSystem eventSystem,
            MetricsCollectorOptions options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _occlumInterface = occlumInterface ?? throw new ArgumentNullException(nameof(occlumInterface));
            _eventSystem = eventSystem ?? throw new ArgumentNullException(nameof(eventSystem));
            _options = options ?? new MetricsCollectorOptions();
            _metrics = new ConcurrentDictionary<string, MetricValue>();
            _cancellationTokenSource = new CancellationTokenSource();
            _fileSemaphore = new SemaphoreSlim(1, 1);
            _uptime = Stopwatch.StartNew();
            _disposed = false;

            // Create the metrics directory if it doesn't exist
            if (!string.IsNullOrEmpty(_options.MetricsDirectory) && !Directory.Exists(_options.MetricsDirectory))
            {
                Directory.CreateDirectory(_options.MetricsDirectory);
            }

            // Initialize default metrics
            InitializeDefaultMetrics();

            // Start the reporting task
            if (_options.EnablePeriodicReporting)
            {
                _reportingTask = Task.Run(() => ReportMetricsPeriodicAsync(_cancellationTokenSource.Token));
            }
            else
            {
                _reportingTask = Task.CompletedTask;
            }

            _logger.LogInformation("Metrics collector initialized");
        }

        /// <summary>
        /// Records a counter metric.
        /// </summary>
        /// <param name="name">The name of the metric.</param>
        /// <param name="value">The value to add to the counter.</param>
        /// <param name="tags">The tags for the metric.</param>
        public void RecordCounter(string name, double value = 1, Dictionary<string, string> tags = null)
        {
            CheckDisposed();

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Metric name cannot be null or empty", nameof(name));
            }

            _logger.LogDebug("Recording counter metric {MetricName} with value {Value}", name, value);

            try
            {
                // Get or create the metric
                var metric = _metrics.GetOrAdd(name, _ => new MetricValue
                {
                    Name = name,
                    Type = MetricType.Counter,
                    Value = 0,
                    Tags = tags ?? new Dictionary<string, string>()
                });

                // Update the metric value
                if (metric.Type == MetricType.Counter)
                {
                    // Increment the counter
                    Interlocked.Exchange(ref metric.Value, metric.Value + value);
                }
                else
                {
                    _logger.LogWarning("Metric {MetricName} is not a counter", name);
                }

                // Update the tags
                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        metric.Tags[tag.Key] = tag.Value;
                    }
                }

                // Update the timestamp
                metric.Timestamp = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording counter metric {MetricName}", name);
            }
        }

        /// <summary>
        /// Records a gauge metric.
        /// </summary>
        /// <param name="name">The name of the metric.</param>
        /// <param name="value">The value of the gauge.</param>
        /// <param name="tags">The tags for the metric.</param>
        public void RecordGauge(string name, double value, Dictionary<string, string> tags = null)
        {
            CheckDisposed();

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Metric name cannot be null or empty", nameof(name));
            }

            _logger.LogDebug("Recording gauge metric {MetricName} with value {Value}", name, value);

            try
            {
                // Get or create the metric
                var metric = _metrics.GetOrAdd(name, _ => new MetricValue
                {
                    Name = name,
                    Type = MetricType.Gauge,
                    Value = 0,
                    Tags = tags ?? new Dictionary<string, string>()
                });

                // Update the metric value
                if (metric.Type == MetricType.Gauge)
                {
                    // Set the gauge value
                    Interlocked.Exchange(ref metric.Value, value);
                }
                else
                {
                    _logger.LogWarning("Metric {MetricName} is not a gauge", name);
                }

                // Update the tags
                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        metric.Tags[tag.Key] = tag.Value;
                    }
                }

                // Update the timestamp
                metric.Timestamp = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording gauge metric {MetricName}", name);
            }
        }

        /// <summary>
        /// Records a histogram metric.
        /// </summary>
        /// <param name="name">The name of the metric.</param>
        /// <param name="value">The value to add to the histogram.</param>
        /// <param name="tags">The tags for the metric.</param>
        public void RecordHistogram(string name, double value, Dictionary<string, string> tags = null)
        {
            CheckDisposed();

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Metric name cannot be null or empty", nameof(name));
            }

            _logger.LogDebug("Recording histogram metric {MetricName} with value {Value}", name, value);

            try
            {
                // Get or create the metric
                var metric = _metrics.GetOrAdd(name, _ => new MetricValue
                {
                    Name = name,
                    Type = MetricType.Histogram,
                    Value = 0,
                    Count = 0,
                    Sum = 0,
                    Min = double.MaxValue,
                    Max = double.MinValue,
                    Tags = tags ?? new Dictionary<string, string>()
                });

                // Update the metric value
                if (metric.Type == MetricType.Histogram)
                {
                    // Update the histogram values
                    Interlocked.Increment(ref metric.Count);
                    Interlocked.Exchange(ref metric.Sum, metric.Sum + value);
                    Interlocked.Exchange(ref metric.Value, metric.Sum / metric.Count); // Average

                    // Update min and max
                    double currentMin = metric.Min;
                    while (value < currentMin)
                    {
                        double newMin = value;
                        double oldMin = Interlocked.CompareExchange(ref metric.Min, newMin, currentMin);
                        if (oldMin == currentMin)
                        {
                            break;
                        }
                        currentMin = oldMin;
                    }

                    double currentMax = metric.Max;
                    while (value > currentMax)
                    {
                        double newMax = value;
                        double oldMax = Interlocked.CompareExchange(ref metric.Max, newMax, currentMax);
                        if (oldMax == currentMax)
                        {
                            break;
                        }
                        currentMax = oldMax;
                    }
                }
                else
                {
                    _logger.LogWarning("Metric {MetricName} is not a histogram", name);
                }

                // Update the tags
                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        metric.Tags[tag.Key] = tag.Value;
                    }
                }

                // Update the timestamp
                metric.Timestamp = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording histogram metric {MetricName}", name);
            }
        }

        /// <summary>
        /// Records a timer metric.
        /// </summary>
        /// <param name="name">The name of the metric.</param>
        /// <param name="milliseconds">The time in milliseconds.</param>
        /// <param name="tags">The tags for the metric.</param>
        public void RecordTimer(string name, double milliseconds, Dictionary<string, string> tags = null)
        {
            CheckDisposed();

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Metric name cannot be null or empty", nameof(name));
            }

            _logger.LogDebug("Recording timer metric {MetricName} with value {Value}ms", name, milliseconds);

            try
            {
                // Get or create the metric
                var metric = _metrics.GetOrAdd(name, _ => new MetricValue
                {
                    Name = name,
                    Type = MetricType.Timer,
                    Value = 0,
                    Count = 0,
                    Sum = 0,
                    Min = double.MaxValue,
                    Max = double.MinValue,
                    Tags = tags ?? new Dictionary<string, string>()
                });

                // Update the metric value
                if (metric.Type == MetricType.Timer)
                {
                    // Update the timer values
                    Interlocked.Increment(ref metric.Count);
                    Interlocked.Exchange(ref metric.Sum, metric.Sum + milliseconds);
                    Interlocked.Exchange(ref metric.Value, metric.Sum / metric.Count); // Average

                    // Update min and max
                    double currentMin = metric.Min;
                    while (milliseconds < currentMin)
                    {
                        double newMin = milliseconds;
                        double oldMin = Interlocked.CompareExchange(ref metric.Min, newMin, currentMin);
                        if (oldMin == currentMin)
                        {
                            break;
                        }
                        currentMin = oldMin;
                    }

                    double currentMax = metric.Max;
                    while (milliseconds > currentMax)
                    {
                        double newMax = milliseconds;
                        double oldMax = Interlocked.CompareExchange(ref metric.Max, newMax, currentMax);
                        if (oldMax == currentMax)
                        {
                            break;
                        }
                        currentMax = oldMax;
                    }
                }
                else
                {
                    _logger.LogWarning("Metric {MetricName} is not a timer", name);
                }

                // Update the tags
                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        metric.Tags[tag.Key] = tag.Value;
                    }
                }

                // Update the timestamp
                metric.Timestamp = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording timer metric {MetricName}", name);
            }
        }

        /// <summary>
        /// Gets a metric by name.
        /// </summary>
        /// <param name="name">The name of the metric.</param>
        /// <returns>The metric, or null if the metric does not exist.</returns>
        public MetricValue GetMetric(string name)
        {
            CheckDisposed();

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Metric name cannot be null or empty", nameof(name));
            }

            _logger.LogDebug("Getting metric {MetricName}", name);

            try
            {
                // Get the metric
                if (_metrics.TryGetValue(name, out var metric))
                {
                    return metric;
                }

                _logger.LogDebug("Metric {MetricName} not found", name);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting metric {MetricName}", name);
                throw new MetricsException($"Error getting metric {name}", ex);
            }
        }

        /// <summary>
        /// Gets all metrics.
        /// </summary>
        /// <returns>A list of all metrics.</returns>
        public IReadOnlyList<MetricValue> GetAllMetrics()
        {
            CheckDisposed();

            _logger.LogDebug("Getting all metrics");

            try
            {
                // Update the uptime metric
                RecordGauge("enclave.uptime", _uptime.Elapsed.TotalSeconds);

                // Get all metrics
                return new List<MetricValue>(_metrics.Values);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all metrics");
                throw new MetricsException("Error getting all metrics", ex);
            }
        }

        /// <summary>
        /// Reports metrics to the configured outputs.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task ReportMetricsAsync()
        {
            CheckDisposed();

            _logger.LogDebug("Reporting metrics");

            try
            {
                // Update the uptime metric
                RecordGauge("enclave.uptime", _uptime.Elapsed.TotalSeconds);

                // Get all metrics
                var metrics = new List<MetricValue>(_metrics.Values);

                // Report metrics to file
                if (_options.EnableFileReporting)
                {
                    await ReportMetricsToFileAsync(metrics);
                }

                // Report metrics as events
                if (_options.EnableEventReporting)
                {
                    await ReportMetricsAsEventsAsync(metrics);
                }

                // Report metrics to the host
                if (_options.EnableHostReporting)
                {
                    ReportMetricsToHost(metrics);
                }

                _logger.LogDebug("Metrics reported successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting metrics");
                throw new MetricsException("Error reporting metrics", ex);
            }
        }

        /// <summary>
        /// Resets all metrics.
        /// </summary>
        public void ResetMetrics()
        {
            CheckDisposed();

            _logger.LogDebug("Resetting metrics");

            try
            {
                // Reset all metrics
                foreach (var metric in _metrics.Values)
                {
                    switch (metric.Type)
                    {
                        case MetricType.Counter:
                            Interlocked.Exchange(ref metric.Value, 0);
                            break;
                        case MetricType.Histogram:
                        case MetricType.Timer:
                            Interlocked.Exchange(ref metric.Value, 0);
                            Interlocked.Exchange(ref metric.Count, 0);
                            Interlocked.Exchange(ref metric.Sum, 0);
                            Interlocked.Exchange(ref metric.Min, double.MaxValue);
                            Interlocked.Exchange(ref metric.Max, double.MinValue);
                            break;
                    }
                }

                _logger.LogDebug("Metrics reset successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting metrics");
                throw new MetricsException("Error resetting metrics", ex);
            }
        }

        /// <summary>
        /// Disposes the metrics collector.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the metrics collector.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Stop the reporting task
                    _cancellationTokenSource.Cancel();
                    try
                    {
                        _reportingTask.Wait();
                    }
                    catch (AggregateException)
                    {
                        // Ignore task cancellation exceptions
                    }

                    // Dispose resources
                    _cancellationTokenSource.Dispose();
                    _fileSemaphore.Dispose();
                }

                _disposed = true;
            }
        }

        private void InitializeDefaultMetrics()
        {
            // Initialize default metrics
            RecordGauge("enclave.uptime", 0);
            RecordGauge("enclave.memory.used", 0);
            RecordGauge("enclave.memory.total", 0);
            RecordGauge("enclave.cpu.usage", 0);
            RecordCounter("enclave.requests.total", 0);
            RecordCounter("enclave.requests.success", 0);
            RecordCounter("enclave.requests.error", 0);
            RecordTimer("enclave.requests.duration", 0);
            RecordCounter("enclave.gas.used", 0);
        }

        private async Task ReportMetricsPeriodicAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Metrics reporting task started");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // Report metrics
                        await ReportMetricsAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reporting metrics");
                    }

                    // Wait for the next reporting interval
                    await Task.Delay(_options.ReportingIntervalMs, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Task was canceled, exit the loop
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in metrics reporting task");
            }

            _logger.LogInformation("Metrics reporting task stopped");
        }

        private async Task ReportMetricsToFileAsync(List<MetricValue> metrics)
        {
            if (string.IsNullOrEmpty(_options.MetricsDirectory))
            {
                return;
            }

            await _fileSemaphore.WaitAsync();
            try
            {
                string filePath = GetMetricsFilePath();
                string metricsJson = JsonSerializer.Serialize(metrics);
                await File.WriteAllTextAsync(filePath, metricsJson);
            }
            finally
            {
                _fileSemaphore.Release();
            }
        }

        private async Task ReportMetricsAsEventsAsync(List<MetricValue> metrics)
        {
            // Publish metrics as events
            await _eventSystem.PublishAsync(_options.MetricsEventType, metrics);
        }

        private void ReportMetricsToHost(List<MetricValue> metrics)
        {
            // Report metrics to the host
            foreach (var metric in metrics)
            {
                string metricName = metric.Name;
                string metricValue = metric.Value.ToString();

                // Add tags to the metric name
                if (metric.Tags != null && metric.Tags.Count > 0)
                {
                    StringBuilder tagString = new StringBuilder();
                    foreach (var tag in metric.Tags)
                    {
                        tagString.Append($",{tag.Key}={tag.Value}");
                    }
                    metricName += tagString.ToString();
                }

                // Send the metric to the host
                _occlumInterface.SendMetricToHost(metricName, metricValue);
            }
        }

        private string GetMetricsFilePath()
        {
            string fileName = $"enclave_{_occlumInterface.EnclaveId}.metrics.json";
            return Path.Combine(_options.MetricsDirectory, fileName);
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(MetricsCollector));
            }
        }
    }
}
