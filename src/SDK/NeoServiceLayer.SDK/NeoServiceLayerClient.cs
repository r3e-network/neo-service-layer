using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.SDK.Clients;
using Polly;
using Polly.Extensions.Http;

namespace NeoServiceLayer.SDK;

/// <summary>
/// Main client for interacting with Neo Service Layer microservices
/// </summary>
public class NeoServiceLayerClient : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NeoServiceLayerClient> _logger;

    public NeoServiceLayerClient(NeoServiceLayerClientOptions options)
    {
        var services = new ServiceCollection();

        // Add configuration
        var configBuilder = new ConfigurationBuilder();
        if (options.Configuration != null)
        {
            configBuilder.AddConfiguration(options.Configuration);
        }
        else
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ServiceDiscovery:Type"] = options.ServiceDiscoveryType.ToString(),
                ["ServiceDiscovery:Address"] = options.ServiceDiscoveryAddress,
                ["Gateway:Address"] = options.GatewayAddress,
                ["Authentication:Type"] = options.AuthenticationType.ToString(),
                ["Authentication:Token"] = options.AuthenticationToken
            });
        }

        _configuration = configBuilder.Build();
        services.AddSingleton<IConfiguration>(_configuration);

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            if (options.LogLevel.HasValue)
            {
                builder.SetMinimumLevel(options.LogLevel.Value);
            }
        });

        // Configure HTTP clients
        ConfigureHttpClients(services, options);

        // Add service clients
        AddServiceClients(services);

        _serviceProvider = services.BuildServiceProvider();
        _httpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
        _logger = _serviceProvider.GetRequiredService<ILogger<NeoServiceLayerClient>>();

        // Initialize properties
        Notification = _serviceProvider.GetRequiredService<INotificationClient>();
        Configuration = _serviceProvider.GetRequiredService<IConfigurationClient>();
        Backup = _serviceProvider.GetRequiredService<IBackupClient>();
        Storage = _serviceProvider.GetRequiredService<IStorageClient>();
        SmartContracts = _serviceProvider.GetRequiredService<ISmartContractsClient>();
        CrossChain = _serviceProvider.GetRequiredService<ICrossChainClient>();
        Oracle = _serviceProvider.GetRequiredService<IOracleClient>();
        ProofOfReserve = _serviceProvider.GetRequiredService<IProofOfReserveClient>();
        KeyManagement = _serviceProvider.GetRequiredService<IKeyManagementClient>();
        AbstractAccount = _serviceProvider.GetRequiredService<IAbstractAccountClient>();
        ZeroKnowledge = _serviceProvider.GetRequiredService<IZeroKnowledgeClient>();
        Compliance = _serviceProvider.GetRequiredService<IComplianceClient>();
        SecretsManagement = _serviceProvider.GetRequiredService<ISecretsManagementClient>();
        SocialRecovery = _serviceProvider.GetRequiredService<ISocialRecoveryClient>();
        NetworkSecurity = _serviceProvider.GetRequiredService<INetworkSecurityClient>();
        Monitoring = _serviceProvider.GetRequiredService<IMonitoringClient>();
        Health = _serviceProvider.GetRequiredService<IHealthClient>();
        Automation = _serviceProvider.GetRequiredService<IAutomationClient>();
        EventSubscription = _serviceProvider.GetRequiredService<IEventSubscriptionClient>();
        Compute = _serviceProvider.GetRequiredService<IComputeClient>();
        Randomness = _serviceProvider.GetRequiredService<IRandomnessClient>();
        Voting = _serviceProvider.GetRequiredService<IVotingClient>();
        EnclaveStorage = _serviceProvider.GetRequiredService<IEnclaveStorageClient>();
    }

    // Service Clients
    public INotificationClient Notification { get; }
    public IConfigurationClient Configuration { get; }
    public IBackupClient Backup { get; }
    public IStorageClient Storage { get; }
    public ISmartContractsClient SmartContracts { get; }
    public ICrossChainClient CrossChain { get; }
    public IOracleClient Oracle { get; }
    public IProofOfReserveClient ProofOfReserve { get; }
    public IKeyManagementClient KeyManagement { get; }
    public IAbstractAccountClient AbstractAccount { get; }
    public IZeroKnowledgeClient ZeroKnowledge { get; }
    public IComplianceClient Compliance { get; }
    public ISecretsManagementClient SecretsManagement { get; }
    public ISocialRecoveryClient SocialRecovery { get; }
    public INetworkSecurityClient NetworkSecurity { get; }
    public IMonitoringClient Monitoring { get; }
    public IHealthClient Health { get; }
    public IAutomationClient Automation { get; }
    public IEventSubscriptionClient EventSubscription { get; }
    public IComputeClient Compute { get; }
    public IRandomnessClient Randomness { get; }
    public IVotingClient Voting { get; }
    public IEnclaveStorageClient EnclaveStorage { get; }

    private void ConfigureHttpClients(IServiceCollection services, NeoServiceLayerClientOptions options)
    {
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                options.RetryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger?.LogWarning("Retry {RetryCount} after {Delay}ms", retryCount, timespan.TotalMilliseconds);
                });

        var circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                options.CircuitBreakerThreshold,
                TimeSpan.FromSeconds(options.CircuitBreakerDuration));

        // Configure base HTTP client
        services.AddHttpClient("neo-service", client =>
        {
            client.BaseAddress = new Uri(options.GatewayAddress);
            client.Timeout = TimeSpan.FromSeconds(options.RequestTimeout);
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            if (!string.IsNullOrEmpty(options.AuthenticationToken))
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.AuthenticationToken}");
            }
        })
        .AddPolicyHandler(retryPolicy)
        .AddPolicyHandler(circuitBreakerPolicy);

        // Add service-specific HTTP clients
        var serviceNames = new[]
        {
            "notification", "configuration", "backup", "storage", "smart-contracts",
            "cross-chain", "oracle", "proof-of-reserve", "key-management",
            "abstract-account", "zero-knowledge", "compliance", "secrets-management",
            "social-recovery", "network-security", "monitoring", "health",
            "automation", "event-subscription", "compute", "randomness",
            "voting", "enclave-storage"
        };

        foreach (var serviceName in serviceNames)
        {
            services.AddHttpClient($"neo-service-{serviceName}", client =>
            {
                client.BaseAddress = new Uri($"{options.GatewayAddress}/api/{serviceName}/");
                client.Timeout = TimeSpan.FromSeconds(options.RequestTimeout);
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                if (!string.IsNullOrEmpty(options.AuthenticationToken))
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.AuthenticationToken}");
                }
            })
            .AddPolicyHandler(retryPolicy)
            .AddPolicyHandler(circuitBreakerPolicy);
        }
    }

    private void AddServiceClients(IServiceCollection services)
    {
        // Add all service clients
        services.AddScoped<INotificationClient, NotificationClient>();
        services.AddScoped<IConfigurationClient, ConfigurationClient>();
        services.AddScoped<IBackupClient, BackupClient>();
        services.AddScoped<IStorageClient, StorageClient>();
        services.AddScoped<ISmartContractsClient, SmartContractsClient>();
        services.AddScoped<ICrossChainClient, CrossChainClient>();
        services.AddScoped<IOracleClient, OracleClient>();
        services.AddScoped<IProofOfReserveClient, ProofOfReserveClient>();
        services.AddScoped<IKeyManagementClient, KeyManagementClient>();
        services.AddScoped<IAbstractAccountClient, AbstractAccountClient>();
        services.AddScoped<IZeroKnowledgeClient, ZeroKnowledgeClient>();
        services.AddScoped<IComplianceClient, ComplianceClient>();
        services.AddScoped<ISecretsManagementClient, SecretsManagementClient>();
        services.AddScoped<ISocialRecoveryClient, SocialRecoveryClient>();
        services.AddScoped<INetworkSecurityClient, NetworkSecurityClient>();
        services.AddScoped<IMonitoringClient, MonitoringClient>();
        services.AddScoped<IHealthClient, HealthClient>();
        services.AddScoped<IAutomationClient, AutomationClient>();
        services.AddScoped<IEventSubscriptionClient, EventSubscriptionClient>();
        services.AddScoped<IComputeClient, ComputeClient>();
        services.AddScoped<IRandomnessClient, RandomnessClient>();
        services.AddScoped<IVotingClient, VotingClient>();
        services.AddScoped<IEnclaveStorageClient, EnclaveStorageClient>();
    }

    /// <summary>
    /// Check the health of all services
    /// </summary>
    public async Task<ServiceHealthReport> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var healthClient = _serviceProvider.GetRequiredService<IHealthClient>();
        return await healthClient.GetSystemHealthAsync(cancellationToken);
    }

    /// <summary>
    /// Get service metrics
    /// </summary>
    public async Task<ServiceMetrics> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        var monitoringClient = _serviceProvider.GetRequiredService<IMonitoringClient>();
        return await monitoringClient.GetSystemMetricsAsync(cancellationToken);
    }

    public void Dispose()
    {
        (_serviceProvider as IDisposable)?.Dispose();
    }
}

