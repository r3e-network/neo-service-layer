using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NeoServiceLayer.Infrastructure.ServiceDiscovery;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton<IServiceRegistry, ConsulServiceRegistry>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add reverse proxy with dynamic service discovery
builder.Services.AddReverseProxy()
    .LoadFromMemory(GetRoutes(), GetClusters())
    .AddServiceDiscoveryDestinationResolver();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Neo Service Layer API Gateway",
        Version = "v1",
        Description = "API Gateway for Neo Service Layer Microservices"
    });
});

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapReverseProxy();

// Map service discovery endpoint
app.MapGet("/api/services", async (IServiceRegistry registry) =>
{
    var services = await registry.GetAllServicesAsync();
    return Results.Ok(services);
});

// Map service status endpoint
app.MapGet("/api/services/{serviceType}", async (string serviceType, IServiceRegistry registry) =>
{
    var services = await registry.DiscoverServicesAsync(serviceType);
    return Results.Ok(services);
});

await app.RunAsync();

// Helper methods for proxy configuration
static RouteConfig[] GetRoutes()
{
    return new[]
    {
        new RouteConfig
        {
            RouteId = "notification-route",
            ClusterId = "notification-cluster",
            Match = new RouteMatch
            {
                Path = "/api/notification/{**catch-all}"
            },
            Transforms = new[]
            {
                new Dictionary<string, string>
                {
                    ["PathPattern"] = "/api/{**catch-all}"
                }
            }
        },
        new RouteConfig
        {
            RouteId = "configuration-route",
            ClusterId = "configuration-cluster",
            Match = new RouteMatch
            {
                Path = "/api/configuration/{**catch-all}"
            },
            Transforms = new[]
            {
                new Dictionary<string, string>
                {
                    ["PathPattern"] = "/api/{**catch-all}"
                }
            }
        },
        new RouteConfig
        {
            RouteId = "backup-route",
            ClusterId = "backup-cluster",
            Match = new RouteMatch
            {
                Path = "/api/backup/{**catch-all}"
            },
            Transforms = new[]
            {
                new Dictionary<string, string>
                {
                    ["PathPattern"] = "/api/{**catch-all}"
                }
            }
        },
        new RouteConfig
        {
            RouteId = "smart-contracts-route",
            ClusterId = "smart-contracts-cluster",
            Match = new RouteMatch
            {
                Path = "/api/smart-contracts/{**catch-all}"
            },
            Transforms = new[]
            {
                new Dictionary<string, string>
                {
                    ["PathPattern"] = "/api/{**catch-all}"
                }
            }
        },
        new RouteConfig
        {
            RouteId = "cross-chain-route",
            ClusterId = "cross-chain-cluster",
            Match = new RouteMatch
            {
                Path = "/api/cross-chain/{**catch-all}"
            },
            Transforms = new[]
            {
                new Dictionary<string, string>
                {
                    ["PathPattern"] = "/api/{**catch-all}"
                }
            }
        },
        new RouteConfig
        {
            RouteId = "monitoring-route",
            ClusterId = "monitoring-cluster",
            Match = new RouteMatch
            {
                Path = "/api/monitoring/{**catch-all}"
            },
            Transforms = new[]
            {
                new Dictionary<string, string>
                {
                    ["PathPattern"] = "/api/{**catch-all}"
                }
            }
        },
        new RouteConfig
        {
            RouteId = "health-route",
            ClusterId = "health-cluster",
            Match = new RouteMatch
            {
                Path = "/api/health/{**catch-all}"
            },
            Transforms = new[]
            {
                new Dictionary<string, string>
                {
                    ["PathPattern"] = "/api/{**catch-all}"
                }
            }
        }
    };
}

static ClusterConfig[] GetClusters()
{
    return new[]
    {
        new ClusterConfig
        {
            ClusterId = "notification-cluster",
            LoadBalancingPolicy = "RoundRobin",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["notification-1"] = new DestinationConfig
                {
                    Address = "http://notification-service:80"
                }
            }
        },
        new ClusterConfig
        {
            ClusterId = "configuration-cluster",
            LoadBalancingPolicy = "RoundRobin",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["configuration-1"] = new DestinationConfig
                {
                    Address = "http://configuration-service:80"
                }
            }
        },
        new ClusterConfig
        {
            ClusterId = "backup-cluster",
            LoadBalancingPolicy = "RoundRobin",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["backup-1"] = new DestinationConfig
                {
                    Address = "http://backup-service:80"
                }
            }
        },
        new ClusterConfig
        {
            ClusterId = "smart-contracts-cluster",
            LoadBalancingPolicy = "RoundRobin",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["smart-contracts-1"] = new DestinationConfig
                {
                    Address = "http://smart-contracts-service:80"
                }
            }
        },
        new ClusterConfig
        {
            ClusterId = "cross-chain-cluster",
            LoadBalancingPolicy = "RoundRobin",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["cross-chain-1"] = new DestinationConfig
                {
                    Address = "http://cross-chain-service:80"
                }
            }
        },
        new ClusterConfig
        {
            ClusterId = "monitoring-cluster",
            LoadBalancingPolicy = "RoundRobin",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["monitoring-1"] = new DestinationConfig
                {
                    Address = "http://monitoring-service:80"
                }
            }
        },
        new ClusterConfig
        {
            ClusterId = "health-cluster",
            LoadBalancingPolicy = "RoundRobin",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["health-1"] = new DestinationConfig
                {
                    Address = "http://health-service:80"
                }
            }
        }
    };
}