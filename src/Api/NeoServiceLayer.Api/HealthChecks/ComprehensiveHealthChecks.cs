using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NeoServiceLayer.Api.HealthChecks;

// Enhanced Database Health Check
public class EnhancedDatabaseHealthCheck : IHealthCheck
{
    private readonly string _connectionString;
    private readonly ILogger<EnhancedDatabaseHealthCheck> _logger;
    private readonly HealthCheckOptions _options;

    public EnhancedDatabaseHealthCheck(
        IConfiguration configuration, 
        ILogger<EnhancedDatabaseHealthCheck> logger,
        IOptions<HealthCheckOptions> options)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
        _logger = logger;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Check basic connectivity
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            var result = await command.ExecuteScalarAsync(cancellationToken);

            // Check database statistics
            var stats = await GetDatabaseStatistics(connection, cancellationToken);
            
            stopwatch.Stop();
            
            var data = new Dictionary<string, object>
            {
                ["responseTime"] = stopwatch.ElapsedMilliseconds,
                ["activeConnections"] = stats["active_connections"],
                ["databaseSize"] = stats["database_size"],
                ["cacheHitRatio"] = stats["cache_hit_ratio"],
                ["deadlocks"] = stats["deadlocks"]
            };

            // Determine health status based on metrics
            if ((int)stats["active_connections"] > _options.MaxDatabaseConnections * 0.9)
            {
                return HealthCheckResult.Degraded("High number of active connections", null, data);
            }

            if ((double)stats["cache_hit_ratio"] < 0.9)
            {
                return HealthCheckResult.Degraded("Low cache hit ratio", null, data);
            }

            return HealthCheckResult.Healthy("Database is responsive", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            
            var data = new Dictionary<string, object>
            {
                ["responseTime"] = stopwatch.ElapsedMilliseconds,
                ["error"] = ex.Message
            };
            
            return HealthCheckResult.Unhealthy("Database connection failed", ex, data);
        }
    }

    private async Task<Dictionary<string, object>> GetDatabaseStatistics(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        var stats = new Dictionary<string, object>();

        // Active connections
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT count(*) FROM pg_stat_activity WHERE state = 'active'";
            stats["active_connections"] = await cmd.ExecuteScalarAsync(cancellationToken) ?? 0;
        }

        // Database size
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT pg_database_size(current_database())";
            stats["database_size"] = await cmd.ExecuteScalarAsync(cancellationToken) ?? 0;
        }

        // Cache hit ratio
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT 
                    CASE 
                        WHEN sum(blks_hit + blks_read) = 0 THEN 1.0
                        ELSE sum(blks_hit)::float / sum(blks_hit + blks_read)::float
                    END as ratio
                FROM pg_stat_database 
                WHERE datname = current_database()";
            stats["cache_hit_ratio"] = await cmd.ExecuteScalarAsync(cancellationToken) ?? 0.0;
        }

        // Deadlocks
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT deadlocks FROM pg_stat_database WHERE datname = current_database()";
            stats["deadlocks"] = await cmd.ExecuteScalarAsync(cancellationToken) ?? 0;
        }

        return stats;
    }
}

// Enhanced Redis Health Check
public class EnhancedRedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<EnhancedRedisHealthCheck> _logger;
    private readonly HealthCheckOptions _options;

    public EnhancedRedisHealthCheck(
        IConnectionMultiplexer redis,
        ILogger<EnhancedRedisHealthCheck> logger,
        IOptions<HealthCheckOptions> options)
    {
        _redis = redis;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var database = _redis.GetDatabase();
            
            // Ping test
            var pingTime = await database.PingAsync();
            
            // Get server info
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var info = await server.InfoAsync();
            
            stopwatch.Stop();
            
            var memorySection = info.FirstOrDefault(s => s.Key == "Memory");
            var statsSection = info.FirstOrDefault(s => s.Key == "Stats");
            
            var data = new Dictionary<string, object>
            {
                ["responseTime"] = stopwatch.ElapsedMilliseconds,
                ["pingTime"] = pingTime.TotalMilliseconds,
                ["connectedClients"] = GetInfoValue(info, "Clients", "connected_clients") ?? "0",
                ["usedMemory"] = GetInfoValue(info, "Memory", "used_memory_human") ?? "0",
                ["hitRate"] = CalculateHitRate(statsSection),
                ["opsPerSecond"] = GetInfoValue(info, "Stats", "instantaneous_ops_per_sec") ?? "0"
            };

            // Check memory usage
            var usedMemoryBytes = long.Parse(GetInfoValue(info, "Memory", "used_memory") ?? "0");
            var maxMemoryBytes = long.Parse(GetInfoValue(info, "Memory", "maxmemory") ?? "0");
            
            if (maxMemoryBytes > 0 && usedMemoryBytes > maxMemoryBytes * 0.9)
            {
                return HealthCheckResult.Degraded("High memory usage", null, data);
            }

            if (pingTime.TotalMilliseconds > _options.RedisPingThreshold)
            {
                return HealthCheckResult.Degraded("High latency", null, data);
            }

            return HealthCheckResult.Healthy("Redis is responsive", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            
            var data = new Dictionary<string, object>
            {
                ["responseTime"] = stopwatch.ElapsedMilliseconds,
                ["error"] = ex.Message
            };
            
            return HealthCheckResult.Unhealthy("Redis connection failed", ex, data);
        }
    }

    private string GetInfoValue(IGrouping<string, KeyValuePair<string, string>>[] info, string section, string key)
    {
        var sectionData = info.FirstOrDefault(s => s.Key == section);
        return sectionData?.FirstOrDefault(kvp => kvp.Key == key).Value;
    }

    private double CalculateHitRate(IGrouping<string, KeyValuePair<string, string>> statsSection)
    {
        if (statsSection == null) return 0;

        var hits = long.Parse(statsSection.FirstOrDefault(kvp => kvp.Key == "keyspace_hits").Value ?? "0");
        var misses = long.Parse(statsSection.FirstOrDefault(kvp => kvp.Key == "keyspace_misses").Value ?? "0");
        
        var total = hits + misses;
        return total == 0 ? 0 : (double)hits / total;
    }
}

