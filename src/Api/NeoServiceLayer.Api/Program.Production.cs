using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Api.Middleware;
using NeoServiceLayer.Infrastructure.Persistence.Migrations;
using NeoServiceLayer.Infrastructure.Resilience;
using NeoServiceLayer.Infrastructure.Secrets;
using Serilog;
using Serilog.Events;

namespace NeoServiceLayer.Api;

public class ProductionProgram
{
    public static void Main(string[] args)
    {
        // Configure Serilog for production
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File(
                Path.Combine("logs", "neo-service-layer-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                fileSizeLimitBytes: 100_000_000,
                rollOnFileSizeLimit: true)
            .CreateLogger();

        try
        {
            Log.Information("Starting Neo Service Layer API");
            CreateHostBuilder(args).Build().Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application start-up failed");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureKestrel(serverOptions =>
                {
                    serverOptions.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
                    serverOptions.Limits.MaxRequestHeaderCount = 100;
                    serverOptions.Limits.MaxRequestLineSize = 8192;
                    serverOptions.Limits.MaxRequestHeadersTotalSize = 32768;

                    // Configure HTTPS
                    serverOptions.ConfigureHttpsDefaults(httpsOptions =>
                    {
                        var certPath = Environment.GetEnvironmentVariable("CERTIFICATE_PATH") ?? "/https/certificate.pfx";
                        var certPassword = Environment.GetEnvironmentVariable("CERTIFICATE_PASSWORD");

                        if (File.Exists(certPath) && !string.IsNullOrEmpty(certPassword))
                        {
                            httpsOptions.ServerCertificate = new X509Certificate2(certPath, certPassword);
                        }
                    });
                })
                .UseStartup<ProductionStartup>();
            });
}

public class ProductionStartup
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public ProductionStartup(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // Add secret provider
        if (_configuration["AZURE_KEY_VAULT_URL"] != null)
        {
            services.AddSingleton<ISecretProvider, AzureKeyVaultSecretProvider>();
        }
        else if (_configuration["AWS_REGION"] != null)
        {
            services.AddSingleton<ISecretProvider, AwsSecretsManagerProvider>();
        }
        else
        {
            services.AddSingleton<ISecretProvider, EnvironmentSecretProvider>();
        }

        // Add database migration service
        services.AddScoped<IDatabaseMigrationService, DatabaseMigrationService>();
        services.AddHostedService<DatabaseMigrationHostedService>();

        // Add resilience policies
        services.AddResiliencePolicies(_configuration);

        // Add rate limiting
        services.AddRateLimiting(_configuration);

        // Configure CORS for production
        services.AddCors(options =>
        {
            options.AddPolicy("Production", builder =>
            {
                var allowedOrigins = _configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                    ?? new[] { "https://neo-service-layer.com" };

                builder
                    .WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            });
        });

        // Add security headers
        services.AddHsts(options =>
        {
            options.Preload = true;
            options.IncludeSubDomains = true;
            options.MaxAge = TimeSpan.FromDays(365);
        });

        // Configure authentication
        services.AddAuthentication()
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                options.SaveToken = true;
                options.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(0);
            });

        // Add distributed caching with Redis
        var redisConnection = _configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "NeoServiceLayer";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        // Add health checks
        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>("database")
            .AddRedis(redisConnection ?? "localhost:6379", name: "redis", tags: new[] { "cache" });

        // Add other services
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<ProductionStartup> logger)
    {
        // Configure forwarded headers for proxy scenarios
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        // Security headers middleware
        app.Use(async (context, next) =>
        {
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Add("X-Frame-Options", "DENY");
            context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
            context.Response.Headers.Add("Content-Security-Policy",
                "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self'; connect-src 'self'; frame-ancestors 'none';");
            context.Response.Headers.Add("Permissions-Policy",
                "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");

            await next();
        });

        // Force HTTPS
        if (_configuration.GetValue<bool>("Security:RequireHttps", true))
        {
            app.UseHttpsRedirection();
            app.UseHsts();
        }

        // Input validation middleware
        app.UseMiddleware<InputValidationMiddleware>();

        // Rate limiting
        app.UseRateLimiter();

        // Exception handling
        app.UseExceptionHandler("/error");

        // Request logging
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
            };
        });

        // CORS
        app.UseCors("Production");

        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Health checks
        app.UseHealthChecks("/health");

        // API endpoints
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready")
            });
            endpoints.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = _ => false
            });
        });

        // Swagger (disabled in production by default)
        if (_configuration.GetValue<bool>("EnableSwaggerInProduction", false))
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        logger.LogInformation("Neo Service Layer API started in Production mode");
    }
}
