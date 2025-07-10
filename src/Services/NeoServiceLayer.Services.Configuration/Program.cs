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
using NeoServiceLayer.Services.Configuration;
using NeoServiceLayer.Services.Configuration.Models;

namespace NeoServiceLayer.Services.Configuration.Host
{
    /// <summary>
    /// Microservice host for ConfigurationService
    /// </summary>
    public class ConfigurationServiceHost : MicroserviceHost<ConfigurationService>
    {
        public ConfigurationServiceHost(string[] args) : base(args)
        {
        }

        protected override void ConfigureServiceSpecific(WebHostBuilderContext context, IServiceCollection services)
        {
            var configuration = context.Configuration;

            // Add configuration-specific dependencies
            services.Configure<ConfigurationServiceOptions>(configuration.GetSection("Configuration"));

            // Add metrics collection
            services.AddSingleton<IMetricsCollector, PrometheusMetricsCollector>();

            // Add health checks
            services.AddHealthChecks()
                .AddCheck<ConfigurationHealthCheck>("configuration_service");
        }

        protected override void MapServiceEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints)
        {
            // Map configuration-specific endpoints
            endpoints.MapGet("/api/configuration/{application}/{key}", async context =>
            {
                var service = context.RequestServices.GetRequiredService<ConfigurationService>();
                var application = context.Request.RouteValues["application"]?.ToString();
                var key = context.Request.RouteValues["key"]?.ToString();

                if (string.IsNullOrEmpty(application) || string.IsNullOrEmpty(key))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid application or key" });
                    return;
                }

                var request = new GetConfigurationRequest
                {
                    Key = $"{application}.{key}"
                };

                var result = await service.GetConfigurationAsync(request, Core.BlockchainType.NeoN3);
                var value = result.Value;
                if (value == null)
                {
                    context.Response.StatusCode = 404;
                    return;
                }

                await context.Response.WriteAsJsonAsync(new { value });
            });

            endpoints.MapPost("/api/configuration/{application}/{key}", async context =>
            {
                var service = context.RequestServices.GetRequiredService<ConfigurationService>();
                var application = context.Request.RouteValues["application"]?.ToString();
                var key = context.Request.RouteValues["key"]?.ToString();
                var body = await context.Request.ReadFromJsonAsync<ConfigurationValue>();

                if (string.IsNullOrEmpty(application) || string.IsNullOrEmpty(key) || body == null)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid request" });
                    return;
                }

                var request = new SetConfigurationRequest
                {
                    Key = $"{application}.{key}",
                    Value = body.Value
                };

                var result = await service.SetConfigurationAsync(request, Core.BlockchainType.NeoN3);
                await context.Response.WriteAsJsonAsync(new { success = result.Success });
            });

            endpoints.MapDelete("/api/configuration/{application}/{key}", async context =>
            {
                var service = context.RequestServices.GetRequiredService<ConfigurationService>();
                var application = context.Request.RouteValues["application"]?.ToString();
                var key = context.Request.RouteValues["key"]?.ToString();

                if (string.IsNullOrEmpty(application) || string.IsNullOrEmpty(key))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid application or key" });
                    return;
                }

                var request = new DeleteConfigurationRequest
                {
                    Key = $"{application}.{key}"
                };

                var result = await service.DeleteConfigurationAsync(request, Core.BlockchainType.NeoN3);
                await context.Response.WriteAsJsonAsync(new { success = result.Success });
            });

            endpoints.MapGet("/api/configuration/metrics", async context =>
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
                Console.WriteLine("Starting Configuration Service...");

                var host = new ConfigurationServiceHost(args);
                return await host.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start Configuration Service: {ex.Message}");
                return 1;
            }
        }
    }

    // Health check implementation
    public class ConfigurationHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
    {
        private readonly ConfigurationService _service;

        public ConfigurationHealthCheck(ConfigurationService service)
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

    // Request/Response models
    public class ConfigurationValue
    {
        public string Value { get; set; } = string.Empty;
        public bool IsEncrypted { get; set; }
    }

    // Metrics collector
    public interface IMetricsCollector
    {
        void RecordConfigOperation(string operation, bool success);
        void RecordOperationDuration(string operation, double milliseconds);
        string GetMetrics();
    }

    public class PrometheusMetricsCollector : IMetricsCollector
    {
        private readonly Dictionary<string, long> _counters = new();
        private readonly Dictionary<string, List<double>> _histograms = new();

        public void RecordConfigOperation(string operation, bool success)
        {
            var key = $"configuration_operations_total{{operation=\"{operation}\",success=\"{success}\"}}";
            _counters.TryGetValue(key, out var count);
            _counters[key] = count + 1;
        }

        public void RecordOperationDuration(string operation, double milliseconds)
        {
            var key = $"configuration_operation_duration_milliseconds{{operation=\"{operation}\"}}";
            if (!_histograms.ContainsKey(key))
            {
                _histograms[key] = new List<double>();
            }
            _histograms[key].Add(milliseconds);
        }

        public string GetMetrics()
        {
            var sb = new System.Text.StringBuilder();

            foreach (var (key, value) in _counters)
            {
                sb.AppendLine($"{key} {value}");
            }

            foreach (var (key, values) in _histograms)
            {
                if (values.Any())
                {
                    var avg = values.Average();
                    var p95 = values.OrderBy(v => v).Skip((int)(values.Count * 0.95)).FirstOrDefault();
                    sb.AppendLine($"{key}_avg {avg:F2}");
                    sb.AppendLine($"{key}_p95 {p95:F2}");
                }
            }

            return sb.ToString();
        }
    }
}
