using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeoServiceLayer.Core;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Core.Configuration;

/// <summary>
/// Comprehensive service registration for dependency injection.
/// Addresses DI configuration issues identified in the code review.
/// </summary>
public static class ServiceRegistration
{
    /// <summary>
    /// Registers all Neo Service Layer services with proper lifetimes and configurations.
    /// </summary>
    public static IServiceCollection AddNeoServiceLayer(this IServiceCollection services, IConfiguration configuration)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        // Note: Infrastructure service registration moved to composition root
        // to avoid circular dependencies. The Core layer should not depend on Infrastructure.

        // Register core infrastructure services
        services.AddCoreInfrastructure(configuration);

        // Register business services
        // services.AddBusinessServices(configuration); // Commented out to avoid dependencies

        // Infrastructure services (security, monitoring, resilience, TEE) will be
        // registered at the application composition root level

        return services;
    }

    /// <summary>
    /// Registers core infrastructure services.
    /// </summary>
    public static IServiceCollection AddCoreInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Service configuration
        services.Configure<ServiceOptions>(configuration.GetSection("Services"));
        services.AddSingleton<IServiceConfiguration, ServiceConfiguration>();

        // Secrets management
        services.AddSingleton<ISecretsManager, SecretsManager>();

        // HTTP client factory
        services.AddHttpClient();

        // Note: Other services moved to infrastructure layer to avoid circular dependencies
        // Services like IServiceRegistry, IHttpClientService, IBlockchainClientFactory
        // should be registered at the application composition root level

        return services;
    }

    /*
    /// <summary>
    /// Registers security services with proper configuration.
    /// NOTE: Moved to Infrastructure layer to avoid circular dependencies
    /// </summary>
    public static IServiceCollection AddSecurityServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Security configuration
        services.Configure<SecurityOptions>(configuration.GetSection("Security"));

        // Main security service
        services.AddSingleton<ISecurityService, SecurityService>();

        // Authentication services
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IRateLimitService, RateLimitService>();
        services.AddScoped<IUserRepository, UserRepository>();

        // Permission services
        services.AddScoped<IPermissionService, PermissionService>();

        return services;
    }
    */

    /*
    /// <summary>
    /// NOTE: Infrastructure services moved to avoid circular dependencies
    /// These should be registered at the application composition root level
    /// </summary>

    /// <summary>
    /// Registers monitoring and observability services.
    /// </summary>
    public static IServiceCollection AddMonitoringServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Observability configuration
        services.Configure<ObservabilityOptions>(configuration.GetSection("Observability"));

        // Main observability service
        services.AddSingleton<IObservabilityService, ObservabilityService>();

        // Monitoring services
        services.AddSingleton<IMonitoringService, MonitoringService>();

        // Add OpenTelemetry if configured
        var openTelemetryEnabled = configuration.GetValue<bool>("Observability:OpenTelemetry:Enabled", false);
        if (openTelemetryEnabled)
        {
            services.AddOpenTelemetryTracing(configuration);
            services.AddOpenTelemetryMetrics(configuration);
        }

        return services;
    }

    /// <summary>
    /// Registers resilience services.
    /// </summary>
    public static IServiceCollection AddResilienceServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Resilience configuration
        services.Configure<ResilienceOptions>(configuration.GetSection("Resilience"));

        // Main resilience service
        services.AddSingleton<IResilienceService, ResilienceService>();

        return services;
    }

    /// <summary>
    /// Registers TEE/SGX services with proper enclave management.
    /// </summary>
    public static IServiceCollection AddTeeServices(this IServiceCollection services, IConfiguration configuration)
    {
        // TEE configuration
        services.Configure<TeeOptions>(configuration.GetSection("Tee"));

        // Enclave services
        services.AddSingleton<IEnclaveManager, EnclaveManager>();

        // Register enclave wrapper based on configuration
        var enclaveType = configuration.GetValue<string>("Tee:EnclaveType", "Occlum");

        switch (enclaveType.ToLowerInvariant())
        {
            case "sgx":
            case "production":
                services.AddSingleton<IEnclaveWrapper, ProductionSGXEnclaveWrapper>();
                break;
            case "occlum":
            case "development":
            default:
                services.AddSingleton<IEnclaveWrapper, OcclumEnclaveWrapper>();
                break;
        }

        // Hosted service for enclave management
        services.AddHostedService<EnclaveHostService>();

        return services;
    }
    */

    /*
    /// <summary>
    /// Registers business services with proper dependencies.
    /// NOTE: Moved to avoid dependencies on Infrastructure layer types
    /// </summary>
    public static IServiceCollection AddBusinessServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Storage services
        services.AddScoped<IEnclaveStorageService, EnclaveStorageService>();

        // Compute services
        services.AddScoped<IComputeService, ComputeService>();

        // Blockchain services
        services.AddScoped<ISmartContractsService, SmartContractsService>();
        services.AddScoped<ICrossChainService, CrossChainService>();

        // Key management
        services.AddScoped<IKeyManagementService, KeyManagementService>();

        // Oracle services
        services.AddScoped<IOracleService, OracleService>();

        // Zero-knowledge services
        services.AddScoped<IZeroKnowledgeService, ZeroKnowledgeService>();

        // Voting services
        services.AddScoped<IVotingService, VotingService>();

        // Abstract account services
        services.AddScoped<IAbstractAccountService, AbstractAccountService>();

        // Notification services
        services.AddScoped<INotificationService, NotificationService>();

        // Configuration services
        services.AddSingleton<IConfigurationService, ConfigurationService>();

        // Health services
        services.AddSingleton<IHealthService, HealthService>();

        // Backup services
        services.AddScoped<IBackupService, BackupService>();

        // Compliance services
        services.AddScoped<IComplianceService, ComplianceService>();

        // Event subscription services
        services.AddScoped<IEventSubscriptionService, EventSubscriptionService>();

        // Network security services
        services.AddScoped<INetworkSecurityService, NetworkSecurityService>();

        // Proof of reserve services
        services.AddScoped<IProofOfReserveService, ProofOfReserveService>();

        // Randomness services
        services.AddScoped<IRandomnessService, RandomnessService>();

        // Secrets management services
        services.AddScoped<ISecretsManagementService, SecretsManagementService>();

        // Statistics services
        services.AddScoped<IStatisticsService, StatisticsService>();

        // Social recovery services
        services.AddScoped<ISocialRecoveryService, SocialRecoveryService>();

        // AI/ML services
        services.AddScoped<IPredictionService, PredictionService>();
        services.AddScoped<IPatternRecognitionService, PatternRecognitionService>();

        // Advanced services
        services.AddScoped<IFairOrderingService, FairOrderingService>();

        return services;
    }
    */

    /*
    /// <summary>
    /// Registers health checks for all services.
    /// NOTE: Commented out to avoid infrastructure dependencies
    /// </summary>
    public static IServiceCollection AddNeoHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddCheck<SecurityServiceHealthCheck>("security")
            .AddCheck<ObservabilityServiceHealthCheck>("observability")
            .AddCheck<ResilienceServiceHealthCheck>("resilience")
            .AddCheck<EnclaveServiceHealthCheck>("enclave")
            .AddCheck<StorageServiceHealthCheck>("storage")
            .AddCheck<DatabaseHealthCheck>("database")
            .AddCheck<ExternalApiHealthCheck>("external-apis");

        return services;
    }

    /// <summary>
    /// Adds OpenTelemetry tracing configuration.
    /// </summary>
    private static IServiceCollection AddOpenTelemetryTracing(this IServiceCollection services, IConfiguration configuration)
    {
        // This would integrate with OpenTelemetry SDK
        // Implementation depends on specific OpenTelemetry packages
        return services;
    }

    /// <summary>
    /// Adds OpenTelemetry metrics configuration.
    /// </summary>
    private static IServiceCollection AddOpenTelemetryMetrics(this IServiceCollection services, IConfiguration configuration)
    {
        // This would integrate with OpenTelemetry SDK
        // Implementation depends on specific OpenTelemetry packages
        return services;
    }

    /// <summary>
    /// Validates service registration and dependencies.
    /// </summary>
    public static void ValidateServiceRegistration(this IServiceProvider serviceProvider)
    {
        try
        {
            // Validate core services
            var logger = serviceProvider.GetService<ILogger<ServiceRegistration>>();
            logger?.LogInformation("Validating service registration...");

            // Validate security service
            var securityService = serviceProvider.GetRequiredService<ISecurityService>();
            _ = securityService.GetHealthAsync().GetAwaiter().GetResult();

            // Validate observability service
            var observabilityService = serviceProvider.GetRequiredService<IObservabilityService>();
            _ = observabilityService.GetHealthAsync().GetAwaiter().GetResult();

            // Validate resilience service
            var resilienceService = serviceProvider.GetRequiredService<IResilienceService>();
            _ = resilienceService.GetHealthAsync().GetAwaiter().GetResult();

            // Validate enclave manager
            var enclaveManager = serviceProvider.GetRequiredService<IEnclaveManager>();

            // Validate service registry
            var serviceRegistry = serviceProvider.GetRequiredService<IServiceRegistry>();

            logger?.LogInformation("Service registration validation completed successfully");
        }
        catch (Exception ex)
        {
            var logger = serviceProvider.GetService<ILogger<ServiceRegistration>>();
            logger?.LogError(ex, "Service registration validation failed");
            throw new InvalidOperationException("Service registration validation failed", ex);
        }
    }
    */
}

