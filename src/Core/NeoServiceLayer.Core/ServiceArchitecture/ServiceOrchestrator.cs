using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Core.ServiceArchitecture
{
    /// <summary>
    /// Service orchestrator for managing service lifecycle and dependencies
    /// </summary>
    public class ServiceOrchestrator : IServiceOrchestrator
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IServiceRegistry _serviceRegistry;
        private readonly IServiceCommunication _serviceCommunication;
        private readonly ILogger<ServiceOrchestrator> _logger;
        private readonly Dictionary<string, ServiceInstance> _services;
        private readonly SemaphoreSlim _semaphore;
        private bool _isRunning;

        public ServiceOrchestrator(
            IServiceProvider serviceProvider,
            IServiceRegistry serviceRegistry,
            IServiceCommunication serviceCommunication,
            ILogger<ServiceOrchestrator> logger)
        {
            _serviceProvider = serviceProvider;
            _serviceRegistry = serviceRegistry;
            _serviceCommunication = serviceCommunication;
            _logger = logger;
            _services = new Dictionary<string, ServiceInstance>();
            _semaphore = new SemaphoreSlim(1, 1);
        }

        public bool IsRunning => _isRunning;
        public IReadOnlyDictionary<string, ServiceInstance> Services => _services;

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (_isRunning)
                {
                    _logger.LogWarning("Service orchestrator is already running");
                    return;
                }

                _logger.LogInformation("Starting service orchestrator");

                // Discover and register all services
                await DiscoverAndRegisterServicesAsync(cancellationToken);

                // Resolve dependencies
                await ResolveDependenciesAsync(cancellationToken);

                // Start services in dependency order
                await StartServicesInOrderAsync(cancellationToken);

                _isRunning = true;
                _logger.LogInformation("Service orchestrator started successfully");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (!_isRunning)
                {
                    _logger.LogWarning("Service orchestrator is not running");
                    return;
                }

                _logger.LogInformation("Stopping service orchestrator");

                // Stop services in reverse dependency order
                await StopServicesInOrderAsync(cancellationToken);

                // Unregister services
                await UnregisterServicesAsync(cancellationToken);

                _services.Clear();
                _isRunning = false;
                _logger.LogInformation("Service orchestrator stopped successfully");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<T> GetServiceAsync<T>(string serviceId = null, CancellationToken cancellationToken = default) 
            where T : class, IService
        {
            if (string.IsNullOrEmpty(serviceId))
            {
                // Find first service of type T
                var serviceInstance = _services.Values.FirstOrDefault(s => s.Service is T);
                return serviceInstance != null ? (T)serviceInstance.Service : default;
            }

            if (_services.TryGetValue(serviceId, out var instance) && instance.Service is T service)
            {
                return service;
            }

            // Try to discover from registry
            var registration = await _serviceRegistry.GetServiceAsync(serviceId);
            if (registration != null)
            {
                return await CreateServiceProxyAsync<T>(registration, cancellationToken);
            }

            return default;
        }

        public async Task<IEnumerable<T>> GetServicesAsync<T>(CancellationToken cancellationToken = default) 
            where T : class, IService
        {
            var localServices = _services.Values
                .Where(s => s.Service is T)
                .Select(s => (T)s.Service)
                .ToList();

            // Discover remote services
            var registrations = await _serviceRegistry.DiscoverServicesAsync(typeof(T));
            var remoteServices = new List<T>();

            foreach (var registration in registrations)
            {
                if (!_services.ContainsKey(registration.ServiceId))
                {
                    var proxy = await CreateServiceProxyAsync<T>(registration, cancellationToken);
                    if (proxy != null)
                    {
                        remoteServices.Add(proxy);
                    }
                }
            }

            return localServices.Concat(remoteServices);
        }

        public async Task RegisterServiceAsync(IService service, ServiceRegistrationOptions options = null, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                var serviceId = options?.ServiceId ?? Guid.NewGuid().ToString();
                
                if (_services.ContainsKey(serviceId))
                {
                    throw new InvalidOperationException($"Service with ID {serviceId} is already registered");
                }

                var registration = new ServiceRegistration
                {
                    ServiceId = serviceId,
                    ServiceName = service.Name,
                    ServiceType = service.GetType().FullName,
                    Version = service.Version,
                    Description = service.Description,
                    Health = ServiceHealth.NotRunning,
                    RegisteredAt = DateTime.UtcNow,
                    Capabilities = service.Capabilities?.Select(c => c.Name).ToList() ?? new List<string>(),
                    Dependencies = service.Dependencies?.Select(d => d.ToString()).ToList() ?? new List<string>(),
                    Metadata = service.Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>(),
                    Configuration = options?.Configuration,
                    LoadBalancing = options?.LoadBalancing,
                    Sla = options?.Sla
                };

                // Add endpoints if provided
                if (options?.Endpoints != null)
                {
                    registration.Endpoints = options.Endpoints;
                }

                // Register with registry
                await _serviceRegistry.RegisterServiceAsync(registration);

                // Add to local services
                _services[serviceId] = new ServiceInstance
                {
                    ServiceId = serviceId,
                    Service = service,
                    Registration = registration,
                    State = ServiceState.Registered,
                    RegisteredAt = DateTime.UtcNow
                };

                _logger.LogInformation("Registered service {ServiceName} with ID {ServiceId}", service.Name, serviceId);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task UnregisterServiceAsync(string serviceId, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (!_services.TryGetValue(serviceId, out var instance))
                {
                    _logger.LogWarning("Service with ID {ServiceId} not found", serviceId);
                    return;
                }

                // Stop service if running
                if (instance.State == ServiceState.Running)
                {
                    await instance.Service.StopAsync();
                }

                // Unregister from registry
                await _serviceRegistry.UnregisterServiceAsync(serviceId);

                // Remove from local services
                _services.Remove(serviceId);

                _logger.LogInformation("Unregistered service {ServiceName} with ID {ServiceId}", 
                    instance.Service.Name, serviceId);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<ServiceHealth> GetServiceHealthAsync(string serviceId, CancellationToken cancellationToken = default)
        {
            if (_services.TryGetValue(serviceId, out var instance))
            {
                return await instance.Service.GetHealthAsync();
            }

            var registration = await _serviceRegistry.GetServiceAsync(serviceId);
            return registration?.Health ?? ServiceHealth.NotRunning;
        }

        public async Task<IDictionary<string, object>> GetServiceMetricsAsync(string serviceId, CancellationToken cancellationToken = default)
        {
            if (_services.TryGetValue(serviceId, out var instance))
            {
                return await instance.Service.GetMetricsAsync();
            }

            return new Dictionary<string, object>();
        }

        private async Task DiscoverAndRegisterServicesAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Discovering services from dependency injection container");

            // Get all registered IService implementations
            var services = _serviceProvider.GetServices<IService>();

            foreach (var service in services)
            {
                try
                {
                    await RegisterServiceAsync(service, new ServiceRegistrationOptions
                    {
                        ServiceId = $"{service.GetType().Name}_{Guid.NewGuid():N}",
                        Configuration = new ServiceConfiguration(),
                        LoadBalancing = new ServiceLoadBalancing(),
                        Sla = new ServiceSla()
                    }, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to register service {ServiceName}", service.Name);
                }
            }

            _logger.LogInformation("Discovered and registered {Count} services", _services.Count);
        }

        private async Task ResolveDependenciesAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Resolving service dependencies");

            var dependencyGraph = new Dictionary<string, List<string>>();
            
            foreach (var (serviceId, instance) in _services)
            {
                var dependencies = new List<string>();
                
                if (instance.Service.Dependencies != null)
                {
                    foreach (var dependency in instance.Service.Dependencies)
                    {
                        // Find service that provides this dependency
                        var providerService = _services.Values.FirstOrDefault(s => 
                            s.Service.GetType().GetInterfaces().Any(i => i.Name == dependency.ToString()));
                        
                        if (providerService != null)
                        {
                            dependencies.Add(providerService.ServiceId);
                        }
                    }
                }
                
                dependencyGraph[serviceId] = dependencies;
            }

            // Check for circular dependencies
            if (HasCircularDependency(dependencyGraph))
            {
                throw new InvalidOperationException("Circular dependency detected in service graph");
            }

            // Store resolved dependencies
            foreach (var (serviceId, dependencies) in dependencyGraph)
            {
                _services[serviceId].Dependencies = dependencies;
            }

            _logger.LogInformation("Resolved dependencies for {Count} services", _services.Count);
        }

        private bool HasCircularDependency(Dictionary<string, List<string>> graph)
        {
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();

            foreach (var node in graph.Keys)
            {
                if (HasCircularDependencyDFS(node, graph, visited, recursionStack))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasCircularDependencyDFS(string node, Dictionary<string, List<string>> graph, 
            HashSet<string> visited, HashSet<string> recursionStack)
        {
            visited.Add(node);
            recursionStack.Add(node);

            if (graph.TryGetValue(node, out var neighbors))
            {
                foreach (var neighbor in neighbors)
                {
                    if (!visited.Contains(neighbor))
                    {
                        if (HasCircularDependencyDFS(neighbor, graph, visited, recursionStack))
                        {
                            return true;
                        }
                    }
                    else if (recursionStack.Contains(neighbor))
                    {
                        return true;
                    }
                }
            }

            recursionStack.Remove(node);
            return false;
        }

        private async Task StartServicesInOrderAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting services in dependency order");

            var startOrder = GetTopologicalSort();
            
            foreach (var serviceId in startOrder)
            {
                if (_services.TryGetValue(serviceId, out var instance))
                {
                    try
                    {
                        _logger.LogInformation("Starting service {ServiceName}", instance.Service.Name);
                        
                        // Initialize service
                        if (await instance.Service.InitializeAsync())
                        {
                            // Start service
                            if (await instance.Service.StartAsync())
                            {
                                instance.State = ServiceState.Running;
                                instance.StartedAt = DateTime.UtcNow;
                                
                                // Update health in registry
                                await _serviceRegistry.UpdateServiceHealthAsync(serviceId, ServiceHealth.Healthy);
                                
                                _logger.LogInformation("Service {ServiceName} started successfully", instance.Service.Name);
                            }
                            else
                            {
                                _logger.LogError("Failed to start service {ServiceName}", instance.Service.Name);
                                instance.State = ServiceState.Failed;
                            }
                        }
                        else
                        {
                            _logger.LogError("Failed to initialize service {ServiceName}", instance.Service.Name);
                            instance.State = ServiceState.Failed;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error starting service {ServiceName}", instance.Service.Name);
                        instance.State = ServiceState.Failed;
                    }
                }
            }
        }

        private async Task StopServicesInOrderAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping services in reverse dependency order");

            var stopOrder = GetTopologicalSort().Reverse();
            
            foreach (var serviceId in stopOrder)
            {
                if (_services.TryGetValue(serviceId, out var instance) && instance.State == ServiceState.Running)
                {
                    try
                    {
                        _logger.LogInformation("Stopping service {ServiceName}", instance.Service.Name);
                        
                        if (await instance.Service.StopAsync())
                        {
                            instance.State = ServiceState.Stopped;
                            instance.StoppedAt = DateTime.UtcNow;
                            
                            // Update health in registry
                            await _serviceRegistry.UpdateServiceHealthAsync(serviceId, ServiceHealth.NotRunning);
                            
                            _logger.LogInformation("Service {ServiceName} stopped successfully", instance.Service.Name);
                        }
                        else
                        {
                            _logger.LogError("Failed to stop service {ServiceName}", instance.Service.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error stopping service {ServiceName}", instance.Service.Name);
                    }
                }
            }
        }

        private async Task UnregisterServicesAsync(CancellationToken cancellationToken)
        {
            foreach (var serviceId in _services.Keys.ToList())
            {
                await _serviceRegistry.UnregisterServiceAsync(serviceId);
            }
        }

        private IEnumerable<string> GetTopologicalSort()
        {
            var result = new List<string>();
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();

            foreach (var serviceId in _services.Keys)
            {
                if (!visited.Contains(serviceId))
                {
                    TopologicalSortDFS(serviceId, visited, recursionStack, result);
                }
            }

            return result;
        }

        private void TopologicalSortDFS(string serviceId, HashSet<string> visited, 
            HashSet<string> recursionStack, List<string> result)
        {
            visited.Add(serviceId);
            recursionStack.Add(serviceId);

            if (_services.TryGetValue(serviceId, out var instance) && instance.Dependencies != null)
            {
                foreach (var dependency in instance.Dependencies)
                {
                    if (!visited.Contains(dependency))
                    {
                        TopologicalSortDFS(dependency, visited, recursionStack, result);
                    }
                }
            }

            recursionStack.Remove(serviceId);
            result.Add(serviceId);
        }

        private async Task<T> CreateServiceProxyAsync<T>(ServiceRegistration registration, CancellationToken cancellationToken) 
            where T : class, IService
        {
            // Create a proxy for remote service communication
            await Task.CompletedTask; // Async method
            return _serviceCommunication.CreateProxy<T>(registration.ServiceId, new ProxyOptions
            {
                EnableCaching = true,
                EnableMetrics = true,
                EnableTracing = true
            });
        }
    }

    /// <summary>
    /// Service orchestrator interface
    /// </summary>
    public interface IServiceOrchestrator
    {
        bool IsRunning { get; }
        IReadOnlyDictionary<string, ServiceInstance> Services { get; }
        
        Task StartAsync(CancellationToken cancellationToken = default);
        Task StopAsync(CancellationToken cancellationToken = default);
        Task<T> GetServiceAsync<T>(string serviceId = null, CancellationToken cancellationToken = default) where T : class, IService;
        Task<IEnumerable<T>> GetServicesAsync<T>(CancellationToken cancellationToken = default) where T : class, IService;
        Task RegisterServiceAsync(IService service, ServiceRegistrationOptions options = null, CancellationToken cancellationToken = default);
        Task UnregisterServiceAsync(string serviceId, CancellationToken cancellationToken = default);
        Task<ServiceHealth> GetServiceHealthAsync(string serviceId, CancellationToken cancellationToken = default);
        Task<IDictionary<string, object>> GetServiceMetricsAsync(string serviceId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Service instance
    /// </summary>
    public class ServiceInstance
    {
        public string ServiceId { get; set; }
        public IService Service { get; set; }
        public ServiceRegistration Registration { get; set; }
        public ServiceState State { get; set; }
        public List<string> Dependencies { get; set; } = new();
        public DateTime RegisteredAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? StoppedAt { get; set; }
    }

    /// <summary>
    /// Service state
    /// </summary>
    public enum ServiceState
    {
        Registered,
        Initializing,
        Running,
        Stopping,
        Stopped,
        Failed
    }

    /// <summary>
    /// Service registration options
    /// </summary>
    public class ServiceRegistrationOptions
    {
        public string ServiceId { get; set; }
        public List<ServiceEndpoint> Endpoints { get; set; }
        public ServiceConfiguration Configuration { get; set; }
        public ServiceLoadBalancing LoadBalancing { get; set; }
        public ServiceSla Sla { get; set; }
    }
}