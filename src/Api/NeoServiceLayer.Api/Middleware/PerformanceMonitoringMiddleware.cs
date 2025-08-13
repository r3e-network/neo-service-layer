using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.Observability.Logging;
using NeoServiceLayer.Infrastructure.Observability.Telemetry;

namespace NeoServiceLayer.Api.Middleware
{
    /// <summary>
    /// Middleware for monitoring request performance and collecting metrics.
    /// </summary>
    public class PerformanceMonitoringMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PerformanceMonitoringMiddleware> _logger;
        private readonly NeoServiceLayerInstrumentation _instrumentation;
        private readonly PerformanceMetricsCollector _metricsCollector;
        private readonly PerformanceThresholds _thresholds;

        public PerformanceMonitoringMiddleware(
            RequestDelegate next,
            ILogger<PerformanceMonitoringMiddleware> logger,
            NeoServiceLayerInstrumentation instrumentation,
            PerformanceMetricsCollector metricsCollector,
            PerformanceThresholds thresholds = null)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _instrumentation = instrumentation ?? throw new ArgumentNullException(nameof(instrumentation));
            _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));
            _thresholds = thresholds ?? new PerformanceThresholds();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var endpoint = context.Request.Path.Value;
            var method = context.Request.Method;
            
            // Skip monitoring for health and metrics endpoints
            if (ShouldSkipMonitoring(endpoint))
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var requestMetrics = new RequestMetrics
            {
                StartTime = DateTime.UtcNow,
                Endpoint = endpoint,
                Method = method,
                CorrelationId = context.Items["CorrelationId"]?.ToString()
            };

            // Capture initial memory
            var initialMemory = GC.GetTotalMemory(false);
            
            try
            {
                // Add performance headers to response
                context.Response.OnStarting(() =>
                {
                    var duration = stopwatch.ElapsedMilliseconds;
                    context.Response.Headers.Add("X-Response-Time", $"{duration}ms");
                    context.Response.Headers.Add("X-Server-Timing", $"total;dur={duration}");
                    return Task.CompletedTask;
                });

                await _next(context);
                
                requestMetrics.StatusCode = context.Response.StatusCode;
                requestMetrics.Success = context.Response.StatusCode < 400;
            }
            catch (Exception ex)
            {
                requestMetrics.Success = false;
                requestMetrics.Exception = ex.GetType().Name;
                requestMetrics.StatusCode = 500;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                
                // Capture final metrics
                requestMetrics.Duration = stopwatch.ElapsedMilliseconds;
                requestMetrics.MemoryDelta = (GC.GetTotalMemory(false) - initialMemory) / 1024.0; // KB
                requestMetrics.EndTime = DateTime.UtcNow;
                
                // Record metrics
                await RecordMetrics(requestMetrics);
                
                // Check performance thresholds
                await CheckThresholds(requestMetrics);
            }
        }

        private async Task RecordMetrics(RequestMetrics metrics)
        {
            // Record to OpenTelemetry
            _instrumentation.RecordRequest(
                metrics.Endpoint,
                metrics.Method,
                metrics.StatusCode,
                metrics.Duration);
            
            // Record to custom metrics collector
            await _metricsCollector.RecordRequestMetricsAsync(metrics);
            
            // Log if slow request
            if (metrics.Duration > _thresholds.SlowRequestThresholdMs)
            {
                _logger.LogWarning(
                    "Slow request detected: {Endpoint} took {Duration}ms (threshold: {Threshold}ms)",
                    metrics.Endpoint,
                    metrics.Duration,
                    _thresholds.SlowRequestThresholdMs);
            }
        }

        private async Task CheckThresholds(RequestMetrics metrics)
        {
            var violations = new List<string>();
            
            // Check duration threshold
            if (metrics.Duration > _thresholds.CriticalRequestThresholdMs)
            {
                violations.Add($"Duration: {metrics.Duration}ms (critical: {_thresholds.CriticalRequestThresholdMs}ms)");
            }
            
            // Check memory threshold
            if (metrics.MemoryDelta > _thresholds.MemoryDeltaThresholdKB)
            {
                violations.Add($"Memory delta: {metrics.MemoryDelta:F2}KB (threshold: {_thresholds.MemoryDeltaThresholdKB}KB)");
            }
            
            // Check error rate
            var errorRate = await _metricsCollector.GetErrorRateAsync(metrics.Endpoint, TimeSpan.FromMinutes(5));
            if (errorRate > _thresholds.ErrorRateThreshold)
            {
                violations.Add($"Error rate: {errorRate:P} (threshold: {_thresholds.ErrorRateThreshold:P})");
            }
            
            if (violations.Any())
            {
                _logger.LogError(
                    "Performance threshold violations for {Endpoint}: {Violations}",
                    metrics.Endpoint,
                    string.Join(", ", violations));
                
                // Trigger alert
                await _metricsCollector.TriggerPerformanceAlertAsync(metrics.Endpoint, violations);
            }
        }

        private bool ShouldSkipMonitoring(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            
            var skipPaths = new[] { "/health", "/metrics", "/swagger", "/favicon" };
            return skipPaths.Any(skip => path.StartsWith(skip, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Performance metrics for a single request.
    /// </summary>
    public class RequestMetrics
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Endpoint { get; set; }
        public string Method { get; set; }
        public string CorrelationId { get; set; }
        public double Duration { get; set; }
        public double MemoryDelta { get; set; }
        public int StatusCode { get; set; }
        public bool Success { get; set; }
        public string Exception { get; set; }
    }

    /// <summary>
    /// Performance threshold configuration.
    /// </summary>
    public class PerformanceThresholds
    {
        public double SlowRequestThresholdMs { get; set; } = 1000;
        public double CriticalRequestThresholdMs { get; set; } = 5000;
        public double MemoryDeltaThresholdKB { get; set; } = 10240; // 10MB
        public double ErrorRateThreshold { get; set; } = 0.05; // 5%
    }

    /// <summary>
    /// Collector for performance metrics.
    /// </summary>
    public class PerformanceMetricsCollector
    {
        private readonly ConcurrentDictionary<string, Queue<RequestMetrics>> _metricsHistory;
        private readonly ILogger<PerformanceMetricsCollector> _logger;
        private readonly int _maxHistorySize = 1000;

        public PerformanceMetricsCollector(ILogger<PerformanceMetricsCollector> logger)
        {
            _logger = logger;
            _metricsHistory = new ConcurrentDictionary<string, Queue<RequestMetrics>>();
        }

        public Task RecordRequestMetricsAsync(RequestMetrics metrics)
        {
            var queue = _metricsHistory.GetOrAdd(metrics.Endpoint, _ => new Queue<RequestMetrics>());
            
            lock (queue)
            {
                queue.Enqueue(metrics);
                
                // Maintain max history size
                while (queue.Count > _maxHistorySize)
                {
                    queue.Dequeue();
                }
            }
            
            return Task.CompletedTask;
        }

        public Task<double> GetErrorRateAsync(string endpoint, TimeSpan period)
        {
            if (!_metricsHistory.TryGetValue(endpoint, out var queue))
            {
                return Task.FromResult(0.0);
            }
            
            var cutoff = DateTime.UtcNow - period;
            
            lock (queue)
            {
                var recentMetrics = queue.Where(m => m.StartTime >= cutoff).ToList();
                
                if (recentMetrics.Count == 0)
                {
                    return Task.FromResult(0.0);
                }
                
                var errorCount = recentMetrics.Count(m => !m.Success);
                return Task.FromResult((double)errorCount / recentMetrics.Count);
            }
        }

        public Task<PerformanceStatistics> GetStatisticsAsync(string endpoint, TimeSpan period)
        {
            if (!_metricsHistory.TryGetValue(endpoint, out var queue))
            {
                return Task.FromResult(new PerformanceStatistics());
            }
            
            var cutoff = DateTime.UtcNow - period;
            
            lock (queue)
            {
                var recentMetrics = queue.Where(m => m.StartTime >= cutoff).ToList();
                
                if (recentMetrics.Count == 0)
                {
                    return Task.FromResult(new PerformanceStatistics());
                }
                
                var durations = recentMetrics.Select(m => m.Duration).OrderBy(d => d).ToList();
                
                return Task.FromResult(new PerformanceStatistics
                {
                    Count = recentMetrics.Count,
                    SuccessCount = recentMetrics.Count(m => m.Success),
                    ErrorCount = recentMetrics.Count(m => !m.Success),
                    AverageDuration = durations.Average(),
                    MinDuration = durations.First(),
                    MaxDuration = durations.Last(),
                    P50Duration = GetPercentile(durations, 0.5),
                    P95Duration = GetPercentile(durations, 0.95),
                    P99Duration = GetPercentile(durations, 0.99)
                });
            }
        }

        public Task TriggerPerformanceAlertAsync(string endpoint, List<string> violations)
        {
            _logger.LogError(
                "Performance alert triggered for {Endpoint}: {Violations}",
                endpoint,
                string.Join(", ", violations));
            
            // Here you would integrate with your alerting system (PagerDuty, Slack, etc.)
            
            return Task.CompletedTask;
        }

        private double GetPercentile(List<double> sortedValues, double percentile)
        {
            if (sortedValues.Count == 0) return 0;
            
            var index = (int)Math.Ceiling(percentile * sortedValues.Count) - 1;
            index = Math.Max(0, Math.Min(index, sortedValues.Count - 1));
            
            return sortedValues[index];
        }
    }

    /// <summary>
    /// Performance statistics for an endpoint.
    /// </summary>
    public class PerformanceStatistics
    {
        public int Count { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public double AverageDuration { get; set; }
        public double MinDuration { get; set; }
        public double MaxDuration { get; set; }
        public double P50Duration { get; set; }
        public double P95Duration { get; set; }
        public double P99Duration { get; set; }
        public double ErrorRate => Count > 0 ? (double)ErrorCount / Count : 0;
        public double SuccessRate => Count > 0 ? (double)SuccessCount / Count : 0;
    }

    /// <summary>
    /// Extension methods for performance monitoring middleware.
    /// </summary>
    public static class PerformanceMonitoringMiddlewareExtensions
    {
        public static IApplicationBuilder UsePerformanceMonitoring(
            this IApplicationBuilder builder,
            PerformanceThresholds thresholds = null)
        {
            return builder.UseMiddleware<PerformanceMonitoringMiddleware>(thresholds);
        }
    }
}