// Service Dependencies Health Check
public class ServiceDependenciesHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ServiceDependenciesHealthCheck> _logger;
    private readonly ServiceDependencyOptions _options;

    public ServiceDependenciesHealthCheck(
        IHttpClientFactory httpClientFactory,
        ILogger<ServiceDependenciesHealthCheck> logger,
        IOptions<ServiceDependencyOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var httpClient = _httpClientFactory.CreateClient("HealthCheck");
        var results = new Dictionary<string, object>();
        var unhealthyServices = new List<string>();
        var degradedServices = new List<string>();

        foreach (var dependency in _options.Dependencies)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var response = await httpClient.GetAsync(dependency.HealthEndpoint, cancellationToken);
                stopwatch.Stop();

                results[dependency.Name] = new
                {
                    status = response.IsSuccessStatusCode ? "Healthy" : "Unhealthy",
                    responseTime = stopwatch.ElapsedMilliseconds,
                    statusCode = (int)response.StatusCode
                };

                if (!response.IsSuccessStatusCode)
                {
                    if (dependency.Critical)
                    {
                        unhealthyServices.Add(dependency.Name);
                    }
                    else
                    {
                        degradedServices.Add(dependency.Name);
                    }
                }
                else if (stopwatch.ElapsedMilliseconds > dependency.TimeoutMs * 0.8)
                {
                    degradedServices.Add($"{dependency.Name} (slow response)");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check health of {Service}", dependency.Name);
                
                results[dependency.Name] = new
                {
                    status = "Unreachable",
                    error = ex.Message
                };

                if (dependency.Critical)
                {
                    unhealthyServices.Add(dependency.Name);
                }
                else
                {
                    degradedServices.Add(dependency.Name);
                }
            }
        }

        if (unhealthyServices.Any())
        {
            return HealthCheckResult.Unhealthy(
                $"Critical services unhealthy: {string.Join(", ", unhealthyServices)}", 
                null, 
                results);
        }

        if (degradedServices.Any())
        {
            return HealthCheckResult.Degraded(
                $"Services degraded: {string.Join(", ", degradedServices)}", 
                null, 
                results);
        }

        return HealthCheckResult.Healthy("All service dependencies are healthy", results);
    }
}

// Disk Space Health Check
public class DiskSpaceHealthCheck : IHealthCheck
{
    private readonly DiskSpaceOptions _options;
    private readonly ILogger<DiskSpaceHealthCheck> _logger;

    public DiskSpaceHealthCheck(IOptions<DiskSpaceOptions> options, ILogger<DiskSpaceHealthCheck> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady && (_options.DrivesToCheck.Any() ? _options.DrivesToCheck.Contains(d.Name) : true))
                .ToList();

            var data = new Dictionary<string, object>();
            var warnings = new List<string>();

            foreach (var drive in drives)
            {
                var freeSpacePercent = (double)drive.AvailableFreeSpace / drive.TotalSize * 100;
                
                data[drive.Name] = new
                {
                    availableGB = drive.AvailableFreeSpace / (1024 * 1024 * 1024),
                    totalGB = drive.TotalSize / (1024 * 1024 * 1024),
                    freePercent = Math.Round(freeSpacePercent, 2)
                };

                if (freeSpacePercent < _options.MinimumFreeDiskPercent)
                {
                    warnings.Add($"{drive.Name} has only {freeSpacePercent:F2}% free space");
                }
            }

