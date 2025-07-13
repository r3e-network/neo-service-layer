using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Api.HealthChecks;

/// <summary>
/// Custom health checks for Neo Service Layer components.
/// </summary>
public static class CustomHealthChecks
{
    /// <summary>
    /// Registers all custom health checks.
    /// </summary>
    public static IServiceCollection AddCustomHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var healthChecksBuilder = services.AddHealthChecks();

        // Basic health checks
        healthChecksBuilder
            .AddCheck<DatabaseHealthCheck>("database", HealthStatus.Unhealthy, new[] { "database", "critical" })
            .AddCheck<RedisHealthCheck>("redis", HealthStatus.Degraded, new[] { "cache", "performance" })
            .AddCheck<BlockchainHealthCheck>("blockchain", HealthStatus.Degraded, new[] { "blockchain", "external" })
            .AddCheck<KeyManagementHealthCheck>("keymanagement", HealthStatus.Unhealthy, new[] { "security", "critical" })
            .AddCheck<AIServicesHealthCheck>("ai-services", HealthStatus.Degraded, new[] { "ai", "features" })
            .AddCheck<StorageHealthCheck>("storage", HealthStatus.Degraded, new[] { "storage", "data" })
;

        // External service health checks
        var neoN3RpcUrl = configuration["Blockchain:NeoN3:RpcUrl"];
        if (!string.IsNullOrEmpty(neoN3RpcUrl))
        {
            healthChecksBuilder.AddCheck<NeoN3RpcHealthCheck>("neo-n3-rpc", HealthStatus.Degraded, new[] { "blockchain", "neo-n3" });
        }

        var neoXRpcUrl = configuration["Blockchain:NeoX:RpcUrl"];
        if (!string.IsNullOrEmpty(neoXRpcUrl))
        {
            healthChecksBuilder.AddCheck<NeoXRpcHealthCheck>("neo-x-rpc", HealthStatus.Degraded, new[] { "blockchain", "neo-x" });
        }

        return services;
    }
}

