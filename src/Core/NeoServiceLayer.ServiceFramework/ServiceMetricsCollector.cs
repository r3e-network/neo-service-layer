using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.ServiceFramework;

/// <summary>
/// Interface for service metrics collectors.
/// </summary>
public interface IServiceMetricsCollector
{
    /// <summary>
    /// Collects metrics from all services.
    /// </summary>
    /// <returns>A dictionary mapping service names to their metrics.</returns>
    Task<IDictionary<string, IDictionary<string, object>>> CollectAllMetricsAsync();

    /// <summary>
    /// Collects metrics from a specific service.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <returns>The service metrics, or null if the service was not found.</returns>
    Task<IDictionary<string, object>?> CollectServiceMetricsAsync(string serviceName);

    /// <summary>
    /// Collects metrics from services matching a pattern.
    /// </summary>
    /// <param name="pattern">The regular expression pattern to match service names.</param>
    /// <returns>A dictionary mapping service names to their metrics.</returns>
    Task<IDictionary<string, IDictionary<string, object>>> CollectMetricsByPatternAsync(string pattern);

    /// <summary>
    /// Starts collecting metrics at regular intervals.
    /// </summary>
    /// <param name="interval">The collection interval.</param>
    void StartCollecting(TimeSpan interval);

    /// <summary>
    /// Stops collecting metrics.
    /// </summary>
    void StopCollecting();

    /// <summary>
    /// Event raised when metrics are collected.
    /// </summary>
    event EventHandler<ServiceMetricsEventArgs>? MetricsCollected;
}

/// <summary>
/// Event arguments for service metrics events.
/// </summary>
public class ServiceMetricsEventArgs : EventArgs
{
    /// <summary>
    /// Gets the metrics.
    /// </summary>
    public IDictionary<string, IDictionary<string, object>> Metrics { get; }

    /// <summary>
    /// Gets the collection timestamp.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceMetricsEventArgs"/> class.
    /// </summary>
    /// <param name="metrics">The metrics.</param>
    /// <param name="timestamp">The collection timestamp.</param>
    public ServiceMetricsEventArgs(IDictionary<string, IDictionary<string, object>> metrics, DateTime timestamp)
    {
        Metrics = metrics;
        Timestamp = timestamp;
    }
}

/// <summary>
/// Implementation of the service metrics collector.
/// </summary>
public class ServiceMetricsCollector : IServiceMetricsCollector, IDisposable
{
    private readonly IServiceRegistry _serviceRegistry;
    private readonly ILogger<ServiceMetricsCollector> _logger;
    private Timer? _timer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceMetricsCollector"/> class.
    /// </summary>
    /// <param name="serviceRegistry">The service registry.</param>
    /// <param name="logger">The logger.</param>
    public ServiceMetricsCollector(IServiceRegistry serviceRegistry, ILogger<ServiceMetricsCollector> logger)
    {
        _serviceRegistry = serviceRegistry;
        _logger = logger;
        _disposed = false;
    }

    /// <inheritdoc/>
    public event EventHandler<ServiceMetricsEventArgs>? MetricsCollected;

    /// <inheritdoc/>
    public async Task<IDictionary<string, IDictionary<string, object>>> CollectAllMetricsAsync()
    {
        var services = _serviceRegistry.GetAllServices().ToList();
        var result = new Dictionary<string, IDictionary<string, object>>();

        foreach (var service in services)
        {
            try
            {
                var metrics = await service.GetMetricsAsync();
                result[service.Name] = metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to collect metrics for service {ServiceName}.", service.Name);
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<IDictionary<string, object>?> CollectServiceMetricsAsync(string serviceName)
    {
        var service = _serviceRegistry.GetService(serviceName);
        if (service == null)
        {
            _logger.LogWarning("Service with name {ServiceName} is not registered.", serviceName);
            return null;
        }

        try
        {
            return await service.GetMetricsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect metrics for service {ServiceName}.", serviceName);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<IDictionary<string, IDictionary<string, object>>> CollectMetricsByPatternAsync(string pattern)
    {
        var services = _serviceRegistry.FindServicesByNamePattern(pattern).ToList();
        var result = new Dictionary<string, IDictionary<string, object>>();

        foreach (var service in services)
        {
            try
            {
                var metrics = await service.GetMetricsAsync();
                result[service.Name] = metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to collect metrics for service {ServiceName}.", service.Name);
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public void StartCollecting(TimeSpan interval)
    {
        if (_timer != null)
        {
            _logger.LogWarning("Metrics collection is already running.");
            return;
        }

        _logger.LogInformation("Starting metrics collection with interval {Interval}.", interval);
        _timer = new Timer(CollectMetricsCallback, null, TimeSpan.Zero, interval);
    }

    /// <inheritdoc/>
    public void StopCollecting()
    {
        if (_timer == null)
        {
            _logger.LogWarning("Metrics collection is not running.");
            return;
        }

        _logger.LogInformation("Stopping metrics collection.");
        _timer.Dispose();
        _timer = null;
    }

    /// <summary>
    /// Disposes the service metrics collector.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the service metrics collector.
    /// </summary>
    /// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _timer?.Dispose();
            _timer = null;
        }

        _disposed = true;
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="ServiceMetricsCollector"/> class.
    /// </summary>
    ~ServiceMetricsCollector()
    {
        Dispose(false);
    }

    private async void CollectMetricsCallback(object? state)
    {
        try
        {
            var metrics = await CollectAllMetricsAsync();
            var timestamp = DateTime.UtcNow;
            OnMetricsCollected(new ServiceMetricsEventArgs(metrics, timestamp));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect metrics.");
        }
    }

    private void OnMetricsCollected(ServiceMetricsEventArgs e)
    {
        MetricsCollected?.Invoke(this, e);
    }
}
