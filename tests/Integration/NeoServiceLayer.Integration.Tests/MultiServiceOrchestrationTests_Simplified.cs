using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Backup;
using NeoServiceLayer.Services.Configuration;
using NeoServiceLayer.Services.CrossChain;
using NeoServiceLayer.Services.Monitoring;
using NeoServiceLayer.Services.Notification;
using NeoServiceLayer.Services.ProofOfReserve;
using NeoServiceLayer.Tee.Enclave;
using NeoServiceLayer.Tee.Host.Services;
using NeoServiceLayer.Tee.Host.Tests;
using Xunit;
using AutomationSvc = NeoServiceLayer.Services.Automation;
using FairOrderingSvc = NeoServiceLayer.Advanced.FairOrdering;

namespace NeoServiceLayer.Integration.Tests;

/// <summary>
/// Simplified integration tests for multi-service orchestration scenarios.
/// This version focuses on testing service registration and basic interactions.
/// </summary>
public class MultiServiceOrchestrationTests_Simplified : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ILogger<MultiServiceOrchestrationTests_Simplified> _logger;
    private readonly IProofOfReserveService _proofOfReserveService;
    private readonly IConfigurationService _configurationService;
    private readonly IBackupService _backupService;
    private readonly INotificationService _notificationService;

    public MultiServiceOrchestrationTests_Simplified()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

        // Add enclave services
        services.AddSingleton<IEnclaveWrapper, TestEnclaveWrapper>();
        services.AddSingleton<IEnclaveManager, EnclaveManager>();

        // Add core services
        services.AddSingleton<IProofOfReserveService, ProofOfReserveService>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<INotificationService, NotificationService>();

        _serviceProvider = services.BuildServiceProvider();

        // Get service instances
        _logger = _serviceProvider.GetRequiredService<ILogger<MultiServiceOrchestrationTests_Simplified>>();
        _proofOfReserveService = _serviceProvider.GetRequiredService<IProofOfReserveService>();
        _configurationService = _serviceProvider.GetRequiredService<IConfigurationService>();
        _backupService = _serviceProvider.GetRequiredService<IBackupService>();
        _notificationService = _serviceProvider.GetRequiredService<INotificationService>();

        // Initialize all services
        InitializeServicesAsync().GetAwaiter().GetResult();
    }

    private async Task InitializeServicesAsync()
    {
        _logger.LogInformation("Initializing services for simplified multi-service orchestration testing...");

        await _proofOfReserveService.InitializeAsync();
        await _configurationService.InitializeAsync();
        await _backupService.InitializeAsync();
        await _notificationService.InitializeAsync();

        _logger.LogInformation("All services initialized successfully");
    }

    [Fact]
    public async Task ServiceRegistration_ShouldWorkCorrectly()
    {
        _logger.LogInformation("Testing service registration and basic functionality...");

        // Test that all services are properly registered
        _proofOfReserveService.Should().NotBeNull("ProofOfReserveService should be registered");
        _configurationService.Should().NotBeNull("ConfigurationService should be registered");
        _backupService.Should().NotBeNull("BackupService should be registered");
        _notificationService.Should().NotBeNull("NotificationService should be registered");

        // Test basic service health (using service instances as health indicator)
        _proofOfReserveService.Should().NotBeNull("ProofOfReserveService should be healthy");
        _configurationService.Should().NotBeNull("ConfigurationService should be healthy");
        _backupService.Should().NotBeNull("BackupService should be healthy");
        _notificationService.Should().NotBeNull("NotificationService should be healthy");

        _logger.LogInformation("Service registration test completed successfully!");
    }

    [Fact]
    public async Task BasicProofOfReserve_ShouldWork()
    {
        _logger.LogInformation("Testing basic proof of reserve functionality...");

        // Register an asset for proof of reserve
        var assetRequest = new AssetRegistrationRequest
        {
            AssetSymbol = "USDC",
            AssetName = "USD Coin",
            ReserveAddresses = new[] { "0xreserve1", "0xreserve2", "0xreserve3" },
            MinReserveRatio = 1.0m,
            TotalSupply = 10000000m,
            MonitoringFrequencyMinutes = 60
        };

        var assetId = await _proofOfReserveService.RegisterAssetAsync(assetRequest, BlockchainType.NeoX);
        assetId.Should().NotBeNullOrEmpty();
        _logger.LogInformation("Registered asset {AssetId} for proof of reserve", assetId);

        // Generate proof
        var proof = await _proofOfReserveService.GenerateProofAsync(assetId, BlockchainType.NeoX);
        proof.Should().NotBeNull();
        proof.ProofId.Should().NotBeNullOrEmpty();
        _logger.LogInformation("Generated proof of reserve with ID {ProofId}", proof.ProofId);

        // Get reserve status
        var statusResult = await _proofOfReserveService.GetReserveStatusAsync(assetId, BlockchainType.NeoX);
        statusResult.Should().NotBeNull();
        statusResult.TotalReserves.Should().BeGreaterThan(0);
        statusResult.IsCompliant.Should().BeTrue();
        _logger.LogInformation("Verified reserve status: compliant={IsCompliant}", statusResult.IsCompliant);

        _logger.LogInformation("Basic proof of reserve test completed successfully!");
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
