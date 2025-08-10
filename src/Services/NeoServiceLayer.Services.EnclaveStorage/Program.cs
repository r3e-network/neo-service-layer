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
using NeoServiceLayer.Services.EnclaveStorage;

namespace NeoServiceLayer.Services.EnclaveStorage.Host
{
    /// <summary>
    /// Microservice host for EnclaveStorageService
    /// </summary>
    public class EnclaveStorageServiceHost : MicroserviceHost<EnclaveStorageService>
    {
        public EnclaveStorageServiceHost(string[] args) : base(args)
        {
        }

        protected override void ConfigureServiceSpecific(WebHostBuilderContext context, IServiceCollection services)
        {
            var configuration = context.Configuration;

            // Add service-specific dependencies
            // services.Configure<EnclaveStorageOptions>(configuration.GetSection("EnclaveStorage"));

            // Add health checks
            services.AddHealthChecks()
                .AddCheck<EnclaveStorageHealthCheck>("enclave-storage_service");
        }

        protected override void MapServiceEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints)
        {
            // Map service-specific endpoints
            endpoints.MapGet("/api/enclave-storage/status", async context =>
            {
                var service = context.RequestServices.GetRequiredService<EnclaveStorageService>();
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
            endpoints.MapGet("/api/enclave-storage/statistics", async context =>
            {
                var service = context.RequestServices.GetRequiredService<EnclaveStorageService>();
                var stats = await service.GetMetricsAsync();
                await context.Response.WriteAsJsonAsync(stats);
            });

            // Map metrics endpoint
            endpoints.MapGet("/api/enclave-storage/metrics", async context =>
            {
                await context.Response.WriteAsync(GetMetrics());
            });



        }

        private string GetMetrics()
        {
            // Metrics collection
            var serviceMetrics = new ServiceMetrics();
            return serviceMetrics.GetPrometheusMetrics() + @"
# HELP enclave-storage_operations_total Total number of operations
# TYPE enclave-storage_operations_total counter
enclave-storage_operations_total{status=""success""} 0
enclave-storage_operations_total{status=""failed""} 0
";
        }
    }

    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting EnclaveStorage Service...");

                var host = new EnclaveStorageServiceHost(args);
                return await host.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start EnclaveStorage Service: {ex.Message}");
                return 1;
            }
        }
    }

    // Health check implementation
    public class EnclaveStorageHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
    {
        private readonly EnclaveStorageService _service;

        public EnclaveStorageHealthCheck(EnclaveStorageService service)
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
