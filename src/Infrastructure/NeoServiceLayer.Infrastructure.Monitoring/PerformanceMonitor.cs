using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;


namespace NeoServiceLayer.Infrastructure.Monitoring
{
    /// <summary>
    /// Concrete implementation of performance monitoring
    /// </summary>
    public class PerformanceMonitor : IPerformanceMonitor
    {
        private readonly ILogger<PerformanceMonitor> _logger;
        private readonly PerformanceCounter? _cpuCounter;
        private readonly PerformanceCounter? _memoryCounter;
        private readonly Dictionary<string, List<double>> _customMetrics = new();
        private readonly object _metricsLock = new();

        public PerformanceMonitor(ILogger<PerformanceMonitor> logger)
        {
            _logger = logger;

            try
            {
                // Initialize performance counters (Windows-specific)
                if (OperatingSystem.IsWindows())
                {
                    _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                    _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize performance counters. System metrics will use alternative methods.");
            }
        }

        public async Task<SystemMetrics> GetSystemMetricsAsync()
        {
            var metrics = new SystemMetrics();

            try
            {
                // CPU Usage
                if (_cpuCounter != null && OperatingSystem.IsWindows())
                {
                    metrics.CpuUsagePercent = _cpuCounter.NextValue();
                    // Wait a bit for accurate reading
                    await Task.Delay(100).ConfigureAwait(false);
                    metrics.CpuUsagePercent = _cpuCounter.NextValue();
                }
                else
                {
                    // Alternative CPU measurement for non-Windows
                    metrics.CpuUsagePercent = await GetCpuUsageAsync().ConfigureAwait(false);
                }

                // Memory Usage
                var process = Process.GetCurrentProcess();
                metrics.MemoryUsageMB = process.WorkingSet64 / (1024 * 1024);

                if (_memoryCounter != null && OperatingSystem.IsWindows())
                {
                    var availableMemoryMB = _memoryCounter.NextValue();
                    // Estimate total memory (this is simplified)
                    metrics.TotalMemoryMB = (long)(metrics.MemoryUsageMB + availableMemoryMB);
                }
                else
                {
                    // For non-Windows, use GC memory info
                    var gcInfo = GC.GetGCMemoryInfo();
                    metrics.TotalMemoryMB = gcInfo.TotalAvailableMemoryBytes / (1024 * 1024);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting system metrics");
            }

            return metrics;
        }

        public async Task<ApplicationMetrics> GetApplicationMetricsAsync()
        {
            var metrics = new ApplicationMetrics();

            try
            {
                // Thread pool information
                ThreadPool.GetAvailableThreads(out var workerThreads, out var completionPortThreads);
                ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxCompletionPortThreads);

                metrics.ThreadPoolThreads = maxWorkerThreads - workerThreads;
                metrics.CompletionPortThreads = maxCompletionPortThreads - completionPortThreads;

                // Custom metrics aggregation
                lock (_metricsLock)
                {
                    if (_customMetrics.TryGetValue("response_time", out var responseTimes) && responseTimes.Count > 0)
                    {
                        responseTimes.Sort();
                        metrics.AverageResponseTime = CalculateAverage(responseTimes);
                        metrics.P95ResponseTime = CalculatePercentile(responseTimes, 0.95);
                        metrics.P99ResponseTime = CalculatePercentile(responseTimes, 0.99);
                    }

                    if (_customMetrics.TryGetValue("total_requests", out var requests) && requests.Count > 0)
                    {
                        metrics.TotalRequests = (long)requests[^1]; // Last value
                    }

                    if (_customMetrics.TryGetValue("error_count", out var errors) && errors.Count > 0)
                    {
                        metrics.ErrorCount = (long)errors[^1]; // Last value
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting application metrics");
            }

            return metrics;
        }

        public void RecordCustomMetric(string name, double value, Dictionary<string, string>? tags = null)
        {
            try
            {
                lock (_metricsLock)
                {
                    if (!_customMetrics.ContainsKey(name))
                    {
                        _customMetrics[name] = new List<double>();
                    }

                    _customMetrics[name].Add(value);

                    // Keep only last 1000 values to prevent memory leaks
                    if (_customMetrics[name].Count > 1000)
                    {
                        _customMetrics[name].RemoveRange(0, 500);
                    }
                }

                _logger.LogDebug("Recorded custom metric {MetricName}: {Value}", name, value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording custom metric {MetricName}", name);
            }
        }

        private async Task<double> GetCpuUsageAsync()
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var startCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;

                await Task.Delay(500).ConfigureAwait(false);

                var endTime = DateTime.UtcNow;
                var endCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;

                var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
                var totalMsPassed = (endTime - startTime).TotalMilliseconds;
                var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

                return cpuUsageTotal * 100;
            }
            catch
            {
                return 0; // Return 0 if unable to calculate
            }
        }

        private static double CalculateAverage(IReadOnlyList<double> values)
        {
            if (values.Count == 0) return 0;

            double sum = 0;
            foreach (var value in values)
            {
                sum += value;
            }
            return sum / values.Count;
        }

        private static double CalculatePercentile(IReadOnlyList<double> sortedValues, double percentile)
        {
            if (sortedValues.Count == 0) return 0;

            var index = (int)Math.Ceiling(sortedValues.Count * percentile) - 1;
            index = Math.Max(0, Math.Min(index, sortedValues.Count - 1));

            return sortedValues[index];
        }

        public void Dispose()
        {
            _cpuCounter?.Dispose();
            _memoryCounter?.Dispose();
        }
    }
}