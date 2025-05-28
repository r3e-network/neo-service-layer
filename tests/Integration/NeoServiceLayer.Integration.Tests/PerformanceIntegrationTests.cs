using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;
using NeoServiceLayer.Services.Randomness;
using NeoServiceLayer.Services.Oracle;
using NeoServiceLayer.Services.AbstractAccount;
using NeoServiceLayer.Services.Storage;
using NeoServiceLayer.Tee.Host.Services;
using NeoServiceLayer.Tee.Host.Tests;
using System.Diagnostics;
using System.Text.Json;

namespace NeoServiceLayer.Integration.Tests;

/// <summary>
/// Performance integration tests that validate system behavior under load.
/// </summary>
public class PerformanceIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IRandomnessService _randomnessService;
    private readonly IOracleService _oracleService;
    private readonly IAbstractAccountService _abstractAccountService;
    private readonly IStorageService _storageService;
    private readonly ILogger<PerformanceIntegrationTests> _logger;

    public PerformanceIntegrationTests()
    {
        var services = new ServiceCollection();
        
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        services.AddSingleton<IEnclaveWrapper, TestEnclaveWrapper>();
        services.AddSingleton<IEnclaveManager, EnclaveManager>();
        services.AddSingleton<IRandomnessService, RandomnessService>();
        services.AddSingleton<IOracleService, OracleService>();
        services.AddSingleton<IAbstractAccountService, AbstractAccountService>();
        services.AddSingleton<IStorageService, StorageService>();
        
        _serviceProvider = services.BuildServiceProvider();
        
        _randomnessService = _serviceProvider.GetRequiredService<IRandomnessService>();
        _oracleService = _serviceProvider.GetRequiredService<IOracleService>();
        _abstractAccountService = _serviceProvider.GetRequiredService<IAbstractAccountService>();
        _storageService = _serviceProvider.GetRequiredService<IStorageService>();
        _logger = _serviceProvider.GetRequiredService<ILogger<PerformanceIntegrationTests>>();
        
        InitializeServicesAsync().GetAwaiter().GetResult();
    }

    private async Task InitializeServicesAsync()
    {
        await _randomnessService.InitializeAsync();
        await _oracleService.InitializeAsync();
        await _abstractAccountService.InitializeAsync();
        await _storageService.InitializeAsync();
    }

    [Fact]
    public async Task HighVolumeRandomnessGeneration_ShouldMaintainPerformance()
    {
        _logger.LogInformation("Testing high-volume randomness generation performance...");

        const int requestCount = 100;
        const int numbersPerRequest = 10;
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task<RandomnessResult>>();

        // Generate multiple concurrent randomness requests
        for (int i = 0; i < requestCount; i++)
        {
            var request = new RandomnessRequest
            {
                MinValue = 1,
                MaxValue = 1000000,
                Count = numbersPerRequest,
                Metadata = new Dictionary<string, object> { ["batch_id"] = i }
            };

            tasks.Add(_randomnessService.GenerateRandomAsync(request, BlockchainType.NeoX));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Verify all requests succeeded
        results.Should().OnlyContain(r => r.Success);
        results.Should().OnlyContain(r => r.RandomValues.Length == numbersPerRequest);

        // Performance assertions
        var totalNumbers = requestCount * numbersPerRequest;
        var numbersPerSecond = totalNumbers / stopwatch.Elapsed.TotalSeconds;
        
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(30));
        numbersPerSecond.Should().BeGreaterThan(100); // At least 100 numbers per second

        _logger.LogInformation("Generated {TotalNumbers} random numbers in {ElapsedMs}ms ({NumbersPerSec:F1} numbers/sec)",
            totalNumbers, stopwatch.ElapsedMilliseconds, numbersPerSecond);
    }

    [Fact]
    public async Task ConcurrentAccountCreation_ShouldHandleLoad()
    {
        _logger.LogInformation("Testing concurrent account creation performance...");

        const int accountCount = 50;
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task<AbstractAccountResult>>();

        // Create multiple accounts concurrently
        for (int i = 0; i < accountCount; i++)
        {
            var request = new CreateAccountRequest
            {
                OwnerPublicKey = $"0xowner_{i:D4}",
                InitialGuardians = new[] { $"0xguardian1_{i:D4}", $"0xguardian2_{i:D4}" },
                RecoveryThreshold = 2,
                EnableGaslessTransactions = true,
                Metadata = new Dictionary<string, object> { ["batch_id"] = i }
            };

            tasks.Add(_abstractAccountService.CreateAccountAsync(request, BlockchainType.NeoX));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Verify all accounts created successfully
        results.Should().OnlyContain(r => r.Success);
        results.Should().OnlyContain(r => !string.IsNullOrEmpty(r.AccountId));
        results.Should().OnlyContain(r => !string.IsNullOrEmpty(r.AccountAddress));

        // Performance assertions
        var accountsPerSecond = accountCount / stopwatch.Elapsed.TotalSeconds;
        
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(60));
        accountsPerSecond.Should().BeGreaterThan(1); // At least 1 account per second

        _logger.LogInformation("Created {AccountCount} accounts in {ElapsedMs}ms ({AccountsPerSec:F1} accounts/sec)",
            accountCount, stopwatch.ElapsedMilliseconds, accountsPerSecond);
    }

    [Fact]
    public async Task HighVolumeDataStorage_ShouldMaintainThroughput()
    {
        _logger.LogInformation("Testing high-volume data storage performance...");

        const int storageOperations = 200;
        const int dataSize = 1024; // 1KB per operation
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task<StorageResult>>();

        // Generate test data
        var testData = new byte[dataSize];
        Random.Shared.NextBytes(testData);

        // Perform multiple concurrent storage operations
        for (int i = 0; i < storageOperations; i++)
        {
            var request = new StorageRequest
            {
                Key = $"performance_test_{i:D6}",
                Data = testData,
                Metadata = new Dictionary<string, object> 
                { 
                    ["operation_id"] = i,
                    ["data_size"] = dataSize
                }
            };

            tasks.Add(_storageService.StoreAsync(request, BlockchainType.NeoX));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Verify all storage operations succeeded
        results.Should().OnlyContain(r => r.Success);
        results.Should().OnlyContain(r => !string.IsNullOrEmpty(r.StorageId));

        // Performance assertions
        var totalDataMB = (storageOperations * dataSize) / (1024.0 * 1024.0);
        var throughputMBps = totalDataMB / stopwatch.Elapsed.TotalSeconds;
        
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(120));
        throughputMBps.Should().BeGreaterThan(0.1); // At least 0.1 MB/s

        _logger.LogInformation("Stored {TotalDataMB:F2} MB in {ElapsedMs}ms ({ThroughputMBps:F2} MB/s)",
            totalDataMB, stopwatch.ElapsedMilliseconds, throughputMBps);
    }

    [Fact]
    public async Task MixedWorkload_ShouldHandleConcurrentOperations()
    {
        _logger.LogInformation("Testing mixed workload performance...");

        const int operationsPerType = 20;
        var stopwatch = Stopwatch.StartNew();
        var allTasks = new List<Task>();

        // Randomness generation tasks
        var randomnessTasks = new List<Task<RandomnessResult>>();
        for (int i = 0; i < operationsPerType; i++)
        {
            var request = new RandomnessRequest
            {
                MinValue = 1,
                MaxValue = 100,
                Count = 5,
                Metadata = new Dictionary<string, object> { ["workload"] = "mixed", ["type"] = "randomness" }
            };
            randomnessTasks.Add(_randomnessService.GenerateRandomAsync(request, BlockchainType.NeoX));
        }
        allTasks.AddRange(randomnessTasks);

        // Oracle data requests
        var oracleTasks = new List<Task<OracleDataResult>>();
        for (int i = 0; i < operationsPerType; i++)
        {
            var request = new OracleDataRequest
            {
                DataSource = "coinmarketcap",
                DataPath = $"asset_{i % 5}/price",
                Parameters = new Dictionary<string, object> { ["currency"] = "USD" },
                Metadata = new Dictionary<string, object> { ["workload"] = "mixed", ["type"] = "oracle" }
            };
            oracleTasks.Add(_oracleService.GetDataAsync(request, BlockchainType.NeoX));
        }
        allTasks.AddRange(oracleTasks);

        // Account creation tasks
        var accountTasks = new List<Task<AbstractAccountResult>>();
        for (int i = 0; i < operationsPerType; i++)
        {
            var request = new CreateAccountRequest
            {
                OwnerPublicKey = $"0xmixed_owner_{i:D4}",
                InitialGuardians = new[] { $"0xmixed_guardian_{i:D4}" },
                RecoveryThreshold = 1,
                EnableGaslessTransactions = true,
                Metadata = new Dictionary<string, object> { ["workload"] = "mixed", ["type"] = "account" }
            };
            accountTasks.Add(_abstractAccountService.CreateAccountAsync(request, BlockchainType.NeoX));
        }
        allTasks.AddRange(accountTasks);

        // Storage operations
        var storageTasks = new List<Task<StorageResult>>();
        for (int i = 0; i < operationsPerType; i++)
        {
            var testData = JsonSerializer.SerializeToUtf8Bytes(new { id = i, data = $"mixed_workload_{i}" });
            var request = new StorageRequest
            {
                Key = $"mixed_workload_{i:D4}",
                Data = testData,
                Metadata = new Dictionary<string, object> { ["workload"] = "mixed", ["type"] = "storage" }
            };
            storageTasks.Add(_storageService.StoreAsync(request, BlockchainType.NeoX));
        }
        allTasks.AddRange(storageTasks);

        // Wait for all operations to complete
        await Task.WhenAll(allTasks);
        stopwatch.Stop();

        // Verify all operations succeeded
        var randomnessResults = await Task.WhenAll(randomnessTasks);
        var oracleResults = await Task.WhenAll(oracleTasks);
        var accountResults = await Task.WhenAll(accountTasks);
        var storageResults = await Task.WhenAll(storageTasks);

        randomnessResults.Should().OnlyContain(r => r.Success);
        oracleResults.Should().OnlyContain(r => r.Success);
        accountResults.Should().OnlyContain(r => r.Success);
        storageResults.Should().OnlyContain(r => r.Success);

        // Performance assertions
        var totalOperations = operationsPerType * 4;
        var operationsPerSecond = totalOperations / stopwatch.Elapsed.TotalSeconds;
        
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMinutes(5));
        operationsPerSecond.Should().BeGreaterThan(1); // At least 1 operation per second

        _logger.LogInformation("Completed {TotalOperations} mixed operations in {ElapsedMs}ms ({OpsPerSec:F1} ops/sec)",
            totalOperations, stopwatch.ElapsedMilliseconds, operationsPerSecond);
    }

    [Fact]
    public async Task BatchTransactionPerformance_ShouldOptimizeGasUsage()
    {
        _logger.LogInformation("Testing batch transaction performance...");

        // Create account for batch transactions
        var accountResult = await _abstractAccountService.CreateAccountAsync(new CreateAccountRequest
        {
            OwnerPublicKey = "0xbatch_test_owner",
            InitialGuardians = new[] { "0xbatch_guardian" },
            RecoveryThreshold = 1,
            EnableGaslessTransactions = true,
            Metadata = new Dictionary<string, object> { ["purpose"] = "batch_testing" }
        }, BlockchainType.NeoX);

        accountResult.Success.Should().BeTrue();

        const int transactionCount = 50;
        var transactions = new List<ExecuteTransactionRequest>();

        // Create batch of transactions
        for (int i = 0; i < transactionCount; i++)
        {
            transactions.Add(new ExecuteTransactionRequest
            {
                AccountId = accountResult.AccountId,
                ToAddress = $"0xcontract_{i % 5:D2}",
                Value = i * 100,
                Data = JsonSerializer.Serialize(new { operation = "batch_test", index = i }),
                GasLimit = 50000,
                UseSessionKey = false,
                Metadata = new Dictionary<string, object> { ["batch_index"] = i }
            });
        }

        var stopwatch = Stopwatch.StartNew();

        // Execute batch transaction
        var batchRequest = new BatchTransactionRequest
        {
            AccountId = accountResult.AccountId,
            Transactions = transactions.ToArray(),
            StopOnFailure = false,
            Metadata = new Dictionary<string, object> { ["test_type"] = "performance" }
        };

        var batchResult = await _abstractAccountService.ExecuteBatchTransactionAsync(batchRequest, BlockchainType.NeoX);
        stopwatch.Stop();

        // Verify batch execution
        batchResult.AllSuccessful.Should().BeTrue();
        batchResult.Results.Should().HaveCount(transactionCount);
        batchResult.Results.Should().OnlyContain(r => r.Success);

        // Performance assertions
        var transactionsPerSecond = transactionCount / stopwatch.Elapsed.TotalSeconds;
        
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(60));
        transactionsPerSecond.Should().BeGreaterThan(1); // At least 1 transaction per second
        batchResult.TotalGasUsed.Should().BeGreaterThan(0);

        _logger.LogInformation("Executed {TransactionCount} batch transactions in {ElapsedMs}ms ({TxPerSec:F1} tx/sec, {TotalGas} gas)",
            transactionCount, stopwatch.ElapsedMilliseconds, transactionsPerSecond, batchResult.TotalGasUsed);
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
