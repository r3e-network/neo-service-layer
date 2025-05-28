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
/// End-to-end scenario tests that demonstrate complete real-world use cases.
/// </summary>
public class EndToEndScenarioTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceManager _serviceManager;
    private readonly ILogger<EndToEndScenarioTests> _logger;

    public EndToEndScenarioTests()
    {
        var services = new ServiceCollection();
        
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        services.AddSingleton<IEnclaveWrapper, TestEnclaveWrapper>();
        services.AddSingleton<IEnclaveManager, EnclaveManager>();
        
        // Add service manager
        services.AddSingleton<IServiceManager, ServiceManager>();
        
        // Add all services
        services.AddSingleton<IRandomnessService, RandomnessService>();
        services.AddSingleton<IOracleService, OracleService>();
        services.AddSingleton<IKeyManagementService, KeyManagementService>();
        services.AddSingleton<IComputeService, ComputeService>();
        services.AddSingleton<IStorageService, StorageService>();
        services.AddSingleton<IPatternRecognitionService, PatternRecognitionService>();
        services.AddSingleton<IAbstractAccountService, AbstractAccountService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _serviceManager = _serviceProvider.GetRequiredService<IServiceManager>();
        _logger = _serviceProvider.GetRequiredService<ILogger<EndToEndScenarioTests>>();
        
        InitializeSystemAsync().GetAwaiter().GetResult();
    }

    private async Task InitializeSystemAsync()
    {
        _logger.LogInformation("Initializing complete Neo Service Layer system...");
        
        // Register all services with the service manager
        await _serviceManager.RegisterServiceAsync<IRandomnessService>();
        await _serviceManager.RegisterServiceAsync<IOracleService>();
        await _serviceManager.RegisterServiceAsync<IKeyManagementService>();
        await _serviceManager.RegisterServiceAsync<IComputeService>();
        await _serviceManager.RegisterServiceAsync<IStorageService>();
        await _serviceManager.RegisterServiceAsync<IPatternRecognitionService>();
        await _serviceManager.RegisterServiceAsync<IAbstractAccountService>();
        
        // Start all services
        await _serviceManager.StartAllServicesAsync();
        
        _logger.LogInformation("Neo Service Layer system initialized successfully");
    }

    [Fact]
    public async Task DecentralizedTradingBot_CompleteWorkflow()
    {
        _logger.LogInformation("🤖 Starting Decentralized Trading Bot end-to-end scenario...");

        var randomnessService = _serviceManager.GetService<IRandomnessService>();
        var oracleService = _serviceManager.GetService<IOracleService>();
        var keyManagementService = _serviceManager.GetService<IKeyManagementService>();
        var computeService = _serviceManager.GetService<IComputeService>();
        var storageService = _serviceManager.GetService<IStorageService>();
        var aiService = _serviceManager.GetService<IPatternRecognitionService>();
        var accountService = _serviceManager.GetService<IAbstractAccountService>();

        // 1. Create trading bot identity and keys
        _logger.LogInformation("📋 Step 1: Creating trading bot identity...");
        
        var botKeyRequest = new KeyGenerationRequest
        {
            KeyType = KeyType.Secp256k1,
            Purpose = "trading_bot_master",
            Metadata = new Dictionary<string, object> { ["bot_version"] = "1.0" }
        };
        var botKey = await keyManagementService.GenerateKeyAsync(botKeyRequest, BlockchainType.NeoX);
        botKey.Success.Should().BeTrue();

        // 2. Create abstract account for the trading bot
        var botAccountRequest = new CreateAccountRequest
        {
            OwnerPublicKey = botKey.PublicKey,
            InitialGuardians = new[] { "0xtrading_operator", "0xrisk_manager" },
            RecoveryThreshold = 2,
            EnableGaslessTransactions = true,
            Metadata = new Dictionary<string, object> { ["bot_type"] = "algorithmic_trader" }
        };
        var botAccount = await accountService.CreateAccountAsync(botAccountRequest, BlockchainType.NeoX);
        botAccount.Success.Should().BeTrue();

        // 3. Get market data from multiple sources
        _logger.LogInformation("📊 Step 2: Fetching market data...");
        
        var marketAssets = new[] { "bitcoin", "ethereum", "chainlink", "polygon" };
        var marketData = new Dictionary<string, object>();
        
        foreach (var asset in marketAssets)
        {
            var priceRequest = new OracleDataRequest
            {
                DataSource = "coinmarketcap",
                DataPath = $"{asset}/price",
                Parameters = new Dictionary<string, object> { ["currency"] = "USD" },
                Metadata = new Dictionary<string, object> { ["asset"] = asset, ["bot_id"] = botAccount.AccountId }
            };
            
            var priceResult = await oracleService.GetDataAsync(priceRequest, BlockchainType.NeoX);
            priceResult.Success.Should().BeTrue();
            marketData[asset] = priceResult.Data;
        }

        // 4. Generate random trading parameters
        _logger.LogInformation("🎲 Step 3: Generating trading parameters...");
        
        var tradingParamsRequest = new RandomnessRequest
        {
            MinValue = 1,
            MaxValue = 100,
            Count = 10,
            Metadata = new Dictionary<string, object> { ["purpose"] = "trading_parameters" }
        };
        var tradingParams = await randomnessService.GenerateRandomAsync(tradingParamsRequest, BlockchainType.NeoX);
        tradingParams.Success.Should().BeTrue();

        // 5. Execute trading algorithm using compute service
        _logger.LogInformation("⚙️ Step 4: Executing trading algorithm...");
        
        var tradingAlgorithm = @"
            function executeTradingStrategy(marketData, tradingParams) {
                const signals = [];
                const assets = Object.keys(marketData);
                
                for (let i = 0; i < assets.length; i++) {
                    const asset = assets[i];
                    const price = parseFloat(marketData[asset]);
                    const randomFactor = tradingParams[i] / 100.0;
                    
                    // Simple momentum strategy with random factor
                    const signal = {
                        asset: asset,
                        price: price,
                        action: randomFactor > 0.6 ? 'BUY' : randomFactor < 0.4 ? 'SELL' : 'HOLD',
                        confidence: Math.abs(randomFactor - 0.5) * 2,
                        amount: Math.floor(randomFactor * 1000),
                        timestamp: Date.now()
                    };
                    
                    signals.push(signal);
                }
                
                return {
                    signals: signals,
                    totalSignals: signals.length,
                    buySignals: signals.filter(s => s.action === 'BUY').length,
                    sellSignals: signals.filter(s => s.action === 'SELL').length,
                    strategy: 'momentum_with_randomization'
                };
            }
            
            executeTradingStrategy(input.marketData, input.tradingParams);
        ";

        var computeRequest = new ComputeRequest
        {
            FunctionCode = tradingAlgorithm,
            InputData = new Dictionary<string, object>
            {
                ["marketData"] = marketData,
                ["tradingParams"] = tradingParams.RandomValues
            },
            TimeoutMs = 10000,
            Metadata = new Dictionary<string, object> { ["bot_id"] = botAccount.AccountId }
        };

        var tradingResult = await computeService.ExecuteAsync(computeRequest, BlockchainType.NeoX);
        tradingResult.Success.Should().BeTrue();

        // 6. Analyze trading signals with AI
        _logger.LogInformation("🧠 Step 5: AI analysis of trading signals...");
        
        var aiAnalysisRequest = new PatternAnalysisRequest
        {
            ModelId = "trading_signal_analyzer",
            InputData = new Dictionary<string, object>
            {
                ["trading_signals"] = tradingResult.Result,
                ["market_data"] = marketData,
                ["bot_performance"] = 0.75
            },
            Metadata = new Dictionary<string, object> { ["analysis_type"] = "trading_signal_validation" }
        };

        var aiAnalysis = await aiService.AnalyzePatternAsync(aiAnalysisRequest, BlockchainType.NeoX);
        aiAnalysis.Success.Should().BeTrue();

        // 7. Store trading session data
        _logger.LogInformation("💾 Step 6: Storing trading session...");
        
        var tradingSession = new
        {
            BotId = botAccount.AccountId,
            SessionId = Guid.NewGuid().ToString(),
            MarketData = marketData,
            TradingParams = tradingParams.RandomValues,
            TradingSignals = tradingResult.Result,
            AIAnalysis = aiAnalysis.DetectedPatterns,
            Timestamp = DateTime.UtcNow,
            Performance = new
            {
                SignalsGenerated = JsonSerializer.Deserialize<JsonElement>(tradingResult.Result).GetProperty("totalSignals").GetInt32(),
                AIConfidence = aiAnalysis.DetectedPatterns.FirstOrDefault()?.Confidence ?? 0.0,
                RiskScore = 0.3
            }
        };

        var storageRequest = new StorageRequest
        {
            Key = $"trading_session_{tradingSession.SessionId}",
            Data = JsonSerializer.SerializeToUtf8Bytes(tradingSession),
            Metadata = new Dictionary<string, object> { ["type"] = "trading_session", ["bot_id"] = botAccount.AccountId }
        };

        var storageResult = await storageService.StoreAsync(storageRequest, BlockchainType.NeoX);
        storageResult.Success.Should().BeTrue();

        // 8. Execute trading transactions
        _logger.LogInformation("💰 Step 7: Executing trading transactions...");
        
        var tradingSignals = JsonSerializer.Deserialize<JsonElement>(tradingResult.Result);
        var signals = tradingSignals.GetProperty("signals").EnumerateArray().ToArray();
        var transactions = new List<ExecuteTransactionRequest>();

        foreach (var signal in signals.Take(3)) // Execute first 3 signals
        {
            var action = signal.GetProperty("action").GetString();
            if (action != "HOLD")
            {
                var transaction = new ExecuteTransactionRequest
                {
                    AccountId = botAccount.AccountId,
                    ToAddress = $"0x{signal.GetProperty("asset").GetString()}_exchange",
                    Value = signal.GetProperty("amount").GetInt32(),
                    Data = JsonSerializer.Serialize(new
                    {
                        action = action,
                        asset = signal.GetProperty("asset").GetString(),
                        price = signal.GetProperty("price").GetDouble(),
                        confidence = signal.GetProperty("confidence").GetDouble()
                    }),
                    GasLimit = 150000,
                    UseSessionKey = false,
                    Metadata = new Dictionary<string, object> { ["transaction_type"] = "trading_execution" }
                };
                
                transactions.Add(transaction);
            }
        }

        if (transactions.Any())
        {
            var batchRequest = new BatchTransactionRequest
            {
                AccountId = botAccount.AccountId,
                Transactions = transactions.ToArray(),
                StopOnFailure = false,
                Metadata = new Dictionary<string, object> { ["session_id"] = tradingSession.SessionId }
            };

            var batchResult = await accountService.ExecuteBatchTransactionAsync(batchRequest, BlockchainType.NeoX);
            batchResult.AllSuccessful.Should().BeTrue();
            
            _logger.LogInformation("✅ Executed {TransactionCount} trading transactions", transactions.Count);
        }

        // 9. Verify complete workflow
        _logger.LogInformation("🔍 Step 8: Verifying complete workflow...");
        
        botKey.KeyId.Should().NotBeNullOrEmpty();
        botAccount.AccountId.Should().NotBeNullOrEmpty();
        marketData.Should().HaveCount(4);
        tradingParams.RandomValues.Should().HaveCount(10);
        tradingResult.Result.Should().NotBeNull();
        aiAnalysis.DetectedPatterns.Should().NotBeEmpty();
        storageResult.StorageId.Should().NotBeNullOrEmpty();

        _logger.LogInformation("🎉 Decentralized Trading Bot workflow completed successfully!");
        _logger.LogInformation("📈 Generated {SignalCount} trading signals with {BuyCount} buy and {SellCount} sell orders",
            tradingSignals.GetProperty("totalSignals").GetInt32(),
            tradingSignals.GetProperty("buySignals").GetInt32(),
            tradingSignals.GetProperty("sellSignals").GetInt32());
    }

    [Fact]
    public async Task DecentralizedGamingPlatform_CompleteWorkflow()
    {
        _logger.LogInformation("🎮 Starting Decentralized Gaming Platform end-to-end scenario...");

        var services = new
        {
            Randomness = _serviceManager.GetService<IRandomnessService>(),
            Oracle = _serviceManager.GetService<IOracleService>(),
            KeyManagement = _serviceManager.GetService<IKeyManagementService>(),
            Compute = _serviceManager.GetService<IComputeService>(),
            Storage = _serviceManager.GetService<IStorageService>(),
            AI = _serviceManager.GetService<IPatternRecognitionService>(),
            Account = _serviceManager.GetService<IAbstractAccountService>()
        };

        // 1. Create game operator account
        _logger.LogInformation("🏗️ Step 1: Setting up gaming platform...");
        
        var operatorKey = await services.KeyManagement.GenerateKeyAsync(new KeyGenerationRequest
        {
            KeyType = KeyType.Secp256k1,
            Purpose = "game_operator",
            Metadata = new Dictionary<string, object> { ["platform"] = "neo_gaming" }
        }, BlockchainType.NeoX);

        var operatorAccount = await services.Account.CreateAccountAsync(new CreateAccountRequest
        {
            OwnerPublicKey = operatorKey.PublicKey,
            InitialGuardians = new[] { "0xplatform_admin" },
            RecoveryThreshold = 1,
            EnableGaslessTransactions = true,
            Metadata = new Dictionary<string, object> { ["role"] = "game_operator" }
        }, BlockchainType.NeoX);

        // 2. Create multiple player accounts
        _logger.LogInformation("👥 Step 2: Creating player accounts...");
        
        var players = new List<(string KeyId, AbstractAccountResult Account)>();
        for (int i = 1; i <= 5; i++)
        {
            var playerKey = await services.KeyManagement.GenerateKeyAsync(new KeyGenerationRequest
            {
                KeyType = KeyType.Secp256k1,
                Purpose = $"player_{i}",
                Metadata = new Dictionary<string, object> { ["player_id"] = i }
            }, BlockchainType.NeoX);

            var playerAccount = await services.Account.CreateAccountAsync(new CreateAccountRequest
            {
                OwnerPublicKey = playerKey.PublicKey,
                InitialGuardians = new[] { operatorAccount.AccountId },
                RecoveryThreshold = 1,
                EnableGaslessTransactions = true,
                Metadata = new Dictionary<string, object> { ["role"] = "player", ["level"] = 1 }
            }, BlockchainType.NeoX);

            players.Add((playerKey.KeyId, playerAccount));
        }

        // 3. Generate random game events
        _logger.LogInformation("🎲 Step 3: Generating game events...");
        
        var gameEvents = new Dictionary<string, int[]>();
        var eventTypes = new[] { "dice_roll", "card_draw", "loot_drop", "boss_encounter", "treasure_find" };
        
        foreach (var eventType in eventTypes)
        {
            var eventRequest = new RandomnessRequest
            {
                MinValue = eventType == "dice_roll" ? 1 : eventType == "card_draw" ? 1 : 1,
                MaxValue = eventType == "dice_roll" ? 7 : eventType == "card_draw" ? 53 : 101,
                Count = players.Count,
                Metadata = new Dictionary<string, object> { ["event_type"] = eventType }
            };
            
            var eventResult = await services.Randomness.GenerateRandomAsync(eventRequest, BlockchainType.NeoX);
            gameEvents[eventType] = eventResult.RandomValues;
        }

        // 4. Execute game logic for each player
        _logger.LogInformation("⚙️ Step 4: Processing game logic...");
        
        var gameLogic = @"
            function processGameSession(playerEvents, playerLevel) {
                let score = 0;
                let experience = 0;
                let achievements = [];
                
                // Process dice roll
                const diceRoll = playerEvents.dice_roll;
                score += diceRoll * 10;
                if (diceRoll === 6) achievements.push('LUCKY_ROLL');
                
                // Process card draw
                const cardDraw = playerEvents.card_draw;
                if (cardDraw > 40) {
                    score += 50;
                    achievements.push('HIGH_CARD');
                }
                
                // Process loot drop
                const lootDrop = playerEvents.loot_drop;
                if (lootDrop > 80) {
                    score += 100;
                    achievements.push('RARE_LOOT');
                }
                
                // Calculate experience
                experience = Math.floor(score / 10) + playerLevel * 5;
                
                // Determine new level
                const newLevel = Math.floor(experience / 100) + 1;
                
                return {
                    score: score,
                    experience: experience,
                    level: newLevel,
                    achievements: achievements,
                    events: playerEvents,
                    levelUp: newLevel > playerLevel
                };
            }
            
            processGameSession(input.playerEvents, input.playerLevel);
        ";

        var playerResults = new List<object>();
        for (int i = 0; i < players.Count; i++)
        {
            var playerEvents = new Dictionary<string, int>();
            foreach (var eventType in eventTypes)
            {
                playerEvents[eventType] = gameEvents[eventType][i];
            }

            var computeRequest = new ComputeRequest
            {
                FunctionCode = gameLogic,
                InputData = new Dictionary<string, object>
                {
                    ["playerEvents"] = playerEvents,
                    ["playerLevel"] = 1
                },
                TimeoutMs = 5000,
                Metadata = new Dictionary<string, object> { ["player_id"] = players[i].Account.AccountId }
            };

            var gameResult = await services.Compute.ExecuteAsync(computeRequest, BlockchainType.NeoX);
            gameResult.Success.Should().BeTrue();
            
            playerResults.Add(new
            {
                PlayerId = players[i].Account.AccountId,
                GameResult = gameResult.Result,
                Events = playerEvents
            });
        }

        // 5. AI analysis for anti-cheat detection
        _logger.LogInformation("🛡️ Step 5: AI anti-cheat analysis...");
        
        var antiCheatRequest = new PatternAnalysisRequest
        {
            ModelId = "anti_cheat_detector",
            InputData = new Dictionary<string, object>
            {
                ["player_results"] = playerResults,
                ["game_events"] = gameEvents,
                ["session_duration"] = 300
            },
            Metadata = new Dictionary<string, object> { ["analysis_type"] = "anti_cheat" }
        };

        var antiCheatAnalysis = await services.AI.AnalyzePatternAsync(antiCheatRequest, BlockchainType.NeoX);
        antiCheatAnalysis.Success.Should().BeTrue();

        // 6. Store game session data
        _logger.LogInformation("💾 Step 6: Storing game session...");
        
        var gameSession = new
        {
            SessionId = Guid.NewGuid().ToString(),
            OperatorId = operatorAccount.AccountId,
            Players = playerResults,
            GameEvents = gameEvents,
            AntiCheatAnalysis = antiCheatAnalysis.DetectedPatterns,
            Timestamp = DateTime.UtcNow,
            GameType = "adventure_rpg"
        };

        var sessionStorage = await services.Storage.StoreAsync(new StorageRequest
        {
            Key = $"game_session_{gameSession.SessionId}",
            Data = JsonSerializer.SerializeToUtf8Bytes(gameSession),
            Metadata = new Dictionary<string, object> { ["type"] = "game_session" }
        }, BlockchainType.NeoX);

        // 7. Execute reward transactions
        _logger.LogInformation("🏆 Step 7: Distributing rewards...");
        
        var rewardTransactions = new List<ExecuteTransactionRequest>();
        foreach (var playerResult in playerResults)
        {
            var playerId = ((JsonElement)playerResult).GetProperty("PlayerId").GetString();
            var gameResultJson = JsonSerializer.Deserialize<JsonElement>(
                ((JsonElement)playerResult).GetProperty("GameResult").GetString()!);
            
            var score = gameResultJson.GetProperty("score").GetInt32();
            var achievements = gameResultJson.GetProperty("achievements").EnumerateArray().ToArray();
            
            if (score > 100) // Reward high-scoring players
            {
                var rewardTransaction = new ExecuteTransactionRequest
                {
                    AccountId = playerId!,
                    ToAddress = "0xgame_rewards_contract",
                    Value = score,
                    Data = JsonSerializer.Serialize(new
                    {
                        reward_type = "score_bonus",
                        score = score,
                        achievements = achievements.Select(a => a.GetString()).ToArray()
                    }),
                    GasLimit = 100000,
                    UseSessionKey = false,
                    Metadata = new Dictionary<string, object> { ["transaction_type"] = "reward_distribution" }
                };
                
                rewardTransactions.Add(rewardTransaction);
            }
        }

        foreach (var transaction in rewardTransactions)
        {
            var result = await services.Account.ExecuteTransactionAsync(transaction, BlockchainType.NeoX);
            result.Success.Should().BeTrue();
        }

        // 8. Verify complete gaming workflow
        _logger.LogInformation("🔍 Step 8: Verifying gaming platform workflow...");
        
        operatorAccount.Success.Should().BeTrue();
        players.Should().HaveCount(5);
        players.Should().OnlyContain(p => p.Account.Success);
        gameEvents.Should().HaveCount(5);
        playerResults.Should().HaveCount(5);
        antiCheatAnalysis.DetectedPatterns.Should().NotBeEmpty();
        sessionStorage.Success.Should().BeTrue();

        _logger.LogInformation("🎉 Decentralized Gaming Platform workflow completed successfully!");
        _logger.LogInformation("🎮 Processed {PlayerCount} players with {EventCount} event types and {RewardCount} rewards distributed",
            players.Count, gameEvents.Count, rewardTransactions.Count);
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
