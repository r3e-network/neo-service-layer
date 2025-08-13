using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Performance.Tests.RegressionTests
{
    /// <summary>
    /// Automated performance regression detection system.
    /// </summary>
    public class PerformanceRegressionDetector
    {
        private readonly ILogger<PerformanceRegressionDetector> _logger;
        private readonly string _baselineFilePath;
        private readonly PerformanceThresholds _thresholds;

        public PerformanceRegressionDetector(
            ILogger<PerformanceRegressionDetector> logger,
            string baselineFilePath = "baseline-metrics.json")
        {
            _logger = logger;
            _baselineFilePath = baselineFilePath;
            _thresholds = LoadThresholds();
        }

        /// <summary>
        /// Analyzes performance results against baseline metrics.
        /// </summary>
        /// <param name="currentResults">Current performance test results.</param>
        /// <returns>Regression analysis results.</returns>
        public async Task<RegressionAnalysisResult> AnalyzeRegressionAsync(PerformanceResults currentResults)
        {
            try
            {
                var baseline = await LoadBaselineMetricsAsync().ConfigureAwait(false);
                var regressions = new List<PerformanceRegression>();
                var improvements = new List<PerformanceImprovement>();

                // Analyze each metric category
                AnalyzeResponseTime(baseline, currentResults, regressions, improvements);
                AnalyzeThroughput(baseline, currentResults, regressions, improvements);
                AnalyzeMemoryUsage(baseline, currentResults, regressions, improvements);
                AnalyzeCachePerformance(baseline, currentResults, regressions, improvements);

                var result = new RegressionAnalysisResult
                {
                    Timestamp = DateTime.UtcNow,
                    BaselineVersion = baseline.Version,
                    CurrentVersion = currentResults.Version,
                    TotalTests = currentResults.Benchmarks.Count,
                    Regressions = regressions.ToArray(),
                    Improvements = improvements.ToArray(),
                    OverallStatus = DetermineOverallStatus(regressions)
                };

                _logger.LogInformation(
                    "Regression analysis completed: {RegressionCount} regressions, {ImprovementCount} improvements",
                    regressions.Count, improvements.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze performance regression");
                throw;
            }
        }

        /// <summary>
        /// Updates baseline metrics with new performance data.
        /// </summary>
        /// <param name="newResults">New performance results to set as baseline.</param>
        public async Task UpdateBaselineAsync(PerformanceResults newResults)
        {
            try
            {
                var baseline = new BaselineMetrics
                {
                    Version = newResults.Version,
                    Timestamp = DateTime.UtcNow,
                    Metrics = ConvertToBaselineMetrics(newResults)
                };

                var json = JsonSerializer.Serialize(baseline, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(_baselineFilePath, json).ConfigureAwait(false);

                _logger.LogInformation("Baseline metrics updated with version {Version}", newResults.Version);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update baseline metrics");
                throw;
            }
        }

        private void AnalyzeResponseTime(
            BaselineMetrics baseline,
            PerformanceResults current,
            List<PerformanceRegression> regressions,
            List<PerformanceImprovement> improvements)
        {
            foreach (var benchmark in current.Benchmarks)
            {
                if (!baseline.Metrics.TryGetValue(benchmark.Name, out var baselineMetric))
                    continue;

                var responseTimeChange = CalculatePercentageChange(
                    baselineMetric.AverageResponseTimeMs,
                    benchmark.AverageResponseTimeMs);

                if (responseTimeChange > _thresholds.ResponseTimeWarningThreshold)
                {
                    regressions.Add(new PerformanceRegression
                    {
                        BenchmarkName = benchmark.Name,
                        MetricName = "ResponseTime",
                        BaselineValue = baselineMetric.AverageResponseTimeMs,
                        CurrentValue = benchmark.AverageResponseTimeMs,
                        PercentageChange = responseTimeChange,
                        Severity = responseTimeChange > _thresholds.ResponseTimeCriticalThreshold
                            ? RegressionSeverity.Critical
                            : RegressionSeverity.Warning,
                        Description = $"Response time increased by {responseTimeChange:F1}%"
                    });
                }
                else if (responseTimeChange < -_thresholds.ImprovementThreshold)
                {
                    improvements.Add(new PerformanceImprovement
                    {
                        BenchmarkName = benchmark.Name,
                        MetricName = "ResponseTime",
                        BaselineValue = baselineMetric.AverageResponseTimeMs,
                        CurrentValue = benchmark.AverageResponseTimeMs,
                        PercentageChange = Math.Abs(responseTimeChange),
                        Description = $"Response time improved by {Math.Abs(responseTimeChange):F1}%"
                    });
                }
            }
        }

        private void AnalyzeThroughput(
            BaselineMetrics baseline,
            PerformanceResults current,
            List<PerformanceRegression> regressions,
            List<PerformanceImprovement> improvements)
        {
            foreach (var benchmark in current.Benchmarks)
            {
                if (!baseline.Metrics.TryGetValue(benchmark.Name, out var baselineMetric))
                    continue;

                var throughputChange = CalculatePercentageChange(
                    baselineMetric.ThroughputPerSecond,
                    benchmark.ThroughputPerSecond);

                if (throughputChange < -_thresholds.ThroughputWarningThreshold)
                {
                    regressions.Add(new PerformanceRegression
                    {
                        BenchmarkName = benchmark.Name,
                        MetricName = "Throughput",
                        BaselineValue = baselineMetric.ThroughputPerSecond,
                        CurrentValue = benchmark.ThroughputPerSecond,
                        PercentageChange = Math.Abs(throughputChange),
                        Severity = Math.Abs(throughputChange) > _thresholds.ThroughputCriticalThreshold
                            ? RegressionSeverity.Critical
                            : RegressionSeverity.Warning,
                        Description = $"Throughput decreased by {Math.Abs(throughputChange):F1}%"
                    });
                }
                else if (throughputChange > _thresholds.ImprovementThreshold)
                {
                    improvements.Add(new PerformanceImprovement
                    {
                        BenchmarkName = benchmark.Name,
                        MetricName = "Throughput",
                        BaselineValue = baselineMetric.ThroughputPerSecond,
                        CurrentValue = benchmark.ThroughputPerSecond,
                        PercentageChange = throughputChange,
                        Description = $"Throughput improved by {throughputChange:F1}%"
                    });
                }
            }
        }

        private void AnalyzeMemoryUsage(
            BaselineMetrics baseline,
            PerformanceResults current,
            List<PerformanceRegression> regressions,
            List<PerformanceImprovement> improvements)
        {
            foreach (var benchmark in current.Benchmarks)
            {
                if (!baseline.Metrics.TryGetValue(benchmark.Name, out var baselineMetric))
                    continue;

                var memoryChange = CalculatePercentageChange(
                    baselineMetric.MemoryUsageMB,
                    benchmark.MemoryUsageMB);

                if (memoryChange > _thresholds.MemoryWarningThreshold)
                {
                    regressions.Add(new PerformanceRegression
                    {
                        BenchmarkName = benchmark.Name,
                        MetricName = "MemoryUsage",
                        BaselineValue = baselineMetric.MemoryUsageMB,
                        CurrentValue = benchmark.MemoryUsageMB,
                        PercentageChange = memoryChange,
                        Severity = memoryChange > _thresholds.MemoryCriticalThreshold
                            ? RegressionSeverity.Critical
                            : RegressionSeverity.Warning,
                        Description = $"Memory usage increased by {memoryChange:F1}%"
                    });
                }
                else if (memoryChange < -_thresholds.ImprovementThreshold)
                {
                    improvements.Add(new PerformanceImprovement
                    {
                        BenchmarkName = benchmark.Name,
                        MetricName = "MemoryUsage",
                        BaselineValue = baselineMetric.MemoryUsageMB,
                        CurrentValue = benchmark.MemoryUsageMB,
                        PercentageChange = Math.Abs(memoryChange),
                        Description = $"Memory usage improved by {Math.Abs(memoryChange):F1}%"
                    });
                }
            }
        }

        private void AnalyzeCachePerformance(
            BaselineMetrics baseline,
            PerformanceResults current,
            List<PerformanceRegression> regressions,
            List<PerformanceImprovement> improvements)
        {
            // Analyze cache-specific metrics
            var cacheResults = current.Benchmarks.Where(b => b.Name.Contains("Cache")).ToList();

            foreach (var benchmark in cacheResults)
            {
                if (!baseline.Metrics.TryGetValue(benchmark.Name, out var baselineMetric))
                    continue;

                // Check hit ratio if available
                if (benchmark.CustomMetrics.TryGetValue("HitRatio", out var hitRatio) &&
                    baselineMetric.CustomMetrics.TryGetValue("HitRatio", out var baselineHitRatio))
                {
                    var hitRatioChange = CalculatePercentageChange(baselineHitRatio, hitRatio);

                    if (hitRatioChange < -_thresholds.CacheHitRatioThreshold)
                    {
                        regressions.Add(new PerformanceRegression
                        {
                            BenchmarkName = benchmark.Name,
                            MetricName = "CacheHitRatio",
                            BaselineValue = baselineHitRatio,
                            CurrentValue = hitRatio,
                            PercentageChange = Math.Abs(hitRatioChange),
                            Severity = RegressionSeverity.Warning,
                            Description = $"Cache hit ratio decreased by {Math.Abs(hitRatioChange):F1}%"
                        });
                    }
                }
            }
        }

        private static double CalculatePercentageChange(double baseline, double current)
        {
            if (baseline == 0) return current == 0 ? 0 : 100;
            return ((current - baseline) / baseline) * 100;
        }

        private static OverallStatus DetermineOverallStatus(List<PerformanceRegression> regressions)
        {
            if (!regressions.Any()) return OverallStatus.Passed;
            if (regressions.Any(r => r.Severity == RegressionSeverity.Critical)) return OverallStatus.Failed;
            return OverallStatus.Warning;
        }

        private async Task<BaselineMetrics> LoadBaselineMetricsAsync()
        {
            if (!File.Exists(_baselineFilePath))
            {
                _logger.LogWarning("Baseline metrics file not found: {FilePath}", _baselineFilePath);
                return new BaselineMetrics
                {
                    Version = "1.0.0",
                    Timestamp = DateTime.UtcNow,
                    Metrics = new Dictionary<string, PerformanceMetric>()
                };
            }

            var json = await File.ReadAllTextAsync(_baselineFilePath).ConfigureAwait(false);
            return JsonSerializer.Deserialize<BaselineMetrics>(json)
                ?? throw new InvalidOperationException("Failed to deserialize baseline metrics");
        }

        private static Dictionary<string, PerformanceMetric> ConvertToBaselineMetrics(PerformanceResults results)
        {
            return results.Benchmarks.ToDictionary(
                b => b.Name,
                b => new PerformanceMetric
                {
                    AverageResponseTimeMs = b.AverageResponseTimeMs,
                    ThroughputPerSecond = b.ThroughputPerSecond,
                    MemoryUsageMB = b.MemoryUsageMB,
                    CustomMetrics = new Dictionary<string, double>(b.CustomMetrics)
                });
        }

        private static PerformanceThresholds LoadThresholds()
        {
            return new PerformanceThresholds
            {
                ResponseTimeWarningThreshold = 10.0,   // 10% increase
                ResponseTimeCriticalThreshold = 20.0,  // 20% increase
                ThroughputWarningThreshold = 10.0,     // 10% decrease
                ThroughputCriticalThreshold = 20.0,    // 20% decrease
                MemoryWarningThreshold = 15.0,         // 15% increase
                MemoryCriticalThreshold = 30.0,        // 30% increase
                CacheHitRatioThreshold = 5.0,          // 5% decrease
                ImprovementThreshold = 5.0             // 5% improvement
            };
        }
    }

    /// <summary>
    /// Performance regression analysis results.
    /// </summary>
    public class RegressionAnalysisResult
    {
        public DateTime Timestamp { get; set; }
        public string BaselineVersion { get; set; } = string.Empty;
        public string CurrentVersion { get; set; } = string.Empty;
        public int TotalTests { get; set; }
        public PerformanceRegression[] Regressions { get; set; } = Array.Empty<PerformanceRegression>();
        public PerformanceImprovement[] Improvements { get; set; } = Array.Empty<PerformanceImprovement>();
        public OverallStatus OverallStatus { get; set; }
    }

    /// <summary>
    /// Performance regression details.
    /// </summary>
    public class PerformanceRegression
    {
        public string BenchmarkName { get; set; } = string.Empty;
        public string MetricName { get; set; } = string.Empty;
        public double BaselineValue { get; set; }
        public double CurrentValue { get; set; }
        public double PercentageChange { get; set; }
        public RegressionSeverity Severity { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Performance improvement details.
    /// </summary>
    public class PerformanceImprovement
    {
        public string BenchmarkName { get; set; } = string.Empty;
        public string MetricName { get; set; } = string.Empty;
        public double BaselineValue { get; set; }
        public double CurrentValue { get; set; }
        public double PercentageChange { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Baseline performance metrics storage.
    /// </summary>
    public class BaselineMetrics
    {
        public string Version { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Dictionary<string, PerformanceMetric> Metrics { get; set; } = new();
    }

    /// <summary>
    /// Individual performance metric data.
    /// </summary>
    public class PerformanceMetric
    {
        public double AverageResponseTimeMs { get; set; }
        public double ThroughputPerSecond { get; set; }
        public double MemoryUsageMB { get; set; }
        public Dictionary<string, double> CustomMetrics { get; set; } = new();
    }

    /// <summary>
    /// Performance test results structure.
    /// </summary>
    public class PerformanceResults
    {
        public string Version { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public List<BenchmarkResult> Benchmarks { get; set; } = new();
    }

    /// <summary>
    /// Individual benchmark result.
    /// </summary>
    public class BenchmarkResult
    {
        public string Name { get; set; } = string.Empty;
        public double AverageResponseTimeMs { get; set; }
        public double ThroughputPerSecond { get; set; }
        public double MemoryUsageMB { get; set; }
        public Dictionary<string, double> CustomMetrics { get; set; } = new();
    }

    /// <summary>
    /// Performance thresholds configuration.
    /// </summary>
    public class PerformanceThresholds
    {
        public double ResponseTimeWarningThreshold { get; set; }
        public double ResponseTimeCriticalThreshold { get; set; }
        public double ThroughputWarningThreshold { get; set; }
        public double ThroughputCriticalThreshold { get; set; }
        public double MemoryWarningThreshold { get; set; }
        public double MemoryCriticalThreshold { get; set; }
        public double CacheHitRatioThreshold { get; set; }
        public double ImprovementThreshold { get; set; }
    }

    /// <summary>
    /// Regression severity levels.
    /// </summary>
    public enum RegressionSeverity
    {
        Warning,
        Critical
    }

    /// <summary>
    /// Overall analysis status.
    /// </summary>
    public enum OverallStatus
    {
        Passed,
        Warning,
        Failed
    }
}
