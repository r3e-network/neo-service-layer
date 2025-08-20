using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Linq;
using System.Net.Http;
using System.Threading;


namespace NeoServiceLayer.Infrastructure.Monitoring
{
    /// <summary>
    /// Comprehensive Application Performance Monitoring (APM) system
    /// Provides real-time performance metrics, distributed tracing, and alerting
    /// </summary>
    public static class ApplicationPerformanceMonitoring
    {
        private static readonly ActivitySource ActivitySource = new("NeoServiceLayer");
        private static readonly Meter Meter = new("NeoServiceLayer");

        // Performance Counters
        private static readonly Counter<long> RequestCounter = Meter.CreateCounter<long>("neo_requests_total", "Total number of requests");
        private static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>("neo_request_duration_seconds", "Request duration in seconds");
        private static readonly Gauge<long> ActiveConnections = Meter.CreateGauge<long>("neo_active_connections", "Number of active connections");
        private static readonly Counter<long> ErrorCounter = Meter.CreateCounter<long>("neo_errors_total", "Total number of errors");

        // Performance Thresholds (configurable via appsettings.json)
        public static class Thresholds
        {
            public static double ResponseTimeWarning { get; set; } = 1.0; // 1 second
            public static double ResponseTimeError { get; set; } = 5.0;   // 5 seconds
            public static long MemoryWarningMB { get; set; } = 500;       // 500 MB
            public static long MemoryErrorMB { get; set; } = 1000;        // 1 GB
            public static double CpuWarningPercent { get; set; } = 70.0;  // 70%
            public static double CpuErrorPercent { get; set; } = 90.0;    // 90%
        }

        /// <summary>
        /// Configure APM services in DI container
        /// </summary>
        public static IServiceCollection AddApplicationPerformanceMonitoring(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Load APM configuration
            var apmConfig = configuration.GetSection("ApplicationPerformanceMonitoring");
            services.Configure<ApmConfiguration>(apmConfig);

            // Configure performance thresholds
            Thresholds.ResponseTimeWarning = apmConfig.GetValue("ResponseTimeWarningSeconds", 1.0);
            Thresholds.ResponseTimeError = apmConfig.GetValue("ResponseTimeErrorSeconds", 5.0);
            Thresholds.MemoryWarningMB = apmConfig.GetValue("MemoryWarningMB", 500L);
            Thresholds.MemoryErrorMB = apmConfig.GetValue("MemoryErrorMB", 1000L);
            Thresholds.CpuWarningPercent = apmConfig.GetValue("CpuWarningPercent", 70.0);
            Thresholds.CpuErrorPercent = apmConfig.GetValue("CpuErrorPercent", 90.0);

            // Register APM services
            services.AddSingleton<IPerformanceMonitor, PerformanceMonitor>();
            services.AddHostedService<MetricsCollectorService>();
            services.AddHostedService<AlertingService>();

            // Configure OpenTelemetry
            services.AddOpenTelemetry()
                .WithTracing(builder =>
                {
                    builder
                        .SetResourceBuilder(ResourceBuilder.CreateDefault()
                            .AddService("NeoServiceLayer", "1.0.0"))
                        .AddSource(ActivitySource.Name)
                        .AddAspNetCoreInstrumentation(options =>
                        {
                            options.RecordException = true;
                            options.EnrichWithHttpRequest = (activity, request) =>
                            {
                                activity.SetTag("http.request.size", request.ContentLength);
                                activity.SetTag("http.request.user_agent", request.Headers.UserAgent?.ToString());
                            };
                            options.EnrichWithHttpResponse = (activity, response) =>
                            {
                                activity.SetTag("http.response.size", response.ContentLength);
                            };
                        })
                        .AddHttpClientInstrumentation()
                        .AddEntityFrameworkCoreInstrumentation()
                        .AddConsoleExporter()
                        .AddJaegerExporter();

                    // Add custom exporters based on configuration
                    var jaegerEndpoint = apmConfig.GetValue<string>("JaegerEndpoint");
                    var zipkinEndpoint = apmConfig.GetValue<string>("ZipkinEndpoint");

                    if (!string.IsNullOrEmpty(jaegerEndpoint))
                    {
                        builder.AddJaegerExporter(options =>
                        {
                            options.Endpoint = new Uri(jaegerEndpoint);
                        });
                    }

                    if (!string.IsNullOrEmpty(zipkinEndpoint))
                    {
                        builder.AddZipkinExporter(options =>
                        {
                            options.Endpoint = new Uri(zipkinEndpoint);
                        });
                    }
                })
                .WithMetrics(builder =>
                {
                    builder
                        .SetResourceBuilder(ResourceBuilder.CreateDefault()
                            .AddService("NeoServiceLayer", "1.0.0"))
                        .AddMeter(Meter.Name)
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddConsoleExporter()
                        .AddPrometheusExporter();
                });

            return services;
        }

