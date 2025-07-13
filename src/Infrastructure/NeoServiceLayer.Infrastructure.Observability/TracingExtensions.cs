using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace NeoServiceLayer.Infrastructure.Observability;

public static class TracingExtensions
{
    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceName = configuration["OpenTelemetry:ServiceName"] ?? "NeoServiceLayer";
        var serviceVersion = configuration["OpenTelemetry:ServiceVersion"] ?? "1.0.0";
        var serviceNamespace = configuration["OpenTelemetry:ServiceNamespace"] ?? "neo-service-layer";

        // Configure Activity Source
        var activitySource = new ActivitySource(serviceName, serviceVersion);
        services.AddSingleton(activitySource);

        // Configure Resource
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName: serviceName, serviceVersion: serviceVersion, serviceNamespace: serviceNamespace)
            .AddTelemetrySdk()
            .AddEnvironmentVariableDetector()
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production",
                ["service.instance.id"] = Environment.MachineName,
                ["host.name"] = Environment.MachineName,
                ["os.type"] = Environment.OSVersion.Platform.ToString(),
                ["process.runtime.name"] = ".NET",
                ["process.runtime.version"] = Environment.Version.ToString(),
            });

        // Configure OpenTelemetry
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder
                    .SetResourceBuilder(resourceBuilder)
                    .SetSampler(new TraceIdRatioBasedSampler(GetSamplingRatio(configuration)))
                    .AddSource(serviceName)
                    .AddSource("Microsoft.AspNetCore.Hosting")
                    .AddSource("Microsoft.AspNetCore.Server.Kestrel")
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.EnableGrpcAspNetCoreSupport = true;
                        options.Filter = (httpContext) =>
                        {
                            // Don't trace health checks and metrics endpoints
                            var path = httpContext.Request.Path.Value;
                            return !path.Contains("/health") && !path.Contains("/metrics");
                        };
                        options.Enrich = (activity, eventName, rawObject) =>
                        {
                            if (eventName == "OnStartActivity")
                            {
                                if (rawObject is HttpRequest httpRequest)
                                {
                                    activity.SetTag("http.request.body.size", httpRequest.ContentLength);
                                    activity.SetTag("http.user_agent", httpRequest.Headers["User-Agent"].FirstOrDefault());
                                    activity.SetTag("http.client_ip", httpRequest.HttpContext.Connection.RemoteIpAddress?.ToString());
                                }
                            }
                        };
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.SetHttpFlavor = true;
                        options.Filter = (httpRequestMessage) =>
                        {
                            // Don't trace calls to telemetry endpoints
                            var uri = httpRequestMessage.RequestUri?.ToString() ?? "";
                            return !uri.Contains("jaeger") && !uri.Contains("prometheus");
                        };
                        options.Enrich = (activity, eventName, rawObject) =>
                        {
                            if (eventName == "OnStartActivity")
                            {
                                if (rawObject is HttpRequestMessage httpRequestMessage)
                                {
                                    activity.SetTag("http.request.body.size", httpRequestMessage.Content?.Headers.ContentLength);
                                }
                            }
                        };
                    })
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.SetDbStatementForText = true;
                        options.SetDbStatementForStoredProcedure = true;
                        options.EnrichWithIDbCommand = (activity, command) =>
                        {
                            activity.SetTag("db.command.timeout", command.CommandTimeout);
                            activity.SetTag("db.command.type", command.CommandType.ToString());
                        };
                    })
                    .AddRedisInstrumentation(options =>
                    {
                        options.SetVerboseDatabaseStatements = true;
                        options.EnrichActivityWithTimingEvents = true;
                    })
                    .AddCustomInstrumentation()
                    .ConfigureExporters(configuration, tracerProviderBuilder);
            })
            .WithMetrics(meterProviderBuilder =>
            {
                meterProviderBuilder
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddMeter("Microsoft.AspNetCore.Hosting")
                    .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
                    .AddMeter("System.Net.Http")
                    .AddMeter(serviceName)
                    .AddView("http.server.duration",
                        new ExplicitBucketHistogramConfiguration
                        {
                            Boundaries = new double[] { 0, 5, 10, 25, 50, 75, 100, 250, 500, 750, 1000, 2500, 5000, 7500, 10000 }
                        })
                    .ConfigureMetricsExporters(configuration, meterProviderBuilder);
            });

        // Add correlation ID support
        services.AddScoped<CorrelationIdMiddleware>();
        services.AddHttpContextAccessor();

        // Add custom activity enricher
        services.AddSingleton<IActivityEnricher, CustomActivityEnricher>();

        return services;
    }

    private static void ConfigureExporters(IConfiguration configuration, TracerProviderBuilder builder)
    {
        // Console exporter (for development)
        if (configuration.GetValue<bool>("OpenTelemetry:Exporters:Console:Enabled"))
        {
            builder.AddConsoleExporter();
        }

        // Jaeger exporter
        if (configuration.GetValue<bool>("OpenTelemetry:Exporters:Jaeger:Enabled"))
        {
            builder.AddJaegerExporter(options =>
            {
                options.AgentHost = configuration["OpenTelemetry:Exporters:Jaeger:AgentHost"] ?? "localhost";
                options.AgentPort = configuration.GetValue<int>("OpenTelemetry:Exporters:Jaeger:AgentPort", 6831);
                options.ExportProcessorType = ExportProcessorType.Batch;
                options.BatchExportProcessorOptions = new BatchExportProcessorOptions<Activity>
                {
                    MaxQueueSize = 2048,
                    ScheduledDelayMilliseconds = 5000,
                    ExporterTimeoutMilliseconds = 30000,
                    MaxExportBatchSize = 512,
                };
            });
        }

        // OTLP exporter (for production)
        if (configuration.GetValue<bool>("OpenTelemetry:Exporters:Otlp:Enabled"))
        {
            builder.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(configuration["OpenTelemetry:Exporters:Otlp:Endpoint"] ?? "http://localhost:4317");
                options.Protocol = OtlpExportProtocol.Grpc;
                options.Headers = configuration["OpenTelemetry:Exporters:Otlp:Headers"];
                options.TimeoutMilliseconds = 30000;
            });
        }
    }

    private static void ConfigureMetricsExporters(IConfiguration configuration, MeterProviderBuilder builder)
    {
        // Console exporter (for development)
        if (configuration.GetValue<bool>("OpenTelemetry:Exporters:Console:Enabled"))
        {
            builder.AddConsoleExporter();
        }

        // Prometheus exporter
        if (configuration.GetValue<bool>("OpenTelemetry:Exporters:Prometheus:Enabled"))
        {
            builder.AddPrometheusExporter();
        }

        // OTLP exporter (for production)
        if (configuration.GetValue<bool>("OpenTelemetry:Exporters:Otlp:Enabled"))
        {
            builder.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(configuration["OpenTelemetry:Exporters:Otlp:Endpoint"] ?? "http://localhost:4317");
                options.Protocol = OtlpExportProtocol.Grpc;
            });
        }
    }

    private static TracerProviderBuilder AddCustomInstrumentation(this TracerProviderBuilder builder)
    {
        // Add custom instrumentation for Neo blockchain operations
        builder.AddSource("NeoServiceLayer.Blockchain");
        builder.AddSource("NeoServiceLayer.KeyManagement");
        builder.AddSource("NeoServiceLayer.Storage");
        builder.AddSource("NeoServiceLayer.AI");
        builder.AddSource("NeoServiceLayer.Tee");

        return builder;
    }

    private static double GetSamplingRatio(IConfiguration configuration)
    {
        var ratio = configuration.GetValue<double>("OpenTelemetry:SamplingRatio", 1.0);
        return Math.Max(0.0, Math.Min(1.0, ratio));
    }

    public static IApplicationBuilder UseObservability(this IApplicationBuilder app)
    {
        // Add correlation ID middleware
        app.UseMiddleware<CorrelationIdMiddleware>();

        // Add Prometheus metrics endpoint
        app.UseOpenTelemetryPrometheusScrapingEndpoint();

        return app;
    }
}

