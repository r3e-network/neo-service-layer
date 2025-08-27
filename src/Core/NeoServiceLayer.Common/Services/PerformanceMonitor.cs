using System.Collections.Concurrent;
using System.Diagnostics;

namespace NeoServiceLayer.Common.Services;

/// <summary>
/// Implementation of performance monitoring service
/// </summary>
public class PerformanceMonitor : IPerformanceMonitor
{
    private readonly ConcurrentDictionary<string, PerformanceTracker> _trackers = new();
    private readonly ConcurrentDictionary<string, long> _counters = new();

    /// <summary>
    /// Starts timing an operation
    /// </summary>
    /// <param name="operationName">Name of the operation being timed</param>
    /// <returns>A disposable that stops timing when disposed</returns>
    public IDisposable BeginOperation(string operationName)
    {
        return new PerformanceOperation(this, operationName);
    }

    /// <summary>
    /// Records a metric value
    /// </summary>
    /// <param name="metricName">Name of the metric</param>
    /// <param name="value">Value to record</param>
    /// <param name="tags">Optional tags for the metric</param>
    public void RecordValue(string metricName, double value, Dictionary<string, string>? tags = null)
    {
        var tracker = _trackers.GetOrAdd(metricName, _ => new PerformanceTracker());
        tracker.RecordExecution(value);
    }

    /// <summary>
    /// Increments a counter metric
    /// </summary>
    /// <param name="counterName">Name of the counter</param>
    /// <param name="tags">Optional tags for the counter</param>
    public void IncrementCounter(string counterName, Dictionary<string, string>? tags = null)
    {
        _counters.AddOrUpdate(counterName, 1, (_, current) => current + 1);
    }

    /// <summary>
    /// Gets performance statistics for an operation
    /// </summary>
    /// <param name="operationName">Name of the operation</param>
    /// <returns>Performance statistics</returns>
    public PerformanceStatistics? GetStatistics(string operationName)
    {
        return _trackers.TryGetValue(operationName, out var tracker) 
            ? tracker.GetStatistics() 
            : null;
    }

    /// <summary>
    /// Gets all recorded performance statistics
    /// </summary>
    /// <returns>Dictionary of operation names to their statistics</returns>
    public IReadOnlyDictionary<string, PerformanceStatistics> GetAllStatistics()
    {
        return _trackers.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.GetStatistics());
    }

    internal void CompleteOperation(string operationName, double executionTimeMs)
    {
        var tracker = _trackers.GetOrAdd(operationName, _ => new PerformanceTracker());
        tracker.RecordExecution(executionTimeMs);
    }

    private class PerformanceOperation : IDisposable
    {
        private readonly PerformanceMonitor _monitor;
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;
        private bool _disposed;

        public PerformanceOperation(PerformanceMonitor monitor, string operationName)
        {
            _monitor = monitor;
            _operationName = operationName;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            if (_disposed) return;
            
            _stopwatch.Stop();
            _monitor.CompleteOperation(_operationName, _stopwatch.Elapsed.TotalMilliseconds);
            _disposed = true;
        }
    }

    private class PerformanceTracker
    {
        private readonly object _lock = new();
        private long _executionCount;
        private double _totalExecutionTime;
        private double _minExecutionTime = double.MaxValue;
        private double _maxExecutionTime = double.MinValue;
        private double _lastExecutionTime;
        private DateTime _lastExecution;

        public void RecordExecution(double executionTimeMs)
        {
            lock (_lock)
            {
                _executionCount++;
                _totalExecutionTime += executionTimeMs;
                _lastExecutionTime = executionTimeMs;
                _lastExecution = DateTime.UtcNow;
                
                if (executionTimeMs < _minExecutionTime)
                    _minExecutionTime = executionTimeMs;
                    
                if (executionTimeMs > _maxExecutionTime)
                    _maxExecutionTime = executionTimeMs;
            }
        }

        public PerformanceStatistics GetStatistics()
        {
            lock (_lock)
            {
                return new PerformanceStatistics
                {
                    ExecutionCount = _executionCount,
                    AverageExecutionTime = _executionCount > 0 ? _totalExecutionTime / _executionCount : 0,
                    MinExecutionTime = _minExecutionTime == double.MaxValue ? 0 : _minExecutionTime,
                    MaxExecutionTime = _maxExecutionTime == double.MinValue ? 0 : _maxExecutionTime,
                    LastExecutionTime = _lastExecutionTime,
                    TotalExecutionTime = _totalExecutionTime,
                    LastExecution = _lastExecution
                };
            }
        }
    }
}