/// <summary>
/// Options for configuring the Neo Service Layer client
/// </summary>
public class NeoServiceLayerClientOptions
{
    /// <summary>
    /// Gateway address (default: http://localhost:5000)
    /// </summary>
    public string GatewayAddress { get; set; } = "http://localhost:5000";

    /// <summary>
    /// Service discovery type (default: Consul)
    /// </summary>
    public ServiceDiscoveryType ServiceDiscoveryType { get; set; } = ServiceDiscoveryType.Consul;

    /// <summary>
    /// Service discovery address (default: http://localhost:8500)
    /// </summary>
    public string ServiceDiscoveryAddress { get; set; } = "http://localhost:8500";

    /// <summary>
    /// Authentication type (default: JWT)
    /// </summary>
    public AuthenticationType AuthenticationType { get; set; } = AuthenticationType.JWT;

    /// <summary>
    /// Authentication token
    /// </summary>
    public string AuthenticationToken { get; set; }

    /// <summary>
    /// Request timeout in seconds (default: 30)
    /// </summary>
    public int RequestTimeout { get; set; } = 30;

    /// <summary>
    /// Number of retry attempts (default: 3)
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Circuit breaker threshold (default: 5)
    /// </summary>
    public int CircuitBreakerThreshold { get; set; } = 5;

    /// <summary>
    /// Circuit breaker duration in seconds (default: 30)
    /// </summary>
    public int CircuitBreakerDuration { get; set; } = 30;

    /// <summary>
    /// Log level (optional)
    /// </summary>
    public LogLevel? LogLevel { get; set; }

    /// <summary>
    /// Custom configuration (optional)
    /// </summary>
    public IConfiguration Configuration { get; set; }
}

public enum ServiceDiscoveryType
{
    None,
    Consul,
    Kubernetes,
    Static
}

public enum AuthenticationType
{
    None,
    JWT,
    ApiKey,
    Certificate
}