        /// <summary>
        /// Start a new performance monitoring activity
        /// </summary>
        public static Activity? StartActivity(string name, Dictionary<string, object?>? tags = null)
        {
            var activity = ActivitySource.StartActivity(name);

            if (activity != null && tags != null)
            {
                foreach (var tag in tags)
                {
                    activity.SetTag(tag.Key, tag.Value?.ToString());
                }
            }

            return activity;
        }

        /// <summary>
        /// Record request metrics
        /// </summary>
        public static void RecordRequest(string method, string endpoint, double durationSeconds, int statusCode)
        {
            var tags = new KeyValuePair<string, object?>[]
            {
                new("method", method),
                new("endpoint", endpoint),
                new("status_code", statusCode),
                new("status_class", GetStatusClass(statusCode))
            };

            RequestCounter.Add(1, tags);
            RequestDuration.Record(durationSeconds, tags);

            // Check performance thresholds
            if (durationSeconds > Thresholds.ResponseTimeError)
            {
                ErrorCounter.Add(1, new KeyValuePair<string, object?>[]
                {
                    new("type", "slow_response"),
                    new("severity", "error"),
                    new("endpoint", endpoint),
                    new("duration", durationSeconds)
                });
            }
            else if (durationSeconds > Thresholds.ResponseTimeWarning)
            {
                ErrorCounter.Add(1, new KeyValuePair<string, object?>[]
                {
                    new("type", "slow_response"),
                    new("severity", "warning"),
                    new("endpoint", endpoint),
                    new("duration", durationSeconds)
                });
            }
        }

        /// <summary>
        /// Record error metrics
        /// </summary>
        public static void RecordError(string errorType, string component, Exception? exception = null)
        {
            var tags = new KeyValuePair<string, object?>[]
            {
                new("error_type", errorType),
                new("component", component),
                new("exception_type", exception?.GetType().Name ?? "unknown")
            };

            ErrorCounter.Add(1, tags);
        }

        /// <summary>
        /// Update active connections count
        /// </summary>
        public static void UpdateActiveConnections(long count)
        {
            ActiveConnections.Record(count);
        }

        private static string GetStatusClass(int statusCode)
        {
            return statusCode switch
            {
                >= 200 and < 300 => "2xx",
                >= 300 and < 400 => "3xx",
                >= 400 and < 500 => "4xx",
                >= 500 => "5xx",
                _ => "unknown"
            };
        }
    }

    /// <summary>
    /// APM Configuration model
    /// </summary>
    public class ApmConfiguration
    {
        public double ResponseTimeWarningSeconds { get; set; } = 1.0;
        public double ResponseTimeErrorSeconds { get; set; } = 5.0;
        public long MemoryWarningMB { get; set; } = 500;
        public long MemoryErrorMB { get; set; } = 1000;
        public double CpuWarningPercent { get; set; } = 70.0;
        public double CpuErrorPercent { get; set; } = 90.0;
        public string? JaegerEndpoint { get; set; }
        public string? ZipkinEndpoint { get; set; }
        public bool EnableDetailedMetrics { get; set; } = true;
        public bool EnableDistributedTracing { get; set; } = true;
        public bool EnableAlerting { get; set; } = true;
        public int MetricsCollectionIntervalSeconds { get; set; } = 30;
    }

    /// <summary>
    /// Performance monitoring interface
    /// </summary>
    public interface IPerformanceMonitor
    {
        Task<SystemMetrics> GetSystemMetricsAsync();
        Task<ApplicationMetrics> GetApplicationMetricsAsync();
        void RecordCustomMetric(string name, double value, Dictionary<string, string>? tags = null);
    }

    /// <summary>
    /// System performance metrics
    /// </summary>
    public class SystemMetrics
    {
        public double CpuUsagePercent { get; set; }
        public long MemoryUsageMB { get; set; }
        public long TotalMemoryMB { get; set; }
        public double MemoryUsagePercent => TotalMemoryMB > 0 ? (MemoryUsageMB * 100.0) / TotalMemoryMB : 0;
        public long DiskUsageMB { get; set; }
        public long NetworkBytesReceived { get; set; }
        public long NetworkBytesSent { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Application performance metrics
    /// </summary>
    public class ApplicationMetrics
    {
        public long TotalRequests { get; set; }
        public double AverageResponseTime { get; set; }
        public double P95ResponseTime { get; set; }
        public double P99ResponseTime { get; set; }
        public long ErrorCount { get; set; }
        public double ErrorRate => TotalRequests > 0 ? (ErrorCount * 100.0) / TotalRequests : 0;
        public long ActiveConnections { get; set; }
        public int ThreadPoolThreads { get; set; }
        public int CompletionPortThreads { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}