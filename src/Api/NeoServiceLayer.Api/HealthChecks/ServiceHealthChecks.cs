using Microsoft.Extensions.Diagnostics.HealthChecks;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure.Blockchain;
using NeoServiceLayer.Services.Storage;
using NeoServiceLayer.Services.Configuration;
using NeoServiceLayer.Services.Health;

namespace NeoServiceLayer.Api.HealthChecks;

/// <summary>
/// Health checks for all Neo Service Layer services.
/// </summary>
public class BlockchainHealthCheck : IHealthCheck
{
    private readonly IBlockchainClientFactory _blockchainClientFactory;
    private readonly ILogger<BlockchainHealthCheck> _logger;

    public BlockchainHealthCheck(IBlockchainClientFactory blockchainClientFactory, ILogger<BlockchainHealthCheck> logger)
    {
        _blockchainClientFactory = blockchainClientFactory;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var tasks = new List<Task<(BlockchainType Type, bool IsHealthy, string? Error)>>();

            foreach (var blockchainType in _blockchainClientFactory.GetSupportedBlockchainTypes())
            {
                tasks.Add(CheckBlockchainHealth(blockchainType, cancellationToken));
            }

            var results = await Task.WhenAll(tasks);
            var healthyChains = results.Where(r => r.IsHealthy).ToList();
            var unhealthyChains = results.Where(r => !r.IsHealthy).ToList();

            var data = new Dictionary<string, object>
            {
                ["TotalChains"] = results.Length,
                ["HealthyChains"] = healthyChains.Count,
                ["UnhealthyChains"] = unhealthyChains.Count,
                ["ChainStatus"] = results.ToDictionary(r => r.Type.ToString(), r => new { IsHealthy = r.IsHealthy, Error = r.Error })
            };

            if (unhealthyChains.Any())
            {
                var unhealthyChainNames = string.Join(", ", unhealthyChains.Select(c => c.Type));
                return HealthCheckResult.Degraded($"Blockchain connectivity issues: {unhealthyChainNames}", data: data);
            }

            return HealthCheckResult.Healthy("All blockchain connections are healthy", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking blockchain health");
            return HealthCheckResult.Unhealthy("Failed to check blockchain health", ex);
        }
    }

    private async Task<(BlockchainType Type, bool IsHealthy, string? Error)> CheckBlockchainHealth(BlockchainType blockchainType, CancellationToken cancellationToken)
    {
        try
        {
            var client = _blockchainClientFactory.CreateClient(blockchainType);
            var height = await client.GetBlockHeightAsync();
            
            if (height >= 0)
            {
                return (blockchainType, true, null);
            }
            
            return (blockchainType, false, "Invalid block height returned");
        }
        catch (Exception ex)
        {
            return (blockchainType, false, ex.Message);
        }
    }
}

/// <summary>
/// Health check for storage services.
/// </summary>
public class StorageHealthCheck : IHealthCheck
{
    private readonly IStorageService _storageService;
    private readonly ILogger<StorageHealthCheck> _logger;

    public StorageHealthCheck(IStorageService storageService, ILogger<StorageHealthCheck> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Test storage read/write functionality
            var testKey = $"health_check_{Guid.NewGuid()}";
            var testData = "health_check_data";

            await _storageService.StoreDataAsync(testKey, testData, BlockchainType.NeoN3);
            var retrievedData = await _storageService.RetrieveDataAsync(testKey, BlockchainType.NeoN3);
            await _storageService.DeleteDataAsync(testKey, BlockchainType.NeoN3);

            if (retrievedData == testData)
            {
                return HealthCheckResult.Healthy("Storage service is working correctly");
            }

            return HealthCheckResult.Degraded("Storage service data integrity issue");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Storage health check failed");
            return HealthCheckResult.Unhealthy("Storage service is not available", ex);
        }
    }
}

/// <summary>
/// Health check for configuration service.
/// </summary>
public class ConfigurationHealthCheck : IHealthCheck
{
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<ConfigurationHealthCheck> _logger;

    public ConfigurationHealthCheck(IConfigurationService configurationService, ILogger<ConfigurationHealthCheck> logger)
    {
        _configurationService = configurationService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Test configuration service availability
            var configs = await _configurationService.GetAllConfigurationsAsync(BlockchainType.NeoN3);
            
            var data = new Dictionary<string, object>
            {
                ["ConfigurationCount"] = configs?.Count() ?? 0,
                ["ServiceStatus"] = "Available"
            };

            return HealthCheckResult.Healthy("Configuration service is available", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Configuration health check failed");
            return HealthCheckResult.Unhealthy("Configuration service is not available", ex);
        }
    }
}

/// <summary>
/// Health check for Neo service layer services.
/// </summary>
public class NeoServicesHealthCheck : IHealthCheck
{
    private readonly IHealthService _healthService;
    private readonly ILogger<NeoServicesHealthCheck> _logger;

    public NeoServicesHealthCheck(IHealthService healthService, ILogger<NeoServicesHealthCheck> logger)
    {
        _healthService = healthService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var healthStatus = await _healthService.GetOverallHealthAsync();
            
            var data = new Dictionary<string, object>
            {
                ["OverallHealth"] = healthStatus.ToString(),
                ["CheckedAt"] = DateTime.UtcNow
            };

            return healthStatus switch
            {
                ServiceHealth.Healthy => HealthCheckResult.Healthy("All Neo services are healthy", data),
                ServiceHealth.Degraded => HealthCheckResult.Degraded("Some Neo services are experiencing issues", data),
                _ => HealthCheckResult.Unhealthy("Neo services are experiencing critical issues", data: data)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Neo services health check failed");
            return HealthCheckResult.Unhealthy("Failed to check Neo services health", ex);
        }
    }
}

/// <summary>
/// Health check for memory and resource usage.
/// </summary>
public class ResourceHealthCheck : IHealthCheck
{
    private readonly ILogger<ResourceHealthCheck> _logger;

    public ResourceHealthCheck(ILogger<ResourceHealthCheck> logger)
    {
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var memoryUsageMB = process.WorkingSet64 / 1024 / 1024;
            var memoryLimitMB = 2048; // 2GB limit

            var data = new Dictionary<string, object>
            {
                ["MemoryUsageMB"] = memoryUsageMB,
                ["MemoryLimitMB"] = memoryLimitMB,
                ["MemoryUsagePercentage"] = (double)memoryUsageMB / memoryLimitMB * 100,
                ["ThreadCount"] = process.Threads.Count,
                ["HandleCount"] = process.HandleCount
            };

            if (memoryUsageMB > memoryLimitMB * 0.9) // 90% threshold
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Memory usage is critically high", data: data));
            }

            if (memoryUsageMB > memoryLimitMB * 0.7) // 70% threshold
            {
                return Task.FromResult(HealthCheckResult.Degraded("Memory usage is high", data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy("Resource usage is normal", data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resource health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy("Failed to check resource usage", ex));
        }
    }
}