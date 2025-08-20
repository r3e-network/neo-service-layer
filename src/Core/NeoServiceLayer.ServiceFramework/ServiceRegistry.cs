using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.ServiceFramework;

/// <summary>
/// Implementation of the service registry.
/// </summary>
public class ServiceRegistry : IServiceRegistry
{
    private readonly Dictionary<string, IService> _services = new();
    private readonly Dictionary<string, ServiceHealth> _serviceHealthCache = new();
    private readonly ILogger<ServiceRegistry> _logger;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceRegistry"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public ServiceRegistry(ILogger<ServiceRegistry> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public event EventHandler<ServiceEventArgs>? ServiceRegistered;

    /// <inheritdoc/>
    public event EventHandler<ServiceEventArgs>? ServiceUnregistered;

    /// <inheritdoc/>
    public event EventHandler<ServiceHealthChangedEventArgs>? ServiceHealthChanged;

    /// <inheritdoc/>
    public Task RegisterServiceAsync(IService service)
    {
        ArgumentNullException.ThrowIfNull(service);

        lock (_lock)
        {
            if (_services.ContainsKey(service.Name))
            {
                _logger.LogWarning("Service with name {ServiceName} is already registered.", service.Name);
                return Task.CompletedTask;
            }

            _services[service.Name] = service;
            _serviceHealthCache[service.Name] = ServiceHealth.NotRunning;
            _logger.LogInformation("Service {ServiceName} registered successfully.", service.Name);

            // Subscribe to service health changes
            service.GetHealthAsync().ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    UpdateServiceHealth(service, t.Result);
                }
            });
        }

        // Raise event outside of lock
        OnServiceRegistered(new ServiceEventArgs(service));

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void RegisterService(IService service)
    {
        ArgumentNullException.ThrowIfNull(service);

        lock (_lock)
        {
            if (_services.ContainsKey(service.Name))
            {
                _logger.LogWarning("Service with name {ServiceName} is already registered.", service.Name);
                return;
            }

            _services[service.Name] = service;
            _serviceHealthCache[service.Name] = ServiceHealth.NotRunning;
            _logger.LogInformation("Service {ServiceName} registered successfully.", service.Name);

            // Subscribe to service health changes
            service.GetHealthAsync().ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    UpdateServiceHealth(service, t.Result);
                }
            });
        }

        // Raise event outside of lock
        OnServiceRegistered(new ServiceEventArgs(service));
    }

    /// <inheritdoc/>
    public bool UnregisterService(string serviceName)
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
            _logger.LogInformation("Service {ServiceName} unregistered successfully.", serviceName);
        }

        // Raise event outside of lock
        if (service != null)
        {
            OnServiceUnregistered(new ServiceEventArgs(service));
        }

        return true;
    }

    /// <inheritdoc/>
    public IService? GetService(string serviceName)
    {
        ArgumentException.ThrowIfNullOrEmpty(serviceName);

        lock (_lock)
        {
            if (!_services.TryGetValue(serviceName, out var service))
            {
                _logger.LogWarning("Service with name {ServiceName} is not registered.", serviceName);
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
            _logger.LogWarning("Service with name {ServiceName} is not of type {ServiceType}.", serviceName, typeof(T).Name);
            return null;
        }

        return typedService;
    }

    /// <inheritdoc/>
    public IEnumerable<IService> GetAllServices()
    {
        lock (_lock)
        {
            return _services.Values.ToList();
        }
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

    /// <inheritdoc/>
    public async Task<bool> InitializeAllServicesAsync()
    {
        _logger.LogInformation("Initializing all services...");
        var services = GetAllServices().ToList();
        var results = new List<bool>();

        foreach (var service in services)
        {
            results.Add(await service.InitializeAsync());
        }

        var allSucceeded = results.All(r => r);
        if (allSucceeded)
        {
            _logger.LogInformation("All services initialized successfully.");
        }
        else
        {
            _logger.LogWarning("Some services failed to initialize.");
        }

        return allSucceeded;
    }

    /// <inheritdoc/>
    public async Task<bool> InitializeServicesByPatternAsync(string pattern)
    {
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        _logger.LogInformation("Initializing services matching pattern {Pattern}...", pattern);
        var services = FindServicesByNamePattern(pattern).ToList();

        if (services.Count == 0)
        {
            _logger.LogWarning("No services found matching pattern {Pattern}.", pattern);
            return true;
        }

        var results = new List<bool>();

        foreach (var service in services)
        {
            results.Add(await service.InitializeAsync());
        }

        var allSucceeded = results.All(r => r);
        if (allSucceeded)
        {
            _logger.LogInformation("All services matching pattern {Pattern} initialized successfully.", pattern);
        }
        else
        {
            _logger.LogWarning("Some services matching pattern {Pattern} failed to initialize.", pattern);
        }

        return allSucceeded;
    }

    /// <inheritdoc/>
    public async Task<bool> StartAllServicesAsync()
    {
        _logger.LogInformation("Starting all services...");
        var services = GetAllServices().ToList();
        var results = new List<bool>();

        foreach (var service in services)
        {
            results.Add(await service.StartAsync());
        }

        var allSucceeded = results.All(r => r);
        if (allSucceeded)
        {
            _logger.LogInformation("All services started successfully.");
        }
        else
        {
            _logger.LogWarning("Some services failed to start.");
        }

        return allSucceeded;
    }

    /// <inheritdoc/>
    public async Task<bool> StartServicesByPatternAsync(string pattern)
    {
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        _logger.LogInformation("Starting services matching pattern {Pattern}...", pattern);
        var services = FindServicesByNamePattern(pattern).ToList();

        if (services.Count == 0)
        {
            _logger.LogWarning("No services found matching pattern {Pattern}.", pattern);
            return true;
        }

        var results = new List<bool>();

        foreach (var service in services)
        {
            results.Add(await service.StartAsync());
        }

        var allSucceeded = results.All(r => r);
        if (allSucceeded)
        {
            _logger.LogInformation("All services matching pattern {Pattern} started successfully.", pattern);
        }
        else
        {
            _logger.LogWarning("Some services matching pattern {Pattern} failed to start.", pattern);
        }

        return allSucceeded;
    }

    /// <inheritdoc/>
    public async Task<bool> StopAllServicesAsync()
    {
        _logger.LogInformation("Stopping all services...");
        var services = GetAllServices().ToList();
        var results = new List<bool>();

        foreach (var service in services)
        {
            results.Add(await service.StopAsync());
        }

        var allSucceeded = results.All(r => r);
        if (allSucceeded)
        {
            _logger.LogInformation("All services stopped successfully.");
        }
        else
        {
            _logger.LogWarning("Some services failed to stop.");
        }

        return allSucceeded;
    }

    /// <inheritdoc/>
    public async Task<bool> StopServicesByPatternAsync(string pattern)
    {
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        _logger.LogInformation("Stopping services matching pattern {Pattern}...", pattern);
        var services = FindServicesByNamePattern(pattern).ToList();

        if (services.Count == 0)
        {
            _logger.LogWarning("No services found matching pattern {Pattern}.", pattern);
            return true;
        }

        var results = new List<bool>();

        foreach (var service in services)
        {
            results.Add(await service.StopAsync());
        }

        var allSucceeded = results.All(r => r);
        if (allSucceeded)
        {
            _logger.LogInformation("All services matching pattern {Pattern} stopped successfully.", pattern);
        }
        else
        {
            _logger.LogWarning("Some services matching pattern {Pattern} failed to stop.", pattern);
        }

        return allSucceeded;
    }

    /// <inheritdoc/>
    public async Task<IDictionary<string, ServiceHealth>> GetAllServicesHealthAsync()
    {
        var services = GetAllServices().ToList();
        var result = new Dictionary<string, ServiceHealth>();

        foreach (var service in services)
        {
            var health = await service.GetHealthAsync();
            result[service.Name] = health;
            UpdateServiceHealth(service, health);
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<IDictionary<string, ServiceHealth>> GetServicesHealthByPatternAsync(string pattern)
    {
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        var services = FindServicesByNamePattern(pattern).ToList();
        var result = new Dictionary<string, ServiceHealth>();

        foreach (var service in services)
        {
            var health = await service.GetHealthAsync();
            result[service.Name] = health;
            UpdateServiceHealth(service, health);
        }

        return result;
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
            OnServiceHealthChanged(new ServiceHealthChangedEventArgs(service, previousHealth, health));
        }
    }

    private void OnServiceRegistered(ServiceEventArgs e)
    {
        ServiceRegistered?.Invoke(this, e);
    }

    private void OnServiceUnregistered(ServiceEventArgs e)
    {
        ServiceUnregistered?.Invoke(this, e);
    }

    private void OnServiceHealthChanged(ServiceHealthChangedEventArgs e)
    {
        ServiceHealthChanged?.Invoke(this, e);
    }
}
