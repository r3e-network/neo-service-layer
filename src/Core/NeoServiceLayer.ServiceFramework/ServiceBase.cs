using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.ServiceFramework;

/// <summary>
/// Base class for all services in the Neo Service Layer.
/// </summary>
public abstract class ServiceBase : IService, IDisposable
{
    protected readonly ILogger Logger;
    private bool _isRunning;
    private readonly List<ServiceDependency> _dependencies = new();
    private readonly List<Type> _capabilities = new();
    private readonly Dictionary<string, string> _metadata = new();
    private readonly Dictionary<string, object> _metrics = new();
    private DateTime _startTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBase"/> class.
    /// </summary>
    /// <param name="name">The name of the service.</param>
    /// <param name="description">The description of the service.</param>
    /// <param name="version">The version of the service.</param>
    /// <param name="logger">The logger.</param>
    protected ServiceBase(string name, string description, string version, ILogger logger)
    {
        Name = name;
        Description = description;
        Version = version;
        Logger = logger;
        _isRunning = false;

        // Add default capabilities
        AddCapability<IService>();
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public string Description { get; }

    /// <inheritdoc/>
    public string Version { get; }

    /// <inheritdoc/>
    public bool IsRunning => _isRunning;

    /// <inheritdoc/>
    public IEnumerable<object> Dependencies => _dependencies;

    /// <inheritdoc/>
    public IEnumerable<Type> Capabilities => _capabilities;

    /// <inheritdoc/>
    public IDictionary<string, string> Metadata => _metadata;

    /// <summary>
    /// Gets the time when the service was started.
    /// </summary>
    public DateTime StartTime => _startTime;

    /// <summary>
    /// Gets the list of capabilities this service provides.
    /// </summary>
    public IEnumerable<string> GetCapabilities()
    {
        return _capabilities.Select(c => c.Name);
    }

    /// <summary>
    /// Gets the list of operations this service supports.
    /// </summary>
    public virtual IEnumerable<string> GetSupportedOperations()
    {
        return new[] { "initialize", "start", "stop", "health", "metrics" };
    }

    /// <summary>
    /// Adds a service dependency.
    /// </summary>
    /// <param name="dependency">The service dependency.</param>
    protected void AddDependency(ServiceDependency dependency)
    {
        _dependencies.Add(dependency);
    }

    /// <summary>
    /// Adds a required service dependency.
    /// </summary>
    /// <param name="serviceName">The name of the required service.</param>
    /// <param name="minimumVersion">The minimum version of the required service.</param>
    /// <param name="maximumVersion">The maximum version of the required service.</param>
    protected void AddRequiredDependency(string serviceName, string? minimumVersion = null, string? maximumVersion = null)
    {
        _dependencies.Add(ServiceDependency.Required(serviceName, minimumVersion, maximumVersion));
    }

    /// <summary>
    /// Adds a required service dependency with a specific type.
    /// </summary>
    /// <typeparam name="T">The type of the required service.</typeparam>
    /// <param name="serviceName">The name of the required service.</param>
    /// <param name="minimumVersion">The minimum version of the required service.</param>
    /// <param name="maximumVersion">The maximum version of the required service.</param>
    protected void AddRequiredDependency<T>(string serviceName, string? minimumVersion = null, string? maximumVersion = null) where T : IService
    {
        _dependencies.Add(ServiceDependency.Required<T>(serviceName, minimumVersion, maximumVersion));
    }

    /// <summary>
    /// Adds an optional service dependency.
    /// </summary>
    /// <param name="serviceName">The name of the required service.</param>
    /// <param name="minimumVersion">The minimum version of the required service.</param>
    /// <param name="maximumVersion">The maximum version of the required service.</param>
    protected void AddOptionalDependency(string serviceName, string? minimumVersion = null, string? maximumVersion = null)
    {
        _dependencies.Add(ServiceDependency.Optional(serviceName, minimumVersion, maximumVersion));
    }

    /// <summary>
    /// Adds an optional service dependency with a specific type.
    /// </summary>
    /// <typeparam name="T">The type of the required service.</typeparam>
    /// <param name="serviceName">The name of the required service.</param>
    /// <param name="minimumVersion">The minimum version of the required service.</param>
    /// <param name="maximumVersion">The maximum version of the required service.</param>
    protected void AddOptionalDependency<T>(string serviceName, string? minimumVersion = null, string? maximumVersion = null) where T : IService
    {
        _dependencies.Add(ServiceDependency.Optional<T>(serviceName, minimumVersion, maximumVersion));
    }

    /// <summary>
    /// Adds a service capability.
    /// </summary>
    /// <typeparam name="T">The capability type.</typeparam>
    protected void AddCapability<T>()
    {
        if (!_capabilities.Contains(typeof(T)))
        {
            _capabilities.Add(typeof(T));
        }
    }

    /// <summary>
    /// Adds a service capability.
    /// </summary>
    /// <param name="capabilityType">The capability type.</param>
    protected void AddCapability(Type capabilityType)
    {
        if (!_capabilities.Contains(capabilityType))
        {
            _capabilities.Add(capabilityType);
        }
    }

    /// <summary>
    /// Adds or updates a metadata entry.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    protected void SetMetadata(string key, string value)
    {
        _metadata[key] = value;
    }

    /// <summary>
    /// Updates a service metric.
    /// </summary>
    /// <param name="key">The metric key.</param>
    /// <param name="value">The metric value.</param>
    protected void UpdateMetric(string key, object value)
    {
        _metrics[key] = value;
    }

    /// <inheritdoc/>
    public virtual async Task<bool> InitializeAsync()
    {
        try
        {
            Logger.LogInformation("Initializing service {ServiceName}...", Name);
            var result = await OnInitializeAsync();
            Logger.LogInformation("Service {ServiceName} initialized successfully.", Name);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize service {ServiceName}.", Name);
            return false;
        }
    }

    /// <inheritdoc/>
    public virtual async Task<bool> StartAsync()
    {
        if (_isRunning)
        {
            Logger.LogWarning("Service {ServiceName} is already running.", Name);
            return true;
        }

        try
        {
            Logger.LogInformation("Starting service {ServiceName}...", Name);
            var result = await OnStartAsync();
            if (result)
            {
                _isRunning = true;
                _startTime = DateTime.UtcNow;
                Logger.LogInformation("Service {ServiceName} started successfully.", Name);
            }
            else
            {
                Logger.LogWarning("Service {ServiceName} failed to start.", Name);
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to start service {ServiceName}.", Name);
            return false;
        }
    }

    /// <inheritdoc/>
    public virtual async Task<bool> StopAsync()
    {
        if (!_isRunning)
        {
            Logger.LogWarning("Service {ServiceName} is not running.", Name);
            return true;
        }

        try
        {
            Logger.LogInformation("Stopping service {ServiceName}...", Name);
            var result = await OnStopAsync();
            if (result)
            {
                _isRunning = false;
                Logger.LogInformation("Service {ServiceName} stopped successfully.", Name);
            }
            else
            {
                Logger.LogWarning("Service {ServiceName} failed to stop.", Name);
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to stop service {ServiceName}.", Name);
            return false;
        }
    }

    /// <inheritdoc/>
    public virtual async Task<ServiceHealth> GetHealthAsync()
    {
        if (!_isRunning)
        {
            return ServiceHealth.NotRunning;
        }

        try
        {
            return await OnGetHealthAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get health status for service {ServiceName}.", Name);
            return ServiceHealth.Unhealthy;
        }
    }

    /// <inheritdoc/>
    public virtual async Task<IDictionary<string, object>> GetMetricsAsync()
    {
        try
        {
            await OnUpdateMetricsAsync();
            return new Dictionary<string, object>(_metrics);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get metrics for service {ServiceName}.", Name);
            return new Dictionary<string, object>();
        }
    }

    /// <inheritdoc/>
    public virtual Task<bool> ValidateDependenciesAsync(IEnumerable<IService> availableServices)
    {
        var services = availableServices.ToList();
        var missingDependencies = new List<ServiceDependency>();

        foreach (var dependency in _dependencies.OfType<ServiceDependency>())
        {
            var service = services.FirstOrDefault(s => s.Name == dependency.ServiceName);
            if (service == null)
            {
                if (dependency.IsRequired)
                {
                    Logger.LogError("Required dependency {DependencyName} not found for service {ServiceName}.", dependency.ServiceName, Name);
                    missingDependencies.Add(dependency);
                }
                else
                {
                    Logger.LogWarning("Optional dependency {DependencyName} not found for service {ServiceName}.", dependency.ServiceName, Name);
                }
                continue;
            }

            if (!dependency.Validate(service))
            {
                if (dependency.IsRequired)
                {
                    Logger.LogError("Required dependency {DependencyName} does not satisfy requirements for service {ServiceName}.", dependency.ServiceName, Name);
                    missingDependencies.Add(dependency);
                }
                else
                {
                    Logger.LogWarning("Optional dependency {DependencyName} does not satisfy requirements for service {ServiceName}.", dependency.ServiceName, Name);
                }
            }
        }

        if (missingDependencies.Any())
        {
            Logger.LogError("Service {ServiceName} is missing {MissingDependencyCount} required dependencies.", Name, missingDependencies.Count);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    /// <summary>
    /// Called when the service is being initialized.
    /// </summary>
    /// <returns>True if initialization was successful, false otherwise.</returns>
    protected abstract Task<bool> OnInitializeAsync();

    /// <summary>
    /// Called when the service is being started.
    /// </summary>
    /// <returns>True if the service was started successfully, false otherwise.</returns>
    protected abstract Task<bool> OnStartAsync();

    /// <summary>
    /// Called when the service is being stopped.
    /// </summary>
    /// <returns>True if the service was stopped successfully, false otherwise.</returns>
    protected abstract Task<bool> OnStopAsync();

    /// <summary>
    /// Called when the service health is being checked.
    /// </summary>
    /// <returns>The health status of the service.</returns>
    protected abstract Task<ServiceHealth> OnGetHealthAsync();

    /// <summary>
    /// Called when the service metrics are being updated.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual Task OnUpdateMetricsAsync()
    {
        // Default implementation does nothing
        return Task.CompletedTask;
    }

    #region IDisposable Implementation

    private bool _disposed = false;

    /// <summary>
    /// Disposes the service and releases all resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the service and releases resources.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
                if (_isRunning)
                {
                    try
                    {
                        StopAsync().GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error stopping service {ServiceName} during disposal", Name);
                    }
                }

                // Clear collections
                _dependencies.Clear();
                _capabilities.Clear();
                _metadata.Clear();
                _metrics.Clear();
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Finalizer for ServiceBase.
    /// </summary>
    ~ServiceBase()
    {
        Dispose(false);
    }

    #endregion
}
