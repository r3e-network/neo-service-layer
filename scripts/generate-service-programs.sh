#!/bin/bash

# Script to generate Program.cs files for all services

SERVICES=(
    "SmartContracts"
    "CrossChain"
    "ProofOfReserve"
    "KeyManagement"
    "Automation"
    "Storage"
    "Oracle"
    "Randomness"
    "Voting"
    "AbstractAccount"
    "ZeroKnowledge"
    "Compliance"
    "SecretsManagement"
    "SocialRecovery"
    "Compute"
    "EventSubscription"
    "EnclaveStorage"
    "NetworkSecurity"
    "Monitoring"
    "Health"
)

for SERVICE in "${SERVICES[@]}"; do
    SERVICE_DIR="src/Services/NeoServiceLayer.Services.$SERVICE"
    PROGRAM_FILE="$SERVICE_DIR/Program.cs"
    
    if [ -f "$PROGRAM_FILE" ]; then
        echo "Program.cs already exists for $SERVICE, skipping..."
        continue
    fi
    
    echo "Generating Program.cs for $SERVICE..."
    
    # Convert service name to lowercase with hyphens for endpoints
    SERVICE_LOWER=$(echo "$SERVICE" | sed 's/\([A-Z]\)/-\1/g' | sed 's/^-//' | tr '[:upper:]' '[:lower:]')
    
    cat > "$PROGRAM_FILE" << EOF
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NeoServiceLayer.ServiceFramework.ServiceHost;
using NeoServiceLayer.Services.$SERVICE;

namespace NeoServiceLayer.Services.$SERVICE.Host
{
    /// <summary>
    /// Microservice host for ${SERVICE}Service
    /// </summary>
    public class ${SERVICE}ServiceHost : MicroserviceHost<${SERVICE}Service>
    {
        public ${SERVICE}ServiceHost(string[] args) : base(args)
        {
        }

        protected override void ConfigureServiceSpecific(WebHostBuilderContext context, IServiceCollection services)
        {
            var configuration = context.Configuration;
            
            // Add service-specific dependencies
            // services.Configure<${SERVICE}Options>(configuration.GetSection("$SERVICE"));
            
            // Add health checks
            services.AddHealthChecks()
                .AddCheck<${SERVICE}HealthCheck>("${SERVICE_LOWER}_service");
        }

        protected override void MapServiceEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints)
        {
            // Map service-specific endpoints
            endpoints.MapGet("/api/$SERVICE_LOWER/status", async context =>
            {
                var service = context.RequestServices.GetRequiredService<${SERVICE}Service>();
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
            endpoints.MapGet("/api/$SERVICE_LOWER/statistics", async context =>
            {
                var service = context.RequestServices.GetRequiredService<${SERVICE}Service>();
                var stats = await service.GetStatisticsAsync();
                await context.Response.WriteAsJsonAsync(stats);
            });

            // Map metrics endpoint
            endpoints.MapGet("/api/$SERVICE_LOWER/metrics", async context =>
            {
                await context.Response.WriteAsync(GetMetrics());
            });

            // TODO: Add service-specific endpoints here
        }

        private string GetMetrics()
        {
            // TODO: Implement proper metrics collection
            return @"
# HELP ${SERVICE_LOWER}_operations_total Total number of operations
# TYPE ${SERVICE_LOWER}_operations_total counter
${SERVICE_LOWER}_operations_total{status=\"success\"} 0
${SERVICE_LOWER}_operations_total{status=\"failed\"} 0
";
        }
    }

    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting $SERVICE Service...");
                
                var host = new ${SERVICE}ServiceHost(args);
                return await host.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(\$"Failed to start $SERVICE Service: {ex.Message}");
                return 1;
            }
        }
    }

    // Health check implementation
    public class ${SERVICE}HealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
    {
        private readonly ${SERVICE}Service _service;

        public ${SERVICE}HealthCheck(${SERVICE}Service service)
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
                    ServiceFramework.ServiceHealth.Healthy => 
                        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Service is healthy"),
                    ServiceFramework.ServiceHealth.Degraded => 
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
EOF

done

echo "Program.cs generation complete!"