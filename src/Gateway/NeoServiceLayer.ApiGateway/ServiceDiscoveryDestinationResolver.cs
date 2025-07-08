using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.ServiceDiscovery;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.LoadBalancing;

namespace NeoServiceLayer.ApiGateway
{
    public class ServiceDiscoveryDestinationResolver : IProxyConfigProvider
    {
        private readonly IServiceRegistry _serviceRegistry;
        private readonly ILogger<ServiceDiscoveryDestinationResolver> _logger;
        private volatile ProxyConfig _config;
        private readonly Timer _updateTimer;

        public ServiceDiscoveryDestinationResolver(
            IServiceRegistry serviceRegistry,
            ILogger<ServiceDiscoveryDestinationResolver> logger)
        {
            _serviceRegistry = serviceRegistry;
            _logger = logger;
            _config = new ProxyConfig(Array.Empty<RouteConfig>(), Array.Empty<ClusterConfig>());
            
            // Update configuration every 10 seconds
            _updateTimer = new Timer(async _ => await UpdateConfiguration(), null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        }

        public IProxyConfig GetConfig() => _config;

        private async Task UpdateConfiguration()
        {
            try
            {
                var services = await _serviceRegistry.GetAllServicesAsync();
                var groupedServices = services.GroupBy(s => s.ServiceType);

                var routes = new List<RouteConfig>();
                var clusters = new List<ClusterConfig>();

                foreach (var serviceGroup in groupedServices)
                {
                    var serviceType = serviceGroup.Key;
                    var serviceName = serviceType.ToLower().Replace("service", "");
                    
                    // Create route
                    routes.Add(new RouteConfig
                    {
                        RouteId = $"{serviceName}-route",
                        ClusterId = $"{serviceName}-cluster",
                        Match = new RouteMatch
                        {
                            Path = $"/api/{serviceName}/{{**catch-all}}"
                        },
                        Transforms = new[]
                        {
                            new Dictionary<string, string>
                            {
                                ["PathPattern"] = "/api/{**catch-all}"
                            }
                        }
                    });

                    // Create cluster with all healthy instances
                    var destinations = new Dictionary<string, DestinationConfig>();
                    var instanceIndex = 1;
                    
                    foreach (var service in serviceGroup.Where(s => s.Status == ServiceStatus.Healthy))
                    {
                        destinations[$"{serviceName}-{instanceIndex++}"] = new DestinationConfig
                        {
                            Address = $"{service.Protocol}://{service.HostName}:{service.Port}",
                            Health = service.HealthCheckUrl,
                            Metadata = service.Metadata
                        };
                    }

                    if (destinations.Any())
                    {
                        clusters.Add(new ClusterConfig
                        {
                            ClusterId = $"{serviceName}-cluster",
                            LoadBalancingPolicy = LoadBalancingPolicies.RoundRobin,
                            HealthCheck = new HealthCheckConfig
                            {
                                Active = new ActiveHealthCheckConfig
                                {
                                    Enabled = true,
                                    Interval = TimeSpan.FromSeconds(10),
                                    Timeout = TimeSpan.FromSeconds(5),
                                    Policy = "ConsecutiveFailures",
                                    Path = "/health"
                                }
                            },
                            Destinations = destinations
                        });
                    }
                }

                _config = new ProxyConfig(routes, clusters);
                _logger.LogInformation("Updated proxy configuration with {RouteCount} routes and {ClusterCount} clusters",
                    routes.Count, clusters.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update proxy configuration from service discovery");
            }
        }

        private class ProxyConfig : IProxyConfig
        {
            private readonly CancellationTokenSource _cts = new();

            public ProxyConfig(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
            {
                Routes = routes;
                Clusters = clusters;
                ChangeToken = new CancellationChangeToken(_cts.Token);
            }

            public IReadOnlyList<RouteConfig> Routes { get; }
            public IReadOnlyList<ClusterConfig> Clusters { get; }
            public IChangeToken ChangeToken { get; }

            internal void SignalChange()
            {
                _cts.Cancel();
            }
        }
    }

    public static class ServiceCollectionExtensions
    {
        public static IReverseProxyBuilder AddServiceDiscoveryDestinationResolver(this IReverseProxyBuilder builder)
        {
            builder.Services.AddSingleton<ServiceDiscoveryDestinationResolver>();
            builder.Services.AddSingleton<IProxyConfigProvider>(provider => 
                provider.GetRequiredService<ServiceDiscoveryDestinationResolver>());
            return builder;
        }
    }
}