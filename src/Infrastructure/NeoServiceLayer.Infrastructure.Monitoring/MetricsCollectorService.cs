using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;


namespace NeoServiceLayer.Infrastructure.Monitoring
{
    /// <summary>
    /// Background service that continuously collects system and application metrics
    /// </summary>
    public class MetricsCollectorService : BackgroundService
    {
        private readonly ILogger<MetricsCollectorService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ApmConfiguration _configuration;

        public MetricsCollectorService(
            ILogger<MetricsCollectorService> logger,
            IServiceProvider serviceProvider,
            IOptions<ApmConfiguration> configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Metrics Collector Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CollectMetricsAsync().ConfigureAwait(false);
                    await Task.Delay(
                        TimeSpan.FromSeconds(_configuration.MetricsCollectionIntervalSeconds),
                        stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Service is being stopped
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while collecting metrics");
                    // Continue collecting metrics even if one iteration fails
                }
            }

            _logger.LogInformation("Metrics Collector Service stopped");
        }

        private async Task CollectMetricsAsync()
        {
            var performanceMonitor = scope.ServiceProvider.GetRequiredService<IPerformanceMonitor>();

            try
            {
                // Collect system metrics
                var systemMetrics = await performanceMonitor.GetSystemMetricsAsync().ConfigureAwait(false);

                // Record system metrics
                performanceMonitor.RecordCustomMetric("cpu_usage_percent", systemMetrics.CpuUsagePercent);
                performanceMonitor.RecordCustomMetric("memory_usage_mb", systemMetrics.MemoryUsageMB);
                performanceMonitor.RecordCustomMetric("memory_usage_percent", systemMetrics.MemoryUsagePercent);

                // Check thresholds and log warnings/errors
                CheckSystemThresholds(systemMetrics);

                // Collect application metrics
                var appMetrics = await performanceMonitor.GetApplicationMetricsAsync().ConfigureAwait(false);

                // Record application metrics
                performanceMonitor.RecordCustomMetric("active_connections", appMetrics.ActiveConnections);
                performanceMonitor.RecordCustomMetric("thread_pool_threads", appMetrics.ThreadPoolThreads);
                performanceMonitor.RecordCustomMetric("error_rate", appMetrics.ErrorRate);

                // Update APM metrics
                ApplicationPerformanceMonitoring.UpdateActiveConnections(appMetrics.ActiveConnections);

                if (_configuration.EnableDetailedMetrics)
                {
                    _logger.LogDebug("Collected metrics - CPU: {CpuUsage:F2}%, Memory: {MemoryUsage:F2}%, Error Rate: {ErrorRate:F2}%",
                        systemMetrics.CpuUsagePercent,
                        systemMetrics.MemoryUsagePercent,
                        appMetrics.ErrorRate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to collect metrics");
                ApplicationPerformanceMonitoring.RecordError("metrics_collection_failed", "MetricsCollectorService", ex);
            }
        }

        private void CheckSystemThresholds(SystemMetrics metrics)
        {
            // CPU threshold checks
            if (metrics.CpuUsagePercent > ApplicationPerformanceMonitoring.Thresholds.CpuErrorPercent)
            {
                _logger.LogError("High CPU usage detected: {CpuUsage:F2}% (threshold: {Threshold:F2}%)",
                    metrics.CpuUsagePercent, ApplicationPerformanceMonitoring.Thresholds.CpuErrorPercent);
                ApplicationPerformanceMonitoring.RecordError("high_cpu_usage", "System");
            }
            else if (metrics.CpuUsagePercent > ApplicationPerformanceMonitoring.Thresholds.CpuWarningPercent)
            {
                _logger.LogWarning("Elevated CPU usage detected: {CpuUsage:F2}% (threshold: {Threshold:F2}%)",
                    metrics.CpuUsagePercent, ApplicationPerformanceMonitoring.Thresholds.CpuWarningPercent);
            }

            // Memory threshold checks
            if (metrics.MemoryUsageMB > ApplicationPerformanceMonitoring.Thresholds.MemoryErrorMB)
            {
                _logger.LogError("High memory usage detected: {MemoryUsage:N0} MB (threshold: {Threshold:N0} MB)",
                    metrics.MemoryUsageMB, ApplicationPerformanceMonitoring.Thresholds.MemoryErrorMB);
                ApplicationPerformanceMonitoring.RecordError("high_memory_usage", "System");
            }
            else if (metrics.MemoryUsageMB > ApplicationPerformanceMonitoring.Thresholds.MemoryWarningMB)
            {
                _logger.LogWarning("Elevated memory usage detected: {MemoryUsage:N0} MB (threshold: {Threshold:N0} MB)",
                    metrics.MemoryUsageMB, ApplicationPerformanceMonitoring.Thresholds.MemoryWarningMB);
            }
        }
    }
}