using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.Randomness;
using NeoServiceLayer.Services.Oracle;
using NeoServiceLayer.Services.KeyManagement;
using NeoServiceLayer.Services.Compute;
using NeoServiceLayer.Services.Storage;
using NeoServiceLayer.Services.AbstractAccount;
using NeoServiceLayer.AI.PatternRecognition;
using NeoServiceLayer.Tee.Host.Services;
using NeoServiceLayer.Tee.Host.Tests;
using System.Text.Json;

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
    private readonly IPatternRecognitionService _aiService;
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
        services.AddSingleton<IPatternRecognitionService, PatternRecognitionService>();
        services.AddSingleton<IAbstractAccountService, AbstractAccountService>();
        
        _serviceProvider = services.BuildServiceProvider();
        
        // Get service instances
        _randomnessService = _serviceProvider.GetRequiredService<IRandomnessService>();
        _oracleService = _serviceProvider.GetRequiredService<IOracleService>();
        _keyManagementService = _serviceProvider.GetRequiredService<IKeyManagementService>();
        _computeService = _serviceProvider.GetRequiredService<IComputeService>();
        _storageService = _serviceProvider.GetRequiredService<IStorageService>();
        _aiService = _serviceProvider.GetRequiredService<IPatternRecognitionService>();
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

    [Fact]
    public async Task DeFiLiquidationBot_ShouldExecuteCompleteWorkflow()
    {
        _logger.LogInformation("Starting DeFi liquidation bot integration test...");

        // 1. Create abstract account for the liquidation bot
        var accountRequest = new CreateAccountRequest
        {
            OwnerPublicKey = "0x1234567890abcdef",
            InitialGuardians = new[] { "0xguardian1", "0xguardian2" },
            RecoveryThreshold = 2,
            EnableGaslessTransactions = true,
            Metadata = new Dictionary<string, object> { ["purpose"] = "liquidation_bot" }
        };

        var accountResult = await _abstractAccountService.CreateAccountAsync(accountRequest, BlockchainType.NeoX);
        accountResult.Success.Should().BeTrue();
        _logger.LogInformation("Created liquidation bot account: {AccountId}", accountResult.AccountId);

        // 2. Get current ETH price from oracle
        var priceRequest = new OracleDataRequest
        {
            DataSource = "coinmarketcap",
            DataPath = "ethereum/price",
            Parameters = new Dictionary<string, object> { ["currency"] = "USD" },
            Metadata = new Dictionary<string, object> { ["purpose"] = "liquidation_check" }
        };

        var priceResult = await _oracleService.GetDataAsync(priceRequest, BlockchainType.NeoX);
        priceResult.Success.Should().BeTrue();
        _logger.LogInformation("Retrieved ETH price: {Price}", priceResult.Data);

        // 3. Generate random liquidation order using randomness service
        var randomRequest = new RandomnessRequest
        {
            MinValue = 1,
            MaxValue = 100,
            Count = 1,
            Metadata = new Dictionary<string, object> { ["purpose"] = "liquidation_order" }
        };

        var randomResult = await _randomnessService.GenerateRandomAsync(randomRequest, BlockchainType.NeoX);
        randomResult.Success.Should().BeTrue();
        randomResult.RandomValues.Should().HaveCount(1);
        _logger.LogInformation("Generated random liquidation order: {Order}", randomResult.RandomValues[0]);

        // 4. Use AI to assess liquidation risk
        var riskRequest = new PatternAnalysisRequest
        {
            ModelId = "liquidation_risk_model",
            InputData = new Dictionary<string, object>
            {
                ["eth_price"] = priceResult.Data,
                ["liquidation_order"] = randomResult.RandomValues[0],
                ["market_volatility"] = 0.15
            },
            Metadata = new Dictionary<string, object> { ["analysis_type"] = "liquidation_risk" }
        };

        var riskResult = await _aiService.AnalyzePatternAsync(riskRequest, BlockchainType.NeoX);
        riskResult.Success.Should().BeTrue();
        _logger.LogInformation("AI risk assessment completed with {PatternCount} patterns detected", 
            riskResult.DetectedPatterns.Length);

        // 5. Store liquidation decision in encrypted storage
        var liquidationDecision = new
        {
            AccountId = accountResult.AccountId,
            EthPrice = priceResult.Data,
            LiquidationOrder = randomResult.RandomValues[0],
            RiskScore = riskResult.DetectedPatterns.FirstOrDefault()?.Confidence ?? 0.5,
            Decision = "EXECUTE",
            Timestamp = DateTime.UtcNow
        };

        var storageRequest = new StorageRequest
        {
            Key = $"liquidation_decision_{accountResult.AccountId}",
            Data = JsonSerializer.SerializeToUtf8Bytes(liquidationDecision),
            Metadata = new Dictionary<string, object> { ["type"] = "liquidation_decision" }
        };

        var storageResult = await _storageService.StoreAsync(storageRequest, BlockchainType.NeoX);
        storageResult.Success.Should().BeTrue();
        _logger.LogInformation("Stored liquidation decision with key: {Key}", storageRequest.Key);

        // 6. Execute liquidation transaction using abstract account
        var transactionRequest = new ExecuteTransactionRequest
        {
            AccountId = accountResult.AccountId,
            ToAddress = "0xliquidation_contract",
            Value = 1000,
            Data = JsonSerializer.Serialize(liquidationDecision),
            GasLimit = 100000,
            UseSessionKey = false,
            Metadata = new Dictionary<string, object> { ["type"] = "liquidation_execution" }
        };

        var transactionResult = await _abstractAccountService.ExecuteTransactionAsync(transactionRequest, BlockchainType.NeoX);
        transactionResult.Success.Should().BeTrue();
        _logger.LogInformation("Executed liquidation transaction: {TxHash}", transactionResult.TransactionHash);

        // 7. Verify the complete workflow
        accountResult.AccountId.Should().NotBeNullOrEmpty();
        priceResult.Data.Should().NotBeNull();
        randomResult.RandomValues[0].Should().BeInRange(1, 100);
        riskResult.DetectedPatterns.Should().NotBeEmpty();
        storageResult.StorageId.Should().NotBeNullOrEmpty();
        transactionResult.TransactionHash.Should().NotBeNullOrEmpty();

        _logger.LogInformation("DeFi liquidation bot workflow completed successfully!");
    }

    [Fact]
    public async Task GamingScenario_ShouldHandleCompleteGameSession()
    {
        _logger.LogInformation("Starting gaming scenario integration test...");

        // 1. Create player account
        var playerAccountRequest = new CreateAccountRequest
        {
            OwnerPublicKey = "0xplayer123",
            InitialGuardians = new[] { "0xgame_operator" },
            RecoveryThreshold = 1,
            EnableGaslessTransactions = true,
            Metadata = new Dictionary<string, object> { ["player_type"] = "premium" }
        };

        var playerAccount = await _abstractAccountService.CreateAccountAsync(playerAccountRequest, BlockchainType.NeoX);
        playerAccount.Success.Should().BeTrue();

        // 2. Generate random dice rolls for game
        var diceRequest = new RandomnessRequest
        {
            MinValue = 1,
            MaxValue = 7,
            Count = 5,
            Metadata = new Dictionary<string, object> { ["game"] = "dice_adventure" }
        };

        var diceResult = await _randomnessService.GenerateRandomAsync(diceRequest, BlockchainType.NeoX);
        diceResult.Success.Should().BeTrue();
        diceResult.RandomValues.Should().HaveCount(5);
        diceResult.RandomValues.Should().OnlyContain(v => v >= 1 && v < 7);

        // 3. Execute game logic using compute service
        var gameLogic = @"
            function calculateScore(diceRolls) {
                let score = 0;
                let bonus = 0;
                
                for (let roll of diceRolls) {
                    score += roll;
                    if (roll === 6) bonus += 10;
                }
                
                return {
                    baseScore: score,
                    bonus: bonus,
                    totalScore: score + bonus,
                    achievement: score > 25 ? 'HIGH_ROLLER' : 'NORMAL'
                };
            }
            
            calculateScore(input.diceRolls);
        ";

        var computeRequest = new ComputeRequest
        {
            FunctionCode = gameLogic,
            InputData = new Dictionary<string, object> { ["diceRolls"] = diceResult.RandomValues },
            TimeoutMs = 5000,
            Metadata = new Dictionary<string, object> { ["game_session"] = Guid.NewGuid().ToString() }
        };

        var computeResult = await _computeService.ExecuteAsync(computeRequest, BlockchainType.NeoX);
        computeResult.Success.Should().BeTrue();

        // 4. Store game state
        var gameState = new
        {
            PlayerId = playerAccount.AccountId,
            DiceRolls = diceResult.RandomValues,
            GameResult = computeResult.Result,
            Timestamp = DateTime.UtcNow,
            SessionId = computeRequest.Metadata["game_session"]
        };

        var gameStateRequest = new StorageRequest
        {
            Key = $"game_state_{playerAccount.AccountId}_{gameState.SessionId}",
            Data = JsonSerializer.SerializeToUtf8Bytes(gameState),
            Metadata = new Dictionary<string, object> { ["type"] = "game_state" }
        };

        var stateResult = await _storageService.StoreAsync(gameStateRequest, BlockchainType.NeoX);
        stateResult.Success.Should().BeTrue();

        // 5. Create session key for in-game transactions
        var sessionKeyRequest = new CreateSessionKeyRequest
        {
            AccountId = playerAccount.AccountId,
            Permissions = new SessionKeyPermissions
            {
                MaxTransactionValue = 100,
                AllowedContracts = new[] { "0xgame_contract", "0xnft_contract" },
                MaxTransactionsPerDay = 50,
                AllowGaslessTransactions = true
            },
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            Name = "Gaming Session Key",
            Metadata = new Dictionary<string, object> { ["purpose"] = "gaming" }
        };

        var sessionKeyResult = await _abstractAccountService.CreateSessionKeyAsync(sessionKeyRequest, BlockchainType.NeoX);
        sessionKeyResult.Success.Should().BeTrue();

        // 6. Verify complete gaming workflow
        playerAccount.AccountId.Should().NotBeNullOrEmpty();
        diceResult.RandomValues.Sum().Should().BeGreaterThan(5);
        computeResult.Result.Should().NotBeNull();
        stateResult.StorageId.Should().NotBeNullOrEmpty();
        sessionKeyResult.SessionKeyId.Should().NotBeNullOrEmpty();

        _logger.LogInformation("Gaming scenario completed successfully with total score: {Score}", 
            JsonSerializer.Deserialize<JsonElement>(computeResult.Result).GetProperty("totalScore").GetInt32());
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
