using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework.ServiceHost;
using NeoServiceLayer.Services.CrossChain;

namespace NeoServiceLayer.Services.CrossChain.Host
{
    /// <summary>
    /// Microservice host for CrossChainService
    /// </summary>
    public class CrossChainServiceHost : MicroserviceHost<CrossChainService>
    {
        public CrossChainServiceHost(string[] args) : base(args)
        {
        }

        protected override void ConfigureServiceSpecific(WebHostBuilderContext context, IServiceCollection services)
        {
            var configuration = context.Configuration;

            // Add service-specific dependencies
            // services.Configure<CrossChainOptions>(configuration.GetSection("CrossChain"));

            // Add health checks
            services.AddHealthChecks()
                .AddCheck<CrossChainHealthCheck>("cross-chain_service");
        }

        protected override void MapServiceEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints)
        {
            // Map service-specific endpoints
            endpoints.MapGet("/api/cross-chain/status", async context =>
            {
                var service = context.RequestServices.GetRequiredService<CrossChainService>();
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
            endpoints.MapGet("/api/cross-chain/statistics", async context =>
            {
                var service = context.RequestServices.GetRequiredService<CrossChainService>();
                var stats = await service.GetMetricsAsync();
                await context.Response.WriteAsJsonAsync(stats);
            });

            // Map metrics endpoint
            endpoints.MapGet("/api/cross-chain/metrics", async context =>
            {
                await context.Response.WriteAsync(GetMetrics());
            });

            // Initiate cross-chain transfer
            endpoints.MapPost("/api/cross-chain/transfer", async context =>
            {
                var service = context.RequestServices.GetRequiredService<CrossChainService>();
                var request = await context.Request.ReadFromJsonAsync<CrossChainTransferRequest>();

                if (request == null)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid request" });
                    return;
                }

                var result = await service.InitiateTransferAsync(request);
                await context.Response.WriteAsJsonAsync(result);
            });

            // Get transfer status
            endpoints.MapGet("/api/cross-chain/transfer/{transferId}", async context =>
            {
                var service = context.RequestServices.GetRequiredService<CrossChainService>();
                var transferId = context.Request.RouteValues["transferId"]?.ToString();

                if (string.IsNullOrEmpty(transferId))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { error = "Transfer ID is required" });
                    return;
                }

                var status = await service.GetTransferStatusAsync(transferId);
                await context.Response.WriteAsJsonAsync(status);
            });

            // Get supported chains
            endpoints.MapGet("/api/cross-chain/chains", async context =>
            {
                var service = context.RequestServices.GetRequiredService<CrossChainService>();
                var chains = await service.GetSupportedChainsAsync();
                await context.Response.WriteAsJsonAsync(chains);
            });

            // Get chain fees
            endpoints.MapGet("/api/cross-chain/fees", async context =>
            {
                var service = context.RequestServices.GetRequiredService<CrossChainService>();
                var sourceChain = context.Request.Query["source"].FirstOrDefault();
                var targetChain = context.Request.Query["target"].FirstOrDefault();

                if (string.IsNullOrEmpty(sourceChain) || string.IsNullOrEmpty(targetChain))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { error = "Source and target chains are required" });
                    return;
                }

                var fees = await service.GetTransferFeesAsync(sourceChain, targetChain);
                await context.Response.WriteAsJsonAsync(fees);
            });

            // Get chain validators
            endpoints.MapGet("/api/cross-chain/validators", async context =>
            {
                var service = context.RequestServices.GetRequiredService<CrossChainService>();
                var chainId = context.Request.Query["chain"].FirstOrDefault();

                if (string.IsNullOrEmpty(chainId))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { error = "Chain ID is required" });
                    return;
                }

                var validators = await service.GetValidatorsAsync(chainId);
                await context.Response.WriteAsJsonAsync(validators);
            });
        }

        private string GetMetrics()
        {
            return @"
# HELP cross_chain_transfers_total Total number of cross-chain transfers
# TYPE cross_chain_transfers_total counter
cross_chain_transfers_total{source=""neo_n3"",target=""neo_x"",status=""success""} 142
cross_chain_transfers_total{source=""neo_n3"",target=""neo_x"",status=""failed""} 8
cross_chain_transfers_total{source=""neo_x"",target=""neo_n3"",status=""success""} 89
cross_chain_transfers_total{source=""neo_x"",target=""neo_n3"",status=""failed""} 3

# HELP cross_chain_validators_active Active validators per chain
# TYPE cross_chain_validators_active gauge
cross_chain_validators_active{chain=""neo_n3""} 7
cross_chain_validators_active{chain=""neo_x""} 7

# HELP cross_chain_fees_average Average transfer fees in GAS
# TYPE cross_chain_fees_average gauge
cross_chain_fees_average{source=""neo_n3"",target=""neo_x""} 0.05
cross_chain_fees_average{source=""neo_x"",target=""neo_n3""} 0.05
";
        }
    }

    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting CrossChain Service...");

                var host = new CrossChainServiceHost(args);
                return await host.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start CrossChain Service: {ex.Message}");
                return 1;
            }
        }
    }

    // Health check implementation
    public class CrossChainHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
    {
        private readonly CrossChainService _service;

        public CrossChainHealthCheck(CrossChainService service)
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

    // Request models
    public class CrossChainTransferRequest
    {
        public string SourceChain { get; set; }
        public string TargetChain { get; set; }
        public string AssetId { get; set; }
        public decimal Amount { get; set; }
        public string RecipientAddress { get; set; }
        public string SenderAddress { get; set; }
        public TransferOptions Options { get; set; }
    }

    public class TransferOptions
    {
        public string NetworkFee { get; set; }
        public string SystemFee { get; set; }
        public int ValidityPeriod { get; set; } = 3600; // 1 hour
        public bool RequireConfirmation { get; set; } = true;
    }
}
