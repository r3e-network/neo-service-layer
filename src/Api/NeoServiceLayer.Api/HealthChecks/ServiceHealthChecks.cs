using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Npgsql;
using StackExchange.Redis;
using RabbitMQ.Client;
using MongoDB.Driver;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Api.HealthChecks
{
    /// <summary>
    /// PostgreSQL database health check
    /// </summary>
    public class PostgreSqlHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;
        private readonly ILogger<PostgreSqlHealthCheck> _logger;

        public PostgreSqlHealthCheck(IConfiguration configuration, ILogger<PostgreSqlHealthCheck> logger)
        {
            _connectionString = configuration.GetConnectionString("PostgreSQL");
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);
                
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                var result = await command.ExecuteScalarAsync(cancellationToken);

                if (result != null && (int)result == 1)
                {
                    return HealthCheckResult.Healthy("PostgreSQL is responsive");
                }

                return HealthCheckResult.Unhealthy("PostgreSQL check query failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PostgreSQL health check failed");
                return HealthCheckResult.Unhealthy($"PostgreSQL connection failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Redis cache health check
    /// </summary>
    public class RedisHealthCheck : IHealthCheck
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisHealthCheck> _logger;

        public RedisHealthCheck(IConnectionMultiplexer redis, ILogger<RedisHealthCheck> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var database = _redis.GetDatabase();
                var pingResult = await database.PingAsync();
                
                if (pingResult.TotalMilliseconds < 1000)
                {
                    var data = new Dictionary<string, object>
                    {
                        ["ResponseTime"] = $"{pingResult.TotalMilliseconds:F2}ms",
                        ["Connected"] = _redis.IsConnected
                    };
                    
                    return HealthCheckResult.Healthy("Redis is responsive", data);
                }

                return HealthCheckResult.Degraded($"Redis is slow: {pingResult.TotalMilliseconds:F2}ms");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis health check failed");
                return HealthCheckResult.Unhealthy($"Redis connection failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// RabbitMQ message broker health check
    /// </summary>
    public class RabbitMqHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitMqHealthCheck> _logger;

        public RabbitMqHealthCheck(IConfiguration configuration, ILogger<RabbitMqHealthCheck> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _configuration["EventBus:RabbitMQ:Host"] ?? "localhost",
                    Port = _configuration.GetValue<int>("EventBus:RabbitMQ:Port", 5672),
                    UserName = _configuration["EventBus:RabbitMQ:Username"] ?? "guest",
                    Password = _configuration["EventBus:RabbitMQ:Password"] ?? "guest"
                };

                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();
                
                var data = new Dictionary<string, object>
                {
                    ["IsOpen"] = connection.IsOpen,
                    ["ChannelOpen"] = channel.IsOpen
                };

                if (connection.IsOpen && channel.IsOpen)
                {
                    return HealthCheckResult.Healthy("RabbitMQ is connected", data);
                }

                return HealthCheckResult.Unhealthy("RabbitMQ connection is not open");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RabbitMQ health check failed");
                return HealthCheckResult.Unhealthy($"RabbitMQ connection failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// MongoDB health check
    /// </summary>
    public class MongoDbHealthCheck : IHealthCheck
    {
        private readonly IMongoDatabase _database;
        private readonly ILogger<MongoDbHealthCheck> _logger;

        public MongoDbHealthCheck(IMongoDatabase database, ILogger<MongoDbHealthCheck> logger)
        {
            _database = database;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var command = new BsonDocumentCommand<BsonDocument>(new BsonDocument { { "ping", 1 } });
                await _database.RunCommandAsync(command, cancellationToken: cancellationToken);
                
                return HealthCheckResult.Healthy("MongoDB is responsive");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MongoDB health check failed");
                return HealthCheckResult.Unhealthy($"MongoDB connection failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// SGX Enclave health check
    /// </summary>
    public class EnclaveHealthCheck : IHealthCheck
    {
        private readonly IEnclaveService _enclaveService;
        private readonly ILogger<EnclaveHealthCheck> _logger;

        public EnclaveHealthCheck(IEnclaveService enclaveService, ILogger<EnclaveHealthCheck> logger)
        {
            _enclaveService = enclaveService;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var isInitialized = _enclaveService.IsInitialized;
                var attestationValid = await _enclaveService.ValidateAttestationAsync();
                
                var data = new Dictionary<string, object>
                {
                    ["Initialized"] = isInitialized,
                    ["AttestationValid"] = attestationValid,
                    ["EnclaveId"] = _enclaveService.EnclaveId
                };

                if (isInitialized && attestationValid)
                {
                    return HealthCheckResult.Healthy("SGX Enclave is operational", data);
                }
                else if (isInitialized)
                {
                    return HealthCheckResult.Degraded("SGX Enclave attestation invalid", data);
                }
                else
                {
                    return HealthCheckResult.Unhealthy("SGX Enclave not initialized", data);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Enclave health check failed");
                return HealthCheckResult.Unhealthy($"Enclave check failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Blockchain node health check
    /// </summary>
    public class BlockchainHealthCheck : IHealthCheck
    {
        private readonly IBlockchainClient _blockchainClient;
        private readonly ILogger<BlockchainHealthCheck> _logger;

        public BlockchainHealthCheck(IBlockchainClient blockchainClient, ILogger<BlockchainHealthCheck> logger)
        {
            _blockchainClient = blockchainClient;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var blockNumber = await _blockchainClient.GetBlockNumberAsync();
                var peerCount = await _blockchainClient.GetPeerCountAsync();
                var isSyncing = await _blockchainClient.IsSyncingAsync();
                
                var data = new Dictionary<string, object>
                {
                    ["BlockNumber"] = blockNumber,
                    ["PeerCount"] = peerCount,
                    ["IsSyncing"] = isSyncing
                };

                if (peerCount > 0 && !isSyncing)
                {
                    return HealthCheckResult.Healthy("Blockchain node is synchronized", data);
                }
                else if (peerCount > 0 && isSyncing)
                {
                    return HealthCheckResult.Degraded("Blockchain node is syncing", data);
                }
                else
                {
                    return HealthCheckResult.Unhealthy("Blockchain node has no peers", data);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Blockchain health check failed");
                return HealthCheckResult.Unhealthy($"Blockchain connection failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// External service health check (for Oracle providers)
    /// </summary>
    public class ExternalServiceHealthCheck : IHealthCheck
    {
        private readonly HttpClient _httpClient;
        private readonly string _serviceName;
        private readonly string _healthEndpoint;
        private readonly ILogger<ExternalServiceHealthCheck> _logger;

        public ExternalServiceHealthCheck(
            HttpClient httpClient,
            string serviceName,
            string healthEndpoint,
            ILogger<ExternalServiceHealthCheck> logger)
        {
            _httpClient = httpClient;
            _serviceName = serviceName;
            _healthEndpoint = healthEndpoint;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync(_healthEndpoint, cancellationToken);
                
                var data = new Dictionary<string, object>
                {
                    ["StatusCode"] = (int)response.StatusCode,
                    ["Service"] = _serviceName
                };

                if (response.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Healthy($"{_serviceName} is accessible", data);
                }

                return HealthCheckResult.Unhealthy($"{_serviceName} returned {response.StatusCode}", data);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "{ServiceName} health check failed", _serviceName);
                return HealthCheckResult.Degraded($"{_serviceName} is not accessible: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ServiceName} health check error", _serviceName);
                return HealthCheckResult.Unhealthy($"{_serviceName} check failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Disk space health check
    /// </summary>
    public class DiskSpaceHealthCheck : IHealthCheck
    {
        private readonly long _minimumFreeMb;
        private readonly ILogger<DiskSpaceHealthCheck> _logger;

        public DiskSpaceHealthCheck(IConfiguration configuration, ILogger<DiskSpaceHealthCheck> logger)
        {
            _minimumFreeMb = configuration.GetValue<long>("HealthChecks:MinimumFreeDiskSpaceMb", 1024);
            _logger = logger;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var drives = System.IO.DriveInfo.GetDrives()
                    .Where(d => d.IsReady && d.DriveType == System.IO.DriveType.Fixed);

                var data = new Dictionary<string, object>();
                var warnings = new List<string>();

                foreach (var drive in drives)
                {
                    var freeSpaceMb = drive.AvailableFreeSpace / (1024 * 1024);
                    var totalSpaceMb = drive.TotalSize / (1024 * 1024);
                    var usedPercentage = ((totalSpaceMb - freeSpaceMb) * 100.0) / totalSpaceMb;

                    data[$"{drive.Name}_FreeMB"] = freeSpaceMb;
                    data[$"{drive.Name}_UsedPercent"] = $"{usedPercentage:F2}%";

                    if (freeSpaceMb < _minimumFreeMb)
                    {
                        warnings.Add($"{drive.Name} has only {freeSpaceMb}MB free");
                    }
                }

                if (warnings.Any())
                {
                    return Task.FromResult(HealthCheckResult.Degraded(
                        $"Low disk space: {string.Join(", ", warnings)}", data));
                }

                return Task.FromResult(HealthCheckResult.Healthy("Sufficient disk space available", data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Disk space health check failed");
                return Task.FromResult(HealthCheckResult.Unhealthy($"Disk check failed: {ex.Message}"));
            }
        }
    }

    /// <summary>
    /// Memory usage health check
    /// </summary>
    public class MemoryHealthCheck : IHealthCheck
    {
        private readonly long _maximumAllocatedMb;
        private readonly ILogger<MemoryHealthCheck> _logger;

        public MemoryHealthCheck(IConfiguration configuration, ILogger<MemoryHealthCheck> logger)
        {
            _maximumAllocatedMb = configuration.GetValue<long>("HealthChecks:MaximumMemoryAllocatedMb", 2048);
            _logger = logger;
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var allocated = GC.GetTotalMemory(false) / (1024 * 1024);
                var gen0 = GC.CollectionCount(0);
                var gen1 = GC.CollectionCount(1);
                var gen2 = GC.CollectionCount(2);

                var data = new Dictionary<string, object>
                {
                    ["AllocatedMB"] = allocated,
                    ["Gen0Collections"] = gen0,
                    ["Gen1Collections"] = gen1,
                    ["Gen2Collections"] = gen2
                };

                if (allocated <= _maximumAllocatedMb)
                {
                    return Task.FromResult(HealthCheckResult.Healthy(
                        $"Memory usage is {allocated}MB", data));
                }

                if (allocated <= _maximumAllocatedMb * 1.5)
                {
                    return Task.FromResult(HealthCheckResult.Degraded(
                        $"Memory usage is high: {allocated}MB", data));
                }

                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Memory usage exceeded limit: {allocated}MB", data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Memory health check failed");
                return Task.FromResult(HealthCheckResult.Unhealthy($"Memory check failed: {ex.Message}"));
            }
        }
    }

    /// <summary>
    /// Composite health check for all services
    /// </summary>
    public class SystemHealthCheck : IHealthCheck
    {
        private readonly IEnumerable<IService> _services;
        private readonly ILogger<SystemHealthCheck> _logger;

        public SystemHealthCheck(IEnumerable<IService> services, ILogger<SystemHealthCheck> logger)
        {
            _services = services;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var results = new Dictionary<string, object>();
            var unhealthyServices = new List<string>();
            var degradedServices = new List<string>();

            foreach (var service in _services)
            {
                try
                {
                    var health = await service.GetHealthAsync();
                    results[service.Name] = health.ToString();

                    if (health == ServiceHealth.Unhealthy)
                        unhealthyServices.Add(service.Name);
                    else if (health == ServiceHealth.Degraded)
                        degradedServices.Add(service.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to check health of service {ServiceName}", service.Name);
                    unhealthyServices.Add(service.Name);
                    results[service.Name] = "Error";
                }
            }

            if (unhealthyServices.Any())
            {
                return HealthCheckResult.Unhealthy(
                    $"Unhealthy services: {string.Join(", ", unhealthyServices)}", results);
            }

            if (degradedServices.Any())
            {
                return HealthCheckResult.Degraded(
                    $"Degraded services: {string.Join(", ", degradedServices)}", results);
            }

            return HealthCheckResult.Healthy("All services are healthy", results);
        }
    }
}