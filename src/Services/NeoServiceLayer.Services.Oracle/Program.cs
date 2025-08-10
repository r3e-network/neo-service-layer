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
using NeoServiceLayer.ServiceFramework.Metrics;
using NeoServiceLayer.Services.Oracle;
using NeoServiceLayer.Infrastructure.Resilience;
using Polly;
using Polly.Extensions.Http;

namespace NeoServiceLayer.Services.Oracle.Host
{
    /// <summary>
    /// Microservice host for OracleService
    /// </summary>
    public class OracleServiceHost : MicroserviceHost<OracleService>
    {
        public OracleServiceHost(string[] args) : base(args)
        {
        }

        protected override void ConfigureServiceSpecific(WebHostBuilderContext context, IServiceCollection services)
        {
            var configuration = context.Configuration;

            // Add resilience infrastructure
            services.AddResilience(configuration);
            services.AddResiliencePolicies(configuration);
            
            // Add resilient HTTP clients for external data sources
            services.AddHttpClient("ExternalOracleClient")
                .AddPolicyHandler(HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(3, retryAttempt => 
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

            // Add service-specific dependencies
            // services.Configure<OracleOptions>(configuration.GetSection("Oracle"));

            // Add health checks
            services.AddHealthChecks()
                .AddCheck<OracleHealthCheck>("oracle_service");
        }

        protected override void MapServiceEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints)
        {
            // Map service-specific endpoints
            endpoints.MapGet("/api/oracle/status", async context =>
            {
                var service = context.RequestServices.GetRequiredService<OracleService>();
                var status = new
                {
                    service = service.Name,
                    version = service.Version,
                    health = await service.GetHealthAsync(),
                    capabilities = service.Capabilities.Select(c => c.Name)
                };
                await context.Response.WriteAsJsonAsync(status);
            });

            // Map service statistics endpoint
            endpoints.MapGet("/api/oracle/statistics", async context =>
            {
                var service = context.RequestServices.GetRequiredService<OracleService>();
                var stats = await service.GetMetricsAsync();
                await context.Response.WriteAsJsonAsync(stats);
            });

            // Map metrics endpoint
            endpoints.MapGet("/api/oracle/metrics", async context =>
            {
                await context.Response.WriteAsync(GetMetrics());
            });



        }

        private string GetMetrics()
        {
            // Metrics collection
            var serviceMetrics = new ServiceMetrics();
            return serviceMetrics.GetPrometheusMetrics() + @"
# HELP oracle_operations_total Total number of operations
# TYPE oracle_operations_total counter
oracle_operations_total{status=""success""} 0
oracle_operations_total{status=""failed""} 0
";
        }
    }

    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting Oracle Service...");

                var host = new OracleServiceHost(args);
                return await host.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start Oracle Service: {ex.Message}");
                return 1;
            }
        }
    }

    // Health check implementation
    public class OracleHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
    {
        private readonly OracleService _service;

        public OracleHealthCheck(OracleService service)
        {
            _service = service;
        }

        public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
            System.Threading.CancellationToken cancellationToken = default)
        {
            try
            {
                var health = await _service.GetHealthAsync();

                return health switch
                {
                    Core.ServiceHealth.Healthy =>
                        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Service is healthy"),
                    Core.ServiceHealth.Degraded =>
                        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded("Service is degraded"),
                    _ =>
                        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Service is unhealthy")
                };
            }
            catch (Exception ex)
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Health check failed", ex);
            }
        }
    }
}
