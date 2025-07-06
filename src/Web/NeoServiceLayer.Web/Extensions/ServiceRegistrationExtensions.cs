using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.ServiceFramework;

namespace NeoServiceLayer.Web.Extensions;

/// <summary>
/// Extension methods for registering all Neo Service Layer services.
/// </summary>
public static class ServiceRegistrationExtensions
{
    /// <summary>
    /// Adds all Neo Service Layer services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNeoServiceLayerServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add persistent storage
        services.AddPersistentStorage(configuration);

        // Add blockchain client factory
        services.AddSingleton<IBlockchainClientFactory, NeoServiceLayer.Infrastructure.Blockchain.BlockchainClientFactory>();

        // Core Services (4)
        services.AddScoped<NeoServiceLayer.Services.KeyManagement.IKeyManagementService, NeoServiceLayer.Services.KeyManagement.KeyManagementService>();
        services.AddScoped<NeoServiceLayer.Services.Randomness.IRandomnessService, NeoServiceLayer.Services.Randomness.RandomnessService>();
        services.AddScoped<NeoServiceLayer.Services.Oracle.IOracleService, NeoServiceLayer.Services.Oracle.OracleService>();
        services.AddScoped<NeoServiceLayer.Services.Voting.IVotingService, NeoServiceLayer.Services.Voting.VotingService>();

        // Storage & Data Services (3)
        services.AddScoped<NeoServiceLayer.Services.Storage.IStorageService, NeoServiceLayer.Services.Storage.StorageService>();
        services.AddScoped<NeoServiceLayer.Services.Backup.IBackupService, NeoServiceLayer.Services.Backup.BackupService>();
        services.AddScoped<NeoServiceLayer.Services.Configuration.IConfigurationService, NeoServiceLayer.Services.Configuration.ConfigurationService>();

        // Security Services (5)
        services.AddScoped<NeoServiceLayer.Services.ZeroKnowledge.IZeroKnowledgeService, NeoServiceLayer.Services.ZeroKnowledge.ZeroKnowledgeService>();
        services.AddScoped<NeoServiceLayer.Services.AbstractAccount.IAbstractAccountService, NeoServiceLayer.Services.AbstractAccount.AbstractAccountService>();
        services.AddScoped<NeoServiceLayer.Services.Compliance.IComplianceService, NeoServiceLayer.Services.Compliance.ComplianceService>();
        services.AddScoped<NeoServiceLayer.Services.ProofOfReserve.IProofOfReserveService, NeoServiceLayer.Services.ProofOfReserve.ProofOfReserveService>();
        services.AddScoped<NeoServiceLayer.Services.SecretsManagement.ISecretsManagementService, NeoServiceLayer.Services.SecretsManagement.SecretsManagementService>();
        services.AddScoped<NeoServiceLayer.Services.Abstractions.ISocialRecoveryService, NeoServiceLayer.Services.SocialRecovery.SocialRecoveryService>();
        services.Configure<NeoServiceLayer.Services.SocialRecovery.SocialRecoveryOptions>(configuration.GetSection("SocialRecovery"));

        // Operations Services (4)
        services.AddScoped<NeoServiceLayer.Services.Automation.IAutomationService, NeoServiceLayer.Services.Automation.AutomationService>();
        services.AddScoped<NeoServiceLayer.Services.Monitoring.IMonitoringService, NeoServiceLayer.Services.Monitoring.MonitoringService>();
        services.AddScoped<NeoServiceLayer.Services.Health.IHealthService, NeoServiceLayer.Services.Health.HealthService>();
        services.AddScoped<NeoServiceLayer.Services.Notification.INotificationService, NeoServiceLayer.Services.Notification.NotificationService>();

        // Infrastructure Services (4)
        services.AddScoped<NeoServiceLayer.Services.CrossChain.ICrossChainService, NeoServiceLayer.Services.CrossChain.CrossChainService>();
        services.AddScoped<NeoServiceLayer.Services.Compute.IComputeService, NeoServiceLayer.Services.Compute.ComputeService>();
        services.AddScoped<NeoServiceLayer.Services.EventSubscription.IEventSubscriptionService, NeoServiceLayer.Services.EventSubscription.EventSubscriptionService>();

