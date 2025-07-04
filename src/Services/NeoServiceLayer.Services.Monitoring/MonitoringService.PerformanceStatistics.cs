using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Monitoring.Models;

namespace NeoServiceLayer.Services.Monitoring;

/// <summary>
/// Performance statistics operations for the Monitoring Service.
/// </summary>
public partial class MonitoringService
{
    /// <inheritdoc/>
    public async Task<PerformanceStatisticsResult> GetPerformanceStatisticsAsync(PerformanceStatisticsRequest request, BlockchainType blockchainType)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!SupportsBlockchain(blockchainType))
        {
            throw new NotSupportedException($"Blockchain {blockchainType} is not supported");
        }

        return await ExecuteAsync(async () =>
        {
            try
            {
                Logger.LogDebug("Getting performance statistics for time range {TimeRange}", request.TimeRange);

                var cutoffTime = DateTime.UtcNow - request.TimeRange;
                var servicePerformances = new List<ServicePerformance>();

                lock (_cacheLock)
                {
                    foreach (var serviceName in request.ServiceNames.Length > 0 ? request.ServiceNames : _metricsCache.Keys)
                    {
                        if (_metricsCache.TryGetValue(serviceName, out var metrics))
                        {
                            var recentMetrics = metrics.Where(m => m.Timestamp >= cutoffTime).ToArray();

                            var performance = new ServicePerformance
                            {
                                ServiceName = serviceName,
                                RequestsPerSecond = CalculateAverage(recentMetrics, "requests_per_second"),
                                AverageResponseTimeMs = CalculateAverage(recentMetrics, "response_time_ms"),
                                ErrorRatePercent = CalculateAverage(recentMetrics, "error_rate_percent"),
                                SuccessRatePercent = 100 - CalculateAverage(recentMetrics, "error_rate_percent"),
                                TotalRequests = (long)CalculateSum(recentMetrics, "requests_per_second") * (long)request.TimeRange.TotalSeconds,
                                Metadata = new Dictionary<string, object>
                                {
                                    ["metric_count"] = recentMetrics.Length,
                                    ["time_range"] = request.TimeRange.ToString()
                                }
                            };

                            servicePerformances.Add(performance);
                        }
                    }
                }

                var systemPerformance = new SystemPerformance
                {
                    CpuUsagePercent = await GetCurrentCpuUsageAsync(),
                    MemoryUsagePercent = await GetCurrentMemoryUsageAsync(),
                    RequestsPerSecond = servicePerformances.Sum(s => s.RequestsPerSecond),
                    AverageResponseTimeMs = servicePerformances.Any() ? servicePerformances.Average(s => s.AverageResponseTimeMs) : 0,
                    ErrorRatePercent = servicePerformances.Any() ? servicePerformances.Average(s => s.ErrorRatePercent) : 0,
                    Metadata = new Dictionary<string, object>
                    {
                        ["service_count"] = servicePerformances.Count,
                        ["time_range"] = request.TimeRange.ToString(),
                        ["measurement_time"] = DateTime.UtcNow
                    }
                };

                Logger.LogInformation("Performance statistics calculated for {ServiceCount} services over {TimeRange}",
                    servicePerformances.Count, request.TimeRange);

                return new PerformanceStatisticsResult
                {
                    SystemPerformance = systemPerformance,
                    ServicePerformances = servicePerformances.ToArray(),
                    TimeRange = request.TimeRange,
                    Success = true,
                    Metadata = new Dictionary<string, object>
                    {
                        ["total_services"] = servicePerformances.Count,
                        ["calculation_time"] = DateTime.UtcNow
                    }
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get performance statistics");

                return new PerformanceStatisticsResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    TimeRange = request.TimeRange
                };
            }
        });
    }

    /// <summary>
    /// Calculates performance trends over time.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <param name="timeRange">The time range to analyze.</param>
    /// <returns>Performance trend data.</returns>
    public async Task<Models.PerformanceTrend> CalculatePerformanceTrendAsync(string serviceName, TimeSpan timeRange)
    {
        ArgumentException.ThrowIfNullOrEmpty(serviceName);

        try
        {
            Logger.LogDebug("Calculating performance trend for service {ServiceName} over {TimeRange}",
                serviceName, timeRange);

            var cutoffTime = DateTime.UtcNow - timeRange;
            var metrics = new List<ServiceMetric>();

            lock (_cacheLock)
            {
                if (_metricsCache.TryGetValue(serviceName, out var serviceMetrics))
                {
                    metrics.AddRange(serviceMetrics.Where(m => m.Timestamp >= cutoffTime));
                }
            }

            if (metrics.Count == 0)
            {
                return new Models.PerformanceTrend
                {
                    ServiceName = serviceName,
                    TimeRange = timeRange,
                    TrendDirection = TrendDirection.Stable,
                    ConfidenceLevel = 0.0
                };
            }

            // Group metrics by time intervals
            var intervalMinutes = Math.Max(1, (int)(timeRange.TotalMinutes / 20)); // 20 data points max
            var groupedMetrics = metrics
                .GroupBy(m => new DateTime(m.Timestamp.Year, m.Timestamp.Month, m.Timestamp.Day,
                    m.Timestamp.Hour, m.Timestamp.Minute / intervalMinutes * intervalMinutes, 0))
                .OrderBy(g => g.Key)
                .ToArray();

            // Calculate trends for key metrics
            var responseTimes = groupedMetrics.Select(g => g.Where(m => m.Name == "response_time_ms").Average(m => m.Value)).ToArray();
            var errorRates = groupedMetrics.Select(g => g.Where(m => m.Name == "error_rate_percent").Average(m => m.Value)).ToArray();
            var requestRates = groupedMetrics.Select(g => g.Where(m => m.Name == "requests_per_second").Average(m => m.Value)).ToArray();

            var responseTimeTrend = CalculateTrendDirection(responseTimes);
            var errorRateTrend = CalculateTrendDirection(errorRates);
            var requestRateTrend = CalculateTrendDirection(requestRates);

            // Determine overall trend
            var overallTrend = DetermineOverallTrend(responseTimeTrend, errorRateTrend, requestRateTrend);

            return new Models.PerformanceTrend
            {
                ServiceName = serviceName,
                TimeRange = timeRange,
                TrendDirection = overallTrend,
                ResponseTimeTrend = responseTimeTrend,
                ErrorRateTrend = errorRateTrend,
                RequestRateTrend = requestRateTrend,
                ConfidenceLevel = CalculateConfidenceLevel(groupedMetrics.Length),
                DataPoints = groupedMetrics.Length,
                AnalyzedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to calculate performance trend for service {ServiceName}", serviceName);

            return new Models.PerformanceTrend
            {
                ServiceName = serviceName,
                TimeRange = timeRange,
                TrendDirection = TrendDirection.Unknown,
                ConfidenceLevel = 0.0,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Calculates trend direction from a series of values.
    /// </summary>
    /// <param name="values">The values to analyze.</param>
    /// <returns>The trend direction.</returns>
    private TrendDirection CalculateTrendDirection(double[] values)
    {
        if (values.Length < 2)
            return TrendDirection.Stable;

        var increases = 0;
        var decreases = 0;

        for (int i = 1; i < values.Length; i++)
        {
            var change = values[i] - values[i - 1];
            var changePercent = Math.Abs(change) / Math.Max(values[i - 1], 0.001) * 100;

            if (changePercent > 5) // Significant change threshold
            {
                if (change > 0)
                    increases++;
                else
                    decreases++;
            }
        }

        if (increases > decreases * 1.5)
            return TrendDirection.Increasing;
        if (decreases > increases * 1.5)
            return TrendDirection.Decreasing;

        return TrendDirection.Stable;
    }

    /// <summary>
    /// Determines overall trend from individual metric trends.
    /// </summary>
    /// <param name="responseTimeTrend">Response time trend.</param>
    /// <param name="errorRateTrend">Error rate trend.</param>
    /// <param name="requestRateTrend">Request rate trend.</param>
    /// <returns>Overall trend direction.</returns>
    private TrendDirection DetermineOverallTrend(TrendDirection responseTimeTrend, TrendDirection errorRateTrend, TrendDirection requestRateTrend)
    {
        // Increasing response time or error rate is bad
        if (responseTimeTrend == TrendDirection.Increasing || errorRateTrend == TrendDirection.Increasing)
            return TrendDirection.Degrading;

        // Decreasing response time and error rate is good
        if (responseTimeTrend == TrendDirection.Decreasing && errorRateTrend == TrendDirection.Decreasing)
            return TrendDirection.Improving;

        // Increasing request rate with stable performance is good
        if (requestRateTrend == TrendDirection.Increasing &&
            responseTimeTrend == TrendDirection.Stable &&
            errorRateTrend == TrendDirection.Stable)
            return TrendDirection.Improving;

        return TrendDirection.Stable;
    }

    /// <summary>
    /// Calculates confidence level based on data points.
    /// </summary>
    /// <param name="dataPoints">Number of data points.</param>
    /// <returns>Confidence level between 0 and 1.</returns>
    private double CalculateConfidenceLevel(int dataPoints)
    {
        return dataPoints switch
        {
            >= 20 => 0.95,
            >= 15 => 0.85,
            >= 10 => 0.75,
            >= 5 => 0.60,
            >= 3 => 0.40,
            _ => 0.20
        };
    }

    /// <summary>
    /// Gets performance summary for all services.
    /// </summary>
    /// <returns>Performance summary.</returns>
    public async Task<PerformanceSummary> GetPerformanceSummaryAsync()
    {
        try
        {
            var serviceCount = 0;
            var totalRequests = 0.0;
            var totalResponseTime = 0.0;
            var totalErrorRate = 0.0;
            var healthyServices = 0;

            lock (_cacheLock)
            {
                serviceCount = _metricsCache.Count;

                foreach (var serviceMetrics in _metricsCache.Values)
                {
                    var recentMetrics = serviceMetrics.Where(m => m.Timestamp >= DateTime.UtcNow.AddHours(-1)).ToArray();

                    if (recentMetrics.Length > 0)
                    {
                        totalRequests += CalculateAverage(recentMetrics, "requests_per_second");
                        totalResponseTime += CalculateAverage(recentMetrics, "response_time_ms");
                        var errorRate = CalculateAverage(recentMetrics, "error_rate_percent");
                        totalErrorRate += errorRate;

                        if (errorRate < 5.0) // Less than 5% error rate considered healthy
                            healthyServices++;
                    }
                }
            }

            return new PerformanceSummary
            {
                TotalServices = serviceCount,
                HealthyServices = healthyServices,
                TotalRequestsPerSecond = totalRequests,
                AverageResponseTimeMs = serviceCount > 0 ? totalResponseTime / serviceCount : 0,
                AverageErrorRatePercent = serviceCount > 0 ? totalErrorRate / serviceCount : 0,
                OverallHealthPercentage = serviceCount > 0 ? (double)healthyServices / serviceCount * 100 : 0,
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to generate performance summary");

            return new PerformanceSummary
            {
                GeneratedAt = DateTime.UtcNow,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Gets the current CPU usage percentage.
    /// </summary>
    /// <returns>CPU usage percentage (0-100).</returns>
    private async Task<double> GetCurrentCpuUsageAsync()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return await GetWindowsCpuUsageAsync();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return await GetLinuxCpuUsageAsync();
            }
            else
            {
                // Fallback for other platforms
                return await GetProcessCpuUsageAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to get CPU usage");
            return 0.0;
        }
    }

    /// <summary>
    /// Gets the current memory usage percentage.
    /// </summary>
    /// <returns>Memory usage percentage (0-100).</returns>
    private async Task<double> GetCurrentMemoryUsageAsync()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return await GetWindowsMemoryUsageAsync();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return await GetLinuxMemoryUsageAsync();
            }
            else
            {
                // Fallback for other platforms
                return await GetProcessMemoryUsageAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to get memory usage");
            return 0.0;
        }
    }

    /// <summary>
    /// Gets CPU usage on Windows.
    /// </summary>
    private async Task<double> GetWindowsCpuUsageAsync()
    {
        try
        {
            // Use process CPU time calculation as the primary method on Windows
            return await GetProcessCpuUsageAsync();
        }
        catch
        {
            return 0.0;
        }
    }

    /// <summary>
    /// Gets CPU usage on Linux by reading /proc/stat.
    /// </summary>
    private async Task<double> GetLinuxCpuUsageAsync()
    {
        try
        {
            if (!File.Exists("/proc/stat"))
            {
                return await GetProcessCpuUsageAsync();
            }

            // Read CPU stats twice with a delay to calculate usage
            var (idle1, total1) = await ReadCpuStatsAsync();
            await Task.Delay(100);
            var (idle2, total2) = await ReadCpuStatsAsync();

            var idleDiff = idle2 - idle1;
            var totalDiff = total2 - total1;

            if (totalDiff == 0)
            {
                return await GetProcessCpuUsageAsync();
            }

            var cpuUsage = (1.0 - (double)idleDiff / totalDiff) * 100.0;
            return Math.Min(100.0, Math.Max(0.0, cpuUsage));
        }
        catch
        {
            return await GetProcessCpuUsageAsync();
        }
    }

    /// <summary>
    /// Reads CPU statistics from /proc/stat.
    /// </summary>
    private async Task<(long idle, long total)> ReadCpuStatsAsync()
    {
        var lines = await File.ReadAllLinesAsync("/proc/stat");
        var cpuLine = lines.FirstOrDefault(line => line.StartsWith("cpu "));

        if (cpuLine == null)
        {
            return (0, 0);
        }

        var values = cpuLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (values.Length < 8)
        {
            return (0, 0);
        }

        // Parse CPU time values: user, nice, system, idle, iowait, irq, softirq, steal
        var cpuTimes = values.Skip(1).Take(7).Select(long.Parse).ToArray();
        var total = cpuTimes.Sum();
        var idle = cpuTimes[3]; // idle time is the 4th value

        return (idle, total);
    }

    /// <summary>
    /// Gets process-specific CPU usage as a fallback.
    /// </summary>
    private async Task<double> GetProcessCpuUsageAsync()
    {
        try
        {
            using var currentProcess = Process.GetCurrentProcess();
            var startTime = DateTime.UtcNow;
            var startCpuUsage = currentProcess.TotalProcessorTime;

            await Task.Delay(100);

            currentProcess.Refresh();
            var endTime = DateTime.UtcNow;
            var endCpuUsage = currentProcess.TotalProcessorTime;

            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

            return Math.Min(100.0, Math.Max(0.0, cpuUsageTotal * 100.0));
        }
        catch
        {
            return 0.0;
        }
    }

    /// <summary>
    /// Gets memory usage on Windows.
    /// </summary>
    private async Task<double> GetWindowsMemoryUsageAsync()
    {
        try
        {
            // Use GC memory pressure and process working set as indicators
            var gcMemory = GC.GetTotalMemory(false);
            var workingSet = Environment.WorkingSet;

            // Estimate system memory usage based on process characteristics
            // This is a simplified approach using available .NET APIs
            var estimatedUsage = Math.Min(100.0, (double)workingSet / (1024 * 1024 * 1024) * 100); // As percentage of 1GB

            await Task.CompletedTask;
            return Math.Max(10.0, estimatedUsage); // Minimum 10% to show some usage
        }
        catch
        {
            return await GetProcessMemoryUsageAsync();
        }
    }

    /// <summary>
    /// Gets memory usage on Linux by reading /proc/meminfo.
    /// </summary>
    private async Task<double> GetLinuxMemoryUsageAsync()
    {
        try
        {
            if (!File.Exists("/proc/meminfo"))
            {
                return await GetProcessMemoryUsageAsync();
            }

            var lines = await File.ReadAllLinesAsync("/proc/meminfo");
            var memInfo = new Dictionary<string, long>();

            foreach (var line in lines)
            {
                var parts = line.Split(':', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    var key = parts[0].Trim();
                    var valueStr = parts[1].Trim().Replace(" kB", "");
                    if (long.TryParse(valueStr, out var value))
                    {
                        memInfo[key] = value;
                    }
                }
            }

            var totalKB = memInfo.GetValueOrDefault("MemTotal", 0);
            var availableKB = memInfo.GetValueOrDefault("MemAvailable", memInfo.GetValueOrDefault("MemFree", 0));

            if (totalKB == 0)
            {
                return await GetProcessMemoryUsageAsync();
            }

            var usedKB = totalKB - availableKB;
            var memoryUsage = (double)usedKB / totalKB * 100.0;

            return Math.Min(100.0, Math.Max(0.0, memoryUsage));
        }
        catch
        {
            return await GetProcessMemoryUsageAsync();
        }
    }

    /// <summary>
    /// Gets process-specific memory usage as a fallback.
    /// </summary>
    private async Task<double> GetProcessMemoryUsageAsync()
    {
        try
        {
            using var currentProcess = Process.GetCurrentProcess();
            var workingSetMB = currentProcess.WorkingSet64 / (1024.0 * 1024.0);

            // Estimate system memory and calculate percentage
            // This is a very rough estimate for fallback purposes
            var estimatedSystemMemoryMB = Math.Max(workingSetMB * 4, 1024); // At least 1GB
            var memoryUsage = (workingSetMB / estimatedSystemMemoryMB) * 100.0;

            await Task.CompletedTask;
            return Math.Min(100.0, Math.Max(0.0, memoryUsage));
        }
        catch
        {
            return 0.0;
        }
    }
}
