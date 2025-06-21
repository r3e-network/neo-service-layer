using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using NeoServiceLayer.Core;
using NeoServiceLayer.Tee.Enclave;
using NeoServiceLayer.Tee.Host.Services;
// Service dependencies commented out due to missing project references
// using NeoServiceLayer.Services.KeyManagement;
// using NeoServiceLayer.Services.Oracle;
// using NeoServiceLayer.Services.Storage;
// using NeoServiceLayer.Services.Voting;
// using NeoServiceLayer.Services.Compliance;
// using NeoServiceLayer.Services.CrossChain;
// using NeoServiceLayer.Services.Automation;
// using NeoServiceLayer.Services.Health;
// using NeoServiceLayer.Services.Monitoring;
// using NeoServiceLayer.Services.Notification;
// using NeoServiceLayer.Services.EventSubscription;
// using NeoServiceLayer.Services.Randomness;
// using NeoServiceLayer.Services.Compute;
// using NeoServiceLayer.Services.ProofOfReserve;
// using NeoServiceLayer.Services.ZeroKnowledge;
// using NeoServiceLayer.AI.Prediction;
// using NeoServiceLayer.AI.PatternRecognition;
// using NeoServiceLayer.Advanced.FairOrdering;
// using NeoServiceLayer.Services.AbstractAccount;
// using NeoServiceLayer.Services.Backup;
// using NeoServiceLayer.Services.Configuration;
using NeoServiceLayer.Infrastructure.Security;

namespace NeoServiceLayer.Infrastructure;

