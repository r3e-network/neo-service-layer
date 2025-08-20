using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NeoServiceLayer.Infrastructure.Performance;

/// <summary>
/// Service for performance monitoring and optimization.
/// </summary>
public class PerformanceOptimizationService : BackgroundService, IPerformanceOptimizationService
{
    private readonly ILogger<PerformanceOptimizationService> _logger;
    private readonly PerformanceOptions _options;
    private readonly ConcurrentDictionary<string, PerformanceMetric> _metrics = new();
    private readonly ConcurrentDictionary<string, ThrottledOperation> _throttledOperations = new();
    private readonly Timer _cleanupTimer;

    public PerformanceOptimizationService(
        ILogger<PerformanceOptimizationService> logger,
        IOptions<PerformanceOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new PerformanceOptions();
        
        _cleanupTimer = new Timer(CleanupCallback, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    /// <inheritdoc/>
    public async Task<T> OptimizeAsync<T>(
        string operationName,
        Func<Task<T>> operation,
        PerformanceProfile profile = PerformanceProfile.Balanced,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var operationId = Guid.NewGuid().ToString();

        try
        {
            // Apply throttling if needed
            if (profile == PerformanceProfile.ThrottleHeavy)
            {
                await ApplyThrottlingAsync(operationName, cancellationToken);
            }

            // Execute with performance monitoring
            _logger.LogDebug("Starting optimized operation {OperationName} with profile {Profile}", 
                operationName, profile);

            var result = await ExecuteWithProfileAsync(operation, profile, cancellationToken);

            stopwatch.Stop();
            RecordMetric(operationName, stopwatch.Elapsed, true);

            _logger.LogDebug("Completed optimized operation {OperationName} in {Duration}ms", 
                operationName, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordMetric(operationName, stopwatch.Elapsed, false);
            
            _logger.LogError(ex, "Failed optimized operation {OperationName} after {Duration}ms", 
                operationName, stopwatch.ElapsedMilliseconds);
            
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<T> BatchProcessAsync<T>(
        string operationName,
        IEnumerable<Func<Task<T>>> operations,
        BatchProcessingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var batchOptions = options ?? new BatchProcessingOptions();
        var operationsList = operations.ToList();
        
        if (operationsList.Count == 0)
        {
            return default(T);
        }

        if (operationsList.Count == 1)
        {
            return await operationsList[0]();
        }

        _logger.LogInformation("Starting batch processing for {OperationName} with {Count} operations", 
            operationName, operationsList.Count);

        var semaphore = new SemaphoreSlim(batchOptions.MaxConcurrency);
        var tasks = new List<Task<T>>();

        foreach (var operation in operationsList)
        {
            tasks.Add(ExecuteBatchOperationAsync(operation, semaphore, batchOptions.ContinueOnError, cancellationToken));
        }

        var results = await Task.WhenAll(tasks);
        
        // Return the first successful result or throw if all failed
        var successfulResult = results.FirstOrDefault(r => !EqualityComparer<T>.Default.Equals(r, default(T)));
        return successfulResult;
    }

    /// <inheritdoc/>
    public IPerformanceTracker CreateTracker(string operationName)
    {
        return new PerformanceTracker(this, operationName);
    }

    /// <inheritdoc/>
    public async Task WarmupAsync(string operationName, Func<Task> warmupOperation)
    {
        try
        {
            _logger.LogInformation("Starting warmup for {OperationName}", operationName);
            
            var stopwatch = Stopwatch.StartNew();
            await warmupOperation();
            stopwatch.Stop();
            
            _logger.LogInformation("Completed warmup for {OperationName} in {Duration}ms", 
                operationName, stopwatch.ElapsedMilliseconds);
                
            // Record warmup metric
            RecordMetric($"warmup.{operationName}", stopwatch.Elapsed, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Warmup failed for {OperationName}", operationName);
            throw;
        }
    }

    /// <inheritdoc/>
    public PerformanceReport GetPerformanceReport(string? operationName = null)
    {
        var report = new PerformanceReport
        {
            GeneratedAt = DateTime.UtcNow,
            TotalOperations = _metrics.Values.Sum(m => m.CallCount),
            AverageResponseTime = TimeSpan.FromMilliseconds(
                _metrics.Values.Any() ? _metrics.Values.Average(m => m.AverageResponseTime.TotalMilliseconds) : 0),
            SuccessRate = _metrics.Values.Any() ? 
                _metrics.Values.Sum(m => m.SuccessCount) / (double)_metrics.Values.Sum(m => m.CallCount) : 1.0
        };

        var metricsToInclude = string.IsNullOrEmpty(operationName)
            ? _metrics.Values
            : _metrics.Values.Where(m => m.OperationName.Contains(operationName, StringComparison.OrdinalIgnoreCase));

        report.OperationMetrics = metricsToInclude
            .OrderByDescending(m => m.CallCount)
            .Take(50) // Limit to top 50 operations
            .ToList();

        // Calculate percentiles for overall performance
        var allResponseTimes = _metrics.Values
            .SelectMany(m => Enumerable.Repeat(m.AverageResponseTime.TotalMilliseconds, m.CallCount))
            .OrderBy(t => t)
            .ToArray();

        if (allResponseTimes.Length > 0)
        {
            report.P50ResponseTime = TimeSpan.FromMilliseconds(GetPercentile(allResponseTimes, 0.5));
            report.P95ResponseTime = TimeSpan.FromMilliseconds(GetPercentile(allResponseTimes, 0.95));
            report.P99ResponseTime = TimeSpan.FromMilliseconds(GetPercentile(allResponseTimes, 0.99));
        }

        return report;
    }

    /// <inheritdoc/>
    public async Task OptimizeMemoryUsageAsync()
    {
        _logger.LogInformation("Starting memory optimization");
        
        try
        {
            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Clean up old metrics
            CleanupOldMetrics();

            // Wait for GC to complete
            await Task.Delay(100);

            var memoryAfter = GC.GetTotalMemory(false);
            _logger.LogInformation("Memory optimization completed. Current memory usage: {MemoryUsage:N0} bytes", memoryAfter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Memory optimization failed");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Performance optimization service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Periodic optimization tasks
                await PerformPeriodicOptimization();
                
                // Wait for next cycle
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in performance optimization service");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        _logger.LogInformation("Performance optimization service stopped");
    }

    private async Task<T> ExecuteWithProfileAsync<T>(
        Func<Task<T>> operation,
        PerformanceProfile profile,
        CancellationToken cancellationToken)
    {
        return profile switch
        {
            PerformanceProfile.HighThroughput => await ExecuteHighThroughputAsync(operation, cancellationToken),
            PerformanceProfile.LowLatency => await ExecuteLowLatencyAsync(operation, cancellationToken),
            PerformanceProfile.Balanced => await operation(),
            PerformanceProfile.MemoryOptimized => await ExecuteMemoryOptimizedAsync(operation, cancellationToken),
            PerformanceProfile.ThrottleHeavy => await ExecuteThrottledAsync(operation, cancellationToken),
            _ => await operation()
        };
    }

    private async Task<T> ExecuteHighThroughputAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        // Configure for high throughput
        using var activity = new Activity("HighThroughputOperation");
        activity?.Start();

        try
        {
            // Disable synchronization context for better throughput
            return await operation().ConfigureAwait(false);
        }
        finally
        {
            activity?.Stop();
        }
    }

    private async Task<T> ExecuteLowLatencyAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        // Configure for low latency
        var originalPriority = Thread.CurrentThread.Priority;
        Thread.CurrentThread.Priority = ThreadPriority.Highest;

        try
        {
            return await operation().ConfigureAwait(false);
        }
        finally
        {
            Thread.CurrentThread.Priority = originalPriority;
        }
    }

    private async Task<T> ExecuteMemoryOptimizedAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        // Force GC before operation for cleaner memory state
        if (_options.EnableMemoryOptimization)
        {
            GC.Collect(0, GCCollectionMode.Optimized);
        }

        var result = await operation().ConfigureAwait(false);

        // Cleanup after operation if needed
        if (_options.EnableMemoryOptimization)
        {
            GC.Collect(0, GCCollectionMode.Optimized);
        }

        return result;
    }

    private async Task<T> ExecuteThrottledAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        // Add small delay to throttle execution
        await Task.Delay(10, cancellationToken);
        return await operation().ConfigureAwait(false);
    }

    private async Task<T> ExecuteBatchOperationAsync<T>(
        Func<Task<T>> operation,
        SemaphoreSlim semaphore,
        bool continueOnError,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            if (!continueOnError)
            {
                throw;
            }
            
            _logger.LogWarning(ex, "Batch operation failed, continuing with next operation");
            return default(T);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task ApplyThrottlingAsync(string operationName, CancellationToken cancellationToken)
    {
        var throttle = _throttledOperations.GetOrAdd(operationName, _ => new ThrottledOperation
        {
            LastExecution = DateTime.MinValue,
            MinInterval = TimeSpan.FromMilliseconds(_options.DefaultThrottleIntervalMs)
        });

        var timeSinceLastExecution = DateTime.UtcNow - throttle.LastExecution;
        if (timeSinceLastExecution < throttle.MinInterval)
        {
            var delay = throttle.MinInterval - timeSinceLastExecution;
            await Task.Delay(delay, cancellationToken);
        }

        throttle.LastExecution = DateTime.UtcNow;
    }

    private void RecordMetric(string operationName, TimeSpan duration, bool success)
    {
        var metric = _metrics.GetOrAdd(operationName, _ => new PerformanceMetric
        {
            OperationName = operationName,
            FirstRecorded = DateTime.UtcNow
        });

        Interlocked.Increment(ref metric.CallCount);
        
        if (success)
        {
            Interlocked.Increment(ref metric.SuccessCount);
        }

        // Update running average (simplified)
        metric.TotalResponseTime = metric.TotalResponseTime.Add(duration);
        metric.AverageResponseTime = TimeSpan.FromMilliseconds(
            metric.TotalResponseTime.TotalMilliseconds / metric.CallCount);

        metric.LastUpdated = DateTime.UtcNow;

        // Track min/max
        if (duration < metric.MinResponseTime || metric.MinResponseTime == TimeSpan.Zero)
        {
            metric.MinResponseTime = duration;
        }
        
        if (duration > metric.MaxResponseTime)
        {
            metric.MaxResponseTime = duration;
        }
    }

    private async Task PerformPeriodicOptimization()
    {
        try
        {
            // Memory optimization
            if (_options.EnableMemoryOptimization)
            {
                await OptimizeMemoryUsageAsync();
            }

            // Clean up old data
            CleanupOldMetrics();
            CleanupThrottledOperations();

            // Log performance summary
            LogPerformanceSummary();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Periodic optimization failed");
        }
    }

    private void CleanupOldMetrics()
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-24);
        var keysToRemove = _metrics.Where(kvp => kvp.Value.LastUpdated < cutoffTime).Select(kvp => kvp.Key).ToList();
        
        foreach (var key in keysToRemove)
        {
            _metrics.TryRemove(key, out _);
        }

        if (keysToRemove.Count > 0)
        {
            _logger.LogDebug("Cleaned up {Count} old performance metrics", keysToRemove.Count);
        }
    }

    private void CleanupThrottledOperations()
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-1);
        var keysToRemove = _throttledOperations
            .Where(kvp => kvp.Value.LastExecution < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _throttledOperations.TryRemove(key, out _);
        }
    }

    private void LogPerformanceSummary()
    {
        if (_metrics.Count == 0)
        {
            return;
        }

        var totalOperations = _metrics.Values.Sum(m => m.CallCount);
        var averageResponseTime = _metrics.Values.Average(m => m.AverageResponseTime.TotalMilliseconds);
        var successRate = _metrics.Values.Sum(m => m.SuccessCount) / (double)totalOperations;

        _logger.LogInformation(
            "Performance Summary - Operations: {TotalOperations}, Avg Response Time: {AvgResponseTime:F2}ms, Success Rate: {SuccessRate:P2}",
            totalOperations, averageResponseTime, successRate);
    }

    private void CleanupCallback(object? state)
    {
        try
        {
            CleanupOldMetrics();
            CleanupThrottledOperations();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cleanup callback failed");
        }
    }

    private static double GetPercentile(double[] sortedValues, double percentile)
    {
        if (sortedValues.Length == 0) return 0;
        
        var index = percentile * (sortedValues.Length - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);
        
        if (lower == upper)
        {
            return sortedValues[lower];
        }
        
        var weight = index - lower;
        return sortedValues[lower] * (1 - weight) + sortedValues[upper] * weight;
    }

    public override void Dispose()
    {
        _cleanupTimer?.Dispose();
        base.Dispose();
    }
}

/// <summary>
/// Performance tracker implementation.
/// </summary>
internal class PerformanceTracker : IPerformanceTracker
{
    private readonly PerformanceOptimizationService _service;
    private readonly string _operationName;
    private readonly Stopwatch _stopwatch;