            if (warnings.Any())
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Low disk space: {string.Join(", ", warnings)}", 
                    null, 
                    data));
            }

            return Task.FromResult(HealthCheckResult.Healthy("Sufficient disk space available", data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Disk space health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy("Failed to check disk space", ex));
        }
    }
}

// Memory Health Check
public class MemoryHealthCheck : IHealthCheck
{
    private readonly MemoryHealthCheckOptions _options;

    public MemoryHealthCheck(IOptions<MemoryHealthCheckOptions> options)
    {
        _options = options.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // GC.GetMemoryInfo() not available in current .NET version
        var process = Process.GetCurrentProcess();
        var totalMemory = process.WorkingSet64;
        var usedMemory = GC.GetTotalMemory(false);
        var gen0Collections = GC.CollectionCount(0);
        var gen1Collections = GC.CollectionCount(1);
        var gen2Collections = GC.CollectionCount(2);

        var data = new Dictionary<string, object>
        {
            ["totalMemoryMB"] = totalMemory / (1024 * 1024),
            ["usedMemoryMB"] = usedMemory / (1024 * 1024),
            ["availableMemoryMB"] = (totalMemory - usedMemory) / (1024 * 1024),
            ["gen0Collections"] = gen0Collections,
            ["gen1Collections"] = gen1Collections,
            ["gen2Collections"] = gen2Collections,
            ["memoryPressure"] = usedMemory / (double)totalMemory * 100
        };

        var memoryUsagePercent = (double)usedMemory / totalMemory * 100;

        if (memoryUsagePercent > _options.MaxMemoryUsagePercent)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"High memory usage: {memoryUsagePercent:F2}%", 
                null, 
                data));
        }

        if (memoryUsagePercent > _options.MaxMemoryUsagePercent * 0.8)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Elevated memory usage: {memoryUsagePercent:F2}%", 
                null, 
                data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            $"Memory usage is normal: {memoryUsagePercent:F2}%", 
            data));
    }
}

// Certificate Health Check
public class CertificateHealthCheck : IHealthCheck
{
    private readonly CertificateHealthCheckOptions _options;
    private readonly ILogger<CertificateHealthCheck> _logger;

    public CertificateHealthCheck(IOptions<CertificateHealthCheckOptions> options, ILogger<CertificateHealthCheck> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var warnings = new List<string>();
            var data = new Dictionary<string, object>();

            foreach (var certPath in _options.CertificatePaths)
            {
                if (!File.Exists(certPath))
                {
                    warnings.Add($"Certificate not found: {certPath}");
                    continue;
                }

                var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(certPath);
                var daysUntilExpiry = (cert.NotAfter - DateTime.UtcNow).TotalDays;

                data[Path.GetFileName(certPath)] = new
                {
                    subject = cert.Subject,
                    issuer = cert.Issuer,
                    notBefore = cert.NotBefore,
                    notAfter = cert.NotAfter,
                    daysUntilExpiry = Math.Round(daysUntilExpiry, 0),
                    thumbprint = cert.Thumbprint
                };

                if (daysUntilExpiry <= 0)
                {
                    return Task.FromResult(HealthCheckResult.Unhealthy(
                        $"Certificate expired: {cert.Subject}", 
                        null, 
                        data));
                }

                if (daysUntilExpiry < _options.ExpiryWarningDays)
                {
                    warnings.Add($"Certificate expiring soon: {cert.Subject} ({daysUntilExpiry:F0} days)");
                }
            }

            if (warnings.Any())
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    string.Join("; ", warnings), 
                    null, 
                    data));
            }

            return Task.FromResult(HealthCheckResult.Healthy("All certificates are valid", data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Certificate health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy("Failed to check certificates", ex));
        }
    }
}

// Configuration classes
public class HealthCheckOptions
{
    public int MaxDatabaseConnections { get; set; } = 100;
    public int RedisPingThreshold { get; set; } = 100;
}

public class ServiceDependencyOptions
{
    public List<ServiceDependency> Dependencies { get; set; } = new();
}

public class ServiceDependency
{
    public string Name { get; set; }
    public string HealthEndpoint { get; set; }
    public bool Critical { get; set; }
    public int TimeoutMs { get; set; } = 5000;
}

public class DiskSpaceOptions
{
    public double MinimumFreeDiskPercent { get; set; } = 10.0;
    public List<string> DrivesToCheck { get; set; } = new();
}

public class MemoryHealthCheckOptions
{
    public double MaxMemoryUsagePercent { get; set; } = 90.0;
}

public class CertificateHealthCheckOptions
{
    public List<string> CertificatePaths { get; set; } = new();
    public int ExpiryWarningDays { get; set; } = 30;
}