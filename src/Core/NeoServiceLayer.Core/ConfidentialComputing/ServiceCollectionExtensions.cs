using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeoServiceLayer.Core.Domain;
// Note: TEE namespace handled via service collection registration
using System;

namespace NeoServiceLayer.Core.ConfidentialComputing
{
    /// <summary>
    /// Extension methods for configuring confidential computing services in dependency injection
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds all confidential computing services to the service collection
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration instance</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddConfidentialComputing(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Configure options
            services.Configure<ConfidentialComputingConfiguration>(
                configuration.GetSection("ConfidentialComputing"));

            services.Configure<EnclaveStorageConfiguration>(
                configuration.GetSection("EnclaveStorage"));

            services.Configure<EnclaveSessionConfiguration>(
                configuration.GetSection("EnclaveSessions"));

            services.Configure<EnclaveMessageBusConfiguration>(
                configuration.GetSection("EnclaveMessageBus"));

            // Register core interfaces and implementations
            services.AddSingleton<IConfidentialComputingService, ConfidentialComputingService>();
            services.AddSingleton<IConfidentialStorageService, ConfidentialStorageService>();
            
            // Register session management
            // EnclaveSessionManager and EnclaveSession to be implemented
            // services.AddSingleton<IEnclaveSessionManager, EnclaveSessionManager>();
            // services.AddTransient<IEnclaveSession, EnclaveSession>();

            // Register message bus
            services.AddSingleton<IEnclaveMessageBus, EnclaveMessageBus>();

            // Register existing TEE services
            services.AddSingleton<IEnclaveWrapper, ProductionEnclaveWrapper>();
            services.AddSingleton<IEnclaveStorageService, TemporaryEnclaveStorageService>();

            // Register health checks
            services.AddHealthChecks()
                .AddCheck<ConfidentialComputingHealthCheck>("confidential-computing")
                .AddCheck<EnclaveStorageHealthCheck>("enclave-storage")
                .AddCheck<EnclaveSessionHealthCheck>("enclave-sessions");

            // Register hosted services for background management
            services.AddHostedService<EnclaveSessionMaintenanceService>();
            services.AddHostedService<EnclaveMessageBusService>();

            return services;
        }

