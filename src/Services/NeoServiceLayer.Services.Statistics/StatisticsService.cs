using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.EnclaveStorage;
using NeoServiceLayer.Services.Statistics.Models;
using NeoServiceLayer.Tee.Host.Services;

namespace NeoServiceLayer.Services.Statistics;

/// <summary>
/// Implementation of the statistics service that collects and provides metrics for the Neo Service Layer.
/// </summary>
public class StatisticsService : EnclaveBlockchainServiceBase, IStatisticsService
{
    private readonly SGXPersistence _sgxPersistence;
    private readonly ConcurrentDictionary<string, ServiceStatistics> _serviceStats = new();
    private readonly ConcurrentDictionary<string, BlockchainStatistics> _blockchainStats = new();
    private readonly ConcurrentDictionary<string, List<long>> _responseTimesBuffer = new();
    private readonly Timer _aggregationTimer;
    private readonly Timer _cleanupTimer;
    private readonly DateTime _startTime;
    private readonly Process _currentProcess;
    private long _totalOperations;
    private long _successfulOperations;
    private long _totalSGXOperations;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatisticsService"/> class.
    /// </summary>
    public StatisticsService(
        ILogger<StatisticsService> logger,
        IEnclaveManager enclaveManager,
        IEnclaveStorageService? enclaveStorage = null)
        : base("StatisticsService", "Comprehensive statistics and monitoring service", "1.0.0", 
               logger, new[] { BlockchainType.NeoN3, BlockchainType.NeoX }, enclaveManager)
    {
        _sgxPersistence = new SGXPersistence("StatisticsService", enclaveStorage, logger);
        _startTime = DateTime.UtcNow;
        _currentProcess = Process.GetCurrentProcess();
        
        AddCapability<IStatisticsService>();
        AddDependency(new ServiceDependency("HealthService", false, "1.0.0"));
        AddDependency(new ServiceDependency("EnclaveStorageService", false, "1.0.0"));

        // Initialize timers
        _aggregationTimer = new Timer(AggregateMetrics, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
        _cleanupTimer = new Timer(CleanupOldData, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

        // Initialize blockchain stats
        foreach (var blockchain in Enum.GetValues<BlockchainType>())
        {
            _blockchainStats[blockchain.ToString()] = new BlockchainStatistics
            {
                BlockchainType = blockchain,
                LastUpdated = DateTime.UtcNow
            };
        }
    }

    /// <inheritdoc/>
    public async Task<SystemStatistics> GetSystemStatisticsAsync()
    {
        var activeServices = _serviceStats.Values.Count(s => s.Status == ServiceStatus.Running);
        var healthyServices = _serviceStats.Values.Count(s => s.Health == ServiceHealth.Healthy);
        
        var stats = new SystemStatistics
        {
            ActiveServices = activeServices,
            HealthyServices = healthyServices,
            UptimeSeconds = (long)(DateTime.UtcNow - _startTime).TotalSeconds,
            TotalOperations = _totalOperations,
            SuccessfulOperations = _successfulOperations,
            SuccessRate = _totalOperations > 0 ? (_successfulOperations * 100.0 / _totalOperations) : 100.0,
            AverageResponseTime = CalculateOverallAverageResponseTime(),
            MemoryUsageMB = _currentProcess.WorkingSet64 / (1024 * 1024),
            CpuUsagePercent = await GetCpuUsageAsync(),
            ActiveEnclaves = await GetActiveEnclavesCountAsync(),
            TotalSGXOperations = _totalSGXOperations,
            Timestamp = DateTime.UtcNow
        };

        return stats;
    }

    /// <inheritdoc/>
    public async Task<ServiceStatistics> GetServiceStatisticsAsync(string serviceName)
    {
        if (_serviceStats.TryGetValue(serviceName, out var stats))
        {
            // Update real-time metrics
            stats.MemoryUsageMB = GetServiceMemoryUsage(serviceName);
            stats.OperationsPerSecond = CalculateOperationsPerSecond(serviceName);
            return stats;
        }

        // Create new statistics for the service
        var newStats = new ServiceStatistics
        {
            ServiceName = serviceName,
            Status = ServiceStatus.Running,
            Health = ServiceHealth.Healthy,
            UptimeSeconds = 0,
            LastUpdated = DateTime.UtcNow
        };

        _serviceStats[serviceName] = newStats;
        await Task.CompletedTask;
        return newStats;
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, ServiceStatistics>> GetAllServiceStatisticsAsync()
    {
        var allStats = new Dictionary<string, ServiceStatistics>();
        
        foreach (var kvp in _serviceStats)
        {
            var stats = kvp.Value;
            // Update real-time metrics
            stats.MemoryUsageMB = GetServiceMemoryUsage(kvp.Key);
            stats.OperationsPerSecond = CalculateOperationsPerSecond(kvp.Key);
            allStats[kvp.Key] = stats;
        }

        await Task.CompletedTask;
        return allStats;
    }

    /// <inheritdoc/>
    public async Task<BlockchainStatistics> GetBlockchainStatisticsAsync(BlockchainType blockchainType)
    {
        if (_blockchainStats.TryGetValue(blockchainType.ToString(), out var stats))
        {
            stats.LastUpdated = DateTime.UtcNow;
            return stats;
        }

        var newStats = new BlockchainStatistics
        {
            BlockchainType = blockchainType,
            LastUpdated = DateTime.UtcNow
        };

        _blockchainStats[blockchainType.ToString()] = newStats;
        await Task.CompletedTask;
        return newStats;
    }

    /// <inheritdoc/>
    public async Task<PerformanceMetrics> GetPerformanceMetricsAsync(DateTime startTime, DateTime endTime)
    {
        var totalRequests = _serviceStats.Values.Sum(s => s.TotalOperations);
        var duration = (endTime - startTime).TotalSeconds;
        
        var metrics = new PerformanceMetrics
        {
            StartTime = startTime,
            EndTime = endTime,
            AverageCpuUsage = await GetAverageCpuUsageAsync(),
            PeakCpuUsage = await GetPeakCpuUsageAsync(),
            AverageMemoryUsage = _currentProcess.WorkingSet64 / (1024.0 * 1024),
            PeakMemoryUsage = _currentProcess.PeakWorkingSet64 / (1024.0 * 1024),
            TotalRequests = totalRequests,
            RequestsPerSecond = duration > 0 ? totalRequests / duration : 0,
            AverageLatency = CalculateOverallAverageResponseTime(),
            P50Latency = CalculatePercentileLatency(50),
            P95Latency = CalculatePercentileLatency(95),
            P99Latency = CalculatePercentileLatency(99),
            ErrorRate = CalculateErrorRate(),
            AvailabilityPercent = CalculateAvailability()
        };

        return metrics;
    }

    /// <inheritdoc/>
    public async Task RecordOperationAsync(string serviceName, string operation, bool success, long duration)
    {
        Interlocked.Increment(ref _totalOperations);
        if (success)
        {
            Interlocked.Increment(ref _successfulOperations);
        }

        // Update service statistics
        var stats = await GetServiceStatisticsAsync(serviceName);
        Interlocked.Increment(ref stats.TotalOperations);
        
        if (success)
        {
            Interlocked.Increment(ref stats.SuccessfulOperations);
        }
        else
        {
            Interlocked.Increment(ref stats.FailedOperations);
        }

        // Update operation breakdown
        lock (stats.OperationBreakdown)
        {
            if (!stats.OperationBreakdown.TryGetValue(operation, out var opMetrics))
            {
                opMetrics = new OperationMetrics { OperationName = operation };
                stats.OperationBreakdown[operation] = opMetrics;
            }

            opMetrics.Count++;
            if (success) opMetrics.SuccessCount++;
            
            // Update duration metrics
            if (opMetrics.Count == 1)
            {
                opMetrics.MinDuration = duration;
                opMetrics.MaxDuration = duration;
                opMetrics.AverageDuration = duration;
            }
            else
            {
                opMetrics.MinDuration = Math.Min(opMetrics.MinDuration, duration);
                opMetrics.MaxDuration = Math.Max(opMetrics.MaxDuration, duration);
                opMetrics.AverageDuration = ((opMetrics.AverageDuration * (opMetrics.Count - 1)) + duration) / opMetrics.Count;
            }
        }

        // Record response time for percentile calculations
        var key = $"{serviceName}:{operation}";
        _responseTimesBuffer.AddOrUpdate(key, 
            new List<long> { duration }, 
            (k, list) => { list.Add(duration); return list; });

        // Update success rate
        stats.SuccessRate = stats.TotalOperations > 0 
            ? (stats.SuccessfulOperations * 100.0 / stats.TotalOperations) 
            : 100.0;

        // Check if this is an SGX operation
        if (operation.Contains("SGX", StringComparison.OrdinalIgnoreCase) || 
            operation.Contains("Enclave", StringComparison.OrdinalIgnoreCase))
        {
            Interlocked.Increment(ref _totalSGXOperations);
            
            if (stats.EnclaveStats == null)
            {
                stats.EnclaveStats = new EnclaveStatistics();
            }
            
            Interlocked.Increment(ref stats.EnclaveStats.TotalOperations);
            
            if (operation.Contains("JavaScript", StringComparison.OrdinalIgnoreCase))
            {
                Interlocked.Increment(ref stats.EnclaveStats.JavaScriptExecutions);
            }
        }

        stats.LastUpdated = DateTime.UtcNow;
    }

    /// <inheritdoc/>
    public async Task RecordTransactionAsync(BlockchainType blockchainType, string transactionType, bool success)
    {
        var stats = await GetBlockchainStatisticsAsync(blockchainType);
        
        Interlocked.Increment(ref stats.TotalTransactions);
        if (success)
        {
            Interlocked.Increment(ref stats.SuccessfulTransactions);
        }
        else
        {
            Interlocked.Increment(ref stats.FailedTransactions);
        }

        // Update transaction breakdown
        lock (stats.TransactionsByType)
        {
            if (!stats.TransactionsByType.ContainsKey(transactionType))
            {
                stats.TransactionsByType[transactionType] = 0;
            }
            stats.TransactionsByType[transactionType]++;
        }

        stats.LastUpdated = DateTime.UtcNow;
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<StatisticsUpdate> GetRealTimeStatisticsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // System-wide update
            var systemStats = await GetSystemStatisticsAsync();
            yield return new StatisticsUpdate
            {
                Type = UpdateType.System,
                MetricName = "system.overview",
                Value = systemStats,
                Timestamp = DateTime.UtcNow
            };

            // Service updates
            foreach (var serviceStats in _serviceStats.Values)
            {
                yield return new StatisticsUpdate
                {
                    Type = UpdateType.Service,
                    ServiceName = serviceStats.ServiceName,
                    MetricName = "service.status",
                    Value = serviceStats,
                    Timestamp = DateTime.UtcNow
                };
            }

            // Wait before next update
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task<byte[]> ExportStatisticsAsync(DateTime startTime, DateTime endTime, string format = "json")
    {
        var exportData = new
        {
            ExportTime = DateTime.UtcNow,
            StartTime = startTime,
            EndTime = endTime,
            SystemStats = await GetSystemStatisticsAsync(),
            ServiceStats = await GetAllServiceStatisticsAsync(),
            BlockchainStats = _blockchainStats.Values.ToList(),
            PerformanceMetrics = await GetPerformanceMetricsAsync(startTime, endTime)
        };

        return format.ToLower() switch
        {
            "json" => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true })),
            "csv" => ExportToCsv(exportData),
            "prometheus" => ExportToPrometheus(exportData),
            _ => throw new ArgumentException($"Unsupported export format: {format}")
        };
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnInitializeAsync()
    {
        Logger.LogInformation("Initializing Statistics Service");

        try
        {
            // Load persisted statistics
            await LoadPersistedStatisticsAsync();
            
            Logger.LogInformation("Statistics Service initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize Statistics Service");
            return false;
        }
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStartAsync()
    {
        Logger.LogInformation("Starting Statistics Service");
        
        // Start monitoring all registered services
        await Task.CompletedTask;
        
        Logger.LogInformation("Statistics Service started successfully");
        return true;
    }

    /// <inheritdoc/>
    protected override async Task<bool> OnStopAsync()
    {
        Logger.LogInformation("Stopping Statistics Service");
        
        // Persist current statistics
        await PersistStatisticsAsync();
        
        // Dispose timers
        _aggregationTimer?.Dispose();
        _cleanupTimer?.Dispose();
        
        Logger.LogInformation("Statistics Service stopped successfully");
        return true;
    }

    /// <inheritdoc/>
    protected override Task<ServiceHealth> OnGetHealthAsync()
    {
        return Task.FromResult(NeoServiceLayer.Core.ServiceHealth.Healthy);
    }

    /// <inheritdoc/>
    protected override Task<bool> OnInitializeEnclaveAsync()
    {
        Logger.LogInformation("Initializing Statistics Service enclave operations");
        // Statistics service doesn't require enclave operations
        return Task.FromResult(true);
    }

    // Private helper methods
    private void AggregateMetrics(object? state)
    {
        try
        {
            // Aggregate response times and calculate percentiles
            foreach (var kvp in _responseTimesBuffer)
            {
                if (kvp.Value.Count > 0)
                {
                    var parts = kvp.Key.Split(':');
                    if (parts.Length >= 2 && _serviceStats.TryGetValue(parts[0], out var stats))
                    {
                        var sortedTimes = kvp.Value.OrderBy(t => t).ToList();
                        stats.AverageResponseTime = sortedTimes.Average();
                        stats.P99ResponseTime = GetPercentile(sortedTimes, 99);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error aggregating metrics");
        }
    }

    private void CleanupOldData(object? state)
    {
        try
        {
            // Clear old response time buffers
            foreach (var kvp in _responseTimesBuffer)
            {
                if (kvp.Value.Count > 10000) // Keep only last 10k samples
                {
                    kvp.Value.RemoveRange(0, kvp.Value.Count - 10000);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error cleaning up old data");
        }
    }

    private double CalculateOverallAverageResponseTime()
    {
        var allResponseTimes = _serviceStats.Values
            .Where(s => s.AverageResponseTime > 0)
            .Select(s => s.AverageResponseTime);
        
        return allResponseTimes.Any() ? allResponseTimes.Average() : 0;
    }

    private double CalculatePercentileLatency(int percentile)
    {
        var allResponseTimes = new List<double>();
        
        foreach (var stats in _serviceStats.Values)
        {
            if (percentile >= 99 && stats.P99ResponseTime > 0)
            {
                allResponseTimes.Add(stats.P99ResponseTime);
            }
            else if (stats.AverageResponseTime > 0)
            {
                allResponseTimes.Add(stats.AverageResponseTime);
            }
        }

        if (!allResponseTimes.Any()) return 0;
        
        allResponseTimes.Sort();
        var index = (int)Math.Ceiling(percentile / 100.0 * allResponseTimes.Count) - 1;
        return allResponseTimes[Math.Max(0, Math.Min(index, allResponseTimes.Count - 1))];
    }

    private double CalculateErrorRate()
    {
        var totalOps = _serviceStats.Values.Sum(s => s.TotalOperations);
        var failedOps = _serviceStats.Values.Sum(s => s.FailedOperations);
        
        return totalOps > 0 ? (failedOps * 100.0 / totalOps) : 0;
    }

    private double CalculateAvailability()
    {
        var healthyTime = _serviceStats.Values
            .Where(s => s.Health == ServiceHealth.Healthy)
            .Sum(s => s.UptimeSeconds);
        
        var totalTime = _serviceStats.Values.Sum(s => s.UptimeSeconds);
        
        return totalTime > 0 ? (healthyTime * 100.0 / totalTime) : 100;
    }

    private long GetServiceMemoryUsage(string serviceName)
    {
        // In a real implementation, this would query actual service memory usage
        // For now, return a proportional share of total memory
        var serviceCount = Math.Max(1, _serviceStats.Count);
        return _currentProcess.WorkingSet64 / (1024 * 1024 * serviceCount);
    }

    private double CalculateOperationsPerSecond(string serviceName)
    {
        if (_serviceStats.TryGetValue(serviceName, out var stats) && stats.UptimeSeconds > 0)
        {
            return stats.TotalOperations / (double)stats.UptimeSeconds;
        }
        return 0;
    }

    private async Task<double> GetCpuUsageAsync()
    {
        // Simple CPU usage calculation
        var startTime = DateTime.UtcNow;
        var startCpuUsage = _currentProcess.TotalProcessorTime;
        
        await Task.Delay(100);
        
        var endTime = DateTime.UtcNow;
        var endCpuUsage = _currentProcess.TotalProcessorTime;
        
        var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
        var totalMsPassed = (endTime - startTime).TotalMilliseconds;
        var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
        
        return Math.Round(cpuUsageTotal * 100, 2);
    }

    private async Task<double> GetAverageCpuUsageAsync()
    {
        // In production, this would query historical data
        return await GetCpuUsageAsync();
    }

    private async Task<double> GetPeakCpuUsageAsync()
    {
        // In production, this would query historical data
        return await GetCpuUsageAsync() * 1.2; // Simulated peak
    }

    private async Task<int> GetActiveEnclavesCountAsync()
    {
        // Query enclave manager for active enclaves
        if (_enclaveManager != null)
        {
            try
            {
                var result = await _enclaveManager.ExecuteJavaScriptAsync("getEnclaveCount()");
                if (int.TryParse(result, out var count))
                {
                    return count;
                }
            }
            catch { }
        }
        return 0;
    }

    private double GetPercentile(List<long> sortedValues, int percentile)
    {
        if (sortedValues.Count == 0) return 0;
        
        var index = (int)Math.Ceiling(percentile / 100.0 * sortedValues.Count) - 1;
        return sortedValues[Math.Max(0, Math.Min(index, sortedValues.Count - 1))];
    }

    private byte[] ExportToCsv(object exportData)
    {
        // Simple CSV export implementation
        var csv = new StringBuilder();
        csv.AppendLine("Service,Total Operations,Success Rate,Avg Response Time");
        
        foreach (var stats in _serviceStats.Values)
        {
            csv.AppendLine($"{stats.ServiceName},{stats.TotalOperations},{stats.SuccessRate:F2},{stats.AverageResponseTime:F2}");
        }
        
        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    private byte[] ExportToPrometheus(object exportData)
    {
        // Prometheus format export
        var metrics = new StringBuilder();
        
        // System metrics
        metrics.AppendLine($"# HELP neo_service_layer_uptime_seconds Service uptime in seconds");
        metrics.AppendLine($"# TYPE neo_service_layer_uptime_seconds gauge");
        metrics.AppendLine($"neo_service_layer_uptime_seconds {(DateTime.UtcNow - _startTime).TotalSeconds}");
        
        // Service metrics
        foreach (var stats in _serviceStats.Values)
        {
            var serviceName = stats.ServiceName.ToLower().Replace(" ", "_");
            metrics.AppendLine($"neo_service_operations_total{{service=\"{serviceName}\"}} {stats.TotalOperations}");
            metrics.AppendLine($"neo_service_success_rate{{service=\"{serviceName}\"}} {stats.SuccessRate}");
            metrics.AppendLine($"neo_service_response_time_ms{{service=\"{serviceName}\"}} {stats.AverageResponseTime}");
        }
        
        return Encoding.UTF8.GetBytes(metrics.ToString());
    }

    private async Task LoadPersistedStatisticsAsync()
    {
        try
        {
            var persistedStats = await _sgxPersistence.GetPersistedStatisticsAsync(BlockchainType.NeoN3);
            if (persistedStats != null)
            {
                foreach (var kvp in persistedStats.ServiceStats)
                {
                    _serviceStats[kvp.Key] = kvp.Value;
                }
                
                _totalOperations = persistedStats.TotalOperations;
                _successfulOperations = persistedStats.SuccessfulOperations;
                _totalSGXOperations = persistedStats.TotalSGXOperations;
                
                Logger.LogInformation("Loaded persisted statistics for {Count} services", _serviceStats.Count);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load persisted statistics");
        }
    }

    private async Task PersistStatisticsAsync()
    {
        try
        {
            var persistedStats = new PersistedStatistics
            {
                ServiceStats = _serviceStats.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                BlockchainStats = _blockchainStats.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                TotalOperations = _totalOperations,
                SuccessfulOperations = _successfulOperations,
                TotalSGXOperations = _totalSGXOperations,
                Timestamp = DateTime.UtcNow
            };
            
            await _sgxPersistence.StoreStatisticsAsync(persistedStats, BlockchainType.NeoN3);
            Logger.LogInformation("Persisted statistics for {Count} services", _serviceStats.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist statistics");
        }
    }

    /// <summary>
    /// Inner class for SGX persistence operations.
    /// </summary>
    private class SGXPersistence : NeoServiceLayer.ServiceFramework.SGXPersistenceBase
    {
        public SGXPersistence(string serviceName, IEnclaveStorageService? enclaveStorage, ILogger logger) 
            : base(serviceName, enclaveStorage, logger)
        {
        }

        public async Task<bool> StoreStatisticsAsync(PersistedStatistics stats, BlockchainType blockchainType)
        {
            return await StoreSecurelyAsync("statistics:current", stats, 
                new Dictionary<string, object> 
                { 
                    ["type"] = "statistics",
                    ["timestamp"] = stats.Timestamp
                }, 
                blockchainType);
        }

        public async Task<PersistedStatistics?> GetPersistedStatisticsAsync(BlockchainType blockchainType)
        {
            return await RetrieveSecurelyAsync<PersistedStatistics>("statistics:current", blockchainType);
        }
    }

    /// <summary>
    /// Model for persisted statistics.
    /// </summary>
    private class PersistedStatistics
    {
        public Dictionary<string, ServiceStatistics> ServiceStats { get; set; } = new();
        public Dictionary<string, BlockchainStatistics> BlockchainStats { get; set; } = new();
        public long TotalOperations { get; set; }
        public long SuccessfulOperations { get; set; }
        public long TotalSGXOperations { get; set; }
        public DateTime Timestamp { get; set; }
    }
}