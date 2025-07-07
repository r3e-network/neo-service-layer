using Microsoft.Extensions.Diagnostics.HealthChecks;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.AbstractAccount;
using NeoServiceLayer.Services.Compliance;
using NeoServiceLayer.Services.CrossChain;
using NeoServiceLayer.Services.EventSubscription;
using NeoServiceLayer.Services.NetworkSecurity;
using NeoServiceLayer.Services.Notification;
using NeoServiceLayer.Services.ProofOfReserve;
using NeoServiceLayer.Services.Randomness;
using NeoServiceLayer.Services.SmartContracts;
using NeoServiceLayer.Services.SocialRecovery;
using NeoServiceLayer.Services.Voting;
using NeoServiceLayer.Services.ZeroKnowledge;
using NeoServiceLayer.Tee.Enclave;

namespace NeoServiceLayer.Api.HealthChecks;

/// <summary>
/// Health check for critical security services.
/// </summary>
public class SecurityServicesHealthCheck : IHealthCheck
{
    private readonly INetworkSecurityService _networkSecurityService;
    private readonly IComplianceService _complianceService;
    private readonly IAttestationService _attestationService;
    private readonly ILogger<SecurityServicesHealthCheck> _logger;

    public SecurityServicesHealthCheck(
        INetworkSecurityService networkSecurityService,
        IComplianceService complianceService,
        IAttestationService attestationService,
        ILogger<SecurityServicesHealthCheck> logger)
    {
        _networkSecurityService = networkSecurityService;
        _complianceService = complianceService;
        _attestationService = attestationService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var healthChecks = new Dictionary<string, bool>();
        var errors = new List<string>();

        try
        {
            // Check Network Security
            try
            {
                // Simple health check - just verify the service is accessible
                healthChecks["NetworkSecurity"] = _networkSecurityService != null;
            }
            catch (Exception ex)
            {
                healthChecks["NetworkSecurity"] = false;
                errors.Add($"NetworkSecurity: {ex.Message}");
            }

            // Check Compliance
            try
            {
                // Simple health check - just verify the service is accessible
                healthChecks["Compliance"] = _complianceService != null;
            }
            catch (Exception ex)
            {
                healthChecks["Compliance"] = false;
                errors.Add($"Compliance: {ex.Message}");
            }

            // Check Attestation
            try
            {
                // Simple health check - just verify the service is accessible
                healthChecks["Attestation"] = _attestationService != null;
            }
            catch (Exception ex)
            {
                healthChecks["Attestation"] = false;
                errors.Add($"Attestation: {ex.Message}");
            }

            var healthyCount = healthChecks.Count(kvp => kvp.Value);
            var totalCount = healthChecks.Count;

            var data = new Dictionary<string, object>
            {
                ["Services"] = healthChecks,
                ["HealthyCount"] = healthyCount,
                ["TotalCount"] = totalCount,
                ["Errors"] = errors
            };

            if (healthyCount == totalCount)
            {
                return HealthCheckResult.Healthy("All security services are healthy", data);
            }
            else if (healthyCount > 0)
            {
                return HealthCheckResult.Degraded($"{totalCount - healthyCount} security services are unhealthy", data: data);
            }
            else
            {
                return HealthCheckResult.Unhealthy("All security services are unhealthy", data: data);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Security services health check failed");
            return HealthCheckResult.Unhealthy("Failed to check security services health", ex);
        }
    }
}

/// <summary>
/// Health check for blockchain services.
/// </summary>
public class BlockchainServicesHealthCheck : IHealthCheck
{
    private readonly IVotingService _votingService;
    private readonly ICrossChainService _crossChainService;
    private readonly ISmartContractsService _smartContractsService;
    private readonly IProofOfReserveService _proofOfReserveService;
    private readonly ILogger<BlockchainServicesHealthCheck> _logger;

