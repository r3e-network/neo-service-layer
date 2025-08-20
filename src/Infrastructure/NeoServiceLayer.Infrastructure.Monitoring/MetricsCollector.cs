using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Infrastructure.Monitoring;

/// <summary>
/// Comprehensive metrics collector for monitoring system performance and health.
/// </summary>
public class MetricsCollector : IMetricsCollector
{
    private readonly ILogger<MetricsCollector> _logger;
    private readonly Meter _meter;
    
    // Counters
    private readonly Counter<long> _requestCounter;
    private readonly Counter<long> _errorCounter;
    private readonly Counter<long> _cacheHitCounter;
    private readonly Counter<long> _cacheMissCounter;
    
    // Histograms
    private readonly Histogram<double> _requestDuration;
    private readonly Histogram<double> _databaseQueryDuration;
    private readonly Histogram<double> _externalApiCallDuration;
    
    // Gauges
    private readonly ObservableGauge<double> _cpuUsageGauge;
    private readonly ObservableGauge<long> _memoryUsageGauge;
    private readonly ObservableGauge<int> _activeConnectionsGauge;
    private readonly ObservableGauge<int> _queueLengthGauge;
    
    // UpDownCounters
    private readonly UpDownCounter<int> _activeRequestsCounter;
    private readonly UpDownCounter<int> _circuitBreakerStateCounter;
    
    // Internal tracking
    private int _activeRequests = 0;
    private int _activeConnections = 0;
    private int _queueLength = 0;
    private readonly Dictionary<string, CircuitBreakerState> _circuitBreakerStates = new();

    public MetricsCollector(ILogger<MetricsCollector> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Create meter for Neo Service Layer
        _meter = new Meter("NeoServiceLayer", "1.0.0");
        
        // Initialize counters
        _requestCounter = _meter.CreateCounter<long>(
            "nsl.requests.total",
            description: "Total number of requests processed");
        
        _errorCounter = _meter.CreateCounter<long>(
            "nsl.errors.total",
            description: "Total number of errors encountered");
        
        _cacheHitCounter = _meter.CreateCounter<long>(
            "nsl.cache.hits",
            description: "Total number of cache hits");
        
        _cacheMissCounter = _meter.CreateCounter<long>(
            "nsl.cache.misses",
            description: "Total number of cache misses");
        
        // Initialize histograms
        _requestDuration = _meter.CreateHistogram<double>(
            "nsl.request.duration",
            unit: "ms",
            description: "Request duration in milliseconds");
        
        _databaseQueryDuration = _meter.CreateHistogram<double>(
            "nsl.database.query.duration",
            unit: "ms",
            description: "Database query duration in milliseconds");
        
        _externalApiCallDuration = _meter.CreateHistogram<double>(
            "nsl.external.api.duration",
            unit: "ms",
            description: "External API call duration in milliseconds");
        
        // Initialize gauges
        _cpuUsageGauge = _meter.CreateObservableGauge(
            "nsl.cpu.usage",
            () => GetCpuUsage(),
            unit: "%",
            description: "Current CPU usage percentage");
        
        _memoryUsageGauge = _meter.CreateObservableGauge(
            "nsl.memory.usage",
            () => GetMemoryUsage(),
            unit: "bytes",
            description: "Current memory usage in bytes");
        
        _activeConnectionsGauge = _meter.CreateObservableGauge(
            "nsl.connections.active",
            () => _activeConnections,
            description: "Number of active connections");
        
        _queueLengthGauge = _meter.CreateObservableGauge(
            "nsl.queue.length",
            () => _queueLength,
            description: "Current queue length");
        
        // Initialize UpDownCounters
        _activeRequestsCounter = _meter.CreateUpDownCounter<int>(
            "nsl.requests.active",
            description: "Number of currently active requests");
        
        _circuitBreakerStateCounter = _meter.CreateUpDownCounter<int>(
            "nsl.circuit.breaker.state",
            description: "Circuit breaker state (0=closed, 1=open, 2=half-open)");
        
        _logger.LogInformation("Metrics collector initialized with OpenTelemetry metrics");
    }

    /// <inheritdoc/>
    public void RecordRequest(string endpoint, string method, int statusCode, double durationMs, Dictionary<string, object> tags = null)
    {
        var tagList = CreateTagList(tags);
        tagList.Add("endpoint", endpoint);
        tagList.Add("method", method);
        tagList.Add("status_code", statusCode);
        
        _requestCounter.Add(1, tagList);
        _requestDuration.Record(durationMs, tagList);
        
        if (statusCode >= 400)
        {
            _errorCounter.Add(1, tagList);
        }
        
        _logger.LogDebug("Recorded request: {Endpoint} {Method} {StatusCode} {Duration}ms", 
            endpoint, method, statusCode, durationMs);
    }

    /// <inheritdoc/>
    public void RecordError(string errorType, string service, Dictionary<string, object> tags = null)
    {
        var tagList = CreateTagList(tags);
        tagList.Add("error_type", errorType);
        tagList.Add("service", service);
        
        _errorCounter.Add(1, tagList);
        
        _logger.LogWarning("Recorded error: {ErrorType} in {Service}", errorType, service);
    }

    /// <inheritdoc/>
    public void RecordCacheHit(string cacheKey, Dictionary<string, object> tags = null)
    {
        var tagList = CreateTagList(tags);
        tagList.Add("cache_key", cacheKey);
        
        _cacheHitCounter.Add(1, tagList);
    }

    /// <inheritdoc/>
    public void RecordCacheMiss(string cacheKey, Dictionary<string, object> tags = null)
    {
        var tagList = CreateTagList(tags);
        tagList.Add("cache_key", cacheKey);
        
        _cacheMissCounter.Add(1, tagList);
    }

