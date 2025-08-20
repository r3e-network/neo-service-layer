using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace NeoServiceLayer.Infrastructure.Monitoring
{
    /// <summary>
    /// Background service that monitors metrics and sends alerts when thresholds are exceeded
    /// </summary>
    public class AlertingService : BackgroundService
    {
        private readonly ILogger<AlertingService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ApmConfiguration _configuration;
        private readonly Dictionary<string, DateTime> _lastAlertTimes = new();
        private readonly TimeSpan _alertCooldown = TimeSpan.FromMinutes(5); // Prevent alert spam

        public AlertingService(
            ILogger<AlertingService> logger,
            IServiceProvider serviceProvider,
            IOptions<ApmConfiguration> configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_configuration.EnableAlerting)
            {
                _logger.LogInformation("Alerting is disabled");
                return;
            }

            _logger.LogInformation("Alerting Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAlertsAsync().ConfigureAwait(false);
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken).ConfigureAwait(false); // Check every 30 seconds
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking alerts");
                }
            }

            _logger.LogInformation("Alerting Service stopped");
        }

        private async Task CheckAlertsAsync()
        {
            var performanceMonitor = scope.ServiceProvider.GetRequiredService<IPerformanceMonitor>();

            try
            {
                // Get current metrics
                var systemMetrics = await performanceMonitor.GetSystemMetricsAsync().ConfigureAwait(false);
                var appMetrics = await performanceMonitor.GetApplicationMetricsAsync().ConfigureAwait(false);

                // Check various alert conditions
                await CheckResponseTimeAlertsAsync(appMetrics).ConfigureAwait(false);
                await CheckErrorRateAlertsAsync(appMetrics).ConfigureAwait(false);
                await CheckResourceAlertsAsync(systemMetrics).ConfigureAwait(false);
                await CheckConnectionAlertsAsync(appMetrics).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check alerts");
            }
        }

        private async Task CheckResponseTimeAlertsAsync(ApplicationMetrics metrics)
        {
            if (metrics.P95ResponseTime > ApplicationPerformanceMonitoring.Thresholds.ResponseTimeError)
            {
                await SendAlertAsync(
                    "HighResponseTime",
                    AlertSeverity.Critical,
                    $"P95 response time is {metrics.P95ResponseTime:F2}s (threshold: {ApplicationPerformanceMonitoring.Thresholds.ResponseTimeError:F2}s)",
                    new Dictionary<string, object>
                    {
                        ["p95_response_time"] = metrics.P95ResponseTime,
                        ["threshold"] = ApplicationPerformanceMonitoring.Thresholds.ResponseTimeError,
                        ["avg_response_time"] = metrics.AverageResponseTime
                    }).ConfigureAwait(false);
            }
            else if (metrics.P95ResponseTime > ApplicationPerformanceMonitoring.Thresholds.ResponseTimeWarning)
            {
                await SendAlertAsync(
                    "ElevatedResponseTime",
                    AlertSeverity.Warning,
                    $"P95 response time is {metrics.P95ResponseTime:F2}s (threshold: {ApplicationPerformanceMonitoring.Thresholds.ResponseTimeWarning:F2}s)",
                    new Dictionary<string, object>
                    {
                        ["p95_response_time"] = metrics.P95ResponseTime,
                        ["threshold"] = ApplicationPerformanceMonitoring.Thresholds.ResponseTimeWarning
                    }).ConfigureAwait(false);
            }
        }

        private async Task CheckErrorRateAlertsAsync(ApplicationMetrics metrics)
        {
            const double errorRateWarningThreshold = 5.0; // 5%
            const double errorRateCriticalThreshold = 10.0; // 10%

            if (metrics.ErrorRate > errorRateCriticalThreshold)
            {
                await SendAlertAsync(
                    "HighErrorRate",
                    AlertSeverity.Critical,
                    $"Error rate is {metrics.ErrorRate:F2}% (threshold: {errorRateCriticalThreshold:F2}%)",
                    new Dictionary<string, object>
                    {
                        ["error_rate"] = metrics.ErrorRate,
                        ["error_count"] = metrics.ErrorCount,
                        ["total_requests"] = metrics.TotalRequests
                    }).ConfigureAwait(false);
            }
            else if (metrics.ErrorRate > errorRateWarningThreshold)
            {
                await SendAlertAsync(
                    "ElevatedErrorRate",
                    AlertSeverity.Warning,
                    $"Error rate is {metrics.ErrorRate:F2}% (threshold: {errorRateWarningThreshold:F2}%)",
                    new Dictionary<string, object>
                    {
                        ["error_rate"] = metrics.ErrorRate,
                        ["error_count"] = metrics.ErrorCount,
                        ["total_requests"] = metrics.TotalRequests
                    }).ConfigureAwait(false);
            }
        }

        private async Task CheckResourceAlertsAsync(SystemMetrics metrics)
        {
            // CPU alerts
            if (metrics.CpuUsagePercent > ApplicationPerformanceMonitoring.Thresholds.CpuErrorPercent)
            {
                await SendAlertAsync(
                    "HighCpuUsage",
                    AlertSeverity.Critical,
                    $"CPU usage is {metrics.CpuUsagePercent:F2}% (threshold: {ApplicationPerformanceMonitoring.Thresholds.CpuErrorPercent:F2}%)",
                    new Dictionary<string, object>
                    {
                        ["cpu_usage"] = metrics.CpuUsagePercent,
                        ["threshold"] = ApplicationPerformanceMonitoring.Thresholds.CpuErrorPercent
                    }).ConfigureAwait(false);
            }

            // Memory alerts
            if (metrics.MemoryUsageMB > ApplicationPerformanceMonitoring.Thresholds.MemoryErrorMB)
            {
                await SendAlertAsync(
                    "HighMemoryUsage",
                    AlertSeverity.Critical,
                    $"Memory usage is {metrics.MemoryUsageMB:N0} MB (threshold: {ApplicationPerformanceMonitoring.Thresholds.MemoryErrorMB:N0} MB)",
                    new Dictionary<string, object>
                    {
                        ["memory_usage_mb"] = metrics.MemoryUsageMB,
                        ["memory_usage_percent"] = metrics.MemoryUsagePercent,
                        ["threshold"] = ApplicationPerformanceMonitoring.Thresholds.MemoryErrorMB
                    }).ConfigureAwait(false);
            }
        }

        private async Task CheckConnectionAlertsAsync(ApplicationMetrics metrics)
        {
            const long maxConnectionsWarning = 1000;
            const long maxConnectionsCritical = 2000;

            if (metrics.ActiveConnections > maxConnectionsCritical)
            {
                await SendAlertAsync(
                    "HighConnectionCount",
                    AlertSeverity.Critical,
                    $"Active connections: {metrics.ActiveConnections:N0} (threshold: {maxConnectionsCritical:N0})",
                    new Dictionary<string, object>
                    {
                        ["active_connections"] = metrics.ActiveConnections,
                        ["threshold"] = maxConnectionsCritical
                    }).ConfigureAwait(false);
            }
            else if (metrics.ActiveConnections > maxConnectionsWarning)
            {
                await SendAlertAsync(
                    "ElevatedConnectionCount",
                    AlertSeverity.Warning,
                    $"Active connections: {metrics.ActiveConnections:N0} (threshold: {maxConnectionsWarning:N0})",
                    new Dictionary<string, object>
                    {
                        ["active_connections"] = metrics.ActiveConnections,
                        ["threshold"] = maxConnectionsWarning
                    }).ConfigureAwait(false);
            }
        }

        private async Task SendAlertAsync(
            string alertType,
            AlertSeverity severity,
            string message,
            Dictionary<string, object> context)
        {
            var alertKey = $"{alertType}_{severity}";

            // Check cooldown period
            if (_lastAlertTimes.TryGetValue(alertKey, out var lastAlert) &&
                DateTime.UtcNow - lastAlert < _alertCooldown)
            {
                return; // Skip alert due to cooldown
            }

            _lastAlertTimes[alertKey] = DateTime.UtcNow;

            var alert = new Alert
            {
                Type = alertType,
                Severity = severity,
                Message = message,
                Context = context,
                Timestamp = DateTime.UtcNow
            };

            // Log the alert
            switch (severity)
            {
                case AlertSeverity.Critical:
                    _logger.LogCritical("ALERT: {AlertType} - {Message}", alertType, message);
                    break;
                case AlertSeverity.Warning:
                    _logger.LogWarning("ALERT: {AlertType} - {Message}", alertType, message);
                    break;
                default:
                    _logger.LogInformation("ALERT: {AlertType} - {Message}", alertType, message);
                    break;
            }

            // Integrate with configured notification systems
            await ProcessAlertAsync(alert).ConfigureAwait(false);
        }

        private async Task ProcessAlertAsync(Alert alert)
        {
            try
            {
                // In a real implementation, this would send notifications via:
                // - Email
                // - Slack
                // - PagerDuty
                // - SMS
                // - Microsoft Teams
                // - Webhook endpoints

                // For now, we'll just record the alert in APM
                ApplicationPerformanceMonitoring.RecordError(
                    $"alert_{alert.Type.ToLowerInvariant()}",
                    "AlertingService");

                _logger.LogDebug("Processed alert: {AlertType} with severity {Severity}",
                    alert.Type, alert.Severity);

                await Task.CompletedTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process alert: {AlertType}", alert.Type);
            }
        }
    }

    /// <summary>
    /// Alert severity levels
    /// </summary>
    public enum AlertSeverity
    {
        Info,
        Warning,
        Critical
    }

    /// <summary>
    /// Alert model
    /// </summary>
    public class Alert
    {
        public string Type { get; set; } = string.Empty;
        public AlertSeverity Severity { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object> Context { get; set; } = new();
        public DateTime Timestamp { get; set; }
    }
}