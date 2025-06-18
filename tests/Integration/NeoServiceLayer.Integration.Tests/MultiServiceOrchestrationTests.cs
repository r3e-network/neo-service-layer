using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.ProofOfReserve;
using NeoServiceLayer.Services.Automation;
using NeoServiceLayer.Services.Configuration;
using NeoServiceLayer.Services.CrossChain;
using NeoServiceLayer.Services.Monitoring;
using NeoServiceLayer.Services.Backup;
using NeoServiceLayer.Services.Notification;
using NeoServiceLayer.Advanced.FairOrdering;
using NeoServiceLayer.Tee.Host.Services;
using NeoServiceLayer.Tee.Host.Tests;
using System.Text.Json;
using Xunit;

namespace NeoServiceLayer.Integration.Tests;

/// <summary>
/// Integration tests for complex multi-service orchestration scenarios.
/// </summary>
public class MultiServiceOrchestrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ILogger<MultiServiceOrchestrationTests> _logger;
    private readonly IProofOfReserveService _proofOfReserveService;
    private readonly IAutomationService _automationService;
    private readonly IFairOrderingService _fairOrderingService;
    private readonly ICrossChainService _crossChainService;
    private readonly IMonitoringService _monitoringService;
    private readonly IConfigurationService _configurationService;
    private readonly IBackupService _backupService;
    private readonly INotificationService _notificationService;

    public MultiServiceOrchestrationTests()
    {
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        
        // Add enclave services
        services.AddSingleton<IEnclaveWrapper, TestEnclaveWrapper>();
        services.AddSingleton<IEnclaveManager, EnclaveManager>();
        
        // Add all services
        services.AddSingleton<IProofOfReserveService, ProofOfReserveService>();
        services.AddSingleton<IAutomationService, AutomationService>();
        services.AddSingleton<IFairOrderingService, FairOrderingService>();
        services.AddSingleton<ICrossChainService, CrossChainService>();
        services.AddSingleton<IMonitoringService, MonitoringService>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<INotificationService, NotificationService>();
        
        _serviceProvider = services.BuildServiceProvider();
        
        // Get service instances
        _logger = _serviceProvider.GetRequiredService<ILogger<MultiServiceOrchestrationTests>>();
        _proofOfReserveService = _serviceProvider.GetRequiredService<IProofOfReserveService>();
        _automationService = _serviceProvider.GetRequiredService<IAutomationService>();
        _fairOrderingService = _serviceProvider.GetRequiredService<IFairOrderingService>();
        _crossChainService = _serviceProvider.GetRequiredService<ICrossChainService>();
        _monitoringService = _serviceProvider.GetRequiredService<IMonitoringService>();
        _configurationService = _serviceProvider.GetRequiredService<IConfigurationService>();
        _backupService = _serviceProvider.GetRequiredService<IBackupService>();
        _notificationService = _serviceProvider.GetRequiredService<INotificationService>();
        
        // Initialize all services
        InitializeServicesAsync().GetAwaiter().GetResult();
    }

    private async Task InitializeServicesAsync()
    {
        _logger.LogInformation("Initializing all services for multi-service orchestration testing...");
        
        await _proofOfReserveService.InitializeAsync();
        await _automationService.InitializeAsync();
        await _fairOrderingService.InitializeAsync();
        await _crossChainService.InitializeAsync();
        await _monitoringService.InitializeAsync();
        await _configurationService.InitializeAsync();
        await _backupService.InitializeAsync();
        await _notificationService.InitializeAsync();
        
        _logger.LogInformation("All services initialized successfully");
    }

    [Fact]
    public async Task AutomatedProofOfReserve_WithFairOrdering_ShouldCompleteFullCycle()
    {
        _logger.LogInformation("Starting automated proof of reserve with fair ordering test...");

        // 1. Register an asset for proof of reserve monitoring
        var assetRequest = new AssetRegistrationRequest
        {
            AssetSymbol = "USDC",
            AssetName = "USD Coin",
            ReserveAddresses = new[] { "0xreserve1", "0xreserve2", "0xreserve3" },
            MinReserveRatio = 1.0m,
            TotalSupply = 10000000m,
            UpdateFrequency = TimeSpan.FromHours(1)
        };

        var assetResult = await _proofOfReserveService.RegisterAssetAsync(assetRequest, BlockchainType.NeoX);
        assetResult.Success.Should().BeTrue();
        var assetId = assetResult.AssetId;
        _logger.LogInformation("Registered asset {AssetId} for proof of reserve", assetId);

        // 2. Create fair ordering pool for reserve update transactions
        var poolConfig = new OrderingPoolConfig
        {
            Name = "Reserve Update Pool",
            Description = "Fair ordering pool for proof of reserve updates",
            OrderingAlgorithm = OrderingAlgorithm.FairQueue,
            BatchSize = 10,
            BatchTimeout = TimeSpan.FromSeconds(30),
            FairnessLevel = FairnessLevel.High,
            MevProtectionEnabled = true,
            MaxTransactionValue = 1000000m
        };

        var poolResult = await _fairOrderingService.CreateOrderingPoolAsync(poolConfig, BlockchainType.NeoX);
        poolResult.Success.Should().BeTrue();
        var poolId = poolResult.PoolId;
        _logger.LogInformation("Created fair ordering pool {PoolId}", poolId);

        // 3. Create automation for periodic proof generation
        var automationRequest = new CreateAutomationRequest
        {
            Name = "Hourly Proof Generation",
            Description = "Generate proof of reserve every hour",
            TriggerType = AutomationTriggerType.Schedule,
            TriggerConfiguration = JsonSerializer.Serialize(new 
            { 
                cron = "0 * * * *", // Every hour
                timezone = "UTC"
            }),
            ActionType = AutomationActionType.ServiceCall,
            ActionConfiguration = JsonSerializer.Serialize(new
            {
                service = "ProofOfReserveService",
                method = "GenerateProofAsync",
                parameters = new
                {
                    assetId = assetId,
                    proofType = "MerkleProof",
                    includeTransactionHistory = true
                }
            }),
            IsActive = true,
            RetryPolicy = new RetryPolicyConfig
            {
                MaxRetries = 3,
                RetryDelaySeconds = 60,
                ExponentialBackoff = true
            }
        };

        var automationResult = await _automationService.CreateAutomationAsync(automationRequest, BlockchainType.NeoX);
        automationResult.Success.Should().BeTrue();
        var automationId = automationResult.AutomationId;
        _logger.LogInformation("Created automation {AutomationId} for periodic proof generation", automationId);

        // 4. Submit a reserve update through fair ordering
        var updateRequest = new ReserveUpdateRequest
        {
            AssetId = assetId,
            NewReserveAmount = 10500000m,
            UpdateReason = "Monthly reserve audit",
            AuditorSignature = "0xauditor_signature",
            TransactionData = new TransactionData
            {
                From = "0xreserve_manager",
                To = "0xreserve_contract",
                Value = 0,
                Data = "0xupdateReserves"
            }
        };

        var fairTransaction = new FairTransaction
        {
            PoolId = poolId,
            TransactionData = JsonSerializer.Serialize(updateRequest),
            SubmittedBy = "0xreserve_manager",
            Priority = TransactionPriority.Normal,
            MaxMevProtection = true
        };

        var submissionResult = await _fairOrderingService.SubmitTransactionAsync(fairTransaction, BlockchainType.NeoX);
        submissionResult.Success.Should().BeTrue();
        _logger.LogInformation("Submitted reserve update transaction {TransactionId} to fair ordering pool", 
            submissionResult.TransactionId);

        // 5. Execute the automation manually for testing
        var executionContext = new ExecutionContext
        {
            UserId = "system",
            Parameters = new Dictionary<string, object> { { "manual", true } }
        };

        var executionResult = await _automationService.ExecuteAutomationAsync(automationId, executionContext, BlockchainType.NeoX);
        executionResult.Success.Should().BeTrue();
        _logger.LogInformation("Executed automation {AutomationId} with execution ID {ExecutionId}", 
            automationId, executionResult.ExecutionId);

        // 6. Process the fair ordering batch
        var batchResult = await _fairOrderingService.ProcessBatchAsync(poolId, BlockchainType.NeoX);
        batchResult.Success.Should().BeTrue();
        batchResult.ProcessedTransactions.Should().ContainSingle();
        _logger.LogInformation("Processed fair ordering batch {BatchId} with {Count} transactions", 
            batchResult.BatchId, batchResult.ProcessedTransactions.Count);

        // 7. Verify proof generation
        var proofRequest = new ProofGenerationRequest
        {
            AssetId = assetId,
            ProofType = ProofType.MerkleProof,
            IncludeTransactionHistory = true,
            IncludeSignatures = true
        };

        var proofResult = await _proofOfReserveService.GenerateProofAsync(proofRequest, BlockchainType.NeoX);
        proofResult.Success.Should().BeTrue();
        proofResult.ProofData.Should().NotBeNullOrEmpty();
        _logger.LogInformation("Generated proof of reserve with hash {ProofHash}", proofResult.ProofHash);

        // 8. Verify the reserve status
        var statusResult = await _proofOfReserveService.GetReserveStatusAsync(assetId, BlockchainType.NeoX);
        statusResult.Should().NotBeNull();
        statusResult.TotalReserves.Should().Be(10500000m);
        statusResult.IsCompliant.Should().BeTrue();
        _logger.LogInformation("Verified reserve status: {TotalReserves} reserves, {ReserveRatio} ratio", 
            statusResult.TotalReserves, statusResult.ReserveRatio);

        // Complete test verification
        assetResult.AssetId.Should().NotBeNullOrEmpty();
        poolResult.PoolId.Should().NotBeNullOrEmpty();
        automationResult.AutomationId.Should().NotBeNullOrEmpty();
        submissionResult.TransactionId.Should().NotBeNullOrEmpty();
        executionResult.ExecutionId.Should().NotBeNullOrEmpty();
        batchResult.BatchId.Should().NotBeNullOrEmpty();
        proofResult.ProofHash.Should().NotBeNullOrEmpty();
        statusResult.Health.Should().Be(ReserveHealthStatus.Healthy);

        _logger.LogInformation("Automated proof of reserve with fair ordering completed successfully!");
    }

    [Fact]
    public async Task CrossChainAssetBridge_WithMonitoring_ShouldHandleTransferFlow()
    {
        _logger.LogInformation("Starting cross-chain asset bridge with monitoring test...");

        // 1. Configure cross-chain bridge
        var bridgeConfig = new BridgeConfiguration
        {
            BridgeId = "neo-x-to-n3-bridge",
            SourceChain = BlockchainType.NeoX,
            TargetChain = BlockchainType.NeoN3,
            SupportedAssets = new[] { "GAS", "NEO", "USDC" },
            MinTransferAmount = 10m,
            MaxTransferAmount = 1000000m,
            BridgeFeePercentage = 0.1m,
            RequiredConfirmations = 12,
            IsActive = true
        };

        var configResult = await _crossChainService.ConfigureBridgeAsync(bridgeConfig);
        configResult.Success.Should().BeTrue();
        _logger.LogInformation("Configured cross-chain bridge {BridgeId}", bridgeConfig.BridgeId);

        // 2. Set up monitoring for bridge operations
        var monitoringRequest = new CreateMonitorRequest
        {
            Name = "Bridge Health Monitor",
            Description = "Monitor cross-chain bridge health and performance",
            MonitorType = MonitorType.Service,
            Target = "CrossChainService",
            CheckInterval = TimeSpan.FromMinutes(1),
            AlertThresholds = new AlertThresholds
            {
                ErrorRate = 0.05m, // Alert if error rate > 5%
                ResponseTime = 5000, // Alert if response time > 5 seconds
                AvailabilityPercentage = 99.5m // Alert if availability < 99.5%
            },
            IsActive = true
        };

        var monitorResult = await _monitoringService.CreateMonitorAsync(monitoringRequest, BlockchainType.NeoX);
        monitorResult.Success.Should().BeTrue();
        var monitorId = monitorResult.MonitorId;
        _logger.LogInformation("Created monitor {MonitorId} for bridge operations", monitorId);

        // 3. Initiate cross-chain transfer
        var transferRequest = new CrossChainTransferRequest
        {
            BridgeId = bridgeConfig.BridgeId,
            Asset = "USDC",
            Amount = 1000m,
            SourceAddress = "0xsource_address",
            TargetAddress = "Ntarget_address",
            Metadata = new Dictionary<string, object>
            {
                { "purpose", "integration_test" },
                { "timestamp", DateTime.UtcNow }
            }
        };

        var transferResult = await _crossChainService.InitiateTransferAsync(transferRequest);
        transferResult.Success.Should().BeTrue();
        var transferId = transferResult.TransferId;
        _logger.LogInformation("Initiated cross-chain transfer {TransferId}", transferId);

        // 4. Create automation to monitor pending transfers
        var transferMonitorAutomation = new CreateAutomationRequest
        {
            Name = "Pending Transfer Monitor",
            Description = "Check and process pending cross-chain transfers",
            TriggerType = AutomationTriggerType.Schedule,
            TriggerConfiguration = JsonSerializer.Serialize(new 
            { 
                cron = "*/5 * * * *", // Every 5 minutes
                timezone = "UTC"
            }),
            ActionType = AutomationActionType.ServiceCall,
            ActionConfiguration = JsonSerializer.Serialize(new
            {
                service = "CrossChainService",
                method = "ProcessPendingTransfersAsync",
                parameters = new { bridgeId = bridgeConfig.BridgeId }
            }),
            IsActive = true
        };

        var automationResult = await _automationService.CreateAutomationAsync(transferMonitorAutomation, BlockchainType.NeoX);
        automationResult.Success.Should().BeTrue();
        _logger.LogInformation("Created automation for monitoring pending transfers");

        // 5. Set up notification for completed transfers
        var notificationChannel = new NotificationChannel
        {
            Name = "Bridge Notifications",
            Type = NotificationChannelType.Webhook,
            Configuration = JsonSerializer.Serialize(new
            {
                url = "https://example.com/webhook",
                headers = new Dictionary<string, string>
                {
                    { "Authorization", "Bearer test-token" }
                }
            }),
            IsActive = true
        };

        var channelResult = await _notificationService.CreateChannelAsync(notificationChannel);
        channelResult.Success.Should().BeTrue();
        var channelId = channelResult.ChannelId;

        var notificationRule = new NotificationRule
        {
            Name = "Transfer Completion Notifications",
            EventType = "CrossChainTransferCompleted",
            ChannelId = channelId,
            Template = "Cross-chain transfer {{transferId}} completed: {{amount}} {{asset}} from {{sourceChain}} to {{targetChain}}",
            IsActive = true
        };

        var ruleResult = await _notificationService.CreateRuleAsync(notificationRule);
        ruleResult.Success.Should().BeTrue();
        _logger.LogInformation("Set up notification rule for transfer completions");

        // 6. Check monitoring status
        var monitorStatus = await _monitoringService.GetMonitorStatusAsync(monitorId, BlockchainType.NeoX);
        monitorStatus.Should().NotBeNull();
        monitorStatus.Status.Should().Be(MonitorStatus.Healthy);
        monitorStatus.LastCheckTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(2));
        _logger.LogInformation("Monitor status: {Status}, Last check: {LastCheck}", 
            monitorStatus.Status, monitorStatus.LastCheckTime);

        // 7. Create backup of bridge configuration
        var backupRequest = new BackupRequest
        {
            BackupType = BackupType.Configuration,
            Description = "Bridge configuration backup",
            IncludeSecrets = false,
            Compression = CompressionType.Gzip,
            Encryption = EncryptionType.AES256,
            TargetLocation = "local",
            RetentionDays = 30
        };

        var backupResult = await _backupService.CreateBackupAsync(backupRequest, BlockchainType.NeoX);
        backupResult.Success.Should().BeTrue();
        _logger.LogInformation("Created backup {BackupId} of bridge configuration", backupResult.BackupId);

        // 8. Verify transfer status
        var transferStatus = await _crossChainService.GetTransferStatusAsync(transferId);
        transferStatus.Should().NotBeNull();
        transferStatus.TransferId.Should().Be(transferId);
        transferStatus.Status.Should().BeOneOf(TransferStatus.Pending, TransferStatus.Processing, TransferStatus.Completed);
        _logger.LogInformation("Transfer {TransferId} status: {Status}", transferId, transferStatus.Status);

        // Complete test verification
        configResult.BridgeId.Should().NotBeNullOrEmpty();
        monitorResult.MonitorId.Should().NotBeNullOrEmpty();
        transferResult.TransferId.Should().NotBeNullOrEmpty();
        automationResult.AutomationId.Should().NotBeNullOrEmpty();
        channelResult.ChannelId.Should().NotBeNullOrEmpty();
        ruleResult.RuleId.Should().NotBeNullOrEmpty();
        backupResult.BackupId.Should().NotBeNullOrEmpty();

        _logger.LogInformation("Cross-chain asset bridge with monitoring test completed successfully!");
    }

    [Fact]
    public async Task DisasterRecovery_WithConfigurationReload_ShouldRestoreServices()
    {
        _logger.LogInformation("Starting disaster recovery with configuration reload test...");

        // 1. Create comprehensive configuration
        var config = new ServiceConfiguration
        {
            ServiceName = "DisasterRecoveryTest",
            Version = "1.0.0",
            Environment = "Testing",
            Features = new Dictionary<string, bool>
            {
                { "EnableAutomation", true },
                { "EnableMonitoring", true },
                { "EnableNotifications", true },
                { "EnableBackups", true }
            },
            Settings = new Dictionary<string, object>
            {
                { "MaxRetries", 3 },
                { "TimeoutSeconds", 30 },
                { "BatchSize", 100 },
                { "CacheEnabled", true }
            }
        };

        var configResult = await _configurationService.SetConfigurationAsync("disaster-recovery-test", config);
        configResult.Success.Should().BeTrue();
        _logger.LogInformation("Created test configuration");

        // 2. Create critical automation workflows
        var criticalAutomations = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            var automation = new CreateAutomationRequest
            {
                Name = $"Critical Automation {i}",
                Description = $"Critical automation workflow {i}",
                TriggerType = AutomationTriggerType.Event,
                TriggerConfiguration = JsonSerializer.Serialize(new { eventType = $"critical_event_{i}" }),
                ActionType = AutomationActionType.Webhook,
                ActionConfiguration = JsonSerializer.Serialize(new 
                { 
                    url = $"https://critical-endpoint-{i}.example.com",
                    method = "POST"
                }),
                IsActive = true,
                IsCritical = true
            };

            var result = await _automationService.CreateAutomationAsync(automation, BlockchainType.NeoX);
            result.Success.Should().BeTrue();
            criticalAutomations.Add(result.AutomationId);
        }
        _logger.LogInformation("Created {Count} critical automations", criticalAutomations.Count);

        // 3. Create full system backup
        var systemBackupRequest = new BackupRequest
        {
            BackupType = BackupType.Full,
            Description = "Pre-disaster full system backup",
            IncludeSecrets = true,
            Compression = CompressionType.Gzip,
            Encryption = EncryptionType.AES256,
            TargetLocation = "remote",
            RetentionDays = 365,
            IncludeServices = new[] 
            { 
                "ConfigurationService", 
                "AutomationService", 
                "MonitoringService",
                "ProofOfReserveService"
            }
        };

        var backupResult = await _backupService.CreateBackupAsync(systemBackupRequest, BlockchainType.NeoX);
        backupResult.Success.Should().BeTrue();
        var backupId = backupResult.BackupId;
        _logger.LogInformation("Created full system backup {BackupId}", backupId);

        // 4. Simulate disaster - clear configurations
        await _configurationService.DeleteConfigurationAsync("disaster-recovery-test");
        foreach (var automationId in criticalAutomations)
        {
            await _automationService.DeleteAutomationAsync(automationId, BlockchainType.NeoX);
        }
        _logger.LogInformation("Simulated disaster by clearing configurations and automations");

        // 5. Verify data is gone
        var deletedConfig = await _configurationService.GetConfigurationAsync("disaster-recovery-test");
        deletedConfig.Should().BeNull();

        var automations = await _automationService.GetAutomationsAsync(new AutomationFilter(), BlockchainType.NeoX);
        automations.Should().NotContain(a => criticalAutomations.Contains(a.AutomationId));
        _logger.LogInformation("Verified data loss after disaster simulation");

        // 6. Initiate recovery from backup
        var restoreRequest = new RestoreRequest
        {
            BackupId = backupId,
            RestoreType = RestoreType.Full,
            TargetEnvironment = "Testing",
            ValidateBeforeRestore = true,
            OverwriteExisting = true
        };

        var restoreResult = await _backupService.RestoreAsync(restoreRequest, BlockchainType.NeoX);
        restoreResult.Success.Should().BeTrue();
        _logger.LogInformation("Restored from backup {BackupId}", backupId);

        // 7. Verify configuration is restored
        var restoredConfig = await _configurationService.GetConfigurationAsync("disaster-recovery-test");
        restoredConfig.Should().NotBeNull();
        restoredConfig.ServiceName.Should().Be("DisasterRecoveryTest");
        restoredConfig.Features["EnableAutomation"].Should().BeTrue();
        _logger.LogInformation("Verified configuration restoration");

        // 8. Verify automations are restored
        var restoredAutomations = await _automationService.GetAutomationsAsync(
            new AutomationFilter { IsCritical = true }, 
            BlockchainType.NeoX);
        restoredAutomations.Should().HaveCountGreaterOrEqualTo(3);
        _logger.LogInformation("Verified automation restoration");

        // 9. Test service health after recovery
        var healthChecks = new List<(string service, bool healthy)>
        {
            ("ConfigurationService", await _configurationService.IsHealthyAsync()),
            ("AutomationService", await _automationService.IsHealthyAsync()),
            ("BackupService", await _backupService.IsHealthyAsync()),
            ("MonitoringService", await _monitoringService.IsHealthyAsync())
        };

        foreach (var (service, healthy) in healthChecks)
        {
            healthy.Should().BeTrue($"{service} should be healthy after recovery");
            _logger.LogInformation("{Service} health check: {Status}", service, healthy ? "Healthy" : "Unhealthy");
        }

        // 10. Send recovery notification
        var recoveryNotification = new Notification
        {
            Type = NotificationType.Critical,
            Subject = "Disaster Recovery Completed",
            Body = $"System successfully recovered from backup {backupId}",
            Recipients = new[] { "admin@example.com" },
            Metadata = new Dictionary<string, object>
            {
                { "backup_id", backupId },
                { "recovery_time", DateTime.UtcNow },
                { "restored_services", healthChecks.Count }
            }
        };

        var notificationResult = await _notificationService.SendNotificationAsync(recoveryNotification);
        notificationResult.Success.Should().BeTrue();
        _logger.LogInformation("Sent recovery completion notification");

        // Complete test verification
        backupResult.BackupId.Should().NotBeNullOrEmpty();
        restoreResult.RestoredItems.Should().BeGreaterThan(0);
        restoredConfig.Should().NotBeNull();
        healthChecks.Should().OnlyContain(h => h.healthy);
        notificationResult.NotificationId.Should().NotBeNullOrEmpty();

        _logger.LogInformation("Disaster recovery with configuration reload test completed successfully!");
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}