using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NeoServiceLayer.Core.ServiceArchitecture
{
    /// <summary>
    /// Service registry for managing service discovery and registration
    /// </summary>
    public interface IServiceRegistry
    {
        /// <summary>
        /// Registers a service with the registry
        /// </summary>
        Task<ServiceRegistration> RegisterServiceAsync(ServiceRegistration registration);

        /// <summary>
        /// Unregisters a service from the registry
        /// </summary>
        Task<bool> UnregisterServiceAsync(string serviceId);

        /// <summary>
        /// Discovers services by type
        /// </summary>
        Task<IEnumerable<ServiceRegistration>> DiscoverServicesAsync(Type serviceType);

        /// <summary>
        /// Discovers services by capability
        /// </summary>
        Task<IEnumerable<ServiceRegistration>> DiscoverServicesByCapabilityAsync(string capability);

        /// <summary>
        /// Gets a specific service registration
        /// </summary>
        Task<ServiceRegistration> GetServiceAsync(string serviceId);

        /// <summary>
        /// Updates service health status
        /// </summary>
        Task UpdateServiceHealthAsync(string serviceId, ServiceHealth health);

        /// <summary>
        /// Gets all registered services
        /// </summary>
        Task<IEnumerable<ServiceRegistration>> GetAllServicesAsync();

        /// <summary>
        /// Checks if a service is available
        /// </summary>
        Task<bool> IsServiceAvailableAsync(string serviceId);

        /// <summary>
        /// Gets service endpoints
        /// </summary>
        Task<IEnumerable<ServiceEndpoint>> GetServiceEndpointsAsync(string serviceId);

        /// <summary>
        /// Subscribes to service registry events
        /// </summary>
        void Subscribe(IServiceRegistryObserver observer);

        /// <summary>
        /// Unsubscribes from service registry events
        /// </summary>
        void Unsubscribe(IServiceRegistryObserver observer);
    }

    /// <summary>
    /// Service registration information
    /// </summary>
    public class ServiceRegistration
    {
        public string ServiceId { get; set; }
        public string ServiceName { get; set; }
        public string ServiceType { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public ServiceHealth Health { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime LastHealthCheck { get; set; }
        public List<ServiceEndpoint> Endpoints { get; set; } = new();
        public List<string> Capabilities { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();
        public Dictionary<string, string> Metadata { get; set; } = new();
        public ServiceConfiguration Configuration { get; set; }
        public ServiceLoadBalancing LoadBalancing { get; set; }
        public ServiceSla Sla { get; set; }
    }

    /// <summary>
    /// Service endpoint information
    /// </summary>
    public class ServiceEndpoint
    {
        public string Protocol { get; set; } // HTTP, gRPC, TCP, etc.
        public string Address { get; set; }
        public int Port { get; set; }
        public string Path { get; set; }
        public bool IsSecure { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new();
        public int Priority { get; set; }
        public int Weight { get; set; }
        public EndpointHealth Health { get; set; }
    }

    /// <summary>
    /// Endpoint health status
    /// </summary>
    public enum EndpointHealth
    {
        Unknown,
        Healthy,
        Unhealthy,
        Degraded
    }

    /// <summary>
    /// Service configuration
    /// </summary>
    public class ServiceConfiguration
    {
        public int MaxConcurrentRequests { get; set; } = 100;
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(10);
        public int RetryCount { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
        public bool CircuitBreakerEnabled { get; set; } = true;
        public int CircuitBreakerThreshold { get; set; } = 5;
        public TimeSpan CircuitBreakerTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public bool CacheEnabled { get; set; } = true;
        public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(5);
    }

    /// <summary>
    /// Load balancing configuration
    /// </summary>
    public class ServiceLoadBalancing
    {
        public LoadBalancingStrategy Strategy { get; set; } = LoadBalancingStrategy.RoundRobin;
        public bool HealthCheckEnabled { get; set; } = true;
        public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromSeconds(30);
        public int HealthCheckTimeout { get; set; } = 5000;
        public string HealthCheckPath { get; set; } = "/health";
        public bool StickySession { get; set; } = false;
        public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromMinutes(30);
    }

    /// <summary>
    /// Load balancing strategies
    /// </summary>
    public enum LoadBalancingStrategy
    {
        RoundRobin,
        LeastConnections,
        WeightedRoundRobin,
        Random,
        IpHash,
        ConsistentHash
    }

    /// <summary>
    /// Service level agreement
    /// </summary>
    public class ServiceSla
    {
        public double AvailabilityTarget { get; set; } = 99.9; // Percentage
        public TimeSpan MaxResponseTime { get; set; } = TimeSpan.FromMilliseconds(1000);
        public int MaxErrorRate { get; set; } = 1; // Percentage
        public int ThroughputTarget { get; set; } = 1000; // Requests per second
        public TimeSpan MaintenanceWindow { get; set; } = TimeSpan.FromHours(2);
        public string SupportLevel { get; set; } = "Standard"; // Standard, Premium, Enterprise
    }

    /// <summary>
    /// Observer interface for service registry events
    /// </summary>
    public interface IServiceRegistryObserver
    {
        void OnServiceRegistered(ServiceRegistration registration);
        void OnServiceUnregistered(string serviceId);
        void OnServiceHealthChanged(string serviceId, ServiceHealth health);
        void OnServiceEndpointChanged(string serviceId, ServiceEndpoint endpoint);
    }
}