/// <summary>
/// Service configuration options.
/// </summary>
public class ServiceOptions
{
    public int DefaultTimeoutMs { get; set; } = 30000;
    public int MaxRetries { get; set; } = 3;
    public bool EnableDetailedLogging { get; set; } = false;
    public string LogLevel { get; set; } = "Information";
}

/// <summary>
/// Security configuration options.
/// </summary>
public class SecurityOptions
{
    public string EncryptionAlgorithm { get; set; } = "AES-256-GCM";
    public int KeyRotationIntervalHours { get; set; } = 24;
    public int MaxInputSizeMB { get; set; } = 10;
    public bool EnableRateLimiting { get; set; } = true;
    public int DefaultRateLimitRequests { get; set; } = 100;
    public int RateLimitWindowMinutes { get; set; } = 1;
}

/// <summary>
/// Observability configuration options.
/// </summary>
public class ObservabilityOptions
{
    public bool EnableTracing { get; set; } = true;
    public bool EnableMetrics { get; set; } = true;
    public bool EnableHealthChecks { get; set; } = true;
    public string ServiceName { get; set; } = "NeoServiceLayer";
    public string ServiceVersion { get; set; } = "1.0.0";

    public OpenTelemetryOptions OpenTelemetry { get; set; } = new();
}

/// <summary>
/// OpenTelemetry configuration options.
/// </summary>
public class OpenTelemetryOptions
{
    public bool Enabled { get; set; } = false;
    public string Endpoint { get; set; } = "";
    public string[] Sources { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Resilience configuration options.
/// </summary>
public class ResilienceOptions
{
    public int DefaultMaxRetries { get; set; } = 3;
    public int DefaultBackoffMs { get; set; } = 1000;
    public int CircuitBreakerFailureThreshold { get; set; } = 5;
    public int CircuitBreakerTimeoutMinutes { get; set; } = 1;
    public int CircuitBreakerResetTimeoutMinutes { get; set; } = 5;
}

/// <summary>
/// TEE configuration options.
/// </summary>
public class TeeOptions
{
    public string EnclaveType { get; set; } = "Occlum"; // "Occlum", "SGX", "Production"
    public string EnclavePath { get; set; } = "./enclave";
    public bool DebugMode { get; set; } = true;
    public int MaxEnclaveMemoryMB { get; set; } = 512;
    public bool EnableAttestation { get; set; } = true;
}