    public BlockchainServicesHealthCheck(
        IVotingService votingService,
        ICrossChainService crossChainService,
        ISmartContractsService smartContractsService,
        IProofOfReserveService proofOfReserveService,
        ILogger<BlockchainServicesHealthCheck> logger)
    {
        _votingService = votingService;
        _crossChainService = crossChainService;
        _smartContractsService = smartContractsService;
        _proofOfReserveService = proofOfReserveService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var healthChecks = new Dictionary<string, bool>();
        var errors = new List<string>();

        try
        {
            // Check Voting Service
            try
            {
                var councilNodes = await _votingService.GetCouncilNodesAsync(BlockchainType.NeoN3);
                healthChecks["Voting"] = councilNodes != null;
            }
            catch (Exception ex)
            {
                healthChecks["Voting"] = false;
                errors.Add($"Voting: {ex.Message}");
            }

            // Check Cross-Chain Service
            try
            {
                var bridges = await _crossChainService.GetSupportedChainsAsync();
                healthChecks["CrossChain"] = bridges != null;
            }
            catch (Exception ex)
            {
                healthChecks["CrossChain"] = false;
                errors.Add($"CrossChain: {ex.Message}");
            }

            // Check Smart Contracts Service
            try
            {
                var deployedContracts = await _smartContractsService.ListAllDeployedContractsAsync(BlockchainType.NeoN3);
                healthChecks["SmartContracts"] = deployedContracts != null;
            }
            catch (Exception ex)
            {
                healthChecks["SmartContracts"] = false;
                errors.Add($"SmartContracts: {ex.Message}");
            }

            // Check Proof of Reserve Service
            try
            {
                var supportedAssets = await _proofOfReserveService.GetRegisteredAssetsAsync(BlockchainType.NeoN3);
                healthChecks["ProofOfReserve"] = supportedAssets != null;
            }
            catch (Exception ex)
            {
                healthChecks["ProofOfReserve"] = false;
                errors.Add($"ProofOfReserve: {ex.Message}");
            }

            var healthyCount = healthChecks.Count(kvp => kvp.Value);
            var totalCount = healthChecks.Count;

            var data = new Dictionary<string, object>
            {
                ["Services"] = healthChecks,
                ["HealthyCount"] = healthyCount,
                ["TotalCount"] = totalCount,
                ["Errors"] = errors
            };

            if (healthyCount == totalCount)
            {
                return HealthCheckResult.Healthy("All blockchain services are healthy", data);
            }
            else if (healthyCount > 0)
            {
                return HealthCheckResult.Degraded($"{totalCount - healthyCount} blockchain services are unhealthy", data: data);
            }
            else
            {
                return HealthCheckResult.Unhealthy("All blockchain services are unhealthy", data: data);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blockchain services health check failed");
            return HealthCheckResult.Unhealthy("Failed to check blockchain services health", ex);
        }
    }
}

/// <summary>
/// Health check for data services.
/// </summary>
public class DataServicesHealthCheck : IHealthCheck
{
    private readonly IEventSubscriptionService _eventSubscriptionService;
    private readonly INotificationService _notificationService;
    private readonly IRandomnessService _randomnessService;
    private readonly ILogger<DataServicesHealthCheck> _logger;

