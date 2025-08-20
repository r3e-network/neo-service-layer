using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure.Persistence;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.ServiceFramework;

/// <summary>
/// Enhanced service registry with persistent storage support.
/// Provides professional service management with state persistence and advanced features.
/// </summary>
public class EnhancedServiceRegistry : IServiceRegistry
{
    private readonly Dictionary<string, IService> _services = new();
    private readonly Dictionary<string, ServiceHealth> _serviceHealthCache = new();
    private readonly Dictionary<string, ServiceRegistrationInfo> _serviceRegistrations = new();
    private readonly IPersistentStorageProvider _storageProvider;
    private readonly ILogger<EnhancedServiceRegistry> _logger;
    private readonly object _lock = new();
    private readonly Timer _healthCheckTimer;
    private readonly Timer _persistenceTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnhancedServiceRegistry"/> class.
    /// </summary>
    /// <param name="storageProvider">The persistent storage provider.</param>
    /// <param name="logger">The logger.</param>
    public EnhancedServiceRegistry(IPersistentStorageProvider storageProvider, ILogger<EnhancedServiceRegistry> logger)
    {
        _storageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize periodic health checks
        _healthCheckTimer = new Timer(PerformHealthChecks, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

        // Initialize periodic persistence
        _persistenceTimer = new Timer(PersistRegistryState, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    /// <inheritdoc/>
    public event EventHandler<ServiceEventArgs>? ServiceRegistered;

    /// <inheritdoc/>
    public event EventHandler<ServiceEventArgs>? ServiceUnregistered;

    /// <inheritdoc/>
    public event EventHandler<ServiceHealthChangedEventArgs>? ServiceHealthChanged;

    /// <summary>
    /// Initializes the enhanced service registry.
    /// </summary>
    /// <returns>True if initialization was successful, false otherwise.</returns>
    public async Task<bool> InitializeAsync()
    {
        try
        {
            if (!await _storageProvider.InitializeAsync())
            {
                _logger.LogError("Failed to initialize storage provider for service registry");
                return false;
            }

            await LoadRegistryStateAsync();
            _logger.LogInformation("Enhanced service registry initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize enhanced service registry");
            return false;
        }
    }



    /// <inheritdoc/>
    public async Task RegisterServiceAsync(IService service)
    {
        ArgumentNullException.ThrowIfNull(service);

        var registrationInfo = new ServiceRegistrationInfo
        {
            ServiceName = service.Name,
            ServiceType = service.GetType().FullName ?? service.GetType().Name,
            Version = service.Version,
            Description = service.Description,
            RegisteredAt = DateTime.UtcNow,
            Capabilities = service.Capabilities.Select(c => c.FullName ?? c.Name).ToList(),
            Dependencies = service.Dependencies.Cast<ServiceDependency>().ToList(),
            Metadata = service.Metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };

        lock (_lock)
        {
            if (_services.ContainsKey(service.Name))
            {
                throw new InvalidOperationException($"Service '{service.Name}' is already registered");
            }

            _services[service.Name] = service;
            _serviceRegistrations[service.Name] = registrationInfo;
        }

        // Persist registration
        await PersistServiceRegistrationAsync(service.Name);

        _logger.LogInformation("Service '{ServiceName}' registered successfully", service.Name);

        // Raise event outside of lock
        OnServiceRegistered(new ServiceEventArgs(service));
    }

    /// <inheritdoc/>
    public void RegisterService(IService service)
    {
        RegisterServiceAsync(service).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public async Task<bool> UnregisterServiceAsync(string serviceName)
    {
        ArgumentException.ThrowIfNullOrEmpty(serviceName);

        IService? service;
        lock (_lock)
        {
            if (!_services.TryGetValue(serviceName, out service))
            {
                _logger.LogWarning("Service with name {ServiceName} is not registered.", serviceName);
                return false;
            }

            _services.Remove(serviceName);
            _serviceHealthCache.Remove(serviceName);
            _serviceRegistrations.Remove(serviceName);
            _logger.LogInformation("Service {ServiceName} unregistered successfully.", serviceName);
        }

        // Remove from persistent storage
        await _storageProvider.DeleteAsync($"service_registration:{serviceName}");

        // Raise event outside of lock
        if (service != null)
        {
            OnServiceUnregistered(new ServiceEventArgs(service));
        }

        return true;
    }

    /// <inheritdoc/>
    public bool UnregisterService(string serviceName)
    {
        return UnregisterServiceAsync(serviceName).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public IService? GetService(string serviceName)
    {
        ArgumentException.ThrowIfNullOrEmpty(serviceName);

        lock (_lock)
        {
            if (!_services.TryGetValue(serviceName, out var service))
            {
                _logger.LogDebug("Service with name {ServiceName} is not registered.", serviceName);
                return null;
            }

            return service;
        }
    }

    /// <inheritdoc/>
    public T? GetService<T>(string serviceName) where T : class, IService
    {
        var service = GetService(serviceName);
        if (service is not T typedService)
        {
            _logger.LogDebug("Service with name {ServiceName} is not of type {ServiceType}.", serviceName, typeof(T).Name);
            return null;
        }

        return typedService;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<IService>> GetAllServicesAsync()
    {
        lock (_lock)
        {
            return Task.FromResult<IEnumerable<IService>>(_services.Values.ToList());
        }
    }

    /// <inheritdoc/>
    public IEnumerable<IService> GetAllServices()
    {
        return GetAllServicesAsync().GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public IEnumerable<T> GetAllServices<T>() where T : class, IService
    {
        lock (_lock)
        {
            return _services.Values.OfType<T>().ToList();
        }
    }

    /// <inheritdoc/>
    public IEnumerable<IService> FindServicesByNamePattern(string pattern)
    {
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        try
        {
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            lock (_lock)
            {
                return _services.Values.Where(s => regex.IsMatch(s.Name)).ToList();
            }
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid regular expression pattern: {Pattern}", pattern);
            return Enumerable.Empty<IService>();
        }
    }

    /// <inheritdoc/>
    public IEnumerable<T> FindServicesByCapability<T>() where T : class
    {
        lock (_lock)
        {
            return _services.Values.OfType<T>().ToList();
        }
    }

    /// <inheritdoc/>
    public IEnumerable<IBlockchainService> FindServicesByBlockchainType(BlockchainType blockchainType)
    {
        lock (_lock)
        {
            return _services.Values.OfType<IBlockchainService>()
                .Where(s => s.SupportsBlockchain(blockchainType))
                .ToList();
        }
    }

    /// <inheritdoc/>
    public bool ServiceExists(string serviceName)
    {
        ArgumentException.ThrowIfNullOrEmpty(serviceName);

        lock (_lock)
        {
            return _services.ContainsKey(serviceName);
        }
    }

    /// <inheritdoc/>
    public int GetServiceCount()
    {
        lock (_lock)
        {
            return _services.Count;
        }
    }

    /// <summary>
    /// Gets detailed registration information for a service.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <returns>The service registration information or null if not found.</returns>
    public ServiceRegistrationInfo? GetServiceRegistrationInfo(string serviceName)
    {
        ArgumentException.ThrowIfNullOrEmpty(serviceName);

        lock (_lock)
        {
            return _serviceRegistrations.TryGetValue(serviceName, out var info) ? info : null;
        }
    }

    /// <summary>
    /// Gets all service registration information.
    /// </summary>
    /// <returns>A dictionary of service registration information.</returns>
    public Dictionary<string, ServiceRegistrationInfo> GetAllServiceRegistrationInfo()
    {
        lock (_lock)
        {
            return new Dictionary<string, ServiceRegistrationInfo>(_serviceRegistrations);
        }
    }

    private async Task LoadRegistryStateAsync()
    {
        try
        {
            var keys = await _storageProvider.ListKeysAsync("service_registration:");
            foreach (var key in keys)
            {
                var data = await _storageProvider.RetrieveAsync(key);
                if (data != null)
                {
                    var json = System.Text.Encoding.UTF8.GetString(data);
                    var registrationInfo = JsonSerializer.Deserialize<ServiceRegistrationInfo>(json);
                    if (registrationInfo != null)
                    {
                        _serviceRegistrations[registrationInfo.ServiceName] = registrationInfo;
                    }
                }
            }

            _logger.LogInformation("Loaded {Count} service registrations from persistent storage", _serviceRegistrations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load registry state from persistent storage");
        }
    }

    private async Task PersistServiceRegistrationAsync(string serviceName)
    {
        try
        {
            if (_serviceRegistrations.TryGetValue(serviceName, out var registrationInfo))
            {
                var json = JsonSerializer.Serialize(registrationInfo);
                var data = System.Text.Encoding.UTF8.GetBytes(json);
                await _storageProvider.StoreAsync($"service_registration:{serviceName}", data);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist service registration for {ServiceName}", serviceName);
        }
    }

    private async void PersistRegistryState(object? state)
    {
        try
        {
            var registryState = new
            {
                LastPersisted = DateTime.UtcNow,
                ServiceCount = _services.Count,
                HealthCheckCount = _serviceHealthCache.Count,
                Registrations = _serviceRegistrations
            };

            var json = JsonSerializer.Serialize(registryState);
            var data = System.Text.Encoding.UTF8.GetBytes(json);
            await _storageProvider.StoreAsync("registry_state", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist registry state");
        }
    }

    private async void PerformHealthChecks(object? state)
    {
        try
        {
            var services = GetAllServices().ToList();
            foreach (var service in services)
            {
                try
                {
                    var health = await service.GetHealthAsync();
                    UpdateServiceHealth(service, health);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Health check failed for service {ServiceName}", service.Name);
                    UpdateServiceHealth(service, ServiceHealth.Unhealthy);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform health checks");
        }
    }

    private void UpdateServiceHealth(IService service, ServiceHealth health)
    {
        ServiceHealth previousHealth;
        bool healthChanged = false;

        lock (_lock)
        {
            if (_serviceHealthCache.TryGetValue(service.Name, out previousHealth))
            {
                if (previousHealth != health)
                {
                    _serviceHealthCache[service.Name] = health;
                    healthChanged = true;
                }
            }
            else
            {
                _serviceHealthCache[service.Name] = health;
                previousHealth = ServiceHealth.NotRunning;
                healthChanged = true;
            }
        }

        if (healthChanged)
        {
            _logger.LogInformation("Service {ServiceName} health changed from {PreviousHealth} to {CurrentHealth}",
                service.Name, previousHealth, health);
            OnServiceHealthChanged(new ServiceHealthChangedEventArgs(service, previousHealth, health));
        }
    }

    // Implement remaining IServiceRegistry methods by delegating to existing ServiceRegistry logic
    public async Task<bool> InitializeAllServicesAsync()
    {
        var services = GetAllServices().ToList();
        foreach (var service in services)
        {
            if (!await service.InitializeAsync())
            {
                return false;
            }
        }
        return true;
    }

    public async Task<bool> InitializeServicesByPatternAsync(string pattern)
    {
        var services = FindServicesByNamePattern(pattern);
        foreach (var service in services)
        {
            if (!await service.InitializeAsync())
            {
                return false;
            }
        }
        return true;
    }

    public async Task<bool> StartAllServicesAsync()
    {
        var services = GetAllServices().ToList();
        foreach (var service in services)
        {
            if (!await service.StartAsync())
            {
                return false;
            }
        }
        return true;
    }

    public async Task<bool> StartServicesByPatternAsync(string pattern)
    {
        var services = FindServicesByNamePattern(pattern);
        foreach (var service in services)
        {
            if (!await service.StartAsync())
            {
                return false;
            }
        }
        return true;
    }

    public async Task<bool> StopAllServicesAsync()
    {
        var services = GetAllServices().ToList();
        foreach (var service in services)
        {
            if (!await service.StopAsync())
            {
                return false;
            }
        }
        return true;
    }

    public async Task<bool> StopServicesByPatternAsync(string pattern)
    {
        var services = FindServicesByNamePattern(pattern);
        foreach (var service in services)
        {
            if (!await service.StopAsync())
            {
                return false;
            }
        }
        return true;
    }

    public async Task<IDictionary<string, ServiceHealth>> GetAllServicesHealthAsync()
    {
        var result = new Dictionary<string, ServiceHealth>();
        var services = GetAllServices().ToList();
        foreach (var service in services)
        {
            result[service.Name] = await service.GetHealthAsync();
        }
        return result;
    }

    public async Task<IDictionary<string, ServiceHealth>> GetServicesHealthByPatternAsync(string pattern)
    {
        var result = new Dictionary<string, ServiceHealth>();
        var services = FindServicesByNamePattern(pattern);
        foreach (var service in services)
        {
            result[service.Name] = await service.GetHealthAsync();
        }
        return result;
    }

    private void OnServiceRegistered(ServiceEventArgs e) => ServiceRegistered?.Invoke(this, e);
    private void OnServiceUnregistered(ServiceEventArgs e) => ServiceUnregistered?.Invoke(this, e);
    private void OnServiceHealthChanged(ServiceHealthChangedEventArgs e) => ServiceHealthChanged?.Invoke(this, e);

    public void Dispose()
    {
        _healthCheckTimer?.Dispose();
        _persistenceTimer?.Dispose();
        _storageProvider?.Dispose();
    }
}

/// <summary>
/// Service registration information for persistent storage.
/// </summary>
public class ServiceRegistrationInfo
{
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public List<string> Capabilities { get; set; } = new();
    public List<ServiceDependency> Dependencies { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
}
