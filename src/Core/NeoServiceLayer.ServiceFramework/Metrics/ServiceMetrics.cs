using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace NeoServiceLayer.ServiceFramework.Metrics;

/// <summary>
/// Service metrics collection and reporting.
/// </summary>
public class ServiceMetrics
{
    private readonly Dictionary<string, long> _counters = new();
    private readonly Dictionary<string, double> _gauges = new();
    private readonly Dictionary<string, List<double>> _histograms = new();
    private readonly ReaderWriterLockSlim _lock = new();

    /// <summary>
    /// Increment a counter metric.
    /// </summary>
    public void IncrementCounter(string name, long value = 1, Dictionary<string, string>? labels = null)
    {
        var key = GetMetricKey(name, labels);
        _lock.EnterWriteLock();
        try
        {
            if (!_counters.ContainsKey(key))
                _counters[key] = 0;
            _counters[key] += value;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Set a gauge metric value.
    /// </summary>
    public void SetGauge(string name, double value, Dictionary<string, string>? labels = null)
    {
        var key = GetMetricKey(name, labels);
        _lock.EnterWriteLock();
        try
        {
            _gauges[key] = value;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Record a histogram value.
    /// </summary>
    public void RecordHistogram(string name, double value, Dictionary<string, string>? labels = null)
    {
        var key = GetMetricKey(name, labels);
        _lock.EnterWriteLock();
        try
        {
            if (!_histograms.ContainsKey(key))
                _histograms[key] = new List<double>();
            _histograms[key].Add(value);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Get metrics in Prometheus format.
    /// </summary>
    public string GetPrometheusMetrics()
    {
        var sb = new System.Text.StringBuilder();
        
        _lock.EnterReadLock();
        try
        {
            // Export counters
            foreach (var counter in _counters)
            {
                sb.AppendLine($"# TYPE {GetMetricName(counter.Key)} counter");
                sb.AppendLine($"{counter.Key} {counter.Value}");
            }

            // Export gauges
            foreach (var gauge in _gauges)
            {
                sb.AppendLine($"# TYPE {GetMetricName(gauge.Key)} gauge");
                sb.AppendLine($"{gauge.Key} {gauge.Value}");
            }

            // Export histograms
            foreach (var histogram in _histograms)
            {
                if (histogram.Value.Count == 0) continue;
                
                var name = GetMetricName(histogram.Key);
                var sorted = new List<double>(histogram.Value);
                sorted.Sort();
                
                sb.AppendLine($"# TYPE {name} histogram");
                sb.AppendLine($"{histogram.Key}_count {histogram.Value.Count}");
                sb.AppendLine($"{histogram.Key}_sum {histogram.Value.Sum()}");
                
                // Calculate percentiles
                sb.AppendLine($"{histogram.Key}_bucket{{le=\"0.5\"}} {GetPercentile(sorted, 0.5)}");
                sb.AppendLine($"{histogram.Key}_bucket{{le=\"0.9\"}} {GetPercentile(sorted, 0.9)}");
                sb.AppendLine($"{histogram.Key}_bucket{{le=\"0.95\"}} {GetPercentile(sorted, 0.95)}");
                sb.AppendLine($"{histogram.Key}_bucket{{le=\"0.99\"}} {GetPercentile(sorted, 0.99)}");
                sb.AppendLine($"{histogram.Key}_bucket{{le=\"+Inf\"}} {histogram.Value.Count}");
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }

        return sb.ToString();
    }

    private string GetMetricKey(string name, Dictionary<string, string>? labels)
    {
        if (labels == null || labels.Count == 0)
            return name;

        var labelStr = string.Join(",", labels.Select(kvp => $"{kvp.Key}=\"{kvp.Value}\""));
        return $"{name}{{{labelStr}}}";
    }

    private string GetMetricName(string key)
    {
        var idx = key.IndexOf('{');
        return idx > 0 ? key.Substring(0, idx) : key;
    }

    private double GetPercentile(List<double> sorted, double percentile)
    {
        var index = (int)Math.Ceiling(percentile * sorted.Count) - 1;
        return index >= 0 && index < sorted.Count ? sorted[index] : 0;
    }
}

/// <summary>
/// Extension methods for metrics collection.
/// </summary>
public static class MetricsExtensions
{
    private static readonly ServiceMetrics _metrics = new();

    public static ServiceMetrics GetMetrics() => _metrics;

    /// <summary>
    /// Measure execution time of an operation.
    /// </summary>
    public static T MeasureTime<T>(string metricName, Func<T> operation, Dictionary<string, string>? labels = null)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = operation();
            _metrics.RecordHistogram($"{metricName}_duration_ms", sw.ElapsedMilliseconds, labels);
            _metrics.IncrementCounter($"{metricName}_success_total", 1, labels);
            return result;
        }
        catch
        {
            _metrics.IncrementCounter($"{metricName}_error_total", 1, labels);
            throw;
        }
        finally
        {
            sw.Stop();
        }
    }

    /// <summary>
    /// Measure async execution time.
    /// </summary>
    public static async Task<T> MeasureTimeAsync<T>(string metricName, Func<Task<T>> operation, Dictionary<string, string>? labels = null)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await operation();
            _metrics.RecordHistogram($"{metricName}_duration_ms", sw.ElapsedMilliseconds, labels);
            _metrics.IncrementCounter($"{metricName}_success_total", 1, labels);
            return result;
        }
        catch
        {
            _metrics.IncrementCounter($"{metricName}_error_total", 1, labels);
            throw;
        }
        finally
        {
            sw.Stop();
        }
    }
}