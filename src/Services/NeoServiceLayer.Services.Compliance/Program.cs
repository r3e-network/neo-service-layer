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
using NeoServiceLayer.Services.Compliance;

namespace NeoServiceLayer.Services.Compliance.Host
{
    /// <summary>
    /// Microservice host for ComplianceService
    /// </summary>
    public class ComplianceServiceHost : MicroserviceHost<ComplianceService>
    {
        public ComplianceServiceHost(string[] args) : base(args)
        {
        }

        protected override void ConfigureServiceSpecific(WebHostBuilderContext context, IServiceCollection services)
        {
            var configuration = context.Configuration;

            // Add service-specific dependencies
            // services.Configure<ComplianceOptions>(configuration.GetSection("Compliance"));

            // Add health checks
            services.AddHealthChecks()
                .AddCheck<ComplianceHealthCheck>("compliance_service");
        }

        protected override void MapServiceEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints)
        {
            // Map service-specific endpoints
            endpoints.MapGet("/api/compliance/status", async context =>
            {
                var service = context.RequestServices.GetRequiredService<ComplianceService>();
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
            endpoints.MapGet("/api/compliance/statistics", async context =>
            {
                var service = context.RequestServices.GetRequiredService<ComplianceService>();
                var stats = await service.GetMetricsAsync();
                await context.Response.WriteAsJsonAsync(stats);
            });

            // Map metrics endpoint
            endpoints.MapGet("/api/compliance/metrics", async context =>
            {
                await context.Response.WriteAsync(GetMetrics());
            });

            // TODO: Add service-specific endpoints here
        }

        private string GetMetrics()
        {
            // TODO: Implement proper metrics collection
            return @"
# HELP compliance_operations_total Total number of operations
# TYPE compliance_operations_total counter
compliance_operations_total{status=""success""} 0
compliance_operations_total{status=""failed""} 0
";
        }
    }

    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting Compliance Service...");

                var host = new ComplianceServiceHost(args);
                return await host.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start Compliance Service: {ex.Message}");
                return 1;
            }
        }
    }

    // Health check implementation
    public class ComplianceHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
    {
        private readonly ComplianceService _service;

        public ComplianceHealthCheck(ComplianceService service)
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
