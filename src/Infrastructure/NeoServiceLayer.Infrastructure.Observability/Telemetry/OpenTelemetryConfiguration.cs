using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace NeoServiceLayer.Infrastructure.Observability.Telemetry
{
    /// <summary>
    /// Configuration for OpenTelemetry instrumentation.
    /// </summary>
    public static class OpenTelemetryConfiguration
    {
        public static IServiceCollection AddNeoServiceLayerTelemetry(
            this IServiceCollection services,
            IConfiguration configuration,
            string serviceName = "NeoServiceLayer")
        {
            var telemetryConfig = configuration.GetSection("Telemetry").Get<TelemetryConfiguration>() 
                ?? new TelemetryConfiguration();

            // Configure resource attributes
            var resourceBuilder = ResourceBuilder.CreateDefault()
                .AddService(
                    serviceName: serviceName,
                    serviceVersion: typeof(OpenTelemetryConfiguration).Assembly.GetName().Version?.ToString() ?? "1.0.0",
                    serviceInstanceId: Environment.MachineName)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["environment"] = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production",
                    ["deployment.environment"] = configuration["DEPLOYMENT_ENVIRONMENT"] ?? "production",
                    ["host.name"] = Environment.MachineName,
                    ["os.type"] = Environment.OSVersion.Platform.ToString(),
                    ["process.runtime.name"] = ".NET",
                    ["process.runtime.version"] = Environment.Version.ToString(),
                    ["sgx.enabled"] = configuration["NEO_ALLOW_SGX_SIMULATION"] ?? "false",
                    ["sgx.mode"] = configuration["SGX_MODE"] ?? "SIM"
                });

            // Configure tracing
            if (telemetryConfig.TracingEnabled)
            {
                services.AddOpenTelemetry()
                    .WithTracing(builder =>
                    {
                        builder
                            .SetResourceBuilder(resourceBuilder)
                            .SetSampler(new TraceIdRatioBasedSampler(telemetryConfig.SamplingRatio))
                            .AddAspNetCoreInstrumentation(options =>
                            {
                                options.RecordException = true;
                                options.Filter = (httpContext) =>
                                {
                                    // Skip health check endpoints
                                    var path = httpContext.Request.Path.Value;
                                    return !path.Contains("/health", StringComparison.OrdinalIgnoreCase) &&
                                           !path.Contains("/metrics", StringComparison.OrdinalIgnoreCase);
                                };
                            })
                            .AddHttpClientInstrumentation(options =>
                            {
                                options.RecordException = true;
                                options.SetHttpFlavor = true;
                            })
                            .AddEntityFrameworkCoreInstrumentation(options =>
                            {
                                options.SetDbStatementForText = true;
                                options.SetDbStatementForStoredProcedure = true;
                            })
                            .AddSource("NeoServiceLayer.*")
                            .AddSource("SGX.*")
                            .AddSource("Blockchain.*");

                        // Configure exporters
                        ConfigureTraceExporters(builder, telemetryConfig);
                    });
            }

            // Configure metrics
            if (telemetryConfig.MetricsEnabled)
            {
                services.AddOpenTelemetry()
                    .WithMetrics(builder =>
                    {
                        builder
                            .SetResourceBuilder(resourceBuilder)
                            .AddAspNetCoreInstrumentation()
                            .AddHttpClientInstrumentation()
                            .AddRuntimeInstrumentation()
                            .AddProcessInstrumentation()
                            .AddMeter("NeoServiceLayer.*")
                            .AddMeter("SGX.*")
                            .AddMeter("Blockchain.*")
                            .AddView("request.duration",
                                new ExplicitBucketHistogramConfiguration
                                {
                                    Boundaries = new double[] { 0, 5, 10, 25, 50, 75, 100, 250, 500, 1000, 2500, 5000 }
                                });

                        // Configure exporters
                        ConfigureMetricExporters(builder, telemetryConfig);
                    });
            }

            // Configure logging
            if (telemetryConfig.LoggingEnabled)
            {
                services.AddLogging(builder =>
                {
                    builder.AddOpenTelemetry(options =>
                    {
                        options.SetResourceBuilder(resourceBuilder);
                        options.IncludeFormattedMessage = true;
                        options.IncludeScopes = true;
                        options.ParseStateValues = true;
                        
                        // Configure exporters
                        ConfigureLogExporters(options, telemetryConfig);
                    });
                });
            }

            // Register custom instrumentation
            services.AddSingleton<NeoServiceLayerInstrumentation>();
            services.AddHostedService<TelemetryHostedService>();

            return services;
        }

        private static void ConfigureTraceExporters(TracerProviderBuilder builder, TelemetryConfiguration config)
        {
            // Console exporter (for development)
            if (config.UseConsoleExporter)
            {
                builder.AddConsoleExporter();
            }

            // OTLP exporter (for production)
            if (!string.IsNullOrEmpty(config.OtlpEndpoint))
            {
                builder.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(config.OtlpEndpoint);
                    options.Protocol = config.OtlpProtocol == "grpc" 
                        ? OtlpExportProtocol.Grpc 
                        : OtlpExportProtocol.HttpProtobuf;
                    options.Headers = config.OtlpHeaders;
                    options.TimeoutMilliseconds = config.ExportTimeoutMs;
                });
            }

            // Jaeger exporter
            if (!string.IsNullOrEmpty(config.JaegerEndpoint))
            {
                builder.AddJaegerExporter(options =>
                {
                    options.AgentHost = ExtractHost(config.JaegerEndpoint);
                    options.AgentPort = ExtractPort(config.JaegerEndpoint, 6831);
                    options.ExportProcessorType = ExportProcessorType.Batch;
                    options.MaxPayloadSizeInBytes = 65000;
                });
            }

            // Zipkin exporter
            if (!string.IsNullOrEmpty(config.ZipkinEndpoint))
            {
                builder.AddZipkinExporter(options =>
                {
                    options.Endpoint = new Uri(config.ZipkinEndpoint);
                    options.UseShortTraceIds = false;
                });
            }
        }

        private static void ConfigureMetricExporters(MeterProviderBuilder builder, TelemetryConfiguration config)
        {
            // Console exporter (for development)
            if (config.UseConsoleExporter)
            {
                builder.AddConsoleExporter();
            }

            // OTLP exporter
            if (!string.IsNullOrEmpty(config.OtlpEndpoint))
            {
                builder.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(config.OtlpEndpoint);
                    options.Protocol = config.OtlpProtocol == "grpc"
                        ? OtlpExportProtocol.Grpc
                        : OtlpExportProtocol.HttpProtobuf;
                    options.Headers = config.OtlpHeaders;
                    options.TimeoutMilliseconds = config.ExportTimeoutMs;
                });
            }

            // Prometheus exporter
            if (config.PrometheusEnabled)
            {
                builder.AddPrometheusExporter();
            }
        }

        private static void ConfigureLogExporters(OpenTelemetryLoggerOptions options, TelemetryConfiguration config)
        {
            // Console exporter (for development)
            if (config.UseConsoleExporter)
            {
                options.AddConsoleExporter();
            }

            // OTLP exporter
            if (!string.IsNullOrEmpty(config.OtlpEndpoint))
            {
                options.AddOtlpExporter(otlpOptions =>
                {
                    otlpOptions.Endpoint = new Uri(config.OtlpEndpoint);
                    otlpOptions.Protocol = config.OtlpProtocol == "grpc"
                        ? OtlpExportProtocol.Grpc
                        : OtlpExportProtocol.HttpProtobuf;
                    otlpOptions.Headers = config.OtlpHeaders;
                    otlpOptions.TimeoutMilliseconds = config.ExportTimeoutMs;
                });
            }
        }

        private static string ExtractHost(string endpoint)
        {
            if (Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            {
                return uri.Host;
            }
            
            var parts = endpoint.Split(':');
            return parts.Length > 0 ? parts[0] : "localhost";
        }

        private static int ExtractPort(string endpoint, int defaultPort)
        {
            if (Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            {
                return uri.Port > 0 ? uri.Port : defaultPort;
            }
            
            var parts = endpoint.Split(':');
            if (parts.Length > 1 && int.TryParse(parts[1], out var port))
            {
                return port;
            }
            
            return defaultPort;
        }
    }

    /// <summary>
    /// Telemetry configuration settings.
    /// </summary>
    public class TelemetryConfiguration
    {
        public bool TracingEnabled { get; set; } = true;
        public bool MetricsEnabled { get; set; } = true;
        public bool LoggingEnabled { get; set; } = true;
        public double SamplingRatio { get; set; } = 1.0;
        public bool UseConsoleExporter { get; set; } = false;
        public string OtlpEndpoint { get; set; } = "http://localhost:4317";
        public string OtlpProtocol { get; set; } = "grpc";
        public string OtlpHeaders { get; set; }
        public string JaegerEndpoint { get; set; }
        public string ZipkinEndpoint { get; set; }
        public bool PrometheusEnabled { get; set; } = true;
        public int ExportTimeoutMs { get; set; } = 30000;
    }

    /// <summary>
    /// Custom instrumentation for Neo Service Layer.
    /// </summary>
    public class NeoServiceLayerInstrumentation
    {
        private readonly ActivitySource _activitySource;
        private readonly Meter _meter;
        private readonly Counter<long> _requestCounter;
        private readonly Histogram<double> _requestDuration;
        private readonly ObservableGauge<double> _memoryUsage;

        public NeoServiceLayerInstrumentation()
        {
            var version = typeof(NeoServiceLayerInstrumentation).Assembly.GetName().Version?.ToString() ?? "1.0.0";
            
            _activitySource = new ActivitySource("NeoServiceLayer.Core", version);
            _meter = new Meter("NeoServiceLayer.Metrics", version);
            
            _requestCounter = _meter.CreateCounter<long>(
                "neoservicelayer.requests.total",
                description: "Total number of requests processed");
            
            _requestDuration = _meter.CreateHistogram<double>(
                "neoservicelayer.request.duration",
                unit: "ms",
                description: "Request processing duration");
            
            _memoryUsage = _meter.CreateObservableGauge(
                "neoservicelayer.memory.usage",
                () => GC.GetTotalMemory(false) / (1024.0 * 1024.0),
                unit: "MB",
                description: "Memory usage in megabytes");
        }

        public Activity StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
        {
            return _activitySource.StartActivity(name, kind);
        }

        public void RecordRequest(string endpoint, string method, int statusCode, double durationMs)
        {
            var tags = new TagList
            {
                { "endpoint", endpoint },
                { "method", method },
                { "status_code", statusCode }
            };
            
            _requestCounter.Add(1, tags);
            _requestDuration.Record(durationMs, tags);
        }
    }

    /// <summary>
    /// Hosted service for telemetry initialization.
    /// </summary>
    public class TelemetryHostedService : IHostedService
    {
        private readonly ILogger<TelemetryHostedService> _logger;
        private readonly NeoServiceLayerInstrumentation _instrumentation;

        public TelemetryHostedService(
            ILogger<TelemetryHostedService> logger,
            NeoServiceLayerInstrumentation instrumentation)
        {
            _logger = logger;
            _instrumentation = instrumentation;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Telemetry service started");
            
            using var activity = _instrumentation.StartActivity("TelemetryService.Start");
            activity?.SetTag("service.start", DateTime.UtcNow);
            
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Telemetry service stopped");
            
            using var activity = _instrumentation.StartActivity("TelemetryService.Stop");
            activity?.SetTag("service.stop", DateTime.UtcNow);
            
            return Task.CompletedTask;
        }
    }
}