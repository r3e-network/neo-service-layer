using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Randomness;
using NeoServiceLayer.Services.Oracle;
using NeoServiceLayer.Services.AbstractAccount;
using NeoServiceLayer.Tee.Host.Services;
using NeoServiceLayer.Tee.Host.Tests;
using System.Text.Json;

namespace NeoServiceLayer.Integration.Tests;

/// <summary>
/// Integration tests that validate smart contract interactions with Neo Service Layer.
/// </summary>
public class SmartContractIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IRandomnessService _randomnessService;
    private readonly IOracleService _oracleService;
    private readonly IAbstractAccountService _abstractAccountService;
    private readonly ILogger<SmartContractIntegrationTests> _logger;

    public SmartContractIntegrationTests()
    {
        var services = new ServiceCollection();
        
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        services.AddSingleton<IEnclaveWrapper, TestEnclaveWrapper>();
        services.AddSingleton<IEnclaveManager, EnclaveManager>();
        services.AddSingleton<IRandomnessService, RandomnessService>();
        services.AddSingleton<IOracleService, OracleService>();
        services.AddSingleton<IAbstractAccountService, AbstractAccountService>();
        
        _serviceProvider = services.BuildServiceProvider();
        
        _randomnessService = _serviceProvider.GetRequiredService<IRandomnessService>();
        _oracleService = _serviceProvider.GetRequiredService<IOracleService>();
        _abstractAccountService = _serviceProvider.GetRequiredService<IAbstractAccountService>();
        _logger = _serviceProvider.GetRequiredService<ILogger<SmartContractIntegrationTests>>();
        
        InitializeServicesAsync().GetAwaiter().GetResult();
    }

    private async Task InitializeServicesAsync()
    {
        await _randomnessService.InitializeAsync();
        await _oracleService.InitializeAsync();
        await _abstractAccountService.InitializeAsync();
    }

    [Fact]
    public async Task RandomnessConsumerContract_ShouldReceiveRandomnessFromService()
    {
        _logger.LogInformation("Testing RandomnessConsumer contract integration...");

        // Simulate smart contract requesting randomness
        var contractRequest = new RandomnessRequest
        {
            MinValue = 1,
            MaxValue = 1000000,
            Count = 1,
            Metadata = new Dictionary<string, object>
            {
                ["contract_address"] = "0x1234567890abcdef1234567890abcdef12345678",
                ["request_id"] = "0xrandomness_request_123",
                ["callback_function"] = "fulfillRandomness(bytes32,uint256)"
            }
        };

        var result = await _randomnessService.GenerateRandomAsync(contractRequest, BlockchainType.NeoX);
        result.Success.Should().BeTrue();
        result.RandomValues.Should().HaveCount(1);
        result.RandomValues[0].Should().BeInRange(1, 1000000);

        // Verify callback data format for smart contract
        var callbackData = new
        {
            requestId = contractRequest.Metadata["request_id"],
            randomValue = result.RandomValues[0],
            contractAddress = contractRequest.Metadata["contract_address"],
            gasLimit = 100000,
            timestamp = DateTime.UtcNow.ToUnixTimeSeconds()
        };

        var callbackJson = JsonSerializer.Serialize(callbackData);
        callbackJson.Should().Contain("randomValue");
        callbackJson.Should().Contain("requestId");

        _logger.LogInformation("RandomnessConsumer integration successful. Random value: {Value}", result.RandomValues[0]);
    }

    [Fact]
    public async Task OracleConsumerContract_ShouldReceivePriceDataFromService()
    {
        _logger.LogInformation("Testing OracleConsumer contract integration...");

        // Simulate smart contract requesting price data
        var priceRequest = new OracleDataRequest
        {
            DataSource = "coinmarketcap",
            DataPath = "bitcoin/price",
            Parameters = new Dictionary<string, object> { ["currency"] = "USD" },
            Metadata = new Dictionary<string, object>
            {
                ["contract_address"] = "0xabcdef1234567890abcdef1234567890abcdef12",
                ["request_id"] = "0xoracle_request_456",
                ["callback_function"] = "fulfillOracleRequest(bytes32,bool,bytes)"
            }
        };

        var result = await _oracleService.GetDataAsync(priceRequest, BlockchainType.NeoX);
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();

        // Verify callback data format for smart contract
        var callbackData = new
        {
            requestId = priceRequest.Metadata["request_id"],
            success = result.Success,
            data = result.Data,
            contractAddress = priceRequest.Metadata["contract_address"],
            gasLimit = 150000,
            timestamp = DateTime.UtcNow.ToUnixTimeSeconds()
        };

        var callbackJson = JsonSerializer.Serialize(callbackData);
        callbackJson.Should().Contain("success");
        callbackJson.Should().Contain("data");

        _logger.LogInformation("OracleConsumer integration successful. Price data: {Data}", result.Data);
    }

    [Fact]
    public async Task AbstractAccountFactory_ShouldCreateAccountsForSmartContracts()
    {
        _logger.LogInformation("Testing AbstractAccountFactory contract integration...");

        // Simulate smart contract factory creating accounts
        var factoryRequest = new CreateAccountRequest
        {
            OwnerPublicKey = "0x1234567890abcdef1234567890abcdef12345678901234567890abcdef12345678",
            InitialGuardians = new[] 
            { 
                "0xguardian1234567890abcdef1234567890abcdef12",
                "0xguardian2abcdef1234567890abcdef1234567890"
            },
            RecoveryThreshold = 2,
            EnableGaslessTransactions = true,
            Metadata = new Dictionary<string, object>
            {
                ["factory_contract"] = "0xfactory1234567890abcdef1234567890abcdef",
                ["account_type"] = "smart_wallet",
                ["salt"] = "0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef"
            }
        };

        var result = await _abstractAccountService.CreateAccountAsync(factoryRequest, BlockchainType.NeoX);
        result.Success.Should().BeTrue();
        result.AccountId.Should().NotBeNullOrEmpty();
        result.AccountAddress.Should().NotBeNullOrEmpty();
        result.MasterPublicKey.Should().NotBeNullOrEmpty();

        // Verify account creation event data for smart contract
        var eventData = new
        {
            accountId = result.AccountId,
            accountAddress = result.AccountAddress,
            owner = factoryRequest.OwnerPublicKey,
            guardians = factoryRequest.InitialGuardians,
            recoveryThreshold = factoryRequest.RecoveryThreshold,
            factoryContract = factoryRequest.Metadata["factory_contract"],
            blockNumber = 12345678,
            transactionHash = result.TransactionHash
        };

        var eventJson = JsonSerializer.Serialize(eventData);
        eventJson.Should().Contain("accountId");
        eventJson.Should().Contain("accountAddress");

        _logger.LogInformation("AbstractAccountFactory integration successful. Account: {AccountId}", result.AccountId);
    }

    [Fact]
    public async Task DeFiProtocolIntegration_ShouldExecuteComplexWorkflow()
    {
        _logger.LogInformation("Testing DeFi protocol integration with multiple services...");

        // 1. Create DeFi protocol account
        var protocolAccount = await _abstractAccountService.CreateAccountAsync(new CreateAccountRequest
        {
            OwnerPublicKey = "0xdefi_protocol_owner",
            InitialGuardians = new[] { "0xdefi_guardian1", "0xdefi_guardian2" },
            RecoveryThreshold = 2,
            EnableGaslessTransactions = true,
            Metadata = new Dictionary<string, object> { ["protocol"] = "yield_farming" }
        }, BlockchainType.NeoX);

        protocolAccount.Success.Should().BeTrue();

        // 2. Get multiple price feeds for DeFi calculations
        var priceFeeds = new[]
        {
            ("ethereum", "ETH/USD"),
            ("bitcoin", "BTC/USD"),
            ("chainlink", "LINK/USD")
        };

        var priceResults = new List<OracleDataResult>();
        foreach (var (asset, pair) in priceFeeds)
        {
            var priceRequest = new OracleDataRequest
            {
                DataSource = "coinmarketcap",
                DataPath = $"{asset}/price",
                Parameters = new Dictionary<string, object> { ["currency"] = "USD" },
                Metadata = new Dictionary<string, object> 
                { 
                    ["trading_pair"] = pair,
                    ["protocol_account"] = protocolAccount.AccountId
                }
            };

            var priceResult = await _oracleService.GetDataAsync(priceRequest, BlockchainType.NeoX);
            priceResult.Success.Should().BeTrue();
            priceResults.Add(priceResult);
        }

        // 3. Generate random rebalancing weights
        var rebalanceRequest = new RandomnessRequest
        {
            MinValue = 10,
            MaxValue = 40,
            Count = 3,
            Metadata = new Dictionary<string, object>
            {
                ["purpose"] = "portfolio_rebalancing",
                ["protocol_account"] = protocolAccount.AccountId
            }
        };

        var rebalanceResult = await _randomnessService.GenerateRandomAsync(rebalanceRequest, BlockchainType.NeoX);
        rebalanceResult.Success.Should().BeTrue();
        rebalanceResult.RandomValues.Should().HaveCount(3);

        // 4. Execute rebalancing transactions
        var rebalanceTransactions = new List<ExecuteTransactionRequest>();
        for (int i = 0; i < priceFeeds.Length; i++)
        {
            var transaction = new ExecuteTransactionRequest
            {
                AccountId = protocolAccount.AccountId,
                ToAddress = $"0x{priceFeeds[i].Item1}_pool_contract",
                Value = rebalanceResult.RandomValues[i] * 1000, // Scale up the random weight
                Data = JsonSerializer.Serialize(new
                {
                    action = "rebalance",
                    asset = priceFeeds[i].Item1,
                    price = priceResults[i].Data,
                    weight = rebalanceResult.RandomValues[i]
                }),
                GasLimit = 200000,
                UseSessionKey = false,
                Metadata = new Dictionary<string, object> { ["transaction_type"] = "rebalancing" }
            };

            rebalanceTransactions.Add(transaction);
        }

        // Execute batch transactions
        var batchRequest = new BatchTransactionRequest
        {
            AccountId = protocolAccount.AccountId,
            Transactions = rebalanceTransactions.ToArray(),
            StopOnFailure = false,
            Metadata = new Dictionary<string, object> { ["batch_type"] = "portfolio_rebalancing" }
        };

        var batchResult = await _abstractAccountService.ExecuteBatchTransactionAsync(batchRequest, BlockchainType.NeoX);
        batchResult.AllSuccessful.Should().BeTrue();
        batchResult.Results.Should().HaveCount(3);

        // 5. Verify complete DeFi workflow
        protocolAccount.AccountId.Should().NotBeNullOrEmpty();
        priceResults.Should().HaveCount(3);
        priceResults.Should().OnlyContain(r => r.Success);
        rebalanceResult.RandomValues.Sum().Should().BeGreaterThan(30);
        batchResult.BatchId.Should().NotBeNullOrEmpty();

        _logger.LogInformation("DeFi protocol integration successful. Processed {Count} rebalancing transactions", 
            batchResult.Results.Length);
    }

    [Fact]
    public async Task GameContractIntegration_ShouldHandleRandomnessAndAccounts()
    {
        _logger.LogInformation("Testing game contract integration...");

        // 1. Create game master account
        var gameMasterAccount = await _abstractAccountService.CreateAccountAsync(new CreateAccountRequest
        {
            OwnerPublicKey = "0xgame_master_key",
            InitialGuardians = new[] { "0xgame_operator" },
            RecoveryThreshold = 1,
            EnableGaslessTransactions = true,
            Metadata = new Dictionary<string, object> { ["role"] = "game_master" }
        }, BlockchainType.NeoX);

        // 2. Create multiple player accounts
        var playerAccounts = new List<AbstractAccountResult>();
        for (int i = 1; i <= 3; i++)
        {
            var playerAccount = await _abstractAccountService.CreateAccountAsync(new CreateAccountRequest
            {
                OwnerPublicKey = $"0xplayer_{i}_key",
                InitialGuardians = new[] { gameMasterAccount.AccountId },
                RecoveryThreshold = 1,
                EnableGaslessTransactions = true,
                Metadata = new Dictionary<string, object> { ["role"] = "player", ["player_id"] = i }
            }, BlockchainType.NeoX);

            playerAccount.Success.Should().BeTrue();
            playerAccounts.Add(playerAccount);
        }

        // 3. Generate random game events
        var gameEvents = new[]
        {
            ("dice_roll", 1, 7),
            ("card_draw", 1, 53),
            ("loot_drop", 1, 101)
        };

        var gameResults = new Dictionary<string, int[]>();
        foreach (var (eventType, min, max) in gameEvents)
        {
            var randomRequest = new RandomnessRequest
            {
                MinValue = min,
                MaxValue = max,
                Count = playerAccounts.Count,
                Metadata = new Dictionary<string, object>
                {
                    ["event_type"] = eventType,
                    ["game_master"] = gameMasterAccount.AccountId
                }
            };

            var randomResult = await _randomnessService.GenerateRandomAsync(randomRequest, BlockchainType.NeoX);
            randomResult.Success.Should().BeTrue();
            gameResults[eventType] = randomResult.RandomValues;
        }

        // 4. Execute game transactions for each player
        var gameTransactions = new List<ExecuteTransactionRequest>();
        for (int i = 0; i < playerAccounts.Count; i++)
        {
            var playerResult = new
            {
                playerId = playerAccounts[i].AccountId,
                diceRoll = gameResults["dice_roll"][i],
                cardDraw = gameResults["card_draw"][i],
                lootDrop = gameResults["loot_drop"][i],
                timestamp = DateTime.UtcNow
            };

            var transaction = new ExecuteTransactionRequest
            {
                AccountId = playerAccounts[i].AccountId,
                ToAddress = "0xgame_contract",
                Value = 0,
                Data = JsonSerializer.Serialize(playerResult),
                GasLimit = 100000,
                UseSessionKey = false,
                Metadata = new Dictionary<string, object> { ["transaction_type"] = "game_result" }
            };

            gameTransactions.Add(transaction);
        }

        // Execute all game transactions
        foreach (var transaction in gameTransactions)
        {
            var result = await _abstractAccountService.ExecuteTransactionAsync(transaction, BlockchainType.NeoX);
            result.Success.Should().BeTrue();
        }

        // 5. Verify game integration
        gameMasterAccount.Success.Should().BeTrue();
        playerAccounts.Should().HaveCount(3);
        playerAccounts.Should().OnlyContain(p => p.Success);
        gameResults.Should().HaveCount(3);
        gameResults.Values.Should().OnlyContain(values => values.Length == 3);

        _logger.LogInformation("Game contract integration successful. Created {PlayerCount} players with {EventCount} game events", 
            playerAccounts.Count, gameResults.Count);
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
