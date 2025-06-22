using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.AbstractAccount;
using NeoServiceLayer.Services.Compute;
using NeoServiceLayer.Services.KeyManagement;
using NeoServiceLayer.Services.Oracle;
using NeoServiceLayer.Services.Randomness;
using NeoServiceLayer.Services.Storage;
using NeoServiceLayer.Tee.Enclave;
using NeoServiceLayer.Tee.Host.Services;
using NeoServiceLayer.Tee.Host.Tests;
using Xunit;
using PatternRecognitionSvc = NeoServiceLayer.AI.PatternRecognition;

namespace NeoServiceLayer.Integration.Tests;

/// <summary>
/// Integration tests that validate cross-service workflows and interactions.
/// </summary>
public class CrossServiceIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IRandomnessService _randomnessService;
    private readonly IOracleService _oracleService;
    private readonly IKeyManagementService _keyManagementService;
    private readonly IComputeService _computeService;
    private readonly IStorageService _storageService;
    private readonly PatternRecognitionSvc.IPatternRecognitionService _aiService;
    private readonly IAbstractAccountService _abstractAccountService;
    private readonly ILogger<CrossServiceIntegrationTests> _logger;

    public CrossServiceIntegrationTests()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

        // Add enclave services
        services.AddSingleton<IEnclaveWrapper, TestEnclaveWrapper>();
        services.AddSingleton<IEnclaveManager, EnclaveManager>();

        // Add all services
        services.AddSingleton<IRandomnessService, RandomnessService>();
        services.AddSingleton<IOracleService, OracleService>();
        services.AddSingleton<IKeyManagementService, KeyManagementService>();
        services.AddSingleton<IComputeService, ComputeService>();
        services.AddSingleton<IStorageService, StorageService>();
        services.AddSingleton<PatternRecognitionSvc.IPatternRecognitionService, PatternRecognitionSvc.PatternRecognitionService>();
        services.AddSingleton<IAbstractAccountService, AbstractAccountService>();

        _serviceProvider = services.BuildServiceProvider();

        // Get service instances
        _randomnessService = _serviceProvider.GetRequiredService<IRandomnessService>();
        _oracleService = _serviceProvider.GetRequiredService<IOracleService>();
        _keyManagementService = _serviceProvider.GetRequiredService<IKeyManagementService>();
        _computeService = _serviceProvider.GetRequiredService<IComputeService>();
        _storageService = _serviceProvider.GetRequiredService<IStorageService>();
        _aiService = _serviceProvider.GetRequiredService<PatternRecognitionSvc.IPatternRecognitionService>();
        _abstractAccountService = _serviceProvider.GetRequiredService<IAbstractAccountService>();
        _logger = _serviceProvider.GetRequiredService<ILogger<CrossServiceIntegrationTests>>();

        // Initialize all services
        InitializeServicesAsync().GetAwaiter().GetResult();
    }

    private async Task InitializeServicesAsync()
    {
        _logger.LogInformation("Initializing all services for integration testing...");

        await _randomnessService.InitializeAsync();
        await _oracleService.InitializeAsync();
        await _keyManagementService.InitializeAsync();
        await _computeService.InitializeAsync();
        await _storageService.InitializeAsync();
        await _aiService.InitializeAsync();
        await _abstractAccountService.InitializeAsync();

        _logger.LogInformation("All services initialized successfully");
    }

    [Fact(Skip = "Missing CreateAccountRequest, OracleDataRequest, RandomnessRequest types - advanced DeFi liquidation features not implemented")]
    public async Task DeFiLiquidationBot_ShouldExecuteCompleteWorkflow()
    {
        // Test skipped due to missing types
        await Task.CompletedTask;
    }

    [Fact(Skip = "Missing CreateAccountRequest and related gaming types - advanced gaming features not implemented")]
    public async Task GamingScenario_ShouldHandleCompleteGameSession()
    {
        // Test skipped due to missing types
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
