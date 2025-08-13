using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace NeoServiceLayer.Performance.Tests.RegressionTests
{
    /// <summary>
    /// Automated regression tests for performance validation.
    /// </summary>
    public class AutomatedRegressionTests : IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly PerformanceRegressionDetector _detector;
        private readonly ILogger<AutomatedRegressionTests> _logger;

        public AutomatedRegressionTests()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));

            _serviceProvider = services.BuildServiceProvider();
            _logger = _serviceProvider.GetRequiredService<ILogger<AutomatedRegressionTests>>();
            var detectorLogger = _serviceProvider.GetRequiredService<ILogger<PerformanceRegressionDetector>>();
            _detector = new PerformanceRegressionDetector(detectorLogger);
        }

        [Fact]
        public async Task PerformanceRegression_ShouldDetect_SignificantDeclines()
        {
            // Arrange
            var baselineResults = CreateBaselineResults();
            var regressionResults = CreateRegressionResults();

            // Update baseline
            await _detector.UpdateBaselineAsync(baselineResults);

            // Act
            var analysis = await _detector.AnalyzeRegressionAsync(regressionResults);

            // Assert
            analysis.Should().NotBeNull();
            analysis.OverallStatus.Should().Be(OverallStatus.Failed);
            analysis.Regressions.Should().NotBeEmpty();
            analysis.Regressions.Should().Contain(r => r.Severity == RegressionSeverity.Critical);
        }

        [Fact]
        public async Task PerformanceRegression_ShouldDetect_Improvements()
        {
            // Arrange
            var baselineResults = CreateBaselineResults();
            var improvedResults = CreateImprovedResults();

            // Update baseline
            await _detector.UpdateBaselineAsync(baselineResults);

            // Act
            var analysis = await _detector.AnalyzeRegressionAsync(improvedResults);

            // Assert
            analysis.Should().NotBeNull();
            analysis.Improvements.Should().NotBeEmpty();
            analysis.Improvements.Should().Contain(i => i.PercentageChange > 10.0);
        }

        [Fact]
        public async Task PerformanceRegression_ShouldPass_WithinThresholds()
        {
            // Arrange
            var baselineResults = CreateBaselineResults();
            var stableResults = CreateStableResults();

            // Update baseline
            await _detector.UpdateBaselineAsync(baselineResults);

            // Act
            var analysis = await _detector.AnalyzeRegressionAsync(stableResults);

            // Assert
            analysis.Should().NotBeNull();
            analysis.OverallStatus.Should().Be(OverallStatus.Passed);
            analysis.Regressions.Should().BeEmpty();
        }

        [Theory]
        [InlineData("MemoryCache_Set", 8.5, 15.0)] // Acceptable threshold
        [InlineData("MemoryCache_Get", 4.2, 10.0)] // Warning threshold
        public async Task PerformanceRegression_ShouldValidate_SpecificThresholds(
            string benchmarkName, double baselineMs, double maxAcceptableMs)
        {
            // Arrange
            var results = new PerformanceResults
            {
                Version = "test",
                Timestamp = DateTime.UtcNow,
                Benchmarks = new List<BenchmarkResult>
                {
                    new()
                    {
                        Name = $"SimpleCachingBenchmarks.{benchmarkName}",
                        AverageResponseTimeMs = maxAcceptableMs - 1.0, // Just under threshold
                        ThroughputPerSecond = 10000,
                        MemoryUsageMB = 1.0
                    }
                }
            };

            var baseline = new PerformanceResults
            {
                Version = "baseline",
                Timestamp = DateTime.UtcNow.AddDays(-1),
                Benchmarks = new List<BenchmarkResult>
                {
                    new()
                    {
                        Name = $"SimpleCachingBenchmarks.{benchmarkName}",
                        AverageResponseTimeMs = baselineMs,
                        ThroughputPerSecond = 10000,
                        MemoryUsageMB = 1.0
                    }
                }
            };

            await _detector.UpdateBaselineAsync(baseline);

            // Act
            var analysis = await _detector.AnalyzeRegressionAsync(results);

            // Assert
            var responseTimeRegression = analysis.Regressions
                .FirstOrDefault(r => r.BenchmarkName.Contains(benchmarkName) && r.MetricName == "ResponseTime");

            if (responseTimeRegression != null)
            {
                responseTimeRegression.Severity.Should().Be(RegressionSeverity.Warning);
                responseTimeRegression.PercentageChange.Should().BeLessThan(20.0); // Critical threshold
            }
        }

        [Fact]
        public async Task PerformanceRegression_ShouldHandle_MissingBaseline()
        {
            // Arrange
            var results = CreateBaselineResults();

            // Act & Assert
            var analysis = await _detector.AnalyzeRegressionAsync(results);
            analysis.Should().NotBeNull();
            analysis.OverallStatus.Should().Be(OverallStatus.Passed); // No comparison possible
        }

        [Fact]
        public async Task PerformanceRegression_ShouldAnalyze_MemoryUsage()
        {
            // Arrange
            var baselineResults = CreateBaselineResults();
            var memoryIntensiveResults = CreateMemoryIntensiveResults();

            await _detector.UpdateBaselineAsync(baselineResults);

            // Act
            var analysis = await _detector.AnalyzeRegressionAsync(memoryIntensiveResults);

            // Assert
            analysis.Regressions.Should().Contain(r =>
                r.MetricName == "MemoryUsage" &&
                r.Severity == RegressionSeverity.Critical);
        }

        [Fact]
        public async Task PerformanceRegression_ShouldAnalyze_ThroughputDecline()
        {
            // Arrange
            var baselineResults = CreateBaselineResults();
            var lowThroughputResults = CreateLowThroughputResults();

            await _detector.UpdateBaselineAsync(baselineResults);

            // Act
            var analysis = await _detector.AnalyzeRegressionAsync(lowThroughputResults);

            // Assert
            analysis.Regressions.Should().Contain(r =>
                r.MetricName == "Throughput" &&
                r.PercentageChange > 20.0);
        }

        private static PerformanceResults CreateBaselineResults()
        {
            return new PerformanceResults
            {
                Version = "1.0.0",
                Timestamp = DateTime.UtcNow.AddDays(-1),
                Benchmarks = new List<BenchmarkResult>
                {
                    new()
                    {
                        Name = "SimpleCachingBenchmarks.MemoryCache_Set",
                        AverageResponseTimeMs = 8.5,
                        ThroughputPerSecond = 11764,
                        MemoryUsageMB = 1.2,
                        CustomMetrics = new Dictionary<string, double> { ["HitRatio"] = 100.0 }
                    },
                    new()
                    {
                        Name = "SimpleCachingBenchmarks.MemoryCache_Get",
                        AverageResponseTimeMs = 4.2,
                        ThroughputPerSecond = 23810,
                        MemoryUsageMB = 0.045,
                        CustomMetrics = new Dictionary<string, double> { ["HitRatio"] = 100.0 }
                    }
                }
            };
        }

        private static PerformanceResults CreateRegressionResults()
        {
            return new PerformanceResults
            {
                Version = "1.1.0",
                Timestamp = DateTime.UtcNow,
                Benchmarks = new List<BenchmarkResult>
                {
                    new()
                    {
                        Name = "SimpleCachingBenchmarks.MemoryCache_Set",
                        AverageResponseTimeMs = 12.0, // 41% increase - critical
                        ThroughputPerSecond = 8333, // 29% decrease - critical
                        MemoryUsageMB = 1.8, // 50% increase - critical
                        CustomMetrics = new Dictionary<string, double> { ["HitRatio"] = 90.0 }
                    },
                    new()
                    {
                        Name = "SimpleCachingBenchmarks.MemoryCache_Get",
                        AverageResponseTimeMs = 5.5, // 31% increase - critical
                        ThroughputPerSecond = 18182, // 24% decrease - critical
                        MemoryUsageMB = 0.065, // 44% increase - critical
                        CustomMetrics = new Dictionary<string, double> { ["HitRatio"] = 85.0 }
                    }
                }
            };
        }

        private static PerformanceResults CreateImprovedResults()
        {
            return new PerformanceResults
            {
                Version = "1.1.0",
                Timestamp = DateTime.UtcNow,
                Benchmarks = new List<BenchmarkResult>
                {
                    new()
                    {
                        Name = "SimpleCachingBenchmarks.MemoryCache_Set",
                        AverageResponseTimeMs = 6.0, // 29% improvement
                        ThroughputPerSecond = 16667, // 42% improvement
                        MemoryUsageMB = 0.8, // 33% improvement
                        CustomMetrics = new Dictionary<string, double> { ["HitRatio"] = 100.0 }
                    },
                    new()
                    {
                        Name = "SimpleCachingBenchmarks.MemoryCache_Get",
                        AverageResponseTimeMs = 3.0, // 29% improvement
                        ThroughputPerSecond = 33333, // 40% improvement
                        MemoryUsageMB = 0.03, // 33% improvement
                        CustomMetrics = new Dictionary<string, double> { ["HitRatio"] = 100.0 }
                    }
                }
            };
        }

        private static PerformanceResults CreateStableResults()
        {
            return new PerformanceResults
            {
                Version = "1.1.0",
                Timestamp = DateTime.UtcNow,
                Benchmarks = new List<BenchmarkResult>
                {
                    new()
                    {
                        Name = "SimpleCachingBenchmarks.MemoryCache_Set",
                        AverageResponseTimeMs = 8.7, // 2% increase - acceptable
                        ThroughputPerSecond = 11500, // 2% decrease - acceptable
                        MemoryUsageMB = 1.25, // 4% increase - acceptable
                        CustomMetrics = new Dictionary<string, double> { ["HitRatio"] = 99.0 }
                    },
                    new()
                    {
                        Name = "SimpleCachingBenchmarks.MemoryCache_Get",
                        AverageResponseTimeMs = 4.1, // 2% improvement
                        ThroughputPerSecond = 24390, // 2% improvement
                        MemoryUsageMB = 0.044, // 2% improvement
                        CustomMetrics = new Dictionary<string, double> { ["HitRatio"] = 99.5 }
                    }
                }
            };
        }

        private static PerformanceResults CreateMemoryIntensiveResults()
        {
            return new PerformanceResults
            {
                Version = "1.1.0",
                Timestamp = DateTime.UtcNow,
                Benchmarks = new List<BenchmarkResult>
                {
                    new()
                    {
                        Name = "SimpleCachingBenchmarks.MemoryCache_Set",
                        AverageResponseTimeMs = 8.5,
                        ThroughputPerSecond = 11764,
                        MemoryUsageMB = 2.0, // 67% increase - critical
                        CustomMetrics = new Dictionary<string, double> { ["HitRatio"] = 100.0 }
                    }
                }
            };
        }

        private static PerformanceResults CreateLowThroughputResults()
        {
            return new PerformanceResults
            {
                Version = "1.1.0",
                Timestamp = DateTime.UtcNow,
                Benchmarks = new List<BenchmarkResult>
                {
                    new()
                    {
                        Name = "SimpleCachingBenchmarks.MemoryCache_Get",
                        AverageResponseTimeMs = 4.2,
                        ThroughputPerSecond = 17000, // 29% decrease - critical
                        MemoryUsageMB = 0.045,
                        CustomMetrics = new Dictionary<string, double> { ["HitRatio"] = 100.0 }
                    }
                }
            };
        }

        public void Dispose()
        {
            if (_serviceProvider is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
