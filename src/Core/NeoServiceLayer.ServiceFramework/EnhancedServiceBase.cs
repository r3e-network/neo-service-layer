using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure.Persistence;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.ServiceFramework;

/// <summary>
/// Enhanced base class for all services in the Neo Service Layer with professional practices.
/// Includes persistent storage, dependency injection, configuration management, health checks, and metrics.
/// </summary>
public abstract class EnhancedServiceBase : ServiceBase, IHostedService, IHealthCheck
{
    protected readonly IServiceProvider ServiceProvider;
    protected readonly IConfiguration Configuration;
    protected readonly IPersistentStorageProvider StorageProvider;
    protected readonly Meter ServiceMeter;
    protected readonly Counter<long> RequestCounter;
    protected readonly Counter<long> ErrorCounter;
    protected readonly Histogram<double> RequestDuration;
    protected readonly ObservableGauge<int> ActiveConnections;

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Dictionary<string, object> _serviceState = new();
    private int _activeConnections;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnhancedServiceBase"/> class.
    /// </summary>
    /// <param name="name">The name of the service.</param>
    /// <param name="description">The description of the service.</param>
    /// <param name="version">The version of the service.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="storageProvider">The persistent storage provider.</param>
    protected EnhancedServiceBase(
        string name,
        string description,
        string version,
        ILogger logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IPersistentStorageProvider storageProvider)
        : base(name, description, version, logger)
    {
        ServiceProvider = serviceProvider;
        Configuration = configuration;
        StorageProvider = storageProvider;

        // Initialize metrics
        ServiceMeter = new Meter($"NeoServiceLayer.{name}", version);
        RequestCounter = ServiceMeter.CreateCounter<long>($"{name.ToLower()}_requests_total", "requests", "Total number of requests");
        ErrorCounter = ServiceMeter.CreateCounter<long>($"{name.ToLower()}_errors_total", "errors", "Total number of errors");
        RequestDuration = ServiceMeter.CreateHistogram<double>($"{name.ToLower()}_request_duration_seconds", "seconds", "Request duration in seconds");
        ActiveConnections = ServiceMeter.CreateObservableGauge<int>($"{name.ToLower()}_active_connections", () => _activeConnections);

        // Add enhanced capabilities
        AddCapability<IHostedService>();
        AddCapability<IHealthCheck>();

        // Set enhanced metadata
        SetMetadata("Framework", "Enhanced");
        SetMetadata("StorageProvider", storageProvider.GetType().Name);
        SetMetadata("SupportsMetrics", "true");
        SetMetadata("SupportsHealthChecks", "true");
        SetMetadata("SupportsPersistentStorage", "true");
    }

    /// <summary>
    /// Gets the cancellation token for the service.
    /// </summary>
    protected CancellationToken CancellationToken => _cancellationTokenSource.Token;

    /// <summary>
    /// Gets a service from the dependency injection container.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <returns>The service instance.</returns>
    protected T GetService<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Gets a service from the dependency injection container if available.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <returns>The service instance or null if not available.</returns>
    protected T? GetOptionalService<T>() where T : class
    {
        return ServiceProvider.GetService<T>();
    }

    /// <summary>
    /// Gets a configuration value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">The default value if not found.</param>
    /// <returns>The configuration value.</returns>
    protected T GetConfigurationValue<T>(string key, T defaultValue = default!)
    {
        return Configuration.GetValue<T>(key, defaultValue) ?? defaultValue;
    }

    /// <summary>
    /// Gets a configuration section.
    /// </summary>
    /// <param name="key">The section key.</param>
    /// <returns>The configuration section.</returns>
    protected Microsoft.Extensions.Configuration.IConfigurationSection GetConfigurationSection(string key)
    {
        return Configuration.GetSection(key);
    }

    /// <summary>
    /// Stores data in persistent storage.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <param name="data">The data to store.</param>
    /// <param name="options">Storage options.</param>
    /// <returns>True if successful, false otherwise.</returns>
    protected async Task<bool> StoreDataAsync(string key, byte[] data, StorageOptions? options = null)
    {
        try
        {
            options ??= new StorageOptions { Encrypt = true, Compress = true };
            return await StorageProvider.StoreAsync($"{Name}:{key}", data, options);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to store data with key {Key} for service {ServiceName}", key, Name);
            return false;
        }
    }

    /// <summary>
    /// Retrieves data from persistent storage.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <returns>The retrieved data or null if not found.</returns>
    protected async Task<byte[]?> RetrieveDataAsync(string key)
    {
        try
        {
            return await StorageProvider.RetrieveAsync($"{Name}:{key}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to retrieve data with key {Key} for service {ServiceName}", key, Name);
            return null;
        }
    }

    /// <summary>
    /// Deletes data from persistent storage.
    /// </summary>
    /// <param name="key">The storage key.</param>
    /// <returns>True if successful, false otherwise.</returns>
    protected async Task<bool> DeleteDataAsync(string key)
    {
        try
        {
            return await StorageProvider.DeleteAsync($"{Name}:{key}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete data with key {Key} for service {ServiceName}", key, Name);
            return false;
        }
    }

    /// <summary>
    /// Gets or sets service state.
    /// </summary>
    /// <param name="key">The state key.</param>
    /// <param name="value">The state value.</param>
    protected void SetServiceState(string key, object value)
    {
        lock (_serviceState)
        {
            _serviceState[key] = value;
        }
    }

