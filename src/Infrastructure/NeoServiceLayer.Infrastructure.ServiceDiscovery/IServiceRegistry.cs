using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Infrastructure.ServiceDiscovery
{
    /// <summary>
    /// Service information for registration and discovery
    /// </summary>
    public class ServiceInfo
    {
        public string ServiceId { get; set; } = Guid.NewGuid().ToString();
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public string HostName { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Protocol { get; set; } = "http";
        public string HealthCheckUrl { get; set; } = "/health";
        public Dictionary<string, string> Metadata { get; set; } = new();
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
        public ServiceStatus Status { get; set; } = ServiceStatus.Starting;
    }

    public enum ServiceStatus
    {
        Starting,
        Healthy,
        Unhealthy,
        Stopping,
        Stopped
    }

    /// <summary>
    /// Interface for service registry and discovery
    /// </summary>
    public interface IServiceRegistry
    {
        /// <summary>
        /// Register a service with the registry
        /// </summary>
        Task<bool> RegisterServiceAsync(ServiceInfo serviceInfo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deregister a service from the registry
        /// </summary>
        Task<bool> DeregisterServiceAsync(string serviceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update service heartbeat
        /// </summary>
        Task<bool> UpdateHeartbeatAsync(string serviceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Discover services by type
        /// </summary>
        Task<IEnumerable<ServiceInfo>> DiscoverServicesAsync(string serviceType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a specific service by ID
        /// </summary>
        Task<ServiceInfo?> GetServiceAsync(string serviceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all registered services
        /// </summary>
        Task<IEnumerable<ServiceInfo>> GetAllServicesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Update service status
        /// </summary>
        Task<bool> UpdateServiceStatusAsync(string serviceId, ServiceStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        /// Subscribe to service changes
        /// </summary>
        Task SubscribeToServiceChangesAsync(string serviceType, Func<ServiceInfo, Task> callback, CancellationToken cancellationToken = default);
    }
}
