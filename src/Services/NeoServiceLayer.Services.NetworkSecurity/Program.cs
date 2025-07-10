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
using NeoServiceLayer.Services.NetworkSecurity;

namespace NeoServiceLayer.Services.NetworkSecurity.Host
{
    /// <summary>
    /// Microservice host for NetworkSecurityService
    /// </summary>
    public class NetworkSecurityServiceHost : MicroserviceHost<NetworkSecurityService>
    {
        public NetworkSecurityServiceHost(string[] args) : base(args)
        {
        }

        protected override void ConfigureServiceSpecific(WebHostBuilderContext context, IServiceCollection services)
        {
            var configuration = context.Configuration;

            // Add service-specific dependencies
            // services.Configure<NetworkSecurityOptions>(configuration.GetSection("NetworkSecurity"));

            // Add health checks
            services.AddHealthChecks()
                .AddCheck<NetworkSecurityHealthCheck>("network-security_service");
        }

        protected override void MapServiceEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints)
        {
            // Map service-specific endpoints
            endpoints.MapGet("/api/network-security/status", async context =>
            {
                var service = context.RequestServices.GetRequiredService<NetworkSecurityService>();
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
            endpoints.MapGet("/api/network-security/statistics", async context =>
            {
                var service = context.RequestServices.GetRequiredService<NetworkSecurityService>();
                var stats = await service.GetMetricsAsync();
                await context.Response.WriteAsJsonAsync(stats);
            });

            // Map metrics endpoint
            endpoints.MapGet("/api/network-security/metrics", async context =>
            {
                await context.Response.WriteAsync(GetMetrics());
            });

            // TODO: Add service-specific endpoints here
        }

        private string GetMetrics()
        {
            // TODO: Implement proper metrics collection
            return @"
# HELP network-security_operations_total Total number of operations
# TYPE network-security_operations_total counter
network-security_operations_total{status=""success""} 0
network-security_operations_total{status=""failed""} 0
";
        }
    }

    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting NetworkSecurity Service...");

                var host = new NetworkSecurityServiceHost(args);
                return await host.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start NetworkSecurity Service: {ex.Message}");
                return 1;
            }
        }
    }

    // Health check implementation
    public class NetworkSecurityHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
    {
        private readonly NetworkSecurityService _service;

        public NetworkSecurityHealthCheck(NetworkSecurityService service)
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
