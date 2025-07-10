using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NeoServiceLayer.ServiceFramework.ServiceHost;
using NeoServiceLayer.Services.{{ServiceName}};
using System.Linq;

namespace NeoServiceLayer.Services.{{ServiceName}}.Host
{
    /// <summary>
    /// Microservice host for {{ServiceName}}Service
    /// </summary>
    public class {{ServiceName}}ServiceHost : MicroserviceHost<{{ServiceName}}Service>
    {
        public {{ServiceName}}ServiceHost(string[] args) : base(args)
        {
        }

        protected override void ConfigureServiceSpecific(WebHostBuilderContext context, IServiceCollection services)
        {
            // Add service-specific configuration here
            base.ConfigureServiceSpecific(context, services);
        }

        protected override void MapServiceEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints)
        {
            // Add service-specific endpoints here
            base.MapServiceEndpoints(endpoints);
        }
    }

    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting {{ServiceName}} Service...");
                
                var host = new {{ServiceName}}ServiceHost(args);
                return await host.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start {{ServiceName}} Service: {ex.Message}");
                return 1;
            }
        }
    }
}