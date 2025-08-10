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
using NeoServiceLayer.Services.ProofOfReserve;

namespace NeoServiceLayer.Services.ProofOfReserve.Host
{
    /// <summary>
    /// Microservice host for ProofOfReserveService
    /// </summary>
    public class ProofOfReserveServiceHost : MicroserviceHost<ProofOfReserveService>
    {
        public ProofOfReserveServiceHost(string[] args) : base(args)
        {
        }

        protected override void ConfigureServiceSpecific(WebHostBuilderContext context, IServiceCollection services)
        {
            var configuration = context.Configuration;

            // Add service-specific dependencies
            // services.Configure<ProofOfReserveOptions>(configuration.GetSection("ProofOfReserve"));

            // Add health checks
            services.AddHealthChecks()
                .AddCheck<ProofOfReserveHealthCheck>("proof-of-reserve_service");
        }

        protected override void MapServiceEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints)
        {
            // Map service-specific endpoints
            endpoints.MapGet("/api/proof-of-reserve/status", async context =>
            {
                var service = context.RequestServices.GetRequiredService<ProofOfReserveService>();
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
            endpoints.MapGet("/api/proof-of-reserve/statistics", async context =>
            {
                var service = context.RequestServices.GetRequiredService<ProofOfReserveService>();
                var stats = await service.GetMetricsAsync();
                await context.Response.WriteAsJsonAsync(stats);
            });

            // Map metrics endpoint
            endpoints.MapGet("/api/proof-of-reserve/metrics", async context =>
            {
                await context.Response.WriteAsync(GetMetrics());
            });



        }

        private string GetMetrics()
        {
            // Metrics collection
            var serviceMetrics = new ServiceMetrics();
            return serviceMetrics.GetPrometheusMetrics() + @"
# HELP proof-of-reserve_operations_total Total number of operations
# TYPE proof-of-reserve_operations_total counter
proof-of-reserve_operations_total{status=""success""} 0
proof-of-reserve_operations_total{status=""failed""} 0
";
        }
    }

    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting ProofOfReserve Service...");

                var host = new ProofOfReserveServiceHost(args);
                return await host.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start ProofOfReserve Service: {ex.Message}");
                return 1;
            }
        }
    }

    // Health check implementation
    public class ProofOfReserveHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
    {
        private readonly ProofOfReserveService _service;

        public ProofOfReserveHealthCheck(ProofOfReserveService service)
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