    /// <summary>
    /// Gets service state.
    /// </summary>
    /// <typeparam name="T">The state type.</typeparam>
    /// <param name="key">The state key.</param>
    /// <returns>The state value or default if not found.</returns>
    protected T? GetServiceState<T>(string key)
    {
        lock (_serviceState)
        {
            return _serviceState.TryGetValue(key, out var value) && value is T typedValue ? typedValue : default;
        }
    }

    /// <summary>
    /// Increments the active connections counter.
    /// </summary>
    protected void IncrementActiveConnections()
    {
        Interlocked.Increment(ref _activeConnections);
    }

    /// <summary>
    /// Decrements the active connections counter.
    /// </summary>
    protected void DecrementActiveConnections()
    {
        Interlocked.Decrement(ref _activeConnections);
    }

    /// <summary>
    /// Records a request with metrics.
    /// </summary>
    /// <param name="operation">The operation name.</param>
    /// <param name="duration">The operation duration.</param>
    /// <param name="success">Whether the operation was successful.</param>
    protected void RecordRequest(string operation, TimeSpan duration, bool success)
    {
        RequestCounter.Add(1, new KeyValuePair<string, object?>("operation", operation));
        RequestDuration.Record(duration.TotalSeconds, new KeyValuePair<string, object?>("operation", operation));

        if (!success)
        {
            ErrorCounter.Add(1, new KeyValuePair<string, object?>("operation", operation));
        }
    }

    /// <summary>
    /// Executes an operation with metrics recording.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="operation">The operation name.</param>
    /// <param name="func">The operation function.</param>
    /// <returns>The operation result.</returns>
    protected async Task<T> ExecuteWithMetricsAsync<T>(string operation, Func<Task<T>> func)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var success = false;

        try
        {
            var result = await func();
            success = true;
            return result;
        }
        finally
        {
            stopwatch.Stop();
            RecordRequest(operation, stopwatch.Elapsed, success);
        }
    }

    /// <summary>
    /// Executes an operation with metrics recording.
    /// </summary>
    /// <param name="operation">The operation name.</param>
    /// <param name="func">The operation function.</param>
    protected async Task ExecuteWithMetricsAsync(string operation, Func<Task> func)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var success = false;

        try
        {
            await func();
            success = true;
        }
        finally
        {
            stopwatch.Stop();
            RecordRequest(operation, stopwatch.Elapsed, success);
        }
    }

    #region IHostedService Implementation

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Starting hosted service {ServiceName}...", Name);

        // Initialize storage provider
        if (!await StorageProvider.InitializeAsync())
        {
            throw new InvalidOperationException($"Failed to initialize storage provider for service {Name}");
        }

        // Initialize and start the service
        if (!await InitializeAsync())
        {
            throw new InvalidOperationException($"Failed to initialize service {Name}");
        }

        if (!await StartAsync())
        {
            throw new InvalidOperationException($"Failed to start service {Name}");
        }

        Logger.LogInformation("Hosted service {ServiceName} started successfully", Name);
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Stopping hosted service {ServiceName}...", Name);

        _cancellationTokenSource.Cancel();

        await StopAsync();

        ServiceMeter.Dispose();
        StorageProvider.Dispose();

        Logger.LogInformation("Hosted service {ServiceName} stopped successfully", Name);
    }

    #endregion

    #region IHealthCheck Implementation

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var health = await GetHealthAsync();

            var data = new Dictionary<string, object>
            {
                ["service_name"] = Name,
                ["version"] = Version,
                ["is_running"] = IsRunning,
                ["active_connections"] = _activeConnections
            };

            return health switch
            {
                ServiceHealth.Healthy => HealthCheckResult.Healthy($"Service {Name} is healthy", data),
                ServiceHealth.Degraded => HealthCheckResult.Degraded($"Service {Name} is degraded", null, data),
                ServiceHealth.Unhealthy => HealthCheckResult.Unhealthy($"Service {Name} is unhealthy", null, data),
                ServiceHealth.NotRunning => HealthCheckResult.Unhealthy($"Service {Name} is not running", null, data),
                _ => HealthCheckResult.Unhealthy($"Service {Name} has unknown health status", null, data)
            };
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Health check failed for service {Name}: {ex.Message}");
        }
    }

    #endregion

    /// <summary>
    /// Called when the service is being initialized with enhanced features.
    /// </summary>
    /// <returns>True if initialization was successful, false otherwise.</returns>
    protected virtual Task<bool> OnEnhancedInitializeAsync()
    {
        return Task.FromResult(true);
    }

    /// <summary>
    /// Called when the service is being started with enhanced features.
    /// </summary>
    /// <returns>True if the service was started successfully, false otherwise.</returns>
    protected virtual Task<bool> OnEnhancedStartAsync()
    {
        return Task.FromResult(true);
    }

    /// <summary>
    /// Called when the service is being stopped with enhanced features.
    /// </summary>
    /// <returns>True if the service was stopped successfully, false otherwise.</returns>
    protected virtual Task<bool> OnEnhancedStopAsync()
    {
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        // Call enhanced initialization directly since base is abstract
        return await OnEnhancedInitializeAsync();
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        // Call enhanced start directly since base is abstract
        return await OnEnhancedStartAsync();
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStopAsync()
    {
        // Call enhanced stop directly since base is abstract
        return await OnEnhancedStopAsync();
    }
}