/// <summary>
/// Extension methods for registering Neo Service Layer components.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all Neo Service Layer services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNeoServiceLayer(this IServiceCollection services, IConfiguration configuration)
    {
        // Register core infrastructure
        services.AddCoreInfrastructure(configuration);
        
        // Register blockchain clients
        services.AddBlockchainClients(configuration);
        
        // Register service layer components
        services.AddServiceLayerComponents(configuration);
        
        // Register advanced features
        services.AddAdvancedFeatures(configuration);
        
        // Register security logging and monitoring
        services.AddSecurityLogging(configuration);
        
        return services;
    }

    /// <summary>
    /// Adds core infrastructure services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCoreInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register HTTP client
        services.AddHttpClient();
        
        // Register blockchain configuration
        services.Configure<BlockchainConfiguration>(configuration.GetSection("Blockchain"));
        
        // Register blockchain client factory
        services.AddSingleton<IBlockchainClientFactory, BlockchainClientFactory>();
        
        return services;
    }

    /// <summary>
    /// Adds blockchain client services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBlockchainClients(this IServiceCollection services, IConfiguration configuration)
    {
        // Register blockchain clients as factory-created instances
        services.AddTransient<IBlockchainClient>(provider =>
        {
            var factory = provider.GetRequiredService<IBlockchainClientFactory>();
            // Default to Neo N3, but this could be configurable
            return factory.CreateClient(BlockchainType.NeoN3);
        });

        return services;
    }

    /// <summary>
    /// Adds service layer components.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddServiceLayerComponents(this IServiceCollection services, IConfiguration configuration)
    {
        // Note: Individual service registrations commented out due to missing project references
        // These should be added back when the corresponding service projects are included
        
        // Register key management service
        // services.AddKeyManagementService(configuration);
        
        // Register oracle service
        // services.AddOracleService(configuration);
        
        // Register storage service
        // services.AddStorageService(configuration);
        
        // Register voting service
        // services.AddVotingService(configuration);
        
        // Register compliance service
        // services.AddComplianceService(configuration);
        
        // Register cross-chain service
        // services.AddCrossChainService(configuration);
        
        // Register automation service
        // services.AddAutomationService(configuration);
        
        // Register health service
        // services.AddHealthService(configuration);
        
        // Register monitoring service
        // services.AddMonitoringService(configuration);
        
        // Register notification service
        // services.AddNotificationService(configuration);
        
        // Register event subscription service
        // services.AddEventSubscriptionService(configuration);
        
        // Register randomness service
        // services.AddRandomnessService(configuration);
        
        // Register compute service
        // services.AddComputeService(configuration);
        
        // Register proof of reserve service
        // services.AddProofOfReserveService(configuration);
        
        // Register abstract account service
        // services.AddAbstractAccountService(configuration);
        
        // Register backup service
        // services.AddBackupService(configuration);
        
        // Register configuration service
        // services.AddConfigurationService(configuration);
        
        return services;
    }

    /// <summary>
    /// Adds advanced features.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAdvancedFeatures(this IServiceCollection services, IConfiguration configuration)
    {
        // Register AI services
        // services.AddAIServices(configuration);
        
        // Register zero-knowledge services
        // services.AddZeroKnowledgeService(configuration);
        
        // Register fair ordering service
        // services.AddFairOrderingService(configuration);
        
        // Register TEE services
        // services.AddTeeServices(configuration);
        
        return services;
    }

    /// <summary>
    /// Adds key management service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKeyManagementService(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure key management options
        var keyManagementSection = configuration.GetSection("KeyManagement");
        services.Configure<KeyManagementOptions>(keyManagementSection);
        
        // Register key management service
        // // services.AddSingleton<Services.KeyManagement.IKeyManagementService, Services.KeyManagement.KeyManagementService>();
        
        return services;
    }

    /// <summary>
    /// Adds oracle service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOracleService(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure oracle options
        var oracleSection = configuration.GetSection("Oracle");
        services.Configure<OracleOptions>(oracleSection);
        
        // Register oracle service
        // services.AddSingleton<Services.Oracle.IOracleService, Services.Oracle.OracleService>();
        
        return services;
    }

    /// <summary>
    /// Adds storage service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddStorageService(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure storage options
        var storageSection = configuration.GetSection("Storage");
        services.Configure<StorageOptions>(storageSection);
        
        // Register storage service
        // services.AddSingleton<Services.Storage.IStorageService, Services.Storage.StorageService>();
        
        return services;
    }

    /// <summary>
    /// Adds voting service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddVotingService(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure voting options
        var votingSection = configuration.GetSection("Voting");
        services.Configure<VotingOptions>(votingSection);
        
        // Register voting service
        // services.AddSingleton<Core.IVotingService, Services.Voting.VotingService>();
        
        return services;
    }

    /// <summary>
    /// Adds compliance service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddComplianceService(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure compliance options
        var complianceSection = configuration.GetSection("Compliance");
        services.Configure<ComplianceOptions>(complianceSection);
        
        // Register compliance service
        // services.AddSingleton<Services.Compliance.IComplianceService, Services.Compliance.ComplianceService>();
        
        return services;
    }

    /// <summary>
    /// Adds cross-chain service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCrossChainService(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure cross-chain options
        var crossChainSection = configuration.GetSection("CrossChain");
        services.Configure<CrossChainOptions>(crossChainSection);
        
        // Register cross-chain service
        // services.AddSingleton<Core.ICrossChainService, Services.CrossChain.CrossChainService>();
        
        return services;
    }

    /// <summary>
    /// Adds automation service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAutomationService(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure automation options
        var automationSection = configuration.GetSection("Automation");
        services.Configure<AutomationOptions>(automationSection);
        
        // Register automation service
        // services.AddSingleton<Services.Automation.IAutomationService, Services.Automation.AutomationService>();
        
        return services;
    }

    /// <summary>
    /// Adds health service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHealthService(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure health options
        var healthSection = configuration.GetSection("Health");
        services.Configure<HealthOptions>(healthSection);
        
        // Register health service
        // services.AddSingleton<Core.IHealthService, Services.Health.HealthService>();
        
        return services;
    }

    /// <summary>
    /// Adds monitoring service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMonitoringService(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure monitoring options
        var monitoringSection = configuration.GetSection("Monitoring");
        services.Configure<MonitoringOptions>(monitoringSection);
        
        // Register monitoring service
        // services.AddSingleton<Services.Monitoring.IMonitoringService, Services.Monitoring.MonitoringService>();
        
        return services;
    }

    /// <summary>
    /// Adds notification service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNotificationService(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure notification options
        var notificationSection = configuration.GetSection("Notification");
        services.Configure<NotificationOptions>(notificationSection);
        
        // Register notification service
        // services.AddSingleton<Services.Notification.INotificationService, Services.Notification.NotificationService>();
        
        return services;
    }

    /// <summary>
    /// Adds event subscription service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEventSubscriptionService(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure event subscription options
        var eventSubscriptionSection = configuration.GetSection("EventSubscription");
        services.Configure<EventSubscriptionOptions>(eventSubscriptionSection);
        
        // Register event subscription service
        // services.AddSingleton<Services.EventSubscription.IEventSubscriptionService, Services.EventSubscription.EventSubscriptionService>();
        
        return services;
    }

    /// <summary>
    /// Adds randomness service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRandomnessService(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure randomness options
        var randomnessSection = configuration.GetSection("Randomness");
        services.Configure<RandomnessOptions>(randomnessSection);
        
        // Register randomness service
        // services.AddSingleton<Services.Randomness.IRandomnessService, Services.Randomness.RandomnessService>();
        
        return services;
    }

    /// <summary>
    /// Adds compute service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddComputeService(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure compute options
        var computeSection = configuration.GetSection("Compute");
        services.Configure<ComputeOptions>(computeSection);
        
        // Register compute service
        // services.AddSingleton<Services.Compute.IComputeService, Services.Compute.ComputeService>();
        
        return services;
    }

    /// <summary>
    /// Adds proof of reserve service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddProofOfReserveService(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure proof of reserve options
        var proofOfReserveSection = configuration.GetSection("ProofOfReserve");
        services.Configure<ProofOfReserveOptions>(proofOfReserveSection);
        
        // Register proof of reserve service
        // services.AddSingleton<Core.IProofOfReserveService, Services.ProofOfReserve.ProofOfReserveService>();
        
        return services;
    }

    /// <summary>
    /// Adds AI services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAIServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure AI options
        var aiSection = configuration.GetSection("AI");
        services.Configure<AIOptions>(aiSection);
        
        // Register AI services
        // services.AddSingleton<AI.Prediction.IPredictionService, AI.Prediction.PredictionService>();
        // services.AddSingleton<AI.PatternRecognition.IPatternRecognitionService, AI.PatternRecognition.PatternRecognitionService>();
        
        return services;
    }

    /// <summary>
    /// Adds zero-knowledge service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddZeroKnowledgeService(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure zero-knowledge options
        var zeroKnowledgeSection = configuration.GetSection("ZeroKnowledge");
        services.Configure<ZeroKnowledgeOptions>(zeroKnowledgeSection);
        
        // Register zero-knowledge service
        // services.AddSingleton<Core.IZeroKnowledgeService, Services.ZeroKnowledge.ZeroKnowledgeService>();
        
        return services;
    }

    /// <summary>
    /// Adds fair ordering service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFairOrderingService(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure fair ordering options
        var fairOrderingSection = configuration.GetSection("FairOrdering");
        services.Configure<FairOrderingOptions>(fairOrderingSection);
        
        // Register fair ordering service
        // services.AddSingleton<Core.IFairOrderingService, Advanced.FairOrdering.FairOrderingService>();
        
        return services;
    }

    /// <summary>
    /// Adds TEE services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTeeServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure TEE options
        var teeSection = configuration.GetSection("Tee");
        services.Configure<TeeOptions>(teeSection);
        
        // Configure Enclave options
        var enclaveSection = configuration.GetSection("Enclave");
        services.Configure<EnclaveConfig>(enclaveSection);
        
        // Register enclave services based on configuration
        var teeOptions = new TeeOptions();
        teeSection.Bind(teeOptions);
        
        // Register the appropriate enclave wrapper based on configuration
        if (teeOptions.EnclaveType?.ToUpperInvariant() == "OCCLUM")
        {
            services.AddSingleton<IEnclaveWrapper, OcclumEnclaveWrapper>();
        }
        else if (Environment.GetEnvironmentVariable("SGX_MODE") == "SIM" || 
                 Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Development")
        {
            // Use production SGX wrapper in simulation mode for development/testing
            services.AddSingleton<IEnclaveWrapper, ProductionSGXEnclaveWrapper>();
        }
        else
        {
            // Use full production implementation for hardware mode
            services.AddSingleton<IEnclaveWrapper, ProductionSGXEnclaveWrapper>();
        }
        
        // Register enclave storage services
        services.AddScoped<IEnclaveStorageService, EnclaveStorageService>();
        
        // Register enclave network services
        services.AddScoped<IEnclaveNetworkService, EnclaveNetworkService>();
        
        // Register enclave manager for lifecycle management
        services.AddSingleton<EnclaveManager>();
        
        // Register enclave-based services with adapter
        services.AddScoped<IEnclaveService, EnclaveServiceAdapter>();
        
        // Register health check for enclave (temporarily commented out until health check package is added)
        // services.AddHealthChecks()
        //     .AddCheck<SGXHealthCheck>("sgx-enclave");
        
        // Register background service for enclave management (temporarily commented out until implemented)
        // services.AddHostedService<EnclaveBackgroundService>();
        
        return services;
    }

    /// <summary>
    /// Adds abstract account service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAbstractAccountService(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure abstract account options
        var abstractAccountSection = configuration.GetSection("AbstractAccount");
        services.Configure<AbstractAccountOptions>(abstractAccountSection);
        
        // Register abstract account service
        // services.AddSingleton<Services.AbstractAccount.IAbstractAccountService, Services.AbstractAccount.AbstractAccountService>();
        
        return services;
    }

    /// <summary>
    /// Adds backup service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBackupService(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure backup options
        var backupSection = configuration.GetSection("Backup");
        services.Configure<BackupOptions>(backupSection);
        
        // Register backup service
        // services.AddSingleton<Services.Backup.IBackupService, Services.Backup.BackupService>();
        
        return services;
    }

    /// <summary>
    /// Adds configuration service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddConfigurationService(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure configuration service options
        var configSection = configuration.GetSection("ConfigurationService");
        services.Configure<ConfigurationServiceOptions>(configSection);
        
        // Register configuration service
        // services.AddSingleton<Services.Configuration.IConfigurationService, Services.Configuration.ConfigurationService>();
        
        return services;
    }
}

