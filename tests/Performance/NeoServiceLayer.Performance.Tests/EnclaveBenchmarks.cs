using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure;
using NeoServiceLayer.Infrastructure.Persistence;
using NeoServiceLayer.Tee.Enclave;
using Xunit;

namespace NeoServiceLayer.Performance.Tests;

/// <summary>
/// Comprehensive BenchmarkDotNet micro-benchmarks for enclave operations.
/// Provides precise performance measurements for individual operations.
/// </summary>
[MemoryDiagnoser]
[ThreadingDiagnoser]
[ExceptionDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
[RankColumn]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class EnclaveBenchmarks
{
    private ServiceProvider _serviceProvider = null!;
    private IEnclaveWrapper _enclaveWrapper = null!;
    private BenchmarkConfiguration _benchmarkConfig = null!;

    // Test data for different scenarios
    private byte[] _smallData = null!;     // 256 bytes
    private byte[] _mediumData = null!;    // 4 KB
    private byte[] _largeData = null!;     // 64 KB
    private byte[] _xlargeData = null!;    // 1 MB

    // Pre-sealed data for unsealing benchmarks
    private byte[] _encryptedSmallData = null!;
    private byte[] _encryptedMediumData = null!;
    private byte[] _encryptedLargeData = null!;
    private byte[] _encryptionKey = null!;

    // Crypto test data
    private byte[] _hashData = null!;
    private byte[] _signatureResult = null!;
    private byte[] _signingKey = null!;

    // JavaScript test scripts
    private string _simpleScript = null!;
    private string _mathScript = null!;
    private string _complexScript = null!;

    [Params(256, 1024, 4096, 16384, 65536, 262144)]
    public int DataSize { get; set; }

    [Params("AES-256-GCM")]
    public string EncryptionAlgorithm { get; set; } = "AES-256-GCM";

    [Params("secp256k1")]
    public string SigningAlgorithm { get; set; } = "secp256k1";

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        // Load benchmark configuration
        LoadBenchmarkConfiguration();

        // Set up test environment
        await SetupTestEnvironment();

        // Initialize test data
        InitializeTestData();

        // Pre-create sealed data for unsealing benchmarks
        await PrepareSealedData();

        // Prepare crypto test data
        await PrepareCryptoData();

        // Initialize JavaScript test scripts
        InitializeJavaScriptScripts();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _serviceProvider?.Dispose();
    }

    #region Enclave Initialization Benchmarks

    [Benchmark]
    [BenchmarkCategory("Initialization")]
    public async Task<bool> BenchmarkEnclaveInitialization()
    {
        // Create a new enclave wrapper for initialization testing
        using var scope = _serviceProvider.CreateScope();
        var newEnclaveWrapper = scope.ServiceProvider.GetRequiredService<IEnclaveWrapper>();

        var config = new EnclaveConfig
        {
            SgxMode = "SIM",
            DebugMode = false, // Release mode for benchmarking
            MaxThreads = 4,
            MemoryLimitMb = 256
        };

        return newEnclaveWrapper.Initialize();
    }

    #endregion

    #region Data Sealing Benchmarks

    [Benchmark]
    [BenchmarkCategory("Encryption")]
    public async Task<byte[]> BenchmarkDataEncryption()
    {
        var testData = GenerateTestData(DataSize);

        return await Task.Run(() => _enclaveWrapper.Encrypt(testData, _encryptionKey));
    }

    [Benchmark]
    [BenchmarkCategory("Encryption")]
    public async Task<byte[]> BenchmarkDataEncryption_256B()
    {
        return await Task.Run(() => _enclaveWrapper.Encrypt(_smallData, _encryptionKey));
    }

    [Benchmark]
    [BenchmarkCategory("Encryption")]
    public async Task<byte[]> BenchmarkDataEncryption_4KB()
    {
        return await Task.Run(() => _enclaveWrapper.Encrypt(_mediumData, _encryptionKey));
    }

    [Benchmark]
    [BenchmarkCategory("Encryption")]
    public async Task<byte[]> BenchmarkDataEncryption_64KB()
    {
        return await Task.Run(() => _enclaveWrapper.Encrypt(_largeData, _encryptionKey));
    }

    [Benchmark]
    [BenchmarkCategory("Encryption")]
    public async Task<byte[]> BenchmarkDataEncryption_1MB()
    {
        return await Task.Run(() => _enclaveWrapper.Encrypt(_xlargeData, _encryptionKey));
    }

    #endregion

    #region Data Decryption Benchmarks

    [Benchmark]
    [BenchmarkCategory("Decryption")]
    public async Task<byte[]> BenchmarkDataDecryption_256B()
    {
        return await Task.Run(() => _enclaveWrapper.Decrypt(_encryptedSmallData, _encryptionKey));
    }

    [Benchmark]
    [BenchmarkCategory("Decryption")]
    public async Task<byte[]> BenchmarkDataDecryption_4KB()
    {
        return await Task.Run(() => _enclaveWrapper.Decrypt(_encryptedMediumData, _encryptionKey));
    }

    [Benchmark]
    [BenchmarkCategory("Decryption")]
    public async Task<byte[]> BenchmarkDataDecryption_64KB()
    {
        return await Task.Run(() => _enclaveWrapper.Decrypt(_encryptedLargeData, _encryptionKey));
    }

    #endregion

    #region Cryptography Benchmarks

    [Benchmark]
    [BenchmarkCategory("Cryptography")]
    public async Task<byte[]> BenchmarkSignatureGeneration()
    {
        return await Task.Run(() => _enclaveWrapper.Sign(_hashData, _signingKey));
    }

    [Benchmark]
    [BenchmarkCategory("Cryptography")]
    public async Task<bool> BenchmarkSignatureVerification()
    {
        return await Task.Run(() => _enclaveWrapper.Verify(
            _hashData,
            _signatureResult,
            _signingKey));
    }

    [Benchmark]
    [BenchmarkCategory("Cryptography")]
    public async Task<byte[]> BenchmarkEncryption_256B()
    {
        return await Task.Run(() => _enclaveWrapper.Encrypt(_smallData, _encryptionKey));
    }

    [Benchmark]
    [BenchmarkCategory("Cryptography")]
    public async Task<byte[]> BenchmarkEncryption_4KB()
    {
        return await Task.Run(() => _enclaveWrapper.Encrypt(_mediumData, _encryptionKey));
    }

    [Benchmark]
    [BenchmarkCategory("Cryptography")]
    public async Task<byte[]> BenchmarkEncryption_64KB()
    {
        return await Task.Run(() => _enclaveWrapper.Encrypt(_largeData, _encryptionKey));
    }

    [Benchmark]
    [BenchmarkCategory("Cryptography")]
    public async Task<byte[]> BenchmarkRandomGeneration_256B()
    {
        return await Task.Run(() => _enclaveWrapper.GenerateRandomBytes(256));
    }

    [Benchmark]
    [BenchmarkCategory("Cryptography")]
    public async Task<byte[]> BenchmarkRandomGeneration_4KB()
    {
        return await Task.Run(() => _enclaveWrapper.GenerateRandomBytes(4096));
    }

    [Benchmark]
    [BenchmarkCategory("Cryptography")]
    public async Task<byte[]> BenchmarkRandomGeneration_64KB()
    {
        return await Task.Run(() => _enclaveWrapper.GenerateRandomBytes(65536));
    }

    #endregion

    #region JavaScript Execution Benchmarks

    [Benchmark]
    [BenchmarkCategory("JavaScript")]
    public async Task<string> BenchmarkJavaScript_Simple()
    {
        var input = JsonSerializer.Serialize(new { value = 42 });
        return await Task.Run(() => _enclaveWrapper.ExecuteJavaScript(_simpleScript, input));
    }

    [Benchmark]
    [BenchmarkCategory("JavaScript")]
    public async Task<string> BenchmarkJavaScript_Mathematical()
    {
        var input = JsonSerializer.Serialize(new { iterations = 1000 });
        return await Task.Run(() => _enclaveWrapper.ExecuteJavaScript(_mathScript, input));
    }

    [Benchmark]
    [BenchmarkCategory("JavaScript")]
    public async Task<string> BenchmarkJavaScript_Complex()
    {
        var input = JsonSerializer.Serialize(new { n = 25, data = Enumerable.Range(1, 1000).ToArray() });
        return await Task.Run(() => _enclaveWrapper.ExecuteJavaScript(_complexScript, input));
    }

    #endregion

    #region Key Management Benchmarks

    [Benchmark]
    [BenchmarkCategory("KeyManagement")]
    public async Task<string> BenchmarkKeyGeneration()
    {
        var keyId = $"key-{Guid.NewGuid()}";
        return await Task.Run(() => _enclaveWrapper.GenerateKey(keyId, "Secp256k1", "Sign,Verify", false, "Test key"));
    }

    #endregion

    #region Oracle Benchmarks

    [Benchmark]
    [BenchmarkCategory("Oracle")]
    public async Task<string> BenchmarkOracleDataFetch()
    {
        var url = "https://api.example.com/data";
        return await Task.Run(() => _enclaveWrapper.FetchOracleData(url));
    }

    #endregion

    #region Blockchain Operations Benchmarks

    [Benchmark]
    [BenchmarkCategory("Blockchain")]
    public async Task<string> BenchmarkAbstractAccountTransaction()
    {
        var accountId = "test-account-123";
        var transactionData = JsonSerializer.Serialize(new { to = "0x1234", value = "1000", data = "0x" });
        return await Task.Run(() => _enclaveWrapper.SignAbstractAccountTransaction(accountId, transactionData));
    }

    #endregion

    #region Helper Methods

    private void LoadBenchmarkConfiguration()
    {
        var configJson = File.ReadAllText("benchmark-config.json");
        var configRoot = JsonSerializer.Deserialize<JsonElement>(configJson);
        _benchmarkConfig = JsonSerializer.Deserialize<BenchmarkConfiguration>(
            configRoot.GetProperty("BenchmarkConfiguration").GetRawText())!;
    }

    private async Task SetupTestEnvironment()
    {
        // Set environment variables for optimal benchmarking
        Environment.SetEnvironmentVariable("SGX_MODE", "SIM");
        Environment.SetEnvironmentVariable("SGX_DEBUG", "0"); // Release mode
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Benchmark");

        // Set up service collection with minimal logging for performance
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tee:EnclaveType"] = "SGX",
                ["Tee:EnableRemoteAttestation"] = "false",
                ["Enclave:SGXMode"] = "SIM",
                ["Enclave:EnableDebug"] = "false",
                ["Enclave:Cryptography:EncryptionAlgorithm"] = EncryptionAlgorithm,
                ["Enclave:Cryptography:SigningAlgorithm"] = SigningAlgorithm,
                ["Enclave:Performance:EnableMetrics"] = "false",
                ["Enclave:Performance:EnableBenchmarking"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Critical)); // Minimal logging
        services.AddSingleton<IConfiguration>(configuration);
        services.AddNeoServiceLayer(configuration);

        _serviceProvider = services.BuildServiceProvider();
        _enclaveWrapper = _serviceProvider.GetRequiredService<IEnclaveWrapper>();

        // Initialize enclave once for all benchmarks
        var config = new EnclaveConfig
        {
            SgxMode = "SIM",
            DebugMode = false
        };

        var initResult = _enclaveWrapper.Initialize();
        if (!initResult)
        {
            throw new InvalidOperationException("Failed to initialize enclave for benchmarking");
        }
    }

    private void InitializeTestData()
    {
        _smallData = GenerateTestData(256);
        _mediumData = GenerateTestData(4096);
        _largeData = GenerateTestData(65536);
        _xlargeData = GenerateTestData(1048576);
        _hashData = GenerateTestData(256);
    }

    private async Task PrepareSealedData()
    {
        _encryptionKey = await Task.Run(() => _enclaveWrapper.GenerateRandomBytes(32)); // 256-bit key
        _encryptedSmallData = await Task.Run(() => _enclaveWrapper.Encrypt(_smallData, _encryptionKey));
        _encryptedMediumData = await Task.Run(() => _enclaveWrapper.Encrypt(_mediumData, _encryptionKey));
        _encryptedLargeData = await Task.Run(() => _enclaveWrapper.Encrypt(_largeData, _encryptionKey));
    }

    private async Task PrepareCryptoData()
    {
        _signingKey = await Task.Run(() => _enclaveWrapper.GenerateRandomBytes(32));
        _signatureResult = await Task.Run(() => _enclaveWrapper.Sign(_hashData, _signingKey));
    }

    private void InitializeJavaScriptScripts()
    {
        _simpleScript = "function simple(input) { return input.value * 2; } simple(input);";

        _mathScript = @"
function mathematical(input) { 
    let result = 0;
    for(let i = 0; i < input.iterations; i++) {
        result += Math.sin(i) * Math.cos(i) + Math.sqrt(i);
    }
    return { result, timestamp: Date.now() };
} mathematical(input);";

        _complexScript = @"
function complex(input) {
    function fibonacci(n) {
        if (n <= 1) return n;
        return fibonacci(n-1) + fibonacci(n-2);
    }
    
    function processArray(arr) {
        return arr
            .map(x => x * 2)
            .filter(x => x % 3 === 0)
            .reduce((sum, x) => sum + x, 0);
    }
    
    return {
        fib: fibonacci(input.n),
        processed: processArray(input.data),
        stats: {
            dataLength: input.data.length,
            fibNumber: input.n,
            timestamp: Date.now()
        }
    };
} complex(input);";
    }

    private static byte[] GenerateTestData(int size)
    {
        var data = new byte[size];
        var random = new Random(42); // Fixed seed for reproducible benchmarks
        random.NextBytes(data);
        return data;
    }

    #endregion
}

/// <summary>
/// Benchmark configuration model.
/// </summary>
public class BenchmarkConfiguration
{
    public int WarmupCount { get; set; } = 3;
    public int IterationCount { get; set; } = 10;
    public int InvocationCount { get; set; } = 1000;
    public int UnrollFactor { get; set; } = 16;
    public double MaxRelativeError { get; set; } = 0.02;
    public string OutputDirectory { get; set; } = "./BenchmarkDotNet.Artifacts";
    public bool EnableMemoryDiagnoser { get; set; } = true;
    public bool EnableThreadingDiagnoser { get; set; } = true;
    public bool EnableExceptionDiagnoser { get; set; } = true;
}

/// <summary>
/// Program entry point for running benchmarks.
/// </summary>
public class BenchmarkProgram
{
    public static void RunBenchmarks(string[] args)
    {
        var config = ManualConfig.Create(DefaultConfig.Instance)
            .WithOptions(ConfigOptions.DisableOptimizationsValidator);

        BenchmarkRunner.Run<EnclaveBenchmarks>(config, args);
    }
}
