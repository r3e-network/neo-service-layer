using System.Net;
using System.Threading.RateLimiting;
using Consul;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using NeoServiceLayer.Infrastructure.ServiceDiscovery;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Health;
using Yarp.ReverseProxy.LoadBalancing;

namespace NeoServiceLayer.Gateway.Api;

public class Program
{
    public static void Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/gateway-.txt", rollingInterval: RollingInterval.Day)
            .Enrich.FromLogContext()
            .CreateLogger();

        try
        {
            Log.Information("Starting API Gateway...");
            CreateHostBuilder(args).Build().Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "API Gateway terminated unexpectedly");
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
                webBuilder.UseStartup<Startup>();
            });
}

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        // Add health checks
        services.AddHealthChecks()
            .AddCheck("gateway_health", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

        // Add authentication
        services.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", options =>
            {
                options.Authority = Configuration["Auth:Authority"];
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateAudience = false
                };
            });

        services.AddAuthorization();

        // Add rate limiting
        services.AddRateLimiter(options =>
        {
            // Default policy for authenticated users
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.User?.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            // Strict policy for authentication endpoints
            options.AddPolicy("AuthPolicy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            // Generous policy for health checks
            options.AddPolicy("HealthPolicy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: "health",
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 1000,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = 429;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                }
                await context.HttpContext.Response.WriteAsync("Rate limit exceeded. Too many requests.", cancellationToken: token);
            };
        });

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

        // Add Consul
        services.AddSingleton<IConsulClient>(p => new ConsulClient(config =>
        {
            config.Address = new Uri(Configuration["Consul:Address"] ?? "http://consul:8500");
            config.Datacenter = Configuration["Consul:Datacenter"] ?? "dc1";
        }));

        // Add service discovery
        services.AddSingleton<IServiceRegistry, ConsulServiceRegistry>();

        // Add dynamic proxy configuration
        services.AddSingleton<IProxyConfigProvider, ConsulProxyConfigProvider>();
        services.AddHostedService<ProxyConfigurationUpdater>();

        // Add YARP
        services.AddReverseProxy()
            .LoadFromMemory(new List<RouteConfig>(), new List<ClusterConfig>());

        // Add HTTP client with retry policies
        services.AddHttpClient("gateway")
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        // Add metrics
        services.AddOpenTelemetry()
            .WithMetrics(builder =>
            {
                builder.AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation()
                       .AddPrometheusExporter();
            });

        services.AddControllers();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseSerilogRequestLogging();
        app.UseRateLimiter();
        app.UseRouting();
        app.UseCors("AllowAll");
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapReverseProxy();
            endpoints.MapHealthChecks("/health").RequireRateLimiting("HealthPolicy");
            endpoints.MapControllers();

            // Prometheus metrics endpoint
            endpoints.MapPrometheusScrapingEndpoint();

            // Gateway info endpoint
            endpoints.MapGet("/", async context =>
            {
                await context.Response.WriteAsJsonAsync(new
                {
                    service = "Neo Service Layer API Gateway",
                    version = "1.0.0",
                    status = "running",
                    endpoints = new[]
                    {
                        "/health",
                        "/metrics",
                        "/api/*"
                    },
                    rateLimiting = new
                    {
                        globalLimit = "100 requests per minute per user/IP",
                        authEndpoints = "10 requests per minute per IP",
                        healthEndpoints = "1000 requests per minute"
                    }
                });
            });
        });
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => !msg.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    Log.Warning("Retry {RetryCount} after {Delay}ms", retryCount, timespan.TotalMilliseconds);
                });
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                5,
                TimeSpan.FromSeconds(30),
                onBreak: (result, timespan) => Log.Warning("Circuit breaker opened for {Duration}s", timespan.TotalSeconds),
                onReset: () => Log.Information("Circuit breaker reset"));
    }
}

/// <summary>
/// Dynamic proxy configuration provider that reads from Consul
/// </summary>
public class ConsulProxyConfigProvider : IProxyConfigProvider
{
    private volatile InMemoryConfigProvider _config;
    private readonly IServiceRegistry _serviceRegistry;
    private readonly ILogger<ConsulProxyConfigProvider> _logger;

    public ConsulProxyConfigProvider(IServiceRegistry serviceRegistry, ILogger<ConsulProxyConfigProvider> logger)
    {
        _serviceRegistry = serviceRegistry;
        _logger = logger;
        _config = new InMemoryConfigProvider(new List<RouteConfig>(), new List<ClusterConfig>());
    }

    public IProxyConfig GetConfig() => _config.GetConfig();

    public async Task UpdateConfigAsync()
    {
        try
        {
            var services = await _serviceRegistry.DiscoverServicesAsync("*");
            var routes = new List<RouteConfig>();
            var clusters = new List<ClusterConfig>();

            foreach (var serviceGroup in services.GroupBy(s => s.ServiceType))
            {
                var serviceType = serviceGroup.Key.ToLower().Replace("service", "");
                var clusterId = $"{serviceType}-cluster";

                // Create route with rate limiting
                var routeMetadata = new Dictionary<string, string>();

                // Apply stricter rate limiting for auth-related services
                if (serviceType.Contains("auth") || serviceType.Contains("jwt") || serviceType.Contains("token"))
                {
                    routeMetadata["RateLimitingPolicy"] = "AuthPolicy";
                }

                routes.Add(new RouteConfig
                {
                    RouteId = $"{serviceType}-route",
                    ClusterId = clusterId,
                    Match = new RouteMatch
                    {
                        Path = $"/api/{serviceType.Replace("_", "-")}/{{**catch-all}}"
                    },
                    Transforms = new[]
                    {
                        new Dictionary<string, string>
                        {
                            { "PathPattern", $"/api/{serviceType.Replace("_", "-")}/{{**catch-all}}" }
                        }
                    },
                    Metadata = routeMetadata
                });

                // Create cluster with destinations
                var destinations = new Dictionary<string, DestinationConfig>();
                foreach (var service in serviceGroup)
                {
                    destinations[$"{service.ServiceName}-{service.ServiceId}"] = new DestinationConfig
                    {
                        Address = $"http://{service.HostName}:{service.Port}"
                    };
                }

                clusters.Add(new ClusterConfig
                {
                    ClusterId = clusterId,
                    LoadBalancingPolicy = LoadBalancingPolicies.RoundRobin,
                    HealthCheck = new HealthCheckConfig
                    {
                        Active = new ActiveHealthCheckConfig
                        {
                            Enabled = true,
                            Interval = TimeSpan.FromSeconds(30),
                            Timeout = TimeSpan.FromSeconds(10),
                            Policy = HealthCheckConstants.ActivePolicy.ConsecutiveFailures,
                            Path = "/health"
                        }
                    },
                    Destinations = destinations
                });
            }

            _config.Update(routes, clusters);
            _logger.LogInformation("Updated proxy configuration with {RouteCount} routes and {ClusterCount} clusters",
                routes.Count, clusters.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating proxy configuration from Consul");
        }
    }
}

/// <summary>
/// Background service that periodically updates proxy configuration
/// </summary>
public class ProxyConfigurationUpdater : BackgroundService
{
    private readonly ConsulProxyConfigProvider _configProvider;
    private readonly ILogger<ProxyConfigurationUpdater> _logger;

    public ProxyConfigurationUpdater(
        IProxyConfigProvider configProvider,
        ILogger<ProxyConfigurationUpdater> logger)
    {
        _configProvider = configProvider as ConsulProxyConfigProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_configProvider != null)
                {
                    await _configProvider.UpdateConfigAsync();
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in proxy configuration updater");
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }
    }
}