    public PerformanceTracker(PerformanceOptimizationService service, string operationName)
    {
        _service = service;
        _operationName = operationName;
        _stopwatch = Stopwatch.StartNew();
    }

    public TimeSpan Elapsed => _stopwatch.Elapsed;

    public void RecordCheckpoint(string checkpointName)
    {
        _service.RecordMetric($"{_operationName}.{checkpointName}", _stopwatch.Elapsed, true);
    }

    public void Dispose()
    {
        _stopwatch.Stop();
        _service.RecordMetric(_operationName, _stopwatch.Elapsed, true);
    }
}

// Supporting classes and interfaces would be defined here or in separate files
public interface IPerformanceOptimizationService
{
    Task<T> OptimizeAsync<T>(string operationName, Func<Task<T>> operation, PerformanceProfile profile = PerformanceProfile.Balanced, CancellationToken cancellationToken = default);
    Task<T> BatchProcessAsync<T>(string operationName, IEnumerable<Func<Task<T>>> operations, BatchProcessingOptions? options = null, CancellationToken cancellationToken = default);
    IPerformanceTracker CreateTracker(string operationName);
    Task WarmupAsync(string operationName, Func<Task> warmupOperation);
    PerformanceReport GetPerformanceReport(string? operationName = null);
    Task OptimizeMemoryUsageAsync();
}

public interface IPerformanceTracker : IDisposable
{
    TimeSpan Elapsed { get; }
    void RecordCheckpoint(string checkpointName);
}

public enum PerformanceProfile
{
    Balanced,
    HighThroughput,
    LowLatency,
    MemoryOptimized,
    ThrottleHeavy
}

public class PerformanceOptions
{
    public bool EnableMemoryOptimization { get; set; } = true;
    public int DefaultThrottleIntervalMs { get; set; } = 100;
    public int MaxConcurrentOperations { get; set; } = Environment.ProcessorCount * 2;
}

public class BatchProcessingOptions
{
    public int MaxConcurrency { get; set; } = Environment.ProcessorCount;
    public bool ContinueOnError { get; set; } = true;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
}

public class PerformanceMetric
{
    public string OperationName { get; set; } = string.Empty;
    public int CallCount { get; set; }
    public int SuccessCount { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public TimeSpan MinResponseTime { get; set; }
    public TimeSpan MaxResponseTime { get; set; }
    public TimeSpan TotalResponseTime { get; set; }
    public DateTime FirstRecorded { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class PerformanceReport
{
    public DateTime GeneratedAt { get; set; }
    public int TotalOperations { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public TimeSpan P50ResponseTime { get; set; }
    public TimeSpan P95ResponseTime { get; set; }
    public TimeSpan P99ResponseTime { get; set; }
    public double SuccessRate { get; set; }
    public List<PerformanceMetric> OperationMetrics { get; set; } = new();
}

internal class ThrottledOperation
{
    public DateTime LastExecution { get; set; }
    public TimeSpan MinInterval { get; set; }
}