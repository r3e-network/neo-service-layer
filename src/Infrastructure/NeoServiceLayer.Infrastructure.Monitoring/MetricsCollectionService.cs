using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Infrastructure.Monitoring;

/// <summary>
/// Service for collecting and exposing application metrics.
/// </summary>
public class MetricsCollectionService : BackgroundService, IMetricsCollectionService
{
    private readonly ILogger<MetricsCollectionService> _logger;
    private readonly Meter _meter;
    private readonly ConcurrentDictionary<string, Counter<long>> _counters = new();
    private readonly ConcurrentDictionary<string, Histogram<double>> _histograms = new();
    private readonly ConcurrentDictionary<string, ObservableGauge<double>> _gauges = new();
    private readonly ConcurrentDictionary<string, Func<double>> _gaugeFunctions = new();

    // Performance counters
    private readonly PerformanceCounter _cpuCounter;
    private readonly PerformanceCounter _memoryCounter;
    private readonly Process _currentProcess;

    public MetricsCollectionService(ILogger<MetricsCollectionService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _meter = new Meter("NeoServiceLayer", "1.0.0");
        _currentProcess = Process.GetCurrentProcess();

        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize performance counters. Metrics may be limited.");
        }

        InitializeSystemMetrics();
    }

    /// <inheritdoc/>
    public void IncrementCounter(string name, long value = 1, params (string Key, object Value)[] tags)
    {
        try
        {
            var counter = _counters.GetOrAdd(name, key => _meter.CreateCounter<long>(key));
            
            if (tags?.Length > 0)
            {
                var tagList = new TagList();
                foreach (var (key, tagValue) in tags)
                {
                    tagList.Add(key, tagValue);
                }
                counter.Add(value, tagList);
            }
            else
            {
                counter.Add(value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment counter {CounterName}", name);
        }
    }

    /// <inheritdoc/>
    public void RecordValue(string name, double value, params (string Key, object Value)[] tags)
    {
        try
        {
            var histogram = _histograms.GetOrAdd(name, key => _meter.CreateHistogram<double>(key));
            
            if (tags?.Length > 0)
            {
                var tagList = new TagList();
                foreach (var (key, tagValue) in tags)
                {
                    tagList.Add(key, tagValue);
                }
                histogram.Record(value, tagList);
            }
            else
            {
                histogram.Record(value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record histogram value for {HistogramName}", name);
        }
    }

    /// <inheritdoc/>
    public void SetGauge(string name, Func<double> valueFunction, params (string Key, object Value)[] tags)
    {
        try
        {
            _gaugeFunctions[name] = valueFunction;
            
            var gauge = _gauges.GetOrAdd(name, key => 
            {
                return _meter.CreateObservableGauge(key, () =>
                {
                    try
                    {
                        var value = _gaugeFunctions[key]();
                        
                        if (tags?.Length > 0)
                        {
                            var tagList = new TagList();
                            foreach (var (tagKey, tagValue) in tags)
                            {
                                tagList.Add(tagKey, tagValue);
                            }
                            return new Measurement<double>(value, tagList);
                        }
                        
                        return new Measurement<double>(value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to get gauge value for {GaugeName}", key);
                        return new Measurement<double>(0);
                    }
                });
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set gauge {GaugeName}", name);
        }
    }

    /// <inheritdoc/>
    public IDisposable MeasureDuration(string name, params (string Key, object Value)[] tags)
    {
        return new DurationMeasurement(this, name, tags);
    }

    /// <inheritdoc/>
    public async Task<T> MeasureAsync<T>(string name, Func<Task<T>> operation, params (string Key, object Value)[] tags)
    {
        using var measurement = MeasureDuration(name, tags);
        try
        {
            var result = await operation().ConfigureAwait(false);
            IncrementCounter($"{name}.success", 1, tags);
            return result;
        }
        catch (Exception)
        {
            IncrementCounter($"{name}.error", 1, tags);
            throw;
        }
    }

    /// <inheritdoc/>
    public MetricsSnapshot GetSnapshot()
    {
        try
        {
            var snapshot = new MetricsSnapshot
            {
                Timestamp = DateTime.UtcNow,
                ProcessId = _currentProcess.Id,
                ProcessName = _currentProcess.ProcessName,
                WorkingSet = _currentProcess.WorkingSet64,
                PrivateMemory = _currentProcess.PrivateMemorySize64,
                CpuTime = _currentProcess.TotalProcessorTime,
                ThreadCount = _currentProcess.Threads.Count,
                HandleCount = _currentProcess.HandleCount
            };

            // Add system metrics if available
            if (_cpuCounter != null)
            {
                snapshot.SystemCpuUsage = _cpuCounter.NextValue();
            }

            if (_memoryCounter != null)
            {
                snapshot.AvailableMemoryMB = _memoryCounter.NextValue();
            }

            // Add custom metrics counts
            snapshot.CustomCounters = _counters.Count;
            snapshot.CustomHistograms = _histograms.Count;
            snapshot.CustomGauges = _gauges.Count;

            return snapshot;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create metrics snapshot");
            return new MetricsSnapshot { Timestamp = DateTime.UtcNow };
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Metrics collection service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Collect system metrics periodically
                CollectSystemMetrics();
                
                // Wait for next collection interval
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in metrics collection service");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Metrics collection service stopped");
    }

    private void InitializeSystemMetrics()
    {
        // CPU usage gauge
        SetGauge("system.cpu.usage", () =>
        {
            try
            {
                return _currentProcess.TotalProcessorTime.TotalMilliseconds;
            }
            catch
            {
                return 0;
            }
        });

        // Memory usage gauge
        SetGauge("system.memory.working_set", () =>
        {
            try
            {
                return _currentProcess.WorkingSet64;
            }
            catch
            {
                return 0;
            }
        });

        // Thread count gauge
        SetGauge("system.threads.count", () =>
        {
            try
            {
                return _currentProcess.Threads.Count;
            }
            catch
            {
                return 0;
            }
        });

        // GC metrics
        SetGauge("gc.memory.heap_size", () => GC.GetTotalMemory(false));
        SetGauge("gc.collections.gen0", () => GC.CollectionCount(0));
        SetGauge("gc.collections.gen1", () => GC.CollectionCount(1));
        SetGauge("gc.collections.gen2", () => GC.CollectionCount(2));
    }

    private void CollectSystemMetrics()
    {
        try
        {
            // Record current metrics values
            RecordValue("process.cpu.time", _currentProcess.TotalProcessorTime.TotalMilliseconds);
            RecordValue("process.memory.working_set", _currentProcess.WorkingSet64);
            RecordValue("process.memory.private", _currentProcess.PrivateMemorySize64);
            RecordValue("process.threads.count", _currentProcess.Threads.Count);
            RecordValue("process.handles.count", _currentProcess.HandleCount);
            
            // GC metrics
            RecordValue("gc.memory.heap", GC.GetTotalMemory(false));
            
            // System metrics (if available)
            if (_cpuCounter != null)
            {
                RecordValue("system.cpu.usage_percent", _cpuCounter.NextValue());
            }

            if (_memoryCounter != null)
            {
                RecordValue("system.memory.available_mb", _memoryCounter.NextValue());
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect some system metrics");
        }
    }

    public override void Dispose()
    {
        try
        {
            _meter?.Dispose();
            _cpuCounter?.Dispose();
            _memoryCounter?.Dispose();
            _currentProcess?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing metrics collection service");
        }
        
        base.Dispose();
    }
}

/// <summary>
/// Duration measurement helper class.
/// </summary>
internal class DurationMeasurement : IDisposable
{
    private readonly IMetricsCollectionService _metricsService;
    private readonly string _name;
    private readonly (string Key, object Value)[] _tags;
    private readonly Stopwatch _stopwatch;

    public DurationMeasurement(IMetricsCollectionService metricsService, string name, (string Key, object Value)[] tags)
    {
        _metricsService = metricsService;
        _name = name;
        _tags = tags;
        _stopwatch = Stopwatch.StartNew();
    }

    public void Dispose()
    {
        _stopwatch.Stop();
        _metricsService.RecordValue(_name, _stopwatch.Elapsed.TotalMilliseconds, _tags);
    }
}

/// <summary>
/// Interface for metrics collection service.
/// </summary>
public interface IMetricsCollectionService
{
    void IncrementCounter(string name, long value = 1, params (string Key, object Value)[] tags);
    void RecordValue(string name, double value, params (string Key, object Value)[] tags);
    void SetGauge(string name, Func<double> valueFunction, params (string Key, object Value)[] tags);
    IDisposable MeasureDuration(string name, params (string Key, object Value)[] tags);
    Task<T> MeasureAsync<T>(string name, Func<Task<T>> operation, params (string Key, object Value)[] tags);
    MetricsSnapshot GetSnapshot();
}

/// <summary>
/// Snapshot of current metrics state.
/// </summary>
public class MetricsSnapshot
{
    public DateTime Timestamp { get; set; }
    public int ProcessId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public long WorkingSet { get; set; }
    public long PrivateMemory { get; set; }
    public TimeSpan CpuTime { get; set; }
    public int ThreadCount { get; set; }
    public int HandleCount { get; set; }
    public float SystemCpuUsage { get; set; }
    public float AvailableMemoryMB { get; set; }
    public int CustomCounters { get; set; }
    public int CustomHistograms { get; set; }
    public int CustomGauges { get; set; }
}