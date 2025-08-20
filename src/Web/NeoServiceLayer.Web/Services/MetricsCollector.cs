using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Web.Models;
using System.Threading;
using System;


namespace NeoServiceLayer.Web.Services
{
    /// <summary>
    /// Interface for collecting system and service metrics.
    /// </summary>
    public interface IMetricsCollector
    {
        Task<SystemMetrics> GetSystemMetricsAsync();
        Task<Dictionary<string, double>> GetServiceMetricsAsync(string serviceName);
        Task<List<MetricDataPoint>> GetHistoricalMetricsAsync(string metricName, TimeSpan period);
        Task RecordMetricAsync(string metricName, double value, Dictionary<string, object> tags = null);
        Task<PerformanceTrend> GetPerformanceTrendAsync(TimeSpan period);
    }

    /// <summary>
    /// Implementation of metrics collection.
    /// </summary>
    public class MetricsCollector : IMetricsCollector
    {
        private readonly ILogger<MetricsCollector> _logger;
        private readonly IMemoryCache _cache;
        private readonly Process _currentProcess;
        private readonly Dictionary<string, Queue<MetricDataPoint>> _metricsHistory;
        private readonly object _lockObject = new object();

        public MetricsCollector(
            ILogger<MetricsCollector> logger,
            IMemoryCache cache)
        {
            _logger = logger;
            _cache = cache;
            _currentProcess = Process.GetCurrentProcess();
            _metricsHistory = new Dictionary<string, Queue<MetricDataPoint>>();

            // Initialize metric queues
            InitializeMetricQueues();
        }

        public async Task<SystemMetrics> GetSystemMetricsAsync()
        {
            try
            {
                // Check cache first
                if (_cache.TryGetValue("system_metrics", out SystemMetrics cachedMetrics))
                {
                    return cachedMetrics;
                }

                var metrics = new SystemMetrics
                {
                    CpuUsage = await GetCpuUsageAsync(),
                    MemoryUsage = GetMemoryUsage(),
                    MemoryUsedBytes = GetMemoryUsedBytes(),
                    MemoryTotalBytes = GetTotalMemoryBytes(),
                    ActiveConnections = GetActiveConnections(),
                    RequestsPerSecond = await GetRequestsPerSecondAsync(),
                    AverageResponseTime = await GetAverageResponseTimeAsync(),
                    TotalRequests = await GetTotalRequestsAsync(),
                    FailedRequests = await GetFailedRequestsAsync(),
                    NetworkInMbps = await GetNetworkInMbpsAsync(),
                    NetworkOutMbps = await GetNetworkOutMbpsAsync(),
                    ServiceMetrics = await GetAllServiceMetricsAsync(),
                    HistoricalData = GetRecentMetricHistory("system.performance", 10)
                };

                // Cache for 5 seconds
                _cache.Set("system_metrics", metrics, TimeSpan.FromSeconds(5));

                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting system metrics");
                return new SystemMetrics(); // Return empty metrics on error
            }
        }

        public async Task<Dictionary<string, double>> GetServiceMetricsAsync(string serviceName)
        {
            var cacheKey = $"service_metrics_{serviceName}";
            if (_cache.TryGetValue(cacheKey, out Dictionary<string, double> cachedMetrics))
            {
                return cachedMetrics;
            }

            // Simulate service-specific metrics
            var metrics = new Dictionary<string, double>
            {
                ["requestsPerSecond"] = Random.Shared.Next(10, 100),
                ["averageResponseTime"] = Random.Shared.Next(50, 500),
                ["errorRate"] = Random.Shared.NextDouble() * 5,
                ["cpuUsage"] = Random.Shared.NextDouble() * 100,
                ["memoryUsageMB"] = Random.Shared.Next(100, 500),
                ["activeConnections"] = Random.Shared.Next(1, 50)
            };

            _cache.Set(cacheKey, metrics, TimeSpan.FromSeconds(10));
            return metrics;
        }

        public async Task<List<MetricDataPoint>> GetHistoricalMetricsAsync(string metricName, TimeSpan period)
        {
            lock (_lockObject)
            {
                if (!_metricsHistory.ContainsKey(metricName))
                {
                    return new List<MetricDataPoint>();
                }

                var cutoffTime = DateTime.UtcNow.Subtract(period);
                return _metricsHistory[metricName]
                    .Where(m => m.Timestamp >= cutoffTime)
                    .OrderBy(m => m.Timestamp)
                    .ToList();
            }
        }

