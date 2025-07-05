using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.ServiceFramework;
using StackExchange.Redis;

namespace NeoServiceLayer.Web.Extensions;

/// <summary>
/// Extension methods for configuring persistent storage in the application.
/// </summary>
public static class PersistentStorageExtensions
{
    /// <summary>
    /// Adds persistent storage services with production-ready configuration.
    /// </summary>
    public static IServiceCollection AddPersistentStorageServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add persistent storage provider
        services.AddPersistentStorage(configuration);

        // Add Redis for distributed caching if configured
        var redisConfig = configuration.GetSection("Redis");
        if (redisConfig.GetValue<bool>("EnableCaching", false))
        {
            var redisConnectionString = redisConfig.GetValue<string>("Configuration");
            if (!string.IsNullOrEmpty(redisConnectionString))
            {
                services.AddSingleton<IConnectionMultiplexer>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<IConnectionMultiplexer>>();
                    try
                    {
                        var connection = ConnectionMultiplexer.Connect(redisConnectionString);
                        logger.LogInformation("Successfully connected to Redis at {Endpoint}",
                            connection.Configuration);
                        return connection;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to connect to Redis. Using in-memory fallback.");
                        // Return null to indicate Redis is not available
                        return null!;
                    }
                });

                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConnectionString;
                    options.InstanceName = redisConfig.GetValue<string>("InstanceName", "NeoServiceLayer");
                });

                services.AddSingleton<IDistributedCacheService, RedisDistributedCacheService>();
            }
        }

        // Add storage health checks only if storage is configured
        // TODO: Fix health check registration for Docker environment
        // var storageConfig = configuration.GetSection("Storage");
        // if (storageConfig.Exists() && !string.IsNullOrEmpty(storageConfig.GetValue<string>("Provider")))
        // {
        //     services.Configure<HealthCheckServiceOptions>(options =>
        //     {
        //         options.Registrations.Add(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration(
        //             "persistent-storage",
        //             sp => ActivatorUtilities.CreateInstance<PersistentStorageHealthCheck>(sp),
        //             null,
        //             new[] { "storage", "ready" }));
        //     });
        // }

        // Configure service-specific persistent storage
        services.Configure<PersistentStorageOptions>(configuration.GetSection("Storage:PersistenceOptions"));

        return services;
    }

    /// <summary>
    /// Configures services to use persistent storage based on configuration.
    /// </summary>
    public static IServiceCollection ConfigureServicesWithPersistentStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var servicesConfig = configuration.GetSection("Services");

        // Configure each service based on persistence settings
        services.AddSingleton<IServiceConfiguration>(sp =>
        {
            var config = new ServiceConfiguration();

            // Notification Service
            if (servicesConfig.GetValue<bool>("Notification:UsePersistentStorage", false))
            {
                config.SetValue("Notification:PersistentStorageEnabled", "true");
            }

            // Monitoring Service
            if (servicesConfig.GetValue<bool>("Monitoring:UsePersistentStorage", false))
            {
                config.SetValue("Monitoring:PersistentStorageEnabled", "true");
            }

            // Storage Service
            if (servicesConfig.GetValue<bool>("Storage:UsePersistentMetadata", false))
            {
                config.SetValue("Storage:PersistentMetadataEnabled", "true");
            }

            return config;
        });

        return services;
    }
}

/// <summary>
/// Distributed cache service interface.
/// </summary>
public interface IDistributedCacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>
/// Redis implementation of distributed cache service.
/// </summary>
public class RedisDistributedCacheService : IDistributedCacheService
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<RedisDistributedCacheService> _logger;
    private readonly IDatabase? _database;

    public RedisDistributedCacheService(
        IConnectionMultiplexer? redis,
        ILogger<RedisDistributedCacheService> logger)
    {
        _redis = redis;
        _logger = logger;
        _database = redis?.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_database == null) return default;

        try
        {
            var value = await _database.StringGetAsync(key);
            if (value.HasValue)
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(value!);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving value from Redis for key {Key}", key);
        }

        return default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        if (_database == null) return;

        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(value);
            await _database.StringSetAsync(key, json, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing value in Redis for key {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_database == null) return;

        try
        {
            await _database.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing value from Redis for key {Key}", key);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_database == null) return false;

        try
        {
            return await _database.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence in Redis for key {Key}", key);
            return false;
        }
    }
}

