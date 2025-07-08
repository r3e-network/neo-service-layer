using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NeoServiceLayer.ServiceFramework.ServiceHost;
using NeoServiceLayer.Services.{ServiceName};

namespace NeoServiceLayer.Services.{ServiceName}.Host
{
    /// <summary>
    /// Microservice host for {ServiceName}Service
    /// </summary>
    public class {ServiceName}ServiceHost : MicroserviceHost<{ServiceName}Service>
    {
        public {ServiceName}ServiceHost(string[] args) : base(args)
        {
        }

        protected override void ConfigureServiceSpecific(WebHostBuilderContext context, IServiceCollection services)
        {
            // Add service-specific dependencies
            var configuration = context.Configuration;
            
            // Add any service-specific configuration here
            // For example:
            // services.AddSingleton<ISpecificDependency, SpecificDependency>();
            
            // Add service-specific options
            // services.Configure<{ServiceName}Options>(configuration.GetSection("{ServiceName}"));
        }

        protected override void MapServiceEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints)
        {
            // Map service-specific endpoints
            endpoints.MapGet("/api/{servicename}/status", async context =>
            {
                var service = context.RequestServices.GetRequiredService<{ServiceName}Service>();
                var status = new
                {
                    service = service.Name,
                    version = service.Version,
                    health = await service.GetHealthAsync(),
                    capabilities = service.Capabilities.Select(c => c.Name)
                };
                await context.Response.WriteAsJsonAsync(status);
            });

            // Add more service-specific endpoints as needed
        }
    }

    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting {ServiceName} Service...");
                
                var host = new {ServiceName}ServiceHost(args);
                return await host.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start {ServiceName} Service: {ex.Message}");
                return 1;
            }
        }
    }
}