        public async Task RecordMetricAsync(string metricName, double value, Dictionary<string, object> tags = null)
        {
            try
            {
                var dataPoint = new MetricDataPoint
                {
                    Timestamp = DateTime.UtcNow,
                    Value = value,
                    Label = metricName,
                    Metadata = tags ?? new Dictionary<string, object>()
                };

                lock (_lockObject)
                {
                    if (!_metricsHistory.ContainsKey(metricName))
                    {
                        _metricsHistory[metricName] = new Queue<MetricDataPoint>();
                    }

                    var queue = _metricsHistory[metricName];
                    queue.Enqueue(dataPoint);

                    // Keep only last 1000 data points per metric
                    while (queue.Count > 1000)
                    {
                        queue.Dequeue();
                    }
                }

                // Also update cache for quick access
                _cache.Set($"latest_{metricName}", value, TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording metric {MetricName}", metricName);
            }
        }

        public async Task<PerformanceTrend> GetPerformanceTrendAsync(TimeSpan period)
        {
            var responseTimeHistory = await GetHistoricalMetricsAsync("system.responseTime", period);
            var errorRateHistory = await GetHistoricalMetricsAsync("system.errorRate", period);
            var throughputHistory = await GetHistoricalMetricsAsync("system.throughput", period);

            var trend = new PerformanceTrend
            {
                Period = FormatPeriod(period),
                ResponseTimeHistory = responseTimeHistory,
                ErrorRateHistory = errorRateHistory,
                ThroughputHistory = throughputHistory
            };

            // Calculate trends
            if (responseTimeHistory.Count >= 2)
            {
                var firstHalf = responseTimeHistory.Take(responseTimeHistory.Count / 2).Average(m => m.Value);
                var secondHalf = responseTimeHistory.Skip(responseTimeHistory.Count / 2).Average(m => m.Value);
                trend.ResponseTimeTrend = ((secondHalf - firstHalf) / firstHalf) * 100;
            }

            if (errorRateHistory.Count >= 2)
            {
                var firstHalf = errorRateHistory.Take(errorRateHistory.Count / 2).Average(m => m.Value);
                var secondHalf = errorRateHistory.Skip(errorRateHistory.Count / 2).Average(m => m.Value);
                trend.ErrorRateTrend = ((secondHalf - firstHalf) / Math.Max(firstHalf, 0.01)) * 100;
            }

            if (throughputHistory.Count >= 2)
            {
                var firstHalf = throughputHistory.Take(throughputHistory.Count / 2).Average(m => m.Value);
                var secondHalf = throughputHistory.Skip(throughputHistory.Count / 2).Average(m => m.Value);
                trend.ThroughputTrend = ((secondHalf - firstHalf) / firstHalf) * 100;
            }

            return trend;
        }

        private void InitializeMetricQueues()
        {
            var metricNames = new[]
            {
                "system.cpu",
                "system.memory",
                "system.responseTime",
                "system.errorRate",
                "system.throughput",
                "system.connections",
                "system.performance"
            };

            lock (_lockObject)
            {
                foreach (var metricName in metricNames)
                {
                    _metricsHistory[metricName] = new Queue<MetricDataPoint>();

                    // Add some initial data points for demonstration
                    for (int i = 10; i >= 0; i--)
                    {
                        _metricsHistory[metricName].Enqueue(new MetricDataPoint
                        {
                            Timestamp = DateTime.UtcNow.AddMinutes(-i),
                            Value = Random.Shared.NextDouble() * 100,
                            Label = metricName
                        });
                    }
                }
            }
        }

        private async Task<double> GetCpuUsageAsync()
        {
            try
            {
                // Get CPU usage (this is a simplified version)
                var startTime = DateTime.UtcNow;
                var startCpuUsage = _currentProcess.TotalProcessorTime;

                await Task.Delay(100);

                var endTime = DateTime.UtcNow;
                var endCpuUsage = _currentProcess.TotalProcessorTime;

                var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
                var totalMsPassed = (endTime - startTime).TotalMilliseconds;
                var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

                var cpuUsage = cpuUsageTotal * 100;

                // Record the metric
                await RecordMetricAsync("system.cpu", cpuUsage);

                return Math.Min(100, Math.Max(0, cpuUsage));
            }
            catch
            {
                // Return simulated value on error
                return Random.Shared.NextDouble() * 30 + 20;
            }
        }

        private double GetMemoryUsage()
        {
            try
            {
                var totalMemory = GC.GetTotalMemory(false);
                var workingSet = _currentProcess.WorkingSet64;
                var usage = (double)totalMemory / workingSet * 100;

                // Record the metric
                Task.Run(() => RecordMetricAsync("system.memory", usage));

                return Math.Min(100, Math.Max(0, usage));
            }
            catch
            {
                return Random.Shared.NextDouble() * 40 + 30;
            }
        }

        private long GetMemoryUsedBytes()
        {
            try
            {
                return _currentProcess.WorkingSet64;
            }
            catch
            {
                return 512 * 1024 * 1024; // 512 MB default
            }
        }

        private long GetTotalMemoryBytes()
        {
            // This would need platform-specific implementation
            // For now, return a reasonable default
            return 8L * 1024 * 1024 * 1024; // 8 GB
        }

        private int GetActiveConnections()
        {
            // In a real implementation, this would track actual connections
            return Random.Shared.Next(10, 100);
        }

        private async Task<double> GetRequestsPerSecondAsync()
        {
            if (_cache.TryGetValue("requests_per_second", out double rps))
            {
                return rps;
            }

            // Simulate RPS
            rps = Random.Shared.NextDouble() * 100 + 50;
            await RecordMetricAsync("system.throughput", rps);
            return rps;
        }

        private async Task<double> GetAverageResponseTimeAsync()
        {
            if (_cache.TryGetValue("avg_response_time", out double avgTime))
            {
                return avgTime;
            }

            // Simulate average response time
            avgTime = Random.Shared.NextDouble() * 200 + 50;
            await RecordMetricAsync("system.responseTime", avgTime);
            return avgTime;
        }

        private async Task<int> GetTotalRequestsAsync()
        {
            // In a real implementation, this would track actual requests
            return Random.Shared.Next(10000, 50000);
        }

        private async Task<int> GetFailedRequestsAsync()
        {
            // In a real implementation, this would track actual failures
            var total = await GetTotalRequestsAsync();
            return (int)(total * Random.Shared.NextDouble() * 0.02); // 0-2% failure rate
        }

        private async Task<double> GetNetworkInMbpsAsync()
        {
            // Simulate network traffic
            return Random.Shared.NextDouble() * 10 + 5;
        }

        private async Task<double> GetNetworkOutMbpsAsync()
        {
            // Simulate network traffic
            return Random.Shared.NextDouble() * 15 + 10;
        }

        private async Task<Dictionary<string, double>> GetAllServiceMetricsAsync()
        {
            var metrics = new Dictionary<string, double>();
            var services = new[] { "KeyManagement", "Oracle", "Storage", "Compute", "CrossChain" };

            foreach (var service in services)
            {
                var serviceMetrics = await GetServiceMetricsAsync(service);
                metrics[$"{service}.rps"] = serviceMetrics["requestsPerSecond"];
                metrics[$"{service}.responseTime"] = serviceMetrics["averageResponseTime"];
            }

            return metrics;
        }

        private List<MetricDataPoint> GetRecentMetricHistory(string metricName, int count)
        {
            lock (_lockObject)
            {
                if (!_metricsHistory.ContainsKey(metricName))
                {
                    return new List<MetricDataPoint>();
                }

                return _metricsHistory[metricName]
                    .OrderByDescending(m => m.Timestamp)
                    .Take(count)
                    .OrderBy(m => m.Timestamp)
                    .ToList();
            }
        }

        private string FormatPeriod(TimeSpan period)
        {
            if (period.TotalMinutes < 60)
                return $"last {period.TotalMinutes:0} minutes";
            if (period.TotalHours < 24)
                return $"last {period.TotalHours:0} hours";
            return $"last {period.TotalDays:0} days";
        }
    }
}