/// <summary>
/// Health check for persistent storage.
/// </summary>
public class PersistentStorageHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly IPersistentStorageProvider? _storageProvider;
    private readonly ILogger<PersistentStorageHealthCheck> _logger;

    public PersistentStorageHealthCheck(
        IPersistentStorageProvider? storageProvider,
        ILogger<PersistentStorageHealthCheck> logger)
    {
        _storageProvider = storageProvider;
        _logger = logger;
    }

    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_storageProvider == null)
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded(
                    "Persistent storage provider not configured");
            }

            if (!_storageProvider.IsInitialized)
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                    "Persistent storage provider not initialized");
            }

            // Test write and read
            var testKey = $"health-check-{Guid.NewGuid()}";
            var testData = System.Text.Encoding.UTF8.GetBytes("health-check");

            var writeSuccess = await _storageProvider.StoreAsync(testKey, testData);
            if (!writeSuccess)
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                    "Failed to write to persistent storage");
            }

            var readData = await _storageProvider.RetrieveAsync(testKey);
            if (readData == null)
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                    "Failed to read from persistent storage");
            }

            await _storageProvider.DeleteAsync(testKey);

            // Get storage statistics
            var stats = await _storageProvider.GetStatisticsAsync();

            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(
                $"Persistent storage is healthy. Keys: {stats.TotalKeys}, Size: {stats.TotalSize} bytes");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for persistent storage");
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(
                "Exception during health check", ex);
        }
    }
}

/// <summary>
/// Persistent storage options.
/// </summary>
public class PersistentStorageOptions
{
    public bool AutoSave { get; set; } = true;
    public int AutoSaveInterval { get; set; } = 300; // 5 minutes
    public int CompactionInterval { get; set; } = 3600; // 1 hour
    public long MaxFileSize { get; set; } = 1073741824; // 1GB
    public bool EnableCompression { get; set; } = true;
    public bool EnableEncryption { get; set; } = true;
    public bool BackupEnabled { get; set; } = true;
    public string BackupPath { get; set; } = "/secure_storage/backups";
    public int BackupRetentionDays { get; set; } = 7;
}

/// <summary>
/// Service configuration implementation.
/// </summary>
public class ServiceConfiguration : IServiceConfiguration
{
    private readonly Dictionary<string, string> _values = new();

    public string GetValue(string key, string defaultValue = "")
    {
        return _values.TryGetValue(key, out var value) ? value : defaultValue;
    }

    public void SetValue(string key, string value)
    {
        _values[key] = value;
    }

    public T GetValue<T>(string key)
    {
        if (_values.TryGetValue(key, out var value))
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        throw new KeyNotFoundException($"Configuration key '{key}' not found");
    }

    public T GetValue<T>(string key, T defaultValue)
    {
        if (_values.TryGetValue(key, out var value))
        {
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    public void SetValue<T>(string key, T value)
    {
        _values[key] = value?.ToString() ?? string.Empty;
    }

    public bool ContainsKey(string key)
    {
        return _values.ContainsKey(key);
    }

    public bool RemoveKey(string key)
    {
        return _values.Remove(key);
    }

    public IEnumerable<string> GetAllKeys()
    {
        return _values.Keys;
    }

    public IServiceConfiguration GetSection(string sectionName)
    {
        var section = new ServiceConfiguration();
        var prefix = sectionName + ":";
        foreach (var kvp in _values.Where(x => x.Key.StartsWith(prefix)))
        {
            section.SetValue(kvp.Key.Substring(prefix.Length), kvp.Value);
        }
        return section;
    }

    public string GetConnectionString(string name)
    {
        return GetValue($"ConnectionStrings:{name}", string.Empty);
    }
}
