using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.ServiceFramework;

/// <summary>
/// Unified base class for all services in the Neo Service Layer.
/// Combines the best features from both Core and ServiceFramework implementations.
/// </summary>
public abstract class UnifiedServiceBase : IService, IDisposable
{
    private readonly ConcurrentDictionary<Type, object> _capabilities = new();
    private readonly ConcurrentDictionary<string, object> _metadata = new();
    private readonly ConcurrentDictionary<string, object> _metrics = new();
    private readonly List<ServiceDependency> _dependencies = new();
    private readonly SemaphoreSlim _statusLock = new(1, 1);
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnifiedServiceBase"/> class.
    /// </summary>
    protected UnifiedServiceBase(string name, string description, string version, ILogger logger)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        ServiceId = Guid.NewGuid().ToString();
        Status = ServiceStatus.NotInitialized;
        CreatedAt = DateTime.UtcNow;
        
        // Add default capabilities
        AddCapability<IService>(this);
    }

    #region Properties

    /// <inheritdoc/>
    public string ServiceId { get; }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public string Description { get; }

    /// <inheritdoc/>
    public string Version { get; }

    /// <inheritdoc/>
    public ServiceStatus Status { get; protected set; }

    /// <inheritdoc/>
    public DateTime CreatedAt { get; }

    /// <inheritdoc/>
    public DateTime? LastActivity { get; protected set; }

    /// <inheritdoc/>
    public IServiceProvider? ServiceProvider { get; set; }

    /// <inheritdoc/>
    public bool IsRunning => Status == ServiceStatus.Running;

    /// <inheritdoc/>
    public virtual IEnumerable<object> Dependencies => _dependencies;

    /// <inheritdoc/>
    public virtual IEnumerable<Type> Capabilities => _capabilities.Keys;

    /// <inheritdoc/>
    public virtual IDictionary<string, string> Metadata => _metadata.ToDictionary(
        kvp => kvp.Key,
        kvp => kvp.Value?.ToString() ?? string.Empty);

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger Logger { get; }

    #endregion

    #region Public Methods

    /// <inheritdoc/>
    public virtual async Task<bool> InitializeAsync()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(UnifiedServiceBase));

        await _statusLock.WaitAsync();
        try
        {
            if (Status != ServiceStatus.NotInitialized)
            {
                Logger.LogWarning("Service {ServiceName} is already initialized or in an invalid state", Name);
                return false;
            }

            Logger.LogInformation("Initializing service {ServiceName} (ID: {ServiceId})", Name, ServiceId);
            Status = ServiceStatus.Initializing;

            var result = await OnInitializeAsync();
            
            Status = result ? ServiceStatus.Initialized : ServiceStatus.Failed;
            LastActivity = DateTime.UtcNow;

            if (result)
            {
                Logger.LogInformation("Service {ServiceName} initialized successfully", Name);
            }
            else
            {
                Logger.LogError("Service {ServiceName} failed to initialize", Name);
            }

            return result;
        }
        catch (Exception ex)
        {
            Status = ServiceStatus.Failed;
            Logger.LogError(ex, "Error initializing service {ServiceName}", Name);
            return false;
        }
        finally
        {
            _statusLock.Release();
        }
    }

    /// <inheritdoc/>
    public virtual async Task<bool> StartAsync()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(UnifiedServiceBase));

        await _statusLock.WaitAsync();
        try
        {
            if (Status != ServiceStatus.Initialized)
            {
                Logger.LogWarning("Cannot start service {ServiceName} - not initialized (current status: {Status})", Name, Status);
                return false;
            }

            Logger.LogInformation("Starting service {ServiceName}", Name);
            Status = ServiceStatus.Starting;

            var result = await OnStartAsync();
            
            Status = result ? ServiceStatus.Running : ServiceStatus.Failed;
            LastActivity = DateTime.UtcNow;

            if (result)
            {
                Logger.LogInformation("Service {ServiceName} started successfully", Name);
                UpdateMetric("start_time", DateTime.UtcNow);
            }
            else
            {
                Logger.LogError("Service {ServiceName} failed to start", Name);
            }

            return result;
        }
        catch (Exception ex)
        {
            Status = ServiceStatus.Failed;
            Logger.LogError(ex, "Error starting service {ServiceName}", Name);
            return false;
        }
        finally
        {
            _statusLock.Release();
        }
    }

    /// <inheritdoc/>
    public virtual async Task<bool> StopAsync()
    {
        if (_disposed) return true;

        await _statusLock.WaitAsync();
        try
        {
            if (Status != ServiceStatus.Running)
            {
                Logger.LogWarning("Service {ServiceName} is not running (current status: {Status})", Name, Status);
                return true;
            }

            Logger.LogInformation("Stopping service {ServiceName}", Name);
            Status = ServiceStatus.Stopping;

            var result = await OnStopAsync();
            
            Status = result ? ServiceStatus.Stopped : ServiceStatus.Failed;
            LastActivity = DateTime.UtcNow;

            if (result)
            {
                Logger.LogInformation("Service {ServiceName} stopped successfully", Name);
                UpdateMetric("stop_time", DateTime.UtcNow);
            }
            else
            {
                Logger.LogError("Service {ServiceName} failed to stop cleanly", Name);
            }

            return result;
        }
        catch (Exception ex)
        {
            Status = ServiceStatus.Failed;
            Logger.LogError(ex, "Error stopping service {ServiceName}", Name);
            return false;
        }
        finally
        {
            _statusLock.Release();
        }
    }

    /// <inheritdoc/>
    public virtual async Task<ServiceHealth> GetHealthAsync()
    {
        if (_disposed) return ServiceHealth.Unhealthy;

        try
        {
            LastActivity = DateTime.UtcNow;
            var health = await OnGetHealthAsync();
            UpdateMetric("last_health_check", DateTime.UtcNow);
            UpdateMetric("health_status", health.ToString());
            return health;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking health for service {ServiceName}", Name);
            return ServiceHealth.Unhealthy;
        }
    }

    /// <inheritdoc/>
    public virtual bool HasCapability<T>() where T : class
    {
        return _capabilities.ContainsKey(typeof(T));
    }

    /// <inheritdoc/>
    public virtual T? GetCapability<T>() where T : class
    {
        return _capabilities.TryGetValue(typeof(T), out var capability)
            ? capability as T
            : null;
    }

    /// <inheritdoc/>
    public virtual IReadOnlyDictionary<string, object> GetMetadata()
    {
        return new Dictionary<string, object>(_metadata);
    }

    /// <inheritdoc/>
    public virtual async Task<IDictionary<string, object>> GetMetricsAsync()
    {
        var metrics = new Dictionary<string, object>
        {
            ["service_id"] = ServiceId,
            ["name"] = Name,
            ["version"] = Version,
            ["status"] = Status.ToString(),
            ["is_running"] = IsRunning,
            ["created_at"] = CreatedAt,
            ["last_activity"] = LastActivity,
            ["uptime_seconds"] = LastActivity.HasValue ? (DateTime.UtcNow - CreatedAt).TotalSeconds : 0,
            ["capability_count"] = _capabilities.Count,
            ["dependency_count"] = _dependencies.Count
        };

        try
        {
            // Add current metrics
            foreach (var metric in _metrics)
            {
                metrics[metric.Key] = metric.Value;
            }

            // Get custom metrics from implementation
            var customMetrics = await OnGetMetricsAsync();
            foreach (var metric in customMetrics)
            {
                metrics[metric.Key] = metric.Value;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting custom metrics for service {ServiceName}", Name);
            metrics["metrics_error"] = ex.Message;
        }

        return metrics;
    }

    /// <inheritdoc/>
    public virtual async Task<bool> ValidateDependenciesAsync(IEnumerable<IService> availableServices)
    {
        try
        {
            var services = availableServices.ToList();
            var missingDependencies = new List<ServiceDependency>();

            foreach (var dependency in _dependencies)
            {
                var service = services.FirstOrDefault(s => s.Name == dependency.ServiceName);
                
                if (service == null)
                {
                    if (dependency.IsRequired)
                    {
                        Logger.LogError("Required dependency {DependencyName} not found for service {ServiceName}", 
                            dependency.ServiceName, Name);
                        missingDependencies.Add(dependency);
                    }
                    else
                    {
                        Logger.LogWarning("Optional dependency {DependencyName} not found for service {ServiceName}", 
                            dependency.ServiceName, Name);
                    }
                    continue;
                }

                if (!dependency.Validate(service))
                {
                    if (dependency.IsRequired)
                    {
                        Logger.LogError("Required dependency {DependencyName} does not satisfy version requirements for service {ServiceName}", 
                            dependency.ServiceName, Name);
                        missingDependencies.Add(dependency);
                    }
                    else
                    {
                        Logger.LogWarning("Optional dependency {DependencyName} does not satisfy version requirements for service {ServiceName}", 
                            dependency.ServiceName, Name);
                    }
                }
            }

            if (missingDependencies.Any())
            {
                Logger.LogError("Service {ServiceName} is missing {MissingDependencyCount} required dependencies", 
                    Name, missingDependencies.Count);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error validating dependencies for service {ServiceName}", Name);
            return false;
        }
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Adds a service dependency.
    /// </summary>
    protected void AddDependency(ServiceDependency dependency)
    {
        _dependencies.Add(dependency);
    }

    /// <summary>
    /// Adds a required service dependency.
    /// </summary>
    protected void AddRequiredDependency(string serviceName, string? minimumVersion = null, string? maximumVersion = null)
    {
        _dependencies.Add(ServiceDependency.Required(serviceName, minimumVersion, maximumVersion));
    }

    /// <summary>
    /// Adds a required service dependency with a specific type.
    /// </summary>
    protected void AddRequiredDependency<T>(string serviceName, string? minimumVersion = null, string? maximumVersion = null) 
        where T : IService
    {
        _dependencies.Add(ServiceDependency.Required<T>(serviceName, minimumVersion, maximumVersion));
    }

    /// <summary>
    /// Adds an optional service dependency.
    /// </summary>
    protected void AddOptionalDependency(string serviceName, string? minimumVersion = null, string? maximumVersion = null)
    {
        _dependencies.Add(ServiceDependency.Optional(serviceName, minimumVersion, maximumVersion));
    }

    /// <summary>
    /// Adds an optional service dependency with a specific type.
    /// </summary>
    protected void AddOptionalDependency<T>(string serviceName, string? minimumVersion = null, string? maximumVersion = null) 
        where T : IService
    {
        _dependencies.Add(ServiceDependency.Optional<T>(serviceName, minimumVersion, maximumVersion));
    }

    /// <summary>
    /// Adds a capability to the service.
    /// </summary>
    protected virtual void AddCapability<T>(T? implementation = null) where T : class
    {
        _capabilities[typeof(T)] = implementation ?? (object)this;
    }

    /// <summary>
    /// Sets metadata for the service.
    /// </summary>
    protected virtual void SetMetadata(string key, object value)
    {
        _metadata[key] = value;
    }

    /// <summary>
    /// Updates a service metric.
    /// </summary>
    protected virtual void UpdateMetric(string key, object value)
    {
        _metrics[key] = value;
    }

    #endregion

    #region Abstract Methods

    /// <summary>
    /// Called when the service should initialize.
    /// Override this method to implement service-specific initialization logic.
    /// </summary>
    /// <returns>True if initialization was successful; otherwise, false.</returns>
    protected abstract Task<bool> OnInitializeAsync();

    /// <summary>
    /// Called when the service should start.
    /// Override this method to implement service-specific startup logic.
    /// </summary>
    /// <returns>True if startup was successful; otherwise, false.</returns>
    protected abstract Task<bool> OnStartAsync();

    /// <summary>
    /// Called when the service should stop.
    /// Override this method to implement service-specific shutdown logic.
    /// </summary>
    /// <returns>True if shutdown was successful; otherwise, false.</returns>
    protected abstract Task<bool> OnStopAsync();

    /// <summary>
    /// Called when the service health should be checked.
    /// Override this method to implement service-specific health checking logic.
    /// </summary>
    /// <returns>The current health status of the service.</returns>
    protected abstract Task<ServiceHealth> OnGetHealthAsync();

    /// <summary>
    /// Called when custom metrics should be retrieved.
    /// Override this method to provide service-specific metrics.
    /// </summary>
    /// <returns>A dictionary of custom metrics for the service.</returns>
    protected virtual Task<IDictionary<string, object>> OnGetMetricsAsync()
    {
        return Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object>());
    }

    #endregion

    #region IDisposable Implementation

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of the service resources.
    /// </summary>
    protected virtual async void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                if (Status == ServiceStatus.Running)
                {
                    await StopAsync();
                }

                _dependencies.Clear();
                _capabilities.Clear();
                _metadata.Clear();
                _metrics.Clear();
                _statusLock?.Dispose();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during service disposal for {ServiceName}", Name);
            }
            finally
            {
                Status = ServiceStatus.Disposed;
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Finalizer for UnifiedServiceBase.
    /// </summary>
    ~UnifiedServiceBase()
    {
        Dispose(false);
    }

    #endregion
}