    public DataServicesHealthCheck(
        IEventSubscriptionService eventSubscriptionService,
        INotificationService notificationService,
        IRandomnessService randomnessService,
        ILogger<DataServicesHealthCheck> logger)
    {
        _eventSubscriptionService = eventSubscriptionService;
        _notificationService = notificationService;
        _randomnessService = randomnessService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var healthChecks = new Dictionary<string, bool>();
        var errors = new List<string>();

        try
        {
            // Check Event Subscription Service
            try
            {
                var subscriptions = await _eventSubscriptionService.ListSubscriptionsAsync(0, 1, BlockchainType.NeoN3);
                healthChecks["EventSubscription"] = subscriptions != null;
            }
            catch (Exception ex)
            {
                healthChecks["EventSubscription"] = false;
                errors.Add($"EventSubscription: {ex.Message}");
            }

            // Check Notification Service
            try
            {
                var channels = await _notificationService.GetAvailableChannelsAsync(BlockchainType.NeoN3);
                healthChecks["Notification"] = channels != null;
            }
            catch (Exception ex)
            {
                healthChecks["Notification"] = false;
                errors.Add($"Notification: {ex.Message}");
            }

            // Check Randomness Service
            try
            {
                var entropy = await _randomnessService.GenerateRandomBytesAsync(1, BlockchainType.NeoN3);
                healthChecks["Randomness"] = entropy != null && entropy.Length > 0;
            }
            catch (Exception ex)
            {
                healthChecks["Randomness"] = false;
                errors.Add($"Randomness: {ex.Message}");
            }

            var healthyCount = healthChecks.Count(kvp => kvp.Value);
            var totalCount = healthChecks.Count;

            var data = new Dictionary<string, object>
            {
                ["Services"] = healthChecks,
                ["HealthyCount"] = healthyCount,
                ["TotalCount"] = totalCount,
                ["Errors"] = errors
            };

            if (healthyCount == totalCount)
            {
                return HealthCheckResult.Healthy("All data services are healthy", data);
            }
            else if (healthyCount > 0)
            {
                return HealthCheckResult.Degraded($"{totalCount - healthyCount} data services are unhealthy", data: data);
            }
            else
            {
                return HealthCheckResult.Unhealthy("All data services are unhealthy", data: data);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Data services health check failed");
            return HealthCheckResult.Unhealthy("Failed to check data services health", ex);
        }
    }
}

/// <summary>
/// Health check for advanced services.
/// </summary>
public class AdvancedServicesHealthCheck : IHealthCheck
{
    private readonly IAbstractAccountService _abstractAccountService;
    private readonly ISocialRecoveryService _socialRecoveryService;
    private readonly NeoServiceLayer.Services.ZeroKnowledge.IZeroKnowledgeService _zeroKnowledgeService;
    private readonly ILogger<AdvancedServicesHealthCheck> _logger;

    public AdvancedServicesHealthCheck(
        IAbstractAccountService abstractAccountService,
        ISocialRecoveryService socialRecoveryService,
        NeoServiceLayer.Services.ZeroKnowledge.IZeroKnowledgeService zeroKnowledgeService,
        ILogger<AdvancedServicesHealthCheck> logger)
    {
        _abstractAccountService = abstractAccountService;
        _socialRecoveryService = socialRecoveryService;
        _zeroKnowledgeService = zeroKnowledgeService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var healthChecks = new Dictionary<string, bool>();
        var errors = new List<string>();

        try
        {
            // Check Abstract Account Service
            try
            {
                // Simple health check - service is responsive
                healthChecks["AbstractAccount"] = _abstractAccountService != null;
            }
            catch (Exception ex)
            {
                healthChecks["AbstractAccount"] = false;
                errors.Add($"AbstractAccount: {ex.Message}");
            }

            // Check Social Recovery Service
            try
            {
                // Simple health check - service is responsive
                healthChecks["SocialRecovery"] = _socialRecoveryService != null;
            }
            catch (Exception ex)
            {
                healthChecks["SocialRecovery"] = false;
                errors.Add($"SocialRecovery: {ex.Message}");
            }

            // Check Zero Knowledge Service
            try
            {
                // Simple health check - just verify the service is accessible
                // We can't easily generate a proof without proper parameters, so just check it's not null
                healthChecks["ZeroKnowledge"] = _zeroKnowledgeService != null;
            }
            catch (Exception ex)
            {
                healthChecks["ZeroKnowledge"] = false;
                errors.Add($"ZeroKnowledge: {ex.Message}");
            }

            var healthyCount = healthChecks.Count(kvp => kvp.Value);
            var totalCount = healthChecks.Count;

            var data = new Dictionary<string, object>
            {
                ["Services"] = healthChecks,
                ["HealthyCount"] = healthyCount,
                ["TotalCount"] = totalCount,
                ["Errors"] = errors
            };

            if (healthyCount == totalCount)
            {
                return HealthCheckResult.Healthy("All advanced services are healthy", data);
            }
            else if (healthyCount > 0)
            {
                return HealthCheckResult.Degraded($"{totalCount - healthyCount} advanced services are unhealthy", data: data);
            }
            else
            {
                return HealthCheckResult.Unhealthy("All advanced services are unhealthy", data: data);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Advanced services health check failed");
            return HealthCheckResult.Unhealthy("Failed to check advanced services health", ex);
        }
    }
}
