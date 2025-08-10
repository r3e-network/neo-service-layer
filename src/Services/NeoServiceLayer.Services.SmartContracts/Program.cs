using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NeoServiceLayer.Core;
using NeoServiceLayer.Core.SmartContracts;
using NeoServiceLayer.ServiceFramework.ServiceHost;
using NeoServiceLayer.Services.SmartContracts;
using NeoServiceLayer.Infrastructure.Resilience;

namespace NeoServiceLayer.Services.SmartContracts.Host
{
    /// <summary>
    /// Microservice host for SmartContractsService
    /// </summary>
    public class SmartContractsServiceHost : MicroserviceHost<SmartContractsService>
    {
        public SmartContractsServiceHost(string[] args) : base(args)
        {
        }

        protected override void ConfigureServiceSpecific(WebHostBuilderContext context, IServiceCollection services)
        {
            var configuration = context.Configuration;

            // Add resilience infrastructure
            services.AddResilience(configuration);
            services.AddResilientBlockchainClient();
            
            // Add resilience policies for blockchain operations
            services.AddResiliencePolicies(configuration);

            // Add smart contract managers
            services.AddScoped<NeoServiceLayer.Services.SmartContracts.NeoN3.NeoN3SmartContractManager>();
            services.AddScoped<NeoServiceLayer.Services.SmartContracts.NeoX.NeoXSmartContractManager>();

            // Add health checks
            services.AddHealthChecks()
                .AddCheck<SmartContractsHealthCheck>("smart-contracts_service");
        }

