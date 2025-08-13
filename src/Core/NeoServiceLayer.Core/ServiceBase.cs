using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Core;

/// <summary>
/// Base class for all services in the Neo Service Layer.
/// Provides core service functionality without creating circular dependencies.
/// </summary>
public abstract class ServiceBase : IService, IDisposable
{
    private readonly ConcurrentDictionary<Type, object> _capabilities = new();
    private readonly ConcurrentDictionary<string, object> _metadata = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBase"/> class.
    /// </summary>
    protected ServiceBase(string name, string description, string version, ILogger logger)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ServiceId = Guid.NewGuid().ToString();
        Status = ServiceStatus.NotInitialized;
        CreatedAt = DateTime.UtcNow;
    }

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

    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger Logger { get; }

    /// <inheritdoc/>
    public virtual async Task<bool> InitializeAsync()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ServiceBase));
        
        try
        {
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
    }

    /// <inheritdoc/>
    public virtual async Task<bool> StartAsync()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ServiceBase));
        
        if (Status != ServiceStatus.Initialized)
        {
            Logger.LogWarning("Cannot start service {ServiceName} - not initialized", Name);
            return false;
        }

        try
        {
            Logger.LogInformation("Starting service {ServiceName}", Name);
            Status = ServiceStatus.Starting;
            
            var result = await OnStartAsync();
            
            Status = result ? ServiceStatus.Running : ServiceStatus.Failed;
            LastActivity = DateTime.UtcNow;
            
            if (result)
            {
                Logger.LogInformation("Service {ServiceName} started successfully", Name);
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
    }

    /// <inheritdoc/>
    public virtual async Task<bool> StopAsync()
    {
        if (_disposed) return true;
        
        if (Status != ServiceStatus.Running)
        {
            return true;
        }

        try
        {
            Logger.LogInformation("Stopping service {ServiceName}", Name);
            Status = ServiceStatus.Stopping;
            
            var result = await OnStopAsync();
            
            Status = result ? ServiceStatus.Stopped : ServiceStatus.Failed;
            LastActivity = DateTime.UtcNow;
            
            if (result)
            {
                Logger.LogInformation("Service {ServiceName} stopped successfully", Name);
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
    }

    /// <inheritdoc/>
    public virtual async Task<ServiceHealth> GetHealthAsync()
    {
        if (_disposed) return ServiceHealth.Unhealthy;
        
        try
        {
            LastActivity = DateTime.UtcNow;
            return await OnGetHealthAsync();
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
        return _metadata.AsReadOnly();
    }

    /// <summary>
    /// Adds a capability to the service.
    /// </summary>
    /// <typeparam name="T">The capability type.</typeparam>
    /// <param name="implementation">Optional implementation. If null, uses 'this' as implementation.</param>
    protected virtual void AddCapability<T>(T? implementation = null) where T : class
    {
        _capabilities[typeof(T)] = implementation ?? this;
    }

    /// <summary>
    /// Sets metadata for the service.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    protected virtual void SetMetadata(string key, object value)
    {
        _metadata[key] = value;
    }

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

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of the service resources.
    /// </summary>
    /// <param name="disposing">True if disposing; otherwise, false.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                if (Status == ServiceStatus.Running)
                {
                    StopAsync().GetAwaiter().GetResult();
                }
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
}