// Correlation ID Middleware
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrGenerateCorrelationId(context);

        // Add to response headers
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        // Add to log context
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            // Add to current activity
            Activity.Current?.SetTag("correlation.id", correlationId);

            await _next(context);
        }
    }

    private string GetOrGenerateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId))
        {
            return correlationId;
        }

        return Guid.NewGuid().ToString();
    }
}

// Custom Activity Enricher
public interface IActivityEnricher
{
    void Enrich(Activity activity, string eventName, object obj);
}

public class CustomActivityEnricher : IActivityEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CustomActivityEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(Activity activity, string eventName, object obj)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            // Add user information
            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                activity.SetTag("user.id", httpContext.User.FindFirst("sub")?.Value);
                activity.SetTag("user.name", httpContext.User.Identity.Name);
            }

            // Add custom headers
            if (httpContext.Request.Headers.TryGetValue("X-Client-Version", out var clientVersion))
            {
                activity.SetTag("client.version", clientVersion.ToString());
            }

            // Add request size
            activity.SetTag("http.request.size", httpContext.Request.ContentLength ?? 0);
        }

        // Add environment info
        activity.SetTag("environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production");
        activity.SetTag("hostname", Environment.MachineName);
    }
}

// Extension for creating custom spans
public static class ActivityExtensions
{
    public static Activity? StartActivity(this ActivitySource source, string name, ActivityKind kind = ActivityKind.Internal)
    {
        return source.StartActivity(name, kind);
    }

    public static void RecordException(this Activity activity, Exception exception)
    {
        activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity?.RecordException(exception);
    }

    public static void AddEvent(this Activity activity, string name, Dictionary<string, object>? attributes = null)
    {
        if (activity != null)
        {
            var activityEvent = new ActivityEvent(name, DateTimeOffset.UtcNow, new ActivityTagsCollection(attributes));
            activity.AddEvent(activityEvent);
        }
    }
}