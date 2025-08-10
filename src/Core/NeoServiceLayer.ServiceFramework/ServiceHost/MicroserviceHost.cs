using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure.ServiceDiscovery;
using Polly;
using Polly.Extensions.Http;

namespace NeoServiceLayer.ServiceFramework.ServiceHost
{
    /// <summary>
    /// Base class for hosting individual microservices with auto-registration
    /// </summary>
    public class MicroserviceHost<TService> where TService : class, IService
    {
        private readonly string[] _args;
        private IHost? _host;
        private ServiceInfo? _serviceInfo;
        private Infrastructure.ServiceDiscovery.IServiceRegistry? _serviceRegistry;
        private Timer? _heartbeatTimer;

        public MicroserviceHost(string[] args)
        {
            _args = args;
        }

        public async Task<int> RunAsync()
        {
            try
            {
                _host = CreateHostBuilder(_args).Build();

                // Initialize service registry
                _serviceRegistry = _host.Services.GetService<Infrastructure.ServiceDiscovery.IServiceRegistry>();

                // Get service instance
                var service = _host.Services.GetRequiredService<TService>();

                // Register with service discovery
                if (_serviceRegistry != null)
                {
                    await RegisterServiceAsync(service);
                    StartHeartbeat();
                }

                // Run the host
                await _host.RunAsync();

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Host terminated unexpectedly: {ex.Message}");
                return 1;
            }
            finally
            {
                await DeregisterServiceAsync();
                _heartbeatTimer?.Dispose();
            }
        }

        protected virtual IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureServices((context, services) =>
                    {
                        ConfigureServices(context, services);
                    });

                    webBuilder.Configure((context, app) =>
                    {
                        ConfigureApp(context, app);
                    });

                    webBuilder.UseUrls("http://+:80", "https://+:443");
                })
                .ConfigureLogging((context, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddDebug();

                    if (context.HostingEnvironment.IsDevelopment())
                    {
                        logging.SetMinimumLevel(LogLevel.Debug);
                    }
                    else
                    {
                        logging.SetMinimumLevel(LogLevel.Information);
                    }
                });

        protected virtual void ConfigureServices(WebHostBuilderContext context, IServiceCollection services)
        {
            var configuration = context.Configuration;

            // Add core services
            services.AddControllers();
            services.AddHealthChecks();
            
            // Add default HTTP client with basic retry policy using Polly v8 API
            services.AddHttpClient("default")
                .AddStandardResilienceHandler();

            // Add service discovery
            services.AddSingleton<Infrastructure.ServiceDiscovery.IServiceRegistry, ConsulServiceRegistry>();

            // Add CORS
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            // Add Swagger
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = typeof(TService).Name,
                    Version = "v1"
                });
            });

            // Register the service itself
            services.AddSingleton<TService>();
            services.AddSingleton<IService>(provider => provider.GetRequiredService<TService>());

            // Add service-specific registrations
            ConfigureServiceSpecific(context, services);
        }

        protected virtual void ConfigureApp(WebHostBuilderContext context, IApplicationBuilder app)
        {
            var env = context.HostingEnvironment;

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseRouting();
            app.UseCors("AllowAll");

            // Only use authentication if it's configured
            if (app.ApplicationServices.GetService<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>() != null)
            {
                app.UseAuthentication();
            }
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health");

                // Map service info endpoint
                endpoints.MapGet("/service-info", async context =>
                {
                    if (_serviceInfo != null)
                    {
                        await context.Response.WriteAsJsonAsync(_serviceInfo);
                    }
                    else
                    {
                        context.Response.StatusCode = 503;
                        await context.Response.WriteAsync("Service not registered");
                    }
                });

                // Map custom service endpoints
                MapServiceEndpoints(endpoints);
            });
        }

        protected virtual void ConfigureServiceSpecific(WebHostBuilderContext context, IServiceCollection services)
        {
            // Override in derived classes to add service-specific configuration
        }

        protected virtual void MapServiceEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints)
        {
            // Override in derived classes to map service-specific endpoints
        }

        private async Task RegisterServiceAsync(TService service)
        {
            if (_serviceRegistry == null) return;

            var configuration = _host!.Services.GetRequiredService<IConfiguration>();

            _serviceInfo = new ServiceInfo
            {
                ServiceName = service.Name,
                ServiceType = typeof(TService).Name,
                HostName = Environment.GetEnvironmentVariable("HOSTNAME") ?? "localhost",
                Port = configuration.GetValue<int>("ServicePort", 80),
                Protocol = "http",
                HealthCheckUrl = "/health",
                Metadata = new Dictionary<string, string>
                {
                    ["version"] = service.Version,
                    ["capabilities"] = string.Join(",", service.Capabilities.Select(c => c.Name))
                }
            };

            await _serviceRegistry.RegisterServiceAsync(_serviceInfo);
        }

        private async Task DeregisterServiceAsync()
        {
            if (_serviceRegistry != null && _serviceInfo != null)
            {
                await _serviceRegistry.DeregisterServiceAsync(_serviceInfo.ServiceId);
            }
        }

        private void StartHeartbeat()
        {
            _heartbeatTimer = new Timer(async _ =>
            {
                if (_serviceRegistry != null && _serviceInfo != null)
                {
                    await _serviceRegistry.UpdateHeartbeatAsync(_serviceInfo.ServiceId);
                }
            }, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }
    }
}