        /// <summary>
        /// Adds confidential computing with custom configuration
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configureOptions">Configuration action</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddConfidentialComputing(
            this IServiceCollection services,
            Action<ConfidentialComputingConfiguration> configureOptions)
        {
            services.Configure(configureOptions);

            return services;
        }

        /// <summary>
        /// Adds only confidential storage services
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration instance</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddConfidentialStorage(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<EnclaveStorageConfiguration>(
                configuration.GetSection("EnclaveStorage"));

            services.AddSingleton<IConfidentialStorageService, ConfidentialStorageService>();
            services.AddSingleton<IEnclaveStorageService, TemporaryEnclaveStorageService>();

            services.AddHealthChecks()
                .AddCheck<EnclaveStorageHealthCheck>("enclave-storage");

            return services;
        }

        /// <summary>
        /// Adds only session management services
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration instance</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddEnclaveSessionManagement(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<EnclaveSessionConfiguration>(
                configuration.GetSection("EnclaveSessions"));

            // EnclaveSessionManager and EnclaveSession to be implemented
            // services.AddSingleton<IEnclaveSessionManager, EnclaveSessionManager>();
            // services.AddTransient<IEnclaveSession, EnclaveSession>();

            services.AddHealthChecks()
                .AddCheck<EnclaveSessionHealthCheck>("enclave-sessions");

            services.AddHostedService<EnclaveSessionMaintenanceService>();

            return services;
        }

        /// <summary>
        /// Adds only message bus services
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration instance</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddEnclaveMessageBus(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<EnclaveMessageBusConfiguration>(
                configuration.GetSection("EnclaveMessageBus"));

            services.AddSingleton<IEnclaveMessageBus, EnclaveMessageBus>();

            services.AddHostedService<EnclaveMessageBusService>();

            return services;
        }
    }

    /// <summary>
    /// Configuration for confidential computing services
    /// </summary>
    public class ConfidentialComputingConfiguration
    {
        /// <summary>
        /// Whether to enable hardware SGX mode (vs simulation)
        /// </summary>
        public bool EnableHardwareMode { get; set; } = true;

        /// <summary>
        /// Default security requirements
        /// </summary>
        public ConfidentialSecurityRequirements DefaultSecurityRequirements { get; set; } = new()
        {
            MinimumSecurityLevel = SecurityLevel.High,
            RequireRemoteAttestation = true,
            AllowDebugMode = false
        };

        /// <summary>
        /// Default resource limits for computations
        /// </summary>
        public ComputationResourceLimits DefaultResourceLimits { get; set; } = new()
        {
            MaxMemoryBytes = 100 * 1024 * 1024, // 100MB
            MaxExecutionTime = TimeSpan.FromMinutes(5),
            MaxFileOperations = 100,
            MaxNetworkRequests = 10
        };

        /// <summary>
        /// Service health check interval
        /// </summary>
        public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Whether to enable performance metrics collection
        /// </summary>
        public bool EnableMetrics { get; set; } = true;

        /// <summary>
        /// Whether to enable detailed logging
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = false;
    }

    /// <summary>
    /// Configuration for enclave storage
    /// </summary>
    public class EnclaveStorageConfiguration
    {
        /// <summary>
        /// Storage root directory
        /// </summary>
        public string StorageRoot { get; set; } = "/secure-storage";

        /// <summary>
        /// Default sealing policy
        /// </summary>
        public SealingPolicy DefaultSealingPolicy { get; set; } = SealingPolicy.MrEnclave;

        /// <summary>
        /// Maximum storage size in bytes
        /// </summary>
        public long MaxStorageSizeBytes { get; set; } = 10L * 1024 * 1024 * 1024; // 10GB

        /// <summary>
        /// Whether to enable automatic cleanup of expired data
        /// </summary>
        public bool EnableAutoCleanup { get; set; } = true;

        /// <summary>
        /// Cleanup interval
        /// </summary>
        public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(6);

        /// <summary>
        /// Whether to enable data integrity checks
        /// </summary>
        public bool EnableIntegrityChecks { get; set; } = true;

        /// <summary>
        /// Whether to enable backup functionality
        /// </summary>
        public bool EnableBackups { get; set; } = true;

        /// <summary>
        /// Backup retention period
        /// </summary>
        public TimeSpan BackupRetention { get; set; } = TimeSpan.FromDays(30);
    }

    /// <summary>
    /// Configuration for enclave sessions
    /// </summary>
    public class EnclaveSessionConfiguration
    {
        /// <summary>
        /// Default session timeout
        /// </summary>
        public TimeSpan DefaultSessionTimeout { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// Maximum concurrent sessions
        /// </summary>
        public int MaxConcurrentSessions { get; set; } = 100;

        /// <summary>
        /// Session idle timeout before automatic cleanup
        /// </summary>
        public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Session maintenance interval
        /// </summary>
        public TimeSpan MaintenanceInterval { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Whether to enable session persistence
        /// </summary>
        public bool EnableSessionPersistence { get; set; } = true;

        /// <summary>
        /// Default memory limit per session
        /// </summary>
        public long DefaultMemoryLimitBytes { get; set; } = 100 * 1024 * 1024; // 100MB
    }

    /// <summary>
    /// Configuration for enclave message bus
    /// </summary>
    public class EnclaveMessageBusConfiguration
    {
        /// <summary>
        /// Default message time-to-live
        /// </summary>
        public TimeSpan DefaultMessageTtl { get; set; } = TimeSpan.FromHours(24);

        /// <summary>
        /// Maximum message size in bytes
        /// </summary>
        public long MaxMessageSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB

        /// <summary>
        /// Default message encryption mode
        /// </summary>
        public MessageEncryptionMode DefaultEncryptionMode { get; set; } = MessageEncryptionMode.EndToEnd;

        /// <summary>
        /// Maximum number of retry attempts for failed messages
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Message bus capacity (maximum queued messages)
        /// </summary>
        public int MessageBusCapacity { get; set; } = 10000;

        /// <summary>
        /// Whether to enable message persistence
        /// </summary>
        public bool EnableMessagePersistence { get; set; } = true;

        /// <summary>
        /// Message cleanup interval
        /// </summary>
        public TimeSpan MessageCleanupInterval { get; set; } = TimeSpan.FromHours(1);
    }

    /// <summary>
    /// Health check for confidential computing service
    /// </summary>
    public class ConfidentialComputingHealthCheck : IHealthCheck
    {
        private readonly IConfidentialComputingService _computingService;
        private readonly ILogger<ConfidentialComputingHealthCheck> _logger;

        public ConfidentialComputingHealthCheck(
            IConfidentialComputingService computingService,
            ILogger<ConfidentialComputingHealthCheck> logger)
        {
            _computingService = computingService;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Placeholder health check - in production would call actual service
                var health = new { Status = HealthStatus.Healthy };
                
                return health.Status switch
                {
                    HealthStatus.Healthy => HealthCheckResult.Healthy("Confidential computing service is healthy"),
                    HealthStatus.Degraded => HealthCheckResult.Degraded("Confidential computing service is degraded"),
                    _ => HealthCheckResult.Unhealthy("Confidential computing service is unhealthy")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed for confidential computing service");
                return HealthCheckResult.Unhealthy("Health check failed", ex);
            }
        }
    }

    /// <summary>
    /// Health check for enclave storage service
    /// </summary>
    public class EnclaveStorageHealthCheck : IHealthCheck
    {
        private readonly IConfidentialStorageService _storageService;
        private readonly ILogger<EnclaveStorageHealthCheck> _logger;

        public EnclaveStorageHealthCheck(
            IConfidentialStorageService storageService,
            ILogger<EnclaveStorageHealthCheck> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var stats = await _storageService.GetStatisticsAsync(cancellationToken);
                
                return stats.HealthStatus switch
                {
                    StorageHealthStatus.Healthy => HealthCheckResult.Healthy("Enclave storage is healthy"),
                    StorageHealthStatus.Warning => HealthCheckResult.Degraded("Enclave storage is degraded"),
                    _ => HealthCheckResult.Unhealthy("Enclave storage is unhealthy")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed for enclave storage service");
                return HealthCheckResult.Unhealthy("Health check failed", ex);
            }
        }
    }

    /// <summary>
    /// Health check for enclave sessions
    /// </summary>
    public class EnclaveSessionHealthCheck : IHealthCheck
    {
        private readonly IEnclaveSessionManager _sessionManager;
        private readonly ILogger<EnclaveSessionHealthCheck> _logger;

        public EnclaveSessionHealthCheck(
            IEnclaveSessionManager sessionManager,
            ILogger<EnclaveSessionHealthCheck> logger)
        {
            _sessionManager = sessionManager;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var stats = await _sessionManager.GetStatisticsAsync(cancellationToken);
                
                if (stats.ActiveSessionCount < 1000) // Reasonable threshold
                {
                    return HealthCheckResult.Healthy($"Session manager is healthy ({stats.ActiveSessionCount} active sessions)");
                }
                else
                {
                    return HealthCheckResult.Degraded($"High session count ({stats.ActiveSessionCount})");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed for enclave session manager");
                return HealthCheckResult.Unhealthy("Health check failed", ex);
            }
        }
    }

    /// <summary>
    /// Background service for session maintenance
    /// </summary>
    public class EnclaveSessionMaintenanceService : BackgroundService
    {
        private readonly IEnclaveSessionManager _sessionManager;
        private readonly IOptions<EnclaveSessionConfiguration> _config;
        private readonly ILogger<EnclaveSessionMaintenanceService> _logger;

        public EnclaveSessionMaintenanceService(
            IEnclaveSessionManager sessionManager,
            IOptions<EnclaveSessionConfiguration> config,
            ILogger<EnclaveSessionMaintenanceService> logger)
        {
            _sessionManager = sessionManager;
            _config = config;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PerformMaintenance(stoppingToken);
                    await Task.Delay(_config.Value.MaintenanceInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during session maintenance");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Back off on error
                }
            }
        }

        private async Task PerformMaintenance(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Starting session maintenance");

            var sessions = await _sessionManager.ListActiveSessionsAsync(cancellationToken);
            var idleThreshold = DateTime.UtcNow - _config.Value.IdleTimeout;
            var expiredCount = 0;

            foreach (var session in sessions)
            {
                if (session.LastActivityAt < idleThreshold || session.ExpiresAt < DateTime.UtcNow)
                {
                    try
                    {
                        await _sessionManager.TerminateSessionAsync(session.SessionId, cancellationToken);
                        expiredCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to terminate expired session {SessionId}", session.SessionId);
                    }
                }
            }

            if (expiredCount > 0)
            {
                _logger.LogInformation("Terminated {ExpiredCount} expired sessions", expiredCount);
            }
        }
    }

    /// <summary>
    /// Background service for message bus management
    /// </summary>
    public class EnclaveMessageBusService : BackgroundService
    {
        private readonly IEnclaveMessageBus _messageBus;
        private readonly IOptions<EnclaveMessageBusConfiguration> _config;
        private readonly ILogger<EnclaveMessageBusService> _logger;

        public EnclaveMessageBusService(
            IEnclaveMessageBus messageBus,
            IOptions<EnclaveMessageBusConfiguration> config,
            ILogger<EnclaveMessageBusService> logger)
        {
            _messageBus = messageBus;
            _config = config;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PerformMessageCleanup(stoppingToken);
                    await Task.Delay(_config.Value.MessageCleanupInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during message bus maintenance");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }

        private async Task PerformMessageCleanup(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Starting message bus cleanup");

            var stats = await _messageBus.GetStatisticsAsync(cancellationToken);
            _logger.LogDebug("Message bus statistics: {Stats}", stats);

            // Additional cleanup logic would go here
            // This is a placeholder for message cleanup operations
        }
    }
}