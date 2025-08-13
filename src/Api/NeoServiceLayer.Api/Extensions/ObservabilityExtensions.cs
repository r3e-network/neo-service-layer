using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NeoServiceLayer.Api.Middleware;
using NeoServiceLayer.Infrastructure.Observability.Logging;
using NeoServiceLayer.Infrastructure.Observability.Telemetry;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace NeoServiceLayer.Api.Extensions
{
    /// <summary>
    /// Extension methods for configuring observability features.
    /// </summary>
    public static class ObservabilityExtensions
    {
        /// <summary>
        /// Add comprehensive observability features to the application.
        /// </summary>
        public static IServiceCollection AddObservability(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            // Add structured logging
            services.AddSingleton<IStructuredLoggerFactory, StructuredLoggerFactory>();
            
            // Add performance metrics collector
            services.AddSingleton<PerformanceMetricsCollector>();
            
            // Configure Serilog for structured logging
            ConfigureSerilog(configuration, environment);
            
            // Add OpenTelemetry
            services.AddNeoServiceLayerTelemetry(configuration, "NeoServiceLayer");
            
            // Add performance thresholds from configuration
            services.AddSingleton(provider =>
            {
                var thresholds = configuration.GetSection("PerformanceThresholds").Get<PerformanceThresholds>();
                return thresholds ?? new PerformanceThresholds();
            });
            
            return services;
        }

        /// <summary>
        /// Configure the application to use observability middleware.
        /// </summary>
        public static IApplicationBuilder UseObservability(this IApplicationBuilder app)
        {
            // Add correlation ID middleware first
            app.UseCorrelationId();
            
            // Add performance monitoring
            app.UsePerformanceMonitoring();
            
            // Add request/response logging
            app.UseRequestResponseLogging();
            
            return app;
        }

        /// <summary>
        /// Configure Serilog for structured logging.
        /// </summary>
        private static void ConfigureSerilog(IConfiguration configuration, IHostEnvironment environment)
        {
            var loggerConfiguration = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithProcessId()
                .Enrich.WithProcessName()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("Application", "NeoServiceLayer")
                .Enrich.WithProperty("Version", typeof(ObservabilityExtensions).Assembly.GetName().Version?.ToString() ?? "1.0.0");

            // Configure minimum log level based on environment
            if (environment.IsDevelopment())
            {
                loggerConfiguration.MinimumLevel.Debug();
                loggerConfiguration.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}");
            }
            else
            {
                loggerConfiguration.MinimumLevel.Information();
                loggerConfiguration.WriteTo.Console(new CompactJsonFormatter());
            }

            // Add file logging
            var logPath = configuration["Logging:FilePath"] ?? "logs/neoservicelayer-.txt";
            loggerConfiguration.WriteTo.File(
                new CompactJsonFormatter(),
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                fileSizeLimitBytes: 100_000_000); // 100MB

            // Add Seq logging if configured
            var seqUrl = configuration["Logging:SeqUrl"];
            if (!string.IsNullOrEmpty(seqUrl))
            {
                loggerConfiguration.WriteTo.Seq(seqUrl);
            }

            // Add Application Insights if configured
            var appInsightsKey = configuration["ApplicationInsights:InstrumentationKey"];
            if (!string.IsNullOrEmpty(appInsightsKey))
            {
                loggerConfiguration.WriteTo.ApplicationInsights(appInsightsKey, TelemetryConverter.Traces);
            }

            Log.Logger = loggerConfiguration.CreateLogger();
        }

        /// <summary>
        /// Add request/response logging middleware.
        /// </summary>
        private static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder app)
        {
            app.Use(async (context, next) =>
            {
                // Skip logging for health and metrics endpoints
                var path = context.Request.Path.Value;
                if (path != null && (path.Contains("/health") || path.Contains("/metrics")))
                {
                    await next();
                    return;
                }

                // Get structured logger from context
                if (context.Items.TryGetValue("StructuredLogger", out var loggerObj) && 
                    loggerObj is IStructuredLogger structuredLogger)
                {
                    // Log request body for POST/PUT/PATCH (if not too large)
                    if (context.Request.ContentLength > 0 && 
                        context.Request.ContentLength < 10_000 && // 10KB limit
                        (context.Request.Method == "POST" || 
                         context.Request.Method == "PUT" || 
                         context.Request.Method == "PATCH"))
                    {
                        context.Request.EnableBuffering();
                        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
                        context.Request.Body.Position = 0;
                        
                        structuredLogger.LogOperation("RequestBody", new Dictionary<string, object>
                        {
                            ["ContentType"] = context.Request.ContentType,
                            ["ContentLength"] = context.Request.ContentLength,
                            ["BodyPreview"] = body.Length > 500 ? body.Substring(0, 500) + "..." : body
                        }, LogLevel.Debug);
                    }
                }

                await next();
            });

            return app;
        }
    }
}