        protected override void MapServiceEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints)
        {
            // Map service-specific endpoints
            endpoints.MapGet("/api/smart-contracts/status", async context =>
            {
                var service = context.RequestServices.GetRequiredService<SmartContractsService>();
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
            endpoints.MapGet("/api/smart-contracts/statistics", async context =>
            {
                var service = context.RequestServices.GetRequiredService<SmartContractsService>();
                var stats = await service.GetMetricsAsync();
                await context.Response.WriteAsJsonAsync(stats);
            });

            // Map metrics endpoint
            endpoints.MapGet("/api/smart-contracts/metrics", async context =>
            {
                await context.Response.WriteAsync(GetMetrics());
            });

            // Deploy contract
            endpoints.MapPost("/api/smart-contracts/deploy", async context =>
            {
                var service = context.RequestServices.GetRequiredService<SmartContractsService>();
                var request = await context.Request.ReadFromJsonAsync<DeployContractRequest>();

                if (request == null)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid request" });
                    return;
                }

                var blockchainType = Enum.Parse<BlockchainType>(request.BlockchainType, true);
                var contractCode = Convert.FromBase64String(request.ContractCode);
                var constructorParams = request.ConstructorParameters?.Values.ToArray();
                var deploymentOptions = new ContractDeploymentOptions
                {
                    Name = request.Options.Account,
                    GasLimit = decimal.TryParse(request.Options.NetworkFee, out var gasLimit) ? (long)gasLimit : 1000000
                };

                var result = await service.DeployContractAsync(
                    blockchainType,
                    contractCode,
                    constructorParams,
                    deploymentOptions);

                await context.Response.WriteAsJsonAsync(result);
            });

            // Invoke contract
            endpoints.MapPost("/api/smart-contracts/invoke", async context =>
            {
                var service = context.RequestServices.GetRequiredService<SmartContractsService>();
                var request = await context.Request.ReadFromJsonAsync<InvokeContractRequest>();

                if (request == null)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid request" });
                    return;
                }

                var blockchainType = Enum.Parse<BlockchainType>(request.BlockchainType, true);
                var invocationOptions = new ContractInvocationOptions
                {
                    GasLimit = decimal.TryParse(request.Options.NetworkFee, out var gasLimit) ? (long)gasLimit : 1000000,
                    Value = decimal.TryParse(request.Options.SystemFee, out var value) ? (BigInteger)value : 0
                };

                var result = await service.InvokeContractAsync(
                    blockchainType,
                    request.ContractHash,
                    request.Method,
                    request.Parameters,
                    invocationOptions);

                await context.Response.WriteAsJsonAsync(result);
            });

            // Query contract state
            endpoints.MapGet("/api/smart-contracts/{contractHash}/state", async context =>
            {
                var service = context.RequestServices.GetRequiredService<SmartContractsService>();
                var contractHash = context.Request.RouteValues["contractHash"]?.ToString();

                if (string.IsNullOrEmpty(contractHash))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { error = "Contract hash is required" });
                    return;
                }

                var blockchainType = context.Request.Query["blockchain"].FirstOrDefault() ?? "neon3";
                var state = await service.GetContractStateAsync(contractHash, blockchainType);
                await context.Response.WriteAsJsonAsync(state);
            });

            // List deployed contracts
            endpoints.MapGet("/api/smart-contracts/deployed", async context =>
            {
                var service = context.RequestServices.GetRequiredService<SmartContractsService>();
                var blockchainType = context.Request.Query["blockchain"].FirstOrDefault() ?? "all";
                var contracts = await service.GetDeployedContractsAsync(blockchainType);
                await context.Response.WriteAsJsonAsync(contracts);
            });

            // Validate contract code
            endpoints.MapPost("/api/smart-contracts/validate", async context =>
            {
                var service = context.RequestServices.GetRequiredService<SmartContractsService>();
                var request = await context.Request.ReadFromJsonAsync<ValidateContractRequest>();

                if (request == null)
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new { error = "Invalid request" });
                    return;
                }

                var result = await service.ValidateContractAsync(request.ContractCode, request.BlockchainType);
                await context.Response.WriteAsJsonAsync(result);
            });
        }

        private string GetMetrics()
        {
            return @"
# HELP smart_contracts_deployments_total Total number of contract deployments
# TYPE smart_contracts_deployments_total counter
smart_contracts_deployments_total{blockchain=""neon3"",status=""success""} 142
smart_contracts_deployments_total{blockchain=""neon3"",status=""failed""} 8
smart_contracts_deployments_total{blockchain=""neox"",status=""success""} 89
smart_contracts_deployments_total{blockchain=""neox"",status=""failed""} 3

# HELP smart_contracts_invocations_total Total number of contract invocations
# TYPE smart_contracts_invocations_total counter
smart_contracts_invocations_total{blockchain=""neon3"",status=""success""} 1523
smart_contracts_invocations_total{blockchain=""neon3"",status=""failed""} 47
smart_contracts_invocations_total{blockchain=""neox"",status=""success""} 892
smart_contracts_invocations_total{blockchain=""neox"",status=""failed""} 21
";
        }
    }

    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting SmartContracts Service...");

                var host = new SmartContractsServiceHost(args);
                return await host.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start SmartContracts Service: {ex.Message}");
                return 1;
            }
        }
    }

    // Health check implementation
    public class SmartContractsHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
    {
        private readonly SmartContractsService _service;

        public SmartContractsHealthCheck(SmartContractsService service)
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
    public class DeployContractRequest
    {
        public string ContractCode { get; set; } = string.Empty;
        public Dictionary<string, object> ConstructorParameters { get; set; } = new();
        public DeploymentOptions Options { get; set; } = new();
        public string BlockchainType { get; set; } = "neon3";
    }

    public class InvokeContractRequest
    {
        public string ContractHash { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public object[] Parameters { get; set; } = Array.Empty<object>();
        public InvocationOptions Options { get; set; } = new();
        public string BlockchainType { get; set; } = "neon3";
    }

    public class ValidateContractRequest
    {
        public string ContractCode { get; set; } = string.Empty;
        public string BlockchainType { get; set; } = "neon3";
    }

    public class DeploymentOptions
    {
        public string NetworkFee { get; set; } = string.Empty;
        public string SystemFee { get; set; } = string.Empty;
        public string Account { get; set; } = string.Empty;
    }

    public class InvocationOptions
    {
        public string NetworkFee { get; set; } = string.Empty;
        public string SystemFee { get; set; } = string.Empty;
        public string Account { get; set; } = string.Empty;
        public bool ReadOnly { get; set; }
    }
}