        // Smart Contracts Service with dependencies
        services.AddScoped<NeoServiceLayer.Services.SmartContracts.NeoN3.NeoN3SmartContractManager>();
        services.AddScoped<NeoServiceLayer.Services.SmartContracts.NeoX.NeoXSmartContractManager>();
        services.AddScoped<NeoServiceLayer.Services.SmartContracts.ISmartContractsService, NeoServiceLayer.Services.SmartContracts.SmartContractsService>();

        // AI Services (2)
        services.AddScoped<NeoServiceLayer.AI.PatternRecognition.IPatternRecognitionService, NeoServiceLayer.AI.PatternRecognition.PatternRecognitionService>();
        services.AddScoped<NeoServiceLayer.AI.Prediction.IPredictionService, NeoServiceLayer.AI.Prediction.PredictionService>();

        // Advanced Services (3)
        services.AddScoped<NeoServiceLayer.Advanced.FairOrdering.IFairOrderingService, NeoServiceLayer.Advanced.FairOrdering.FairOrderingService>();
        services.AddScoped<NeoServiceLayer.Tee.Enclave.IAttestationService>(provider =>
        {
            var logger = provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<NeoServiceLayer.Tee.Enclave.AttestationService>>();
            var httpClient = provider.GetRequiredService<System.Net.Http.IHttpClientFactory>().CreateClient();
            var attestationUrl = configuration["AttestationService:EpidUrl"] ?? "https://api.trustedservices.intel.com/sgx/dev/attestation/v4/";
            var apiKey = configuration["AttestationService:IasApiKey"] ?? "";
            return new NeoServiceLayer.Tee.Enclave.AttestationService(logger, httpClient, attestationUrl, apiKey);
        });
        services.AddScoped<NeoServiceLayer.Services.NetworkSecurity.INetworkSecurityService, NeoServiceLayer.Services.NetworkSecurity.NetworkSecurityService>();
        services.AddScoped<NeoServiceLayer.Services.EnclaveStorage.IEnclaveStorageService, NeoServiceLayer.Services.EnclaveStorage.EnclaveStorageService>();

        // Register service configuration
        services.AddScoped<IServiceConfiguration>(provider =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            return new ServiceConfiguration(config);
        });

        return services;
    }

    /// <summary>
    /// Service configuration implementation.
    /// </summary>
    private class ServiceConfiguration : IServiceConfiguration
    {
        private readonly IConfiguration _configuration;
        private readonly Dictionary<string, object> _overrides = new();

        public ServiceConfiguration(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public T? GetValue<T>(string key)
        {
            if (_overrides.TryGetValue(key, out var overrideValue))
            {
                return (T)overrideValue;
            }

            var value = _configuration[key];
            if (string.IsNullOrEmpty(value))
            {
                return default;
            }

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default;
            }
        }

        public T GetValue<T>(string key, T defaultValue)
        {
            var value = GetValue<T>(key);
            return EqualityComparer<T>.Default.Equals(value, default(T)) ? defaultValue : value;
        }

        public void SetValue<T>(string key, T value)
        {
            _overrides[key] = value!;
        }

        public bool ContainsKey(string key)
        {
            return _overrides.ContainsKey(key) || _configuration[key] != null;
        }

        public bool RemoveKey(string key)
        {
            return _overrides.Remove(key);
        }

        public IEnumerable<string> GetAllKeys()
        {
            var configKeys = _configuration.AsEnumerable().Select(kvp => kvp.Key);
            var overrideKeys = _overrides.Keys;
            return configKeys.Union(overrideKeys).Distinct();
        }

        public IServiceConfiguration? GetSection(string sectionName)
        {
            var section = _configuration.GetSection(sectionName);
            return section.Exists() ? new ServiceConfiguration(section) : null;
        }

        public string GetConnectionString(string name)
        {
            return _configuration.GetConnectionString(name) ?? string.Empty;
        }
    }
}
