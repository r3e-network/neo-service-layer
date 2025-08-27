using System.Diagnostics;

namespace NeoServiceLayer.Common.Services;

/// <summary>
/// Service for monitoring application performance metrics
/// </summary>
public interface IPerformanceMonitor
{
    /// <summary>
    /// Starts timing an operation
    /// </summary>
    /// <param name="operationName">Name of the operation being timed</param>
    /// <returns>A disposable that stops timing when disposed</returns>
    IDisposable BeginOperation(string operationName);

    /// <summary>
    /// Records a metric value
    /// </summary>
    /// <param name="metricName">Name of the metric</param>
    /// <param name="value">Value to record</param>
    /// <param name="tags">Optional tags for the metric</param>
    void RecordValue(string metricName, double value, Dictionary<string, string>? tags = null);

    /// <summary>
    /// Increments a counter metric
    /// </summary>
    /// <param name="counterName">Name of the counter</param>
    /// <param name="tags">Optional tags for the counter</param>
    void IncrementCounter(string counterName, Dictionary<string, string>? tags = null);

    /// <summary>
    /// Gets performance statistics for an operation
    /// </summary>
    /// <param name="operationName">Name of the operation</param>
    /// <returns>Performance statistics</returns>
    PerformanceStatistics? GetStatistics(string operationName);

    /// <summary>
    /// Gets all recorded performance statistics
    /// </summary>
    /// <returns>Dictionary of operation names to their statistics</returns>
    IReadOnlyDictionary<string, PerformanceStatistics> GetAllStatistics();
}

/// <summary>
/// Performance statistics for an operation
/// </summary>
public class PerformanceStatistics
{
    /// <summary>
    /// Total number of executions
    /// </summary>
    public long ExecutionCount { get; set; }

    /// <summary>
    /// Average execution time in milliseconds
    /// </summary>
    public double AverageExecutionTime { get; set; }

    /// <summary>
    /// Minimum execution time in milliseconds
    /// </summary>
    public double MinExecutionTime { get; set; }

    /// <summary>
    /// Maximum execution time in milliseconds
    /// </summary>
    public double MaxExecutionTime { get; set; }

    /// <summary>
    /// Last execution time in milliseconds
    /// </summary>
    public double LastExecutionTime { get; set; }

    /// <summary>
    /// Total execution time in milliseconds
    /// </summary>
    public double TotalExecutionTime { get; set; }

    /// <summary>
    /// Last execution timestamp
    /// </summary>
    public DateTime LastExecution { get; set; }
}