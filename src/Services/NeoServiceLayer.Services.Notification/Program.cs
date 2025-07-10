using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework.ServiceHost;
using NeoServiceLayer.Services.Notification;
using NeoServiceLayer.Services.Notification.Models;

namespace NeoServiceLayer.Services.Notification.Host
{
    /// <summary>
    /// Microservice host for NotificationService
    /// </summary>
    public class NotificationServiceHost : MicroserviceHost<NotificationService>
    {
        public NotificationServiceHost(string[] args) : base(args)
        {
        }

        protected override void ConfigureServiceSpecific(WebHostBuilderContext context, IServiceCollection services)
        {
            var configuration = context.Configuration;

            // Add notification-specific configuration
            services.Configure<NotificationOptions>(configuration.GetSection("Notification"));

            // Add metrics collection
            services.AddSingleton<IMetricsCollector, PrometheusMetricsCollector>();

            // Add health checks
            services.AddHealthChecks()
                .AddCheck<NotificationHealthCheck>("notification_service");
        }

        protected override void MapServiceEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints)
        {
            // Map notification-specific endpoints
            endpoints.MapPost("/api/notification/send", async context =>
            {
                var service = context.RequestServices.GetRequiredService<NotificationService>();
                var request = await context.Request.ReadFromJsonAsync<SendNotificationRequest>();

                if (request == null)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid request" });
                    return;
                }

                var result = await service.SendNotificationAsync(request, Core.BlockchainType.NeoN3);
                await context.Response.WriteAsJsonAsync(result);
            });

            endpoints.MapPost("/api/notification/batch", async context =>
            {
                var service = context.RequestServices.GetRequiredService<NotificationService>();
                var requests = await context.Request.ReadFromJsonAsync<BatchNotificationRequest[]>();

                if (requests == null)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid request" });
                    return;
                }

                var results = await service.SendBatchNotificationsAsync(requests, Core.BlockchainType.NeoN3);
                await context.Response.WriteAsJsonAsync(results);
            });

            endpoints.MapGet("/api/notification/status/{notificationId}", async context =>
            {
                var service = context.RequestServices.GetRequiredService<NotificationService>();
                var notificationId = context.Request.RouteValues["notificationId"]?.ToString();

                if (string.IsNullOrEmpty(notificationId))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid notification ID" });
                    return;
                }

                var status = await service.GetNotificationStatusAsync(notificationId, Core.BlockchainType.NeoN3);
                if (status == null)
                {
                    context.Response.StatusCode = 404;
                    return;
                }

                await context.Response.WriteAsJsonAsync(status);
            });

            endpoints.MapGet("/api/notification/metrics", async context =>
            {
                var metricsCollector = context.RequestServices.GetRequiredService<IMetricsCollector>();
                await context.Response.WriteAsync(metricsCollector.GetMetrics());
            });
        }
    }

    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting Notification Service...");

                var host = new NotificationServiceHost(args);
                return await host.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start Notification Service: {ex.Message}");
                return 1;
            }
        }
    }

    // Health check implementation
    public class NotificationHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
    {
        private readonly NotificationService _service;

        public NotificationHealthCheck(NotificationService service)
        {
            _service = service;
        }

        public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
            System.Threading.CancellationToken cancellationToken = default)
        {
            var health = await _service.GetHealthAsync();

            return health switch
            {
                Core.ServiceHealth.Healthy => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Service is healthy"),
                Core.ServiceHealth.Degraded => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded("Service is degraded"),
                _ => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Service is unhealthy")
            };
        }
    }

    // Metrics collector interface
    public interface IMetricsCollector
    {
        void RecordNotificationSent(string channel, bool success);
        void RecordProcessingTime(string operation, double milliseconds);
        string GetMetrics();
    }

    // Prometheus metrics implementation
    public class PrometheusMetricsCollector : IMetricsCollector
    {
        private readonly Dictionary<string, long> _counters = new();
        private readonly Dictionary<string, List<double>> _histograms = new();

        public void RecordNotificationSent(string channel, bool success)
        {
            var key = $"notifications_sent_total{{channel=\"{channel}\",success=\"{success}\"}}";
            _counters.TryGetValue(key, out var count);
            _counters[key] = count + 1;
        }

        public void RecordProcessingTime(string operation, double milliseconds)
        {
            var key = $"notification_processing_duration_milliseconds{{operation=\"{operation}\"}}";
            if (!_histograms.ContainsKey(key))
            {
                _histograms[key] = new List<double>();
            }
            _histograms[key].Add(milliseconds);
        }

        public string GetMetrics()
        {
            var sb = new System.Text.StringBuilder();

            // Output counters
            foreach (var (key, value) in _counters)
            {
                sb.AppendLine($"{key} {value}");
            }

            // Output histograms (simplified - just average for now)
            foreach (var (key, values) in _histograms)
            {
                if (values.Any())
                {
                    var avg = values.Average();
                    sb.AppendLine($"{key}_avg {avg:F2}");
                }
            }

            return sb.ToString();
        }
    }
}
