using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Infrastructure.ServiceDiscovery
{
    /// <summary>
    /// Consul-based implementation of service registry
    /// </summary>
    public class ConsulServiceRegistry : IServiceRegistry
    {
        private readonly IConsulClient _consulClient;
        private readonly ILogger<ConsulServiceRegistry> _logger;
        private readonly string _datacenter;

        public ConsulServiceRegistry(IConfiguration configuration, ILogger<ConsulServiceRegistry> logger)
        {
            _logger = logger;

            var consulConfig = new ConsulClientConfiguration
            {
                Address = new Uri(configuration.GetValue<string>("Consul:Address") ?? "http://consul:8500"),
                Datacenter = configuration.GetValue<string>("Consul:Datacenter") ?? "dc1"
            };

            _datacenter = consulConfig.Datacenter;
            _consulClient = new ConsulClient(consulConfig);
        }

        public async Task<bool> RegisterServiceAsync(ServiceInfo serviceInfo, CancellationToken cancellationToken = default)
        {
            try
            {
                var registration = new AgentServiceRegistration
                {
                    ID = serviceInfo.ServiceId,
                    Name = serviceInfo.ServiceName,
                    Tags = new[] { serviceInfo.ServiceType, "neo-service-layer" },
                    Address = serviceInfo.HostName,
                    Port = serviceInfo.Port,
                    Meta = serviceInfo.Metadata,
                    Check = new AgentServiceCheck
                    {
                        HTTP = $"{serviceInfo.Protocol}://{serviceInfo.HostName}:{serviceInfo.Port}{serviceInfo.HealthCheckUrl}",
                        Interval = TimeSpan.FromSeconds(10),
                        Timeout = TimeSpan.FromSeconds(5),
                        DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(5)
                    }
                };

                var result = await _consulClient.Agent.ServiceRegister(registration, cancellationToken);

                _logger.LogInformation("Service {ServiceName} ({ServiceId}) registered successfully",
                    serviceInfo.ServiceName, serviceInfo.ServiceId);

                return result.StatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register service {ServiceName}", serviceInfo.ServiceName);
                return false;
            }
        }

        public async Task<bool> DeregisterServiceAsync(string serviceId, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _consulClient.Agent.ServiceDeregister(serviceId, cancellationToken);

                _logger.LogInformation("Service {ServiceId} deregistered successfully", serviceId);

                return result.StatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deregister service {ServiceId}", serviceId);
                return false;
            }
        }

        public async Task<bool> UpdateHeartbeatAsync(string serviceId, CancellationToken cancellationToken = default)
        {
            try
            {
                var checkId = $"service:{serviceId}";
                await _consulClient.Agent.PassTTL(checkId, "Service is healthy", cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update heartbeat for service {ServiceId}", serviceId);
                return false;
            }
        }

        public async Task<IEnumerable<ServiceInfo>> DiscoverServicesAsync(string serviceType, CancellationToken cancellationToken = default)
        {
            try
            {
                var services = await _consulClient.Health.Service(serviceType, string.Empty, true, cancellationToken);

                return services.Response.Select(entry => new ServiceInfo
                {
                    ServiceId = entry.Service.ID,
                    ServiceName = entry.Service.Service,
                    ServiceType = serviceType,
                    HostName = entry.Service.Address,
                    Port = entry.Service.Port,
                    Metadata = entry.Service.Meta?.ToDictionary(x => x.Key, x => x.Value) ?? new Dictionary<string, string>(),
                    Status = MapConsulStatus(entry.Checks)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to discover services of type {ServiceType}", serviceType);
                return Enumerable.Empty<ServiceInfo>();
            }
        }

        public async Task<ServiceInfo?> GetServiceAsync(string serviceId, CancellationToken cancellationToken = default)
        {
            try
            {
                var services = await _consulClient.Agent.Services(cancellationToken);

                if (services.Response.TryGetValue(serviceId, out var service))
                {
                    return new ServiceInfo
                    {
                        ServiceId = service.ID,
                        ServiceName = service.Service,
                        ServiceType = service.Tags?.FirstOrDefault() ?? string.Empty,
                        HostName = service.Address,
                        Port = service.Port,
                        Metadata = service.Meta?.ToDictionary(x => x.Key, x => x.Value) ?? new Dictionary<string, string>()
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get service {ServiceId}", serviceId);
                return null;
            }
        }

        public async Task<IEnumerable<ServiceInfo>> GetAllServicesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var services = await _consulClient.Agent.Services(cancellationToken);

                return services.Response.Values.Select(service => new ServiceInfo
                {
                    ServiceId = service.ID,
                    ServiceName = service.Service,
                    ServiceType = service.Tags?.FirstOrDefault() ?? string.Empty,
                    HostName = service.Address,
                    Port = service.Port,
                    Metadata = service.Meta?.ToDictionary(x => x.Key, x => x.Value) ?? new Dictionary<string, string>()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all services");
                return Enumerable.Empty<ServiceInfo>();
            }
        }

        public async Task<bool> UpdateServiceStatusAsync(string serviceId, ServiceStatus status, CancellationToken cancellationToken = default)
        {
            try
            {
                var checkId = $"service:{serviceId}";

                var (checkStatus, output) = status switch
                {
                    ServiceStatus.Healthy => (HealthStatus.Passing, "Service is healthy"),
                    ServiceStatus.Unhealthy => (HealthStatus.Critical, "Service is unhealthy"),
                    ServiceStatus.Starting => (HealthStatus.Warning, "Service is starting"),
                    ServiceStatus.Stopping => (HealthStatus.Warning, "Service is stopping"),
                    _ => (HealthStatus.Critical, "Service is in unknown state")
                };

                await _consulClient.Agent.UpdateTTL(checkId, output, TTLStatus.Pass, cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update status for service {ServiceId}", serviceId);
                return false;
            }
        }

        public async Task SubscribeToServiceChangesAsync(string serviceType, Func<ServiceInfo, Task> callback, CancellationToken cancellationToken = default)
        {
            // Consul doesn't have built-in push notifications, so we'll use polling
            // In production, you might want to use Consul's blocking queries or watches

            _ = Task.Run(async () =>
            {
                var lastIndex = 0UL;
                var knownServices = new Dictionary<string, ServiceInfo>();

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var queryOptions = new QueryOptions
                        {
                            WaitIndex = lastIndex,
                            WaitTime = TimeSpan.FromSeconds(30)
                        };

                        var services = await _consulClient.Health.Service(serviceType, string.Empty, true, queryOptions, cancellationToken);
                        lastIndex = services.LastIndex;

                        var currentServices = services.Response.ToDictionary(
                            s => s.Service.ID,
                            s => new ServiceInfo
                            {
                                ServiceId = s.Service.ID,
                                ServiceName = s.Service.Service,
                                ServiceType = serviceType,
                                HostName = s.Service.Address,
                                Port = s.Service.Port,
                                Metadata = s.Service.Meta?.ToDictionary(x => x.Key, x => x.Value) ?? new Dictionary<string, string>(),
                                Status = MapConsulStatus(s.Checks)
                            });

                        // Check for new or updated services
                        foreach (var serviceKvp in currentServices)
                        {
                            var id = serviceKvp.Key;
                            var service = serviceKvp.Value;
                            if (!knownServices.ContainsKey(id) || !ServicesEqual(knownServices[id], service))
                            {
                                await callback(service);
                            }
                        }

                        knownServices = currentServices;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in service change subscription for type {ServiceType}", serviceType);
                        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    }
                }
            }, cancellationToken);

            await Task.CompletedTask;
        }

        private ServiceStatus MapConsulStatus(HealthCheck[] checks)
        {
            if (checks.All(c => c.Status == HealthStatus.Passing))
                return ServiceStatus.Healthy;

            if (checks.Any(c => c.Status == HealthStatus.Critical))
                return ServiceStatus.Unhealthy;

            return ServiceStatus.Starting;
        }

        private bool ServicesEqual(ServiceInfo a, ServiceInfo b)
        {
            return a.ServiceId == b.ServiceId &&
                   a.HostName == b.HostName &&
                   a.Port == b.Port &&
                   a.Status == b.Status;
        }
    }
}