#region Configuration Options

/// <summary>
/// Configuration options for key management service.
/// </summary>
public class KeyManagementOptions
{
    /// <summary>
    /// Gets or sets the maximum number of keys.
    /// </summary>
    public int MaxKeyCount { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the supported key types.
    /// </summary>
    public string[] SupportedKeyTypes { get; set; } = { "Secp256k1", "Ed25519", "RSA" };

    /// <summary>
    /// Gets or sets the key rotation interval in days.
    /// </summary>
    public int KeyRotationIntervalDays { get; set; } = 90;
}

/// <summary>
/// Configuration options for oracle service.
/// </summary>
public class OracleOptions
{
    /// <summary>
    /// Gets or sets the maximum number of data sources.
    /// </summary>
    public int MaxDataSources { get; set; } = 100;

    /// <summary>
    /// Gets or sets the data refresh interval in seconds.
    /// </summary>
    public int DataRefreshIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the request timeout in seconds.
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Configuration options for storage service.
/// </summary>
public class StorageOptions
{
    /// <summary>
    /// Gets or sets the maximum storage size in bytes.
    /// </summary>
    public long MaxStorageSizeBytes { get; set; } = 1_000_000_000; // 1GB

    /// <summary>
    /// Gets or sets the encryption algorithm.
    /// </summary>
    public string EncryptionAlgorithm { get; set; } = "AES-256-GCM";

