using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.Blockchain;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.Infrastructure.Resilience;
using NeoServiceLayer.Infrastructure.ServiceDiscovery;
using NeoServiceLayer.ServiceFramework;

namespace NeoServiceLayer.Advanced.FairOrdering;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
            });
}

public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // Add framework services
        // TODO: services.AddServiceFramework(_configuration);

        // Add infrastructure services
        // TODO: services.AddPersistence(_configuration);
        // TODO: services.AddBlockchainServices(_configuration);
        services.AddResiliencePolicies(_configuration);

        // Add fair ordering service
        services.AddScoped<IFairOrderingService, FairOrderingService>();

        // Add service discovery
        // TODO: services.AddServiceDiscovery(_configuration, "fair-ordering-service");

        // Add health checks
        services.AddHealthChecks()
            .AddCheck<ServiceHealthCheck>("fair_ordering_health");

        // Add controllers
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "Fair Ordering Service API", Version = "v1" });
        });

        // Add CORS
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Fair Ordering Service API v1"));
        }

        app.UseRouting();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHealthChecks("/health");
        });

        // Initialize service
        using (var scope = app.ApplicationServices.CreateScope())
        {
            var fairOrderingService = scope.ServiceProvider.GetRequiredService<IFairOrderingService>();
            var initTask = fairOrderingService.InitializeAsync();
            initTask.Wait();

            if (initTask.Result)
            {
                logger.LogInformation("Fair Ordering Service initialized successfully");
            }
            else
            {
                logger.LogError("Failed to initialize Fair Ordering Service");
                throw new Exception("Service initialization failed");
            }
        }

        logger.LogInformation($"Fair Ordering Service started on port {_configuration["Port"] ?? "8086"}");
    }
}

public class ServiceHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly IFairOrderingService _service;

    public ServiceHealthCheck(IFairOrderingService service)
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
            return health == Core.ServiceHealth.Healthy
                ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Service is healthy")
                : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Service is unhealthy");
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Service check failed", ex);
        }
    }
}