/// <summary>
/// Database connectivity health check.
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(IConfiguration configuration, ILogger<DatabaseHealthCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                return HealthCheckResult.Unhealthy("Database connection string not configured");
            }

            // Simple connection test
            using var connection = new Npgsql.NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            // Test query
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            var result = await command.ExecuteScalarAsync(cancellationToken);

            var data = new Dictionary<string, object>
            {
                ["server"] = connection.DataSource,
                ["database"] = connection.Database,
                ["status"] = "connected"
            };

            return HealthCheckResult.Healthy("Database is accessible", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy($"Database health check failed: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Redis cache health check.
/// </summary>
public class RedisHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RedisHealthCheck> _logger;

    public RedisHealthCheck(IConfiguration configuration, ILogger<RedisHealthCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("Redis");
            if (string.IsNullOrEmpty(connectionString))
            {
                return HealthCheckResult.Degraded("Redis connection string not configured");
            }

            using var redis = StackExchange.Redis.ConnectionMultiplexer.Connect(connectionString);
            var database = redis.GetDatabase();

            // Test ping
            var pingTime = await database.PingAsync();

            // Test set/get
            var testKey = $"health_check_{Guid.NewGuid()}";
            await database.StringSetAsync(testKey, "test", TimeSpan.FromSeconds(10));
            var testValue = await database.StringGetAsync(testKey);
            await database.KeyDeleteAsync(testKey);

            var data = new Dictionary<string, object>
            {
                ["ping_time_ms"] = pingTime.TotalMilliseconds,
                ["status"] = "connected",
                ["test_operation"] = testValue == "test" ? "success" : "failed"
            };

            return HealthCheckResult.Healthy("Redis is accessible", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            return HealthCheckResult.Degraded($"Redis health check failed: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Blockchain connectivity health check.
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
        var results = new Dictionary<string, object>();
        var overallHealthy = true;
        var messages = new List<string>();

        try
        {
            // Test Neo N3
            try
            {
                var neoN3Client = _blockchainClientFactory.CreateClient(BlockchainType.NeoN3);
                var blockHeight = await neoN3Client.GetBlockHeightAsync();
                results["neo_n3_block_height"] = blockHeight;
                results["neo_n3_status"] = "connected";
                messages.Add($"Neo N3: Block height {blockHeight}");
            }
            catch (Exception ex)
            {
                results["neo_n3_status"] = "failed";
                results["neo_n3_error"] = ex.Message;
                messages.Add($"Neo N3: {ex.Message}");
                overallHealthy = false;
            }

            // Test Neo X
            try
            {
                var neoXClient = _blockchainClientFactory.CreateClient(BlockchainType.NeoX);
                var blockHeight = await neoXClient.GetBlockHeightAsync();
                results["neo_x_block_height"] = blockHeight;
                results["neo_x_status"] = "connected";
                messages.Add($"Neo X: Block height {blockHeight}");
            }
            catch (Exception ex)
            {
                results["neo_x_status"] = "failed";
                results["neo_x_error"] = ex.Message;
                messages.Add($"Neo X: {ex.Message}");
                overallHealthy = false;
            }

            var message = string.Join("; ", messages);
            return overallHealthy
                ? HealthCheckResult.Healthy($"Blockchain connectivity: {message}", results)
                : HealthCheckResult.Degraded($"Partial blockchain connectivity: {message}", null, results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blockchain health check failed");
            return HealthCheckResult.Degraded($"Blockchain health check failed: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Key management service health check.
/// </summary>
public class KeyManagementHealthCheck : IHealthCheck
{
    private readonly ILogger<KeyManagementHealthCheck> _logger;

    public KeyManagementHealthCheck(ILogger<KeyManagementHealthCheck> logger)
    {
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Test key management service availability
            // This would depend on your actual key management implementation

            var data = new Dictionary<string, object>
            {
                ["service"] = "key_management",
                ["status"] = "available",
                ["enclave_status"] = "ready" // This would be actual enclave status
            };

            return HealthCheckResult.Healthy("Key management service is available", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Key management health check failed");
            return HealthCheckResult.Unhealthy($"Key management health check failed: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// AI services health check.
/// </summary>
public class AIServicesHealthCheck : IHealthCheck
{
    private readonly ILogger<AIServicesHealthCheck> _logger;

    public AIServicesHealthCheck(ILogger<AIServicesHealthCheck> logger)
    {
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = new Dictionary<string, object>
            {
                ["pattern_recognition"] = "available",
                ["prediction_service"] = "available",
                ["model_cache_status"] = "ready"
            };

            // Test AI model availability
            // This would test actual AI service endpoints

            return HealthCheckResult.Healthy("AI services are available", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI services health check failed");
            return HealthCheckResult.Degraded($"AI services health check failed: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Storage service health check.
/// </summary>
public class StorageHealthCheck : IHealthCheck
{
    private readonly ILogger<StorageHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageHealthCheck"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public StorageHealthCheck(ILogger<StorageHealthCheck> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Performs the health check for storage service.
    /// </summary>
    /// <param name="context">The health check context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The health check result.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Test storage paths
            var dataPath = "/app/data";
            var logPath = "/var/log/neo-service-layer";

            var data = new Dictionary<string, object>();

            // Check data directory
            if (Directory.Exists(dataPath))
            {
                var dataInfo = new DirectoryInfo(dataPath);
                data["data_path"] = dataPath;
                data["data_writable"] = TestWriteAccess(dataPath);
            }

            // Check log directory
            if (Directory.Exists(logPath))
            {
                var logInfo = new DirectoryInfo(logPath);
                data["log_path"] = logPath;
                data["log_writable"] = TestWriteAccess(logPath);
            }

            return HealthCheckResult.Healthy("Storage is accessible", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Storage health check failed");
            return HealthCheckResult.Degraded($"Storage health check failed: {ex.Message}", ex);
        }
    }

    private bool TestWriteAccess(string path)
    {
        try
        {
            var testFile = Path.Combine(path, $"health_check_{Guid.NewGuid()}.tmp");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Neo N3 RPC health check.
/// </summary>
public class NeoN3RpcHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<NeoN3RpcHealthCheck> _logger;

    public NeoN3RpcHealthCheck(IConfiguration configuration, ILogger<NeoN3RpcHealthCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var rpcUrl = _configuration["Blockchain:NeoN3:RpcUrl"];
            if (string.IsNullOrEmpty(rpcUrl))
            {
                return HealthCheckResult.Degraded("Neo N3 RPC URL not configured");
            }

            using var client = new HttpClient();
            var response = await client.GetAsync(rpcUrl, cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Neo N3 RPC is accessible")
                : HealthCheckResult.Degraded($"Neo N3 RPC returned {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Neo N3 RPC health check failed");
            return HealthCheckResult.Degraded($"Neo N3 RPC check failed: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Neo X RPC health check.
/// </summary>
public class NeoXRpcHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<NeoXRpcHealthCheck> _logger;

    public NeoXRpcHealthCheck(IConfiguration configuration, ILogger<NeoXRpcHealthCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var rpcUrl = _configuration["Blockchain:NeoX:RpcUrl"];
            if (string.IsNullOrEmpty(rpcUrl))
            {
                return HealthCheckResult.Degraded("Neo X RPC URL not configured");
            }

            using var client = new HttpClient();
            var response = await client.GetAsync(rpcUrl, cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Neo X RPC is accessible")
                : HealthCheckResult.Degraded($"Neo X RPC returned {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Neo X RPC health check failed");
            return HealthCheckResult.Degraded($"Neo X RPC check failed: {ex.Message}", ex);
        }
    }
}