    /// <summary>
    /// Gets or sets whether to enable compression.
    /// </summary>
    public bool EnableCompression { get; set; } = true;
}

/// <summary>
/// Configuration options for voting service.
/// </summary>
public class VotingOptions
{
    /// <summary>
    /// Gets or sets the maximum number of active proposals.
    /// </summary>
    public int MaxActiveProposals { get; set; } = 50;

    /// <summary>
    /// Gets or sets the default voting period in hours.
    /// </summary>
    public int DefaultVotingPeriodHours { get; set; } = 168; // 1 week

    /// <summary>
    /// Gets or sets the minimum quorum percentage.
    /// </summary>
    public double MinimumQuorumPercentage { get; set; } = 0.1; // 10%
}

/// <summary>
/// Configuration options for compliance service.
/// </summary>
public class ComplianceOptions
{
    /// <summary>
    /// Gets or sets the enabled compliance rules.
    /// </summary>
    public string[] EnabledRules { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the audit log retention days.
    /// </summary>
    public int AuditLogRetentionDays { get; set; } = 365;

    /// <summary>
    /// Gets or sets whether to enable real-time monitoring.
    /// </summary>
    public bool EnableRealTimeMonitoring { get; set; } = true;
}

/// <summary>
/// Configuration options for cross-chain service.
/// </summary>
public class CrossChainOptions
{
    /// <summary>
    /// Gets or sets the supported chains.
    /// </summary>
    public string[] SupportedChains { get; set; } = { "NeoN3", "NeoX" };

