using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace NeoServiceLayer.Infrastructure.Security
{
    /// <summary>
    /// Production-ready Application Performance Monitoring integration
    /// </summary>
    public interface IApplicationPerformanceMonitoring
    {
        void RecordMetric(string name, double value, Dictionary<string, object>? tags = null);
        void RecordLatency(string operation, double milliseconds);
        void RecordError(string operation, Exception exception);
        Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal);
        void RecordEvent(string eventName, Dictionary<string, object>? attributes = null);
    }

    /// <summary>
    /// OpenTelemetry-based APM implementation for production use
    /// </summary>
    public class OpenTelemetryApmService : IApplicationPerformanceMonitoring
    {
        private readonly Meter _meter;
        private readonly ActivitySource _activitySource;
        private readonly ILogger<OpenTelemetryApmService> _logger;
        
        // Metrics instruments
        private readonly Counter<double> _metricCounter;
        private readonly Histogram<double> _latencyHistogram;
        private readonly Counter<long> _errorCounter;
        private readonly UpDownCounter<long> _activeRequestsCounter;

        public OpenTelemetryApmService(ILogger<OpenTelemetryApmService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Initialize OpenTelemetry instruments
            _meter = new Meter("NeoServiceLayer.Security", "1.0.0");
            _activitySource = new ActivitySource("NeoServiceLayer.Security", "1.0.0");
            
            // Create metric instruments
            _metricCounter = _meter.CreateCounter<double>(
                "neo.security.metrics",
                "count",
                "Security metrics counter");
                
            _latencyHistogram = _meter.CreateHistogram<double>(
                "neo.security.latency",
                "milliseconds",
                "Operation latency histogram");
                
            _errorCounter = _meter.CreateCounter<long>(
                "neo.security.errors",
                "count",
                "Error counter");
                
            _activeRequestsCounter = _meter.CreateUpDownCounter<long>(
                "neo.security.active_requests",
                "count",
                "Active requests counter");
        }

        public void RecordMetric(string name, double value, Dictionary<string, object>? tags = null)
        {
            try
            {
                var tagList = CreateTagList(tags);
                tagList.Add("metric_name", name);
                
                _metricCounter.Add(value, tagList.ToArray());
                
                // Also record to current activity if one exists
                var activity = Activity.Current;
                if (activity != null)
                {
                    activity.SetTag($"metric.{name}", value);
                    foreach (var tag in tagList)
                    {
                        activity.SetTag($"metric.{name}.{tag.Key}", tag.Value);
                    }
                }
                
                _logger.LogDebug("Recorded metric {MetricName}: {Value}", name, value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record metric {MetricName}", name);
            }
        }

        public void RecordLatency(string operation, double milliseconds)
        {
            try
            {
                var tags = new TagList
                {
                    { "operation", operation }
                };
                
                _latencyHistogram.Record(milliseconds, tags.ToArray());
                
                // Add to current span if exists
                Activity.Current?.SetTag($"latency.{operation}", milliseconds);
                
                _logger.LogDebug("Recorded latency for {Operation}: {Milliseconds}ms", operation, milliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record latency for {Operation}", operation);
            }
        }

        public void RecordError(string operation, Exception exception)
        {
            try
            {
                var tags = new TagList
                {
                    { "operation", operation },
                    { "error_type", exception.GetType().Name },
                    { "error_message", exception.Message }
                };
                
                _errorCounter.Add(1, tags.ToArray());
                
                // Mark current activity as error
                var activity = Activity.Current;
                if (activity != null)
                {
                    activity.SetStatus(ActivityStatusCode.Error, exception.Message);
                    activity.RecordException(exception);
                }
                
                _logger.LogError(exception, "Recorded error for operation {Operation}", operation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record error for {Operation}", operation);
            }
        }

        public Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
        {
            try
            {
                var activity = _activitySource.StartActivity(name, kind);
                
                if (activity != null)
                {
                    _activeRequestsCounter.Add(1);
                    
                    // Add default tags
                    activity.SetTag("service.name", "NeoServiceLayer");
                    activity.SetTag("service.version", "1.0.0");
                    activity.SetTag("deployment.environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production");
                    
                    // Register callback to decrement counter when activity ends
                    activity.Disposed += (sender, e) => _activeRequestsCounter.Add(-1);
                }
                
                return activity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start activity {ActivityName}", name);
                return null;
            }
        }

        public void RecordEvent(string eventName, Dictionary<string, object>? attributes = null)
        {
            try
            {
                var activity = Activity.Current;
                if (activity != null)
                {
                    var activityEvent = new ActivityEvent(
                        eventName,
                        DateTimeOffset.UtcNow,
                        CreateActivityTags(attributes));
                    
                    activity.AddEvent(activityEvent);
                }
                
                _logger.LogDebug("Recorded event {EventName}", eventName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record event {EventName}", eventName);
            }
        }

        private TagList CreateTagList(Dictionary<string, object>? tags)
        {
            var tagList = new TagList();
            
            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    tagList.Add(tag.Key, tag.Value?.ToString() ?? "null");
                }
            }
            
            return tagList;
        }

        private ActivityTagsCollection CreateActivityTags(Dictionary<string, object>? attributes)
        {
            var tags = new ActivityTagsCollection();
            
            if (attributes != null)
            {
                foreach (var attr in attributes)
                {
                    tags.Add(attr.Key, attr.Value?.ToString() ?? "null");
                }
            }
            
            return tags;
        }
    }

    /// <summary>
    /// Extension methods for configuring OpenTelemetry in the application
    /// </summary>
    public static class OpenTelemetryExtensions
    {
        public static void ConfigureOpenTelemetry(this IServiceCollection services, IConfiguration configuration)
        {
            // Add OpenTelemetry services
            services.AddSingleton<IApplicationPerformanceMonitoring, OpenTelemetryApmService>();
            
            // Configure OpenTelemetry
            services.AddOpenTelemetry()
                .WithMetrics(metrics =>
                {
                    metrics
                        .AddMeter("NeoServiceLayer.Security")
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddProcessInstrumentation();
                    
                    // Configure exporters based on configuration
                    var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"];
                    if (!string.IsNullOrEmpty(otlpEndpoint))
                    {
                        metrics.AddOtlpExporter(options =>
                        {
                            options.Endpoint = new Uri(otlpEndpoint);
                        });
                    }
                    
                    // Add Prometheus exporter for scraping
                    metrics.AddPrometheusExporter();
                })
                .WithTracing(tracing =>
                {
                    tracing
                        .AddSource("NeoServiceLayer.Security")
                        .AddAspNetCoreInstrumentation(options =>
                        {
                            options.RecordException = true;
                        })
                        .AddHttpClientInstrumentation(options =>
                        {
                            options.RecordException = true;
                        })
                        .AddEntityFrameworkCoreInstrumentation(options =>
                        {
                            options.SetDbStatementForText = true;
                        });
                    
                    // Configure exporters
                    var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"];
                    if (!string.IsNullOrEmpty(otlpEndpoint))
                    {
                        tracing.AddOtlpExporter(options =>
                        {
                            options.Endpoint = new Uri(otlpEndpoint);
                        });
                    }
                    
                    // Add Jaeger exporter if configured
                    var jaegerEndpoint = configuration["OpenTelemetry:JaegerEndpoint"];
                    if (!string.IsNullOrEmpty(jaegerEndpoint))
                    {
                        tracing.AddJaegerExporter(options =>
                        {
                            options.AgentHost = configuration["OpenTelemetry:JaegerHost"] ?? "localhost";
                            options.AgentPort = configuration.GetValue<int>("OpenTelemetry:JaegerPort", 6831);
                        });
                    }
                });
        }
    }
}