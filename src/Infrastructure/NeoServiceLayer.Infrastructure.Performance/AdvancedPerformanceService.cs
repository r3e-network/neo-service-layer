using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.GCMemoryInfo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace NeoServiceLayer.Infrastructure.Performance;

public interface IAdvancedPerformanceService
{
    Task<PerformanceSnapshot> CaptureSnapshotAsync(CancellationToken cancellationToken = default);
    Task<PerformanceRecommendation[]> AnalyzePerformanceAsync(CancellationToken cancellationToken = default);
    Task OptimizeAsync(PerformanceOptimizationRequest request, CancellationToken cancellationToken = default);
    Task<PerformanceTrend> GetTrendAnalysisAsync(TimeSpan period, CancellationToken cancellationToken = default);
}

public class AdvancedPerformanceService : BackgroundService, IAdvancedPerformanceService
{
    private readonly ILogger<AdvancedPerformanceService> _logger;
    private readonly PerformanceOptions _options;
    private readonly ConcurrentQueue<PerformanceSnapshot> _snapshots;
    private readonly ConcurrentDictionary<string, PerformanceCounter> _counters;
    private readonly Timer _snapshotTimer;

    public AdvancedPerformanceService(
        ILogger<AdvancedPerformanceService> logger,
        IOptions<PerformanceOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _snapshots = new ConcurrentQueue<PerformanceSnapshot>();
        _counters = new ConcurrentDictionary<string, PerformanceCounter>();
        
        _snapshotTimer = new Timer(
            async _ => await CaptureSnapshotAsync(),
            null,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(30));
    }

    public async Task<PerformanceSnapshot> CaptureSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var process = Process.GetCurrentProcess();
        var gcInfo = GC.GetGCMemoryInfo();
        
