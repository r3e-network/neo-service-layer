using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.Infrastructure.Resilience;
using NeoServiceLayer.Infrastructure.ServiceDiscovery;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Tee.Host.Services;

namespace NeoServiceLayer.AI.Prediction;

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
        services.AddNeoServiceFramework(_configuration);

        // Add infrastructure services
        services.AddPersistence(_configuration);
        services.AddBlockchainServices(_configuration);
        services.AddResiliencePolicies(_configuration);

        // Add TEE services
        services.AddSingleton<IEnclaveManager, EnclaveManager>();

        // Add prediction service
        services.AddScoped<IPredictionService, PredictionService>();

        // Add service discovery
        services.AddServiceDiscovery(_configuration);

        // Add health checks
        services.AddHealthChecks()
            .AddCheck<ServiceHealthCheck>("prediction_health");

        // Add controllers
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "Prediction Service API", Version = "v1" });
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
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Prediction Service API v1"));
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
            var predictionService = scope.ServiceProvider.GetRequiredService<IPredictionService>();
            var initTask = predictionService.InitializeAsync();
            initTask.Wait();

            if (initTask.Result)
            {
                logger.LogInformation("Prediction Service initialized successfully");
            }
            else
            {
                logger.LogError("Failed to initialize Prediction Service");
                throw new Exception("Service initialization failed");
            }
        }

        logger.LogInformation($"Prediction Service started on port {_configuration["Port"] ?? "8085"}");
    }
}

public class ServiceHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly IPredictionService _service;

    public ServiceHealthCheck(IPredictionService service)
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