    /// <inheritdoc/>
    public void RecordDatabaseQuery(string queryType, double durationMs, Dictionary<string, object> tags = null)
    {
        var tagList = CreateTagList(tags);
        tagList.Add("query_type", queryType);
        
        _databaseQueryDuration.Record(durationMs, tagList);
    }

    /// <inheritdoc/>
    public void RecordExternalApiCall(string apiName, string endpoint, double durationMs, bool success, Dictionary<string, object> tags = null)
    {
        var tagList = CreateTagList(tags);
        tagList.Add("api_name", apiName);
        tagList.Add("endpoint", endpoint);
        tagList.Add("success", success);
        
        _externalApiCallDuration.Record(durationMs, tagList);
        
        if (!success)
        {
            _errorCounter.Add(1, tagList);
        }
    }

    /// <inheritdoc/>
    public void IncrementActiveRequests()
    {
        Interlocked.Increment(ref _activeRequests);
        _activeRequestsCounter.Add(1);
    }

    /// <inheritdoc/>
    public void DecrementActiveRequests()
    {
        Interlocked.Decrement(ref _activeRequests);
        _activeRequestsCounter.Add(-1);
    }

    /// <inheritdoc/>
    public void UpdateCircuitBreakerState(string serviceName, CircuitBreakerState state)
    {
        lock (_circuitBreakerStates)
        {
            _circuitBreakerStates[serviceName] = state;
        }
        
        var tagList = new TagList { { "service", serviceName } };
        _circuitBreakerStateCounter.Add((int)state, tagList);
    }

    /// <inheritdoc/>
    public void UpdateActiveConnections(int count)
    {
        Interlocked.Exchange(ref _activeConnections, count);
    }

    /// <inheritdoc/>
    public void UpdateQueueLength(int length)
    {
        Interlocked.Exchange(ref _queueLength, length);
    }

    /// <inheritdoc/>
    public async Task<MetricsSnapshot> GetSnapshotAsync()
    {
        return await Task.Run(() => new MetricsSnapshot
        {
            Timestamp = DateTime.UtcNow,
            ActiveRequests = _activeRequests,
            ActiveConnections = _activeConnections,
            QueueLength = _queueLength,
            CpuUsage = GetCpuUsage(),
            MemoryUsage = GetMemoryUsage(),
            CircuitBreakerStates = new Dictionary<string, CircuitBreakerState>(_circuitBreakerStates)
        }).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public IDisposable MeasureOperation(string operationName, Dictionary<string, object> tags = null)
    {
        return new OperationTimer(this, operationName, tags);
    }

    private TagList CreateTagList(Dictionary<string, object> tags)
    {
        var tagList = new TagList();
        
        if (tags != null)
        {
            foreach (var kvp in tags)
            {
                tagList.Add(kvp.Key, kvp.Value?.ToString() ?? "null");
            }
        }
        
        return tagList;
    }

    private double GetCpuUsage()
    {
        // This is a simplified implementation
        // In production, use performance counters or /proc/stat on Linux
        using var process = Process.GetCurrentProcess();
        return process.TotalProcessorTime.TotalMilliseconds / Environment.ProcessorCount / Environment.TickCount * 100;
    }

    private long GetMemoryUsage()
    {
        using var process = Process.GetCurrentProcess();
        return process.WorkingSet64;
    }

    /// <summary>
    /// Timer for measuring operation duration.
    /// </summary>
    private class OperationTimer : IDisposable
    {
        private readonly MetricsCollector _collector;
        private readonly string _operationName;
        private readonly Dictionary<string, object> _tags;
        private readonly Stopwatch _stopwatch;

        public OperationTimer(MetricsCollector collector, string operationName, Dictionary<string, object> tags)
        {
            _collector = collector;
            _operationName = operationName;
            _tags = tags;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            var tagList = _collector.CreateTagList(_tags);
            tagList.Add("operation", _operationName);
            _collector._requestDuration.Record(_stopwatch.ElapsedMilliseconds, tagList);
        }
    }
}

/// <summary>
/// Interface for metrics collection.
/// </summary>
public interface IMetricsCollector
{
    void RecordRequest(string endpoint, string method, int statusCode, double durationMs, Dictionary<string, object> tags = null);
    void RecordError(string errorType, string service, Dictionary<string, object> tags = null);
    void RecordCacheHit(string cacheKey, Dictionary<string, object> tags = null);
    void RecordCacheMiss(string cacheKey, Dictionary<string, object> tags = null);
    void RecordDatabaseQuery(string queryType, double durationMs, Dictionary<string, object> tags = null);
    void RecordExternalApiCall(string apiName, string endpoint, double durationMs, bool success, Dictionary<string, object> tags = null);
    void IncrementActiveRequests();
    void DecrementActiveRequests();
    void UpdateCircuitBreakerState(string serviceName, CircuitBreakerState state);
    void UpdateActiveConnections(int count);
    void UpdateQueueLength(int length);
    Task<MetricsSnapshot> GetSnapshotAsync();
    IDisposable MeasureOperation(string operationName, Dictionary<string, object> tags = null);
}

/// <summary>
/// Circuit breaker states.
/// </summary>
public enum CircuitBreakerState
{
    Closed = 0,
    Open = 1,
    HalfOpen = 2
}

/// <summary>
/// Snapshot of current metrics.
/// </summary>
public class MetricsSnapshot
{
    public DateTime Timestamp { get; set; }
    public int ActiveRequests { get; set; }
    public int ActiveConnections { get; set; }
    public int QueueLength { get; set; }
    public double CpuUsage { get; set; }
    public long MemoryUsage { get; set; }
    public Dictionary<string, CircuitBreakerState> CircuitBreakerStates { get; set; }
}