    /// <summary>
    /// Gets or sets the bridge contract addresses.
    /// </summary>
    public Dictionary<string, string> BridgeContracts { get; set; } = new();

    /// <summary>
    /// Gets or sets the confirmation blocks required.
    /// </summary>
    public int ConfirmationBlocks { get; set; } = 6;
}

/// <summary>
/// Configuration options for automation service.
/// </summary>
public class AutomationOptions
{
    /// <summary>
    /// Gets or sets the maximum number of active workflows.
    /// </summary>
    public int MaxActiveWorkflows { get; set; } = 100;

    /// <summary>
    /// Gets or sets the execution interval in seconds.
    /// </summary>
    public int ExecutionIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets whether to enable parallel execution.
    /// </summary>
    public bool EnableParallelExecution { get; set; } = true;
}

/// <summary>
/// Configuration options for health service.
/// </summary>
public class HealthOptions
{
    /// <summary>
    /// Gets or sets the health check interval in seconds.
    /// </summary>
    public int HealthCheckIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the unhealthy threshold.
    /// </summary>
    public int UnhealthyThreshold { get; set; } = 3;

    /// <summary>
    /// Gets or sets whether to enable detailed health reports.
    /// </summary>
    public bool EnableDetailedReports { get; set; } = true;
}

/// <summary>
/// Configuration options for monitoring service.
/// </summary>
public class MonitoringOptions
{
    /// <summary>
    /// Gets or sets the metrics collection interval in seconds.
    /// </summary>
    public int MetricsIntervalSeconds { get; set; } = 15;

    /// <summary>
    /// Gets or sets the metrics retention days.
    /// </summary>
    public int MetricsRetentionDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to enable performance monitoring.
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = true;
}

/// <summary>
/// Configuration options for notification service.
/// </summary>
public class NotificationOptions
{
    /// <summary>
    /// Gets or sets the enabled channels.
    /// </summary>
    public string[] EnabledChannels { get; set; } = { "Email", "Webhook" };