        var snapshot = new PerformanceSnapshot
        {
            Timestamp = DateTime.UtcNow,
            CpuUsage = await GetCpuUsageAsync(process),
            MemoryUsage = new MemoryUsage
            {
                WorkingSet = process.WorkingSet64,
                PrivateMemory = process.PrivateMemorySize64,
                VirtualMemory = process.VirtualMemorySize64,
                GCTotalMemory = GC.GetTotalMemory(false),
                GCMemoryInfo = new GCMemoryInfo
                {
                    HeapSizeBytes = gcInfo.HeapSizeBytes,
                    MemoryLoadBytes = gcInfo.MemoryLoadBytes,
                    TotalAvailableMemoryBytes = gcInfo.TotalAvailableMemoryBytes,
                    HighMemoryLoadThresholdBytes = gcInfo.HighMemoryLoadThresholdBytes,
                    FragmentedBytes = gcInfo.FragmentedBytes
                }
            },
            ThreadUsage = new ThreadUsage
            {
                ThreadCount = process.Threads.Count,
                ThreadPoolWorkerThreads = ThreadPool.ThreadCount,
                ThreadPoolCompletionPortThreads = ThreadPool.CompletedWorkItemCount
            },
            GCStatistics = new GCStatistics
            {
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2),
                TotalPauseTime = GC.GetTotalPauseDuration(),
                IsServerGC = GCSettings.IsServerGC,
                LatencyMode = GCSettings.LatencyMode.ToString()
            },
            HandleCount = process.HandleCount,
            Counters = _counters.Values.Select(c => new CounterSnapshot
            {
                Name = c.Name,
                Value = c.Value,
                Rate = c.Rate,
                LastUpdated = c.LastUpdated
            }).ToArray()
        };

        // Keep only last 1000 snapshots
        _snapshots.Enqueue(snapshot);
        while (_snapshots.Count > 1000)
        {
            _snapshots.TryDequeue(out _);
        }

        return snapshot;
    }

    public async Task<PerformanceRecommendation[]> AnalyzePerformanceAsync(CancellationToken cancellationToken = default)
    {
        var recommendations = new List<PerformanceRecommendation>();
        var recentSnapshots = _snapshots.TakeLast(30).ToArray();
        
        if (recentSnapshots.Length < 5)
        {
            return recommendations.ToArray();
        }

        // Memory Analysis
        var avgMemoryUsage = recentSnapshots.Average(s => s.MemoryUsage.WorkingSet);
        var maxMemoryUsage = recentSnapshots.Max(s => s.MemoryUsage.WorkingSet);
        var gcPressure = recentSnapshots.Average(s => s.GCStatistics.Gen2Collections);

        if (maxMemoryUsage > _options.MemoryThresholds.Critical)
        {
            recommendations.Add(new PerformanceRecommendation
            {
                Category = "Memory",
                Priority = RecommendationPriority.Critical,
                Title = "High Memory Usage Detected",
                Description = $"Memory usage reached {maxMemoryUsage / 1024 / 1024:F0}MB, exceeding critical threshold",
                Actions = new[]
                {
                    "Review memory-intensive operations",
                    "Implement object pooling for frequently allocated objects",
                    "Consider increasing available memory",
                    "Review garbage collection settings"
                },
                Impact = "High memory usage can lead to increased GC pressure and application instability"
            });
        }

        // CPU Analysis
        var avgCpuUsage = recentSnapshots.Average(s => s.CpuUsage);
        if (avgCpuUsage > _options.CpuThresholds.Warning)
        {
            recommendations.Add(new PerformanceRecommendation
            {
                Category = "CPU",
                Priority = avgCpuUsage > _options.CpuThresholds.Critical ? RecommendationPriority.Critical : RecommendationPriority.Warning,
                Title = "High CPU Usage",
                Description = $"Average CPU usage is {avgCpuUsage:P2}",
                Actions = new[]
                {
                    "Profile CPU-intensive operations",
                    "Optimize algorithms and data structures",
                    "Consider horizontal scaling",
                    "Review synchronous operations that could be async"
                },
                Impact = "High CPU usage can lead to increased response times and reduced throughput"
            });
        }

        // GC Analysis
        var gen2Rate = CalculateGen2CollectionRate(recentSnapshots);
        if (gen2Rate > _options.GCThresholds.Gen2CollectionRate)
        {
            recommendations.Add(new PerformanceRecommendation
            {
                Category = "GarbageCollection",
                Priority = RecommendationPriority.Warning,
                Title = "High Gen2 GC Frequency",
                Description = $"Gen2 collections occurring at {gen2Rate:F2} per minute",
                Actions = new[]
                {
                    "Review object allocation patterns",
                    "Implement object pooling for large objects",
                    "Consider using ArrayPool<T> for temporary arrays",
                    "Review long-lived object references"
                },
                Impact = "Frequent Gen2 collections can cause significant application pauses"
            });
        }

        // Thread Analysis
        var avgThreadCount = recentSnapshots.Average(s => s.ThreadUsage.ThreadCount);
        if (avgThreadCount > _options.ThreadThresholds.Warning)
        {
            recommendations.Add(new PerformanceRecommendation
            {
                Category = "Threading",
                Priority = RecommendationPriority.Warning,
                Title = "High Thread Count",
                Description = $"Average thread count is {avgThreadCount:F0}",
                Actions = new[]
                {
                    "Review thread creation patterns",
                    "Use async/await instead of blocking threads",
                    "Consider using SemaphoreSlim for concurrency control",
                    "Review ThreadPool settings"
                },
                Impact = "Excessive threads can lead to context switching overhead and memory pressure"
            });
        }

        return recommendations.ToArray();
    }

    public async Task OptimizeAsync(PerformanceOptimizationRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting performance optimization with profile: {Profile}", request.Profile);

        switch (request.Profile)
        {
            case OptimizationProfile.Memory:
                await OptimizeMemoryAsync(request, cancellationToken);
                break;
            case OptimizationProfile.CPU:
                await OptimizeCpuAsync(request, cancellationToken);
                break;
            case OptimizationProfile.Throughput:
                await OptimizeThroughputAsync(request, cancellationToken);
                break;
            case OptimizationProfile.Latency:
                await OptimizeLatencyAsync(request, cancellationToken);
                break;
            case OptimizationProfile.Balanced:
            default:
                await OptimizeBalancedAsync(request, cancellationToken);
                break;
        }

        _logger.LogInformation("Performance optimization completed");
    }

    public async Task<PerformanceTrend> GetTrendAnalysisAsync(TimeSpan period, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.Subtract(period);
        var snapshots = _snapshots.Where(s => s.Timestamp >= cutoff).ToArray();

        if (snapshots.Length < 2)
        {
            return new PerformanceTrend { Period = period, DataPoints = 0 };
        }

        var trend = new PerformanceTrend
        {
            Period = period,
            DataPoints = snapshots.Length,
            MemoryTrend = CalculateLinearTrend(snapshots.Select(s => (double)s.MemoryUsage.WorkingSet).ToArray()),
            CpuTrend = CalculateLinearTrend(snapshots.Select(s => s.CpuUsage).ToArray()),
            ThreadCountTrend = CalculateLinearTrend(snapshots.Select(s => (double)s.ThreadUsage.ThreadCount).ToArray()),
            GCTrend = CalculateLinearTrend(snapshots.Select(s => (double)s.GCStatistics.Gen2Collections).ToArray()),
            StartTime = snapshots.First().Timestamp,
            EndTime = snapshots.Last().Timestamp
        };

        return trend;
    }

    private async Task<double> GetCpuUsageAsync(Process process)
    {
        var startTime = DateTime.UtcNow;
        var startCpuUsage = process.TotalProcessorTime;
        
        await Task.Delay(100);
        
        var endTime = DateTime.UtcNow;
        var endCpuUsage = process.TotalProcessorTime;
        
        var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
        var totalMsPassed = (endTime - startTime).TotalMilliseconds;
        var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
        
        return Math.Max(0, Math.Min(1, cpuUsageTotal));
    }

    private double CalculateGen2CollectionRate(PerformanceSnapshot[] snapshots)
    {
        if (snapshots.Length < 2) return 0;

        var first = snapshots.First();
        var last = snapshots.Last();
        var timeDiff = (last.Timestamp - first.Timestamp).TotalMinutes;
        var collectionDiff = last.GCStatistics.Gen2Collections - first.GCStatistics.Gen2Collections;

        return timeDiff > 0 ? collectionDiff / timeDiff : 0;
    }

    private double CalculateLinearTrend(double[] values)
    {
        if (values.Length < 2) return 0;

        var n = values.Length;
        var sumX = 0.0;
        var sumY = 0.0;
        var sumXY = 0.0;
        var sumX2 = 0.0;

        for (int i = 0; i < n; i++)
        {
            sumX += i;
            sumY += values[i];
            sumXY += i * values[i];
            sumX2 += i * i;
        }

        var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        return slope;
    }

    private async Task OptimizeMemoryAsync(PerformanceOptimizationRequest request, CancellationToken cancellationToken)
    {
        // Force garbage collection
        if (request.AggressiveOptimization)
        {
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, true, true);
        }
        else
        {
            GC.Collect(0, GCCollectionMode.Optimized);
        }

        // Compact Large Object Heap if available
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        
        _logger.LogInformation("Memory optimization completed");
    }

    private async Task OptimizeCpuAsync(PerformanceOptimizationRequest request, CancellationToken cancellationToken)
    {
        // Adjust thread pool settings for CPU optimization
        int workerThreads, completionPortThreads;
        ThreadPool.GetMaxThreads(out workerThreads, out completionPortThreads);
        
        var optimalWorkerThreads = Environment.ProcessorCount * 2;
        ThreadPool.SetMaxThreads(optimalWorkerThreads, completionPortThreads);
        
        _logger.LogInformation("CPU optimization completed - ThreadPool max threads set to {MaxThreads}", optimalWorkerThreads);
    }

    private async Task OptimizeThroughputAsync(PerformanceOptimizationRequest request, CancellationToken cancellationToken)
    {
        // Configure GC for throughput
        if (request.AggressiveOptimization)
        {
            GCSettings.LatencyMode = GCLatencyMode.Batch;
        }

        // Optimize thread pool for throughput
        int workerThreads, completionPortThreads;
        ThreadPool.GetMaxThreads(out workerThreads, out completionPortThreads);
        ThreadPool.SetMaxThreads(Environment.ProcessorCount * 4, completionPortThreads * 2);
        
        _logger.LogInformation("Throughput optimization completed");
    }

    private async Task OptimizeLatencyAsync(PerformanceOptimizationRequest request, CancellationToken cancellationToken)
    {
        // Configure GC for low latency
        GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
        
        // Warm up thread pool
        ThreadPool.SetMinThreads(Environment.ProcessorCount, Environment.ProcessorCount);
        
        _logger.LogInformation("Latency optimization completed");
    }

    private async Task OptimizeBalancedAsync(PerformanceOptimizationRequest request, CancellationToken cancellationToken)
    {
        // Balanced optimization approach
        GCSettings.LatencyMode = GCLatencyMode.Interactive;
        
        // Set reasonable thread pool limits
        ThreadPool.SetMinThreads(Environment.ProcessorCount, Environment.ProcessorCount / 2);
        
        // Light garbage collection
        GC.Collect(0, GCCollectionMode.Optimized);
        
        _logger.LogInformation("Balanced optimization completed");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var snapshot = await CaptureSnapshotAsync(stoppingToken);
                var recommendations = await AnalyzePerformanceAsync(stoppingToken);
                
                if (recommendations.Any(r => r.Priority == RecommendationPriority.Critical))
                {
                    _logger.LogWarning("Critical performance issues detected: {Issues}", 
                        string.Join(", ", recommendations.Where(r => r.Priority == RecommendationPriority.Critical).Select(r => r.Title)));
                }

                // Auto-optimize if enabled
                if (_options.AutoOptimization.Enabled)
                {
                    var criticalMemoryIssue = recommendations.FirstOrDefault(r => 
                        r.Category == "Memory" && r.Priority == RecommendationPriority.Critical);
                    
                    if (criticalMemoryIssue != null)
                    {
                        await OptimizeAsync(new PerformanceOptimizationRequest
                        {
                            Profile = OptimizationProfile.Memory,
                            AggressiveOptimization = true,
                            Reason = "Auto-optimization triggered by critical memory usage"
                        }, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in performance monitoring background service");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    public override void Dispose()
    {
        _snapshotTimer?.Dispose();
        base.Dispose();
    }
}

public class PerformanceSnapshot
{
    public DateTime Timestamp { get; set; }
    public double CpuUsage { get; set; }
    public MemoryUsage MemoryUsage { get; set; } = new();
    public ThreadUsage ThreadUsage { get; set; } = new();
    public GCStatistics GCStatistics { get; set; } = new();
    public int HandleCount { get; set; }
    public CounterSnapshot[] Counters { get; set; } = Array.Empty<CounterSnapshot>();
}

public class MemoryUsage
{
    public long WorkingSet { get; set; }
    public long PrivateMemory { get; set; }
    public long VirtualMemory { get; set; }
    public long GCTotalMemory { get; set; }
    public GCMemoryInfo GCMemoryInfo { get; set; } = new();
}

public class GCMemoryInfo
{
    public long HeapSizeBytes { get; set; }
    public long MemoryLoadBytes { get; set; }
    public long TotalAvailableMemoryBytes { get; set; }
    public long HighMemoryLoadThresholdBytes { get; set; }
    public long FragmentedBytes { get; set; }
}

public class ThreadUsage
{
    public int ThreadCount { get; set; }
    public int ThreadPoolWorkerThreads { get; set; }
    public long ThreadPoolCompletionPortThreads { get; set; }
}

public class GCStatistics
{
    public int Gen0Collections { get; set; }
    public int Gen1Collections { get; set; }
    public int Gen2Collections { get; set; }
    public TimeSpan TotalPauseTime { get; set; }
    public bool IsServerGC { get; set; }
    public string LatencyMode { get; set; } = string.Empty;
}

public class CounterSnapshot
{
    public string Name { get; set; } = string.Empty;
    public long Value { get; set; }
    public double Rate { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class PerformanceCounter
{
    public string Name { get; set; } = string.Empty;
    public long Value { get; set; }
    public double Rate { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class PerformanceRecommendation
{
    public string Category { get; set; } = string.Empty;
    public RecommendationPriority Priority { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string[] Actions { get; set; } = Array.Empty<string>();
    public string Impact { get; set; } = string.Empty;
}

public enum RecommendationPriority
{
    Info = 0,
    Warning = 1,
    Critical = 2
}

public class PerformanceOptimizationRequest
{
    public OptimizationProfile Profile { get; set; }
    public bool AggressiveOptimization { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public enum OptimizationProfile
{
    Balanced,
    Memory,
    CPU,
    Throughput,
    Latency
}

public class PerformanceTrend
{
    public TimeSpan Period { get; set; }
    public int DataPoints { get; set; }
    public double MemoryTrend { get; set; }
    public double CpuTrend { get; set; }
    public double ThreadCountTrend { get; set; }
    public double GCTrend { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}

public class PerformanceOptions
{
    public MemoryThresholds MemoryThresholds { get; set; } = new();
    public CpuThresholds CpuThresholds { get; set; } = new();
    public GCThresholds GCThresholds { get; set; } = new();
    public ThreadThresholds ThreadThresholds { get; set; } = new();
    public AutoOptimizationOptions AutoOptimization { get; set; } = new();
}

public class MemoryThresholds
{
    public long Warning { get; set; } = 1024 * 1024 * 1024; // 1GB
    public long Critical { get; set; } = 2048 * 1024 * 1024; // 2GB
}

public class CpuThresholds
{
    public double Warning { get; set; } = 0.7; // 70%
    public double Critical { get; set; } = 0.9; // 90%
}

public class GCThresholds
{
    public double Gen2CollectionRate { get; set; } = 10.0; // per minute
}

public class ThreadThresholds
{
    public int Warning { get; set; } = 100;
    public int Critical { get; set; } = 200;
}

public class AutoOptimizationOptions
{
    public bool Enabled { get; set; } = true;
    public bool AggressiveMode { get; set; } = false;
}