    /// <summary>
    /// Gets or sets the retry attempts.
    /// </summary>
    public int RetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the batch size.
    /// </summary>
    public int BatchSize { get; set; } = 100;
}

/// <summary>
/// Configuration options for event subscription service.
/// </summary>
public class EventSubscriptionOptions
{
    /// <summary>
    /// Gets or sets the maximum number of subscriptions.
    /// </summary>
    public int MaxSubscriptions { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the event buffer size.
    /// </summary>
    public int EventBufferSize { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the subscription timeout in seconds.
    /// </summary>
    public int SubscriptionTimeoutSeconds { get; set; } = 300;
}

/// <summary>
/// Configuration options for randomness service.
/// </summary>
public class RandomnessOptions
{
    /// <summary>
    /// Gets or sets the entropy sources.
    /// </summary>
    public string[] EntropySources { get; set; } = { "Hardware", "Blockchain", "External" };

    /// <summary>
    /// Gets or sets the minimum entropy bits.
    /// </summary>
    public int MinimumEntropyBits { get; set; } = 256;

    /// <summary>
    /// Gets or sets whether to enable bias testing.
    /// </summary>
    public bool EnableBiasTesting { get; set; } = true;
}

/// <summary>
/// Configuration options for compute service.
/// </summary>
public class ComputeOptions
{
    /// <summary>
    /// Gets or sets the maximum concurrent computations.
    /// </summary>
    public int MaxConcurrentComputations { get; set; } = 10;

    /// <summary>
    /// Gets or sets the computation timeout in seconds.
    /// </summary>
    public int ComputationTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets the memory limit in MB.
    /// </summary>
    public int MemoryLimitMB { get; set; } = 1024;
}

/// <summary>
/// Configuration options for proof of reserve service.
/// </summary>
public class ProofOfReserveOptions
{
    /// <summary>
    /// Gets or sets the proof generation interval in hours.
    /// </summary>
    public int ProofIntervalHours { get; set; } = 24;

    /// <summary>
    /// Gets or sets the supported assets.
    /// </summary>
    public string[] SupportedAssets { get; set; } = { "NEO", "GAS", "ETH", "BTC" };

    /// <summary>
    /// Gets or sets whether to enable automatic proofs.
    /// </summary>
    public bool EnableAutomaticProofs { get; set; } = true;
}

/// <summary>
/// Configuration options for AI services.
/// </summary>
public class AIOptions
{
    /// <summary>
    /// Gets or sets the model cache size.
    /// </summary>
    public int ModelCacheSize { get; set; } = 10;

    /// <summary>
    /// Gets or sets the inference timeout in seconds.
    /// </summary>
    public int InferenceTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets whether to enable GPU acceleration.
    /// </summary>
    public bool EnableGpuAcceleration { get; set; } = false;
}

/// <summary>
/// Configuration options for zero-knowledge service.
/// </summary>
public class ZeroKnowledgeOptions
{
    /// <summary>
    /// Gets or sets the supported proof systems.
    /// </summary>
    public string[] SupportedProofSystems { get; set; } = { "SNARK", "STARK", "Bulletproof" };

    /// <summary>
    /// Gets or sets the circuit cache size.
    /// </summary>
    public int CircuitCacheSize { get; set; } = 50;

    /// <summary>
    /// Gets or sets the proof generation timeout in seconds.
    /// </summary>
    public int ProofTimeoutSeconds { get; set; } = 300;
}

/// <summary>
/// Configuration options for fair ordering service.
/// </summary>
public class FairOrderingOptions
{
    /// <summary>
    /// Gets or sets the ordering algorithm.
    /// </summary>
    public string OrderingAlgorithm { get; set; } = "TimeWeightedFair";

    /// <summary>
    /// Gets or sets the batch size.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the ordering interval in milliseconds.
    /// </summary>
    public int OrderingIntervalMs { get; set; } = 1000;
}

/// <summary>
/// Configuration options for TEE services.
/// </summary>
public class TeeOptions
{
    /// <summary>
    /// Gets or sets the enclave type.
    /// </summary>
    public string EnclaveType { get; set; } = "SGX";

    /// <summary>
    /// Gets or sets the attestation service URL.
    /// </summary>
    public string? AttestationServiceUrl { get; set; }

    /// <summary>
    /// Gets or sets whether to enable remote attestation.
    /// </summary>
    public bool EnableRemoteAttestation { get; set; } = true;
}

/// <summary>
/// Configuration options for abstract account service.
/// </summary>
public class AbstractAccountOptions
{
    /// <summary>
    /// Gets or sets the maximum number of abstract accounts.
    /// </summary>
    public int MaxAccounts { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the supported account types.
    /// </summary>
    public string[] SupportedAccountTypes { get; set; } = { "MultiSig", "SmartContract", "Threshold" };

    /// <summary>
    /// Gets or sets whether to enable account recovery.
    /// </summary>
    public bool EnableAccountRecovery { get; set; } = true;
}

/// <summary>
/// Configuration options for backup service.
/// </summary>
public class BackupOptions
{
    /// <summary>
    /// Gets or sets the backup retention days.
    /// </summary>
    public int BackupRetentionDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets the backup interval in hours.
    /// </summary>
    public int BackupIntervalHours { get; set; } = 24;

    /// <summary>
    /// Gets or sets whether to enable automatic backups.
    /// </summary>
    public bool EnableAutomaticBackups { get; set; } = true;

    /// <summary>
    /// Gets or sets the backup storage path.
    /// </summary>
    public string BackupStoragePath { get; set; } = "/var/backups/neo-service-layer";
}

/// <summary>
/// Configuration options for configuration service.
/// </summary>
public class ConfigurationServiceOptions
{
    /// <summary>
    /// Gets or sets the configuration refresh interval in seconds.
    /// </summary>
    public int RefreshIntervalSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets whether to enable hot reload.
    /// </summary>
    public bool EnableHotReload { get; set; } = true;

    /// <summary>
    /// Gets or sets the configuration validation mode.
    /// </summary>
    public string ValidationMode { get; set; } = "Strict";
}

#endregion
