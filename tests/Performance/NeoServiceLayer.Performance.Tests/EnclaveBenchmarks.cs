using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Infrastructure;
using NeoServiceLayer.Tee.Enclave;
using System.Text;
using System.Text.Json;

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
    private SealedData _sealedSmallData = null!;
    private SealedData _sealedMediumData = null!;
    private SealedData _sealedLargeData = null!;
    
    // Crypto test data
    private byte[] _hashData = null!;
    private SignatureResult _signatureResult = null!;
    
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
    [Category("Initialization")]
    public async Task<bool> BenchmarkEnclaveInitialization()
    {
        // Create a new enclave wrapper for initialization testing
        using var scope = _serviceProvider.CreateScope();
        var newEnclaveWrapper = scope.ServiceProvider.GetRequiredService<IEnclaveWrapper>();
        
        var config = new EnclaveConfig
        {
            SGXMode = "SIM",
            EnableDebug = false, // Release mode for benchmarking
            Cryptography = new CryptographyConfig
            {
                EncryptionAlgorithm = EncryptionAlgorithm,
                SigningAlgorithm = SigningAlgorithm
            },
            Performance = new PerformanceConfig
            {
                EnableMetrics = false,
                EnableBenchmarking = true
            }
        };

        return await newEnclaveWrapper.InitializeAsync(config);
    }

    #endregion

    #region Data Sealing Benchmarks

    [Benchmark]
    [Category("Sealing")]
    public async Task<SealedData> BenchmarkDataSealing()
    {
        var testData = GenerateTestData(DataSize);
        var keyId = $"benchmark-key-{DataSize}";
        
        return await _enclaveWrapper.SealDataAsync(testData, keyId);
    }

    [Benchmark]
    [Category("Sealing")]
    public async Task<SealedData> BenchmarkDataSealing_256B()
    {
        return await _enclaveWrapper.SealDataAsync(_smallData, "benchmark-key-256");
    }

    [Benchmark]
    [Category("Sealing")]
    public async Task<SealedData> BenchmarkDataSealing_4KB()
    {
        return await _enclaveWrapper.SealDataAsync(_mediumData, "benchmark-key-4KB");
    }

    [Benchmark]
    [Category("Sealing")]
    public async Task<SealedData> BenchmarkDataSealing_64KB()
    {
        return await _enclaveWrapper.SealDataAsync(_largeData, "benchmark-key-64KB");
    }

    [Benchmark]
    [Category("Sealing")]
    public async Task<SealedData> BenchmarkDataSealing_1MB()
    {
        return await _enclaveWrapper.SealDataAsync(_xlargeData, "benchmark-key-1MB");
    }

    #endregion

    #region Data Unsealing Benchmarks

    [Benchmark]
    [Category("Unsealing")]
    public async Task<byte[]> BenchmarkDataUnsealing_256B()
    {
        return await _enclaveWrapper.UnsealDataAsync(_sealedSmallData, "benchmark-key-256");
    }

    [Benchmark]
    [Category("Unsealing")]
    public async Task<byte[]> BenchmarkDataUnsealing_4KB()
    {
        return await _enclaveWrapper.UnsealDataAsync(_sealedMediumData, "benchmark-key-4KB");
    }

    [Benchmark]
    [Category("Unsealing")]
    public async Task<byte[]> BenchmarkDataUnsealing_64KB()
    {
        return await _enclaveWrapper.UnsealDataAsync(_sealedLargeData, "benchmark-key-64KB");
    }

    #endregion

    #region Cryptography Benchmarks

    [Benchmark]
    [Category("Cryptography")]
    public async Task<SignatureResult> BenchmarkSignatureGeneration()
    {
        return await _enclaveWrapper.SignDataAsync(_hashData, "crypto-benchmark-key");
    }

    [Benchmark]
    [Category("Cryptography")]
    public async Task<bool> BenchmarkSignatureVerification()
    {
        return await _enclaveWrapper.VerifySignatureAsync(
            _hashData, 
            _signatureResult.Signature, 
            _signatureResult.PublicKey);
    }

    [Benchmark]
    [Category("Cryptography")]
    public async Task<EncryptionResult> BenchmarkEncryption_256B()
    {
        return await _enclaveWrapper.EncryptDataAsync(_smallData, "encryption-benchmark-key");
    }

    [Benchmark]
    [Category("Cryptography")]
    public async Task<EncryptionResult> BenchmarkEncryption_4KB()
    {
        return await _enclaveWrapper.EncryptDataAsync(_mediumData, "encryption-benchmark-key");
    }

    [Benchmark]
    [Category("Cryptography")]
    public async Task<EncryptionResult> BenchmarkEncryption_64KB()
    {
        return await _enclaveWrapper.EncryptDataAsync(_largeData, "encryption-benchmark-key");
    }

    [Benchmark]
    [Category("Cryptography")]
    public async Task<HashResult> BenchmarkHashGeneration_256B()
    {
        return await _enclaveWrapper.GenerateHashAsync(_smallData, "SHA-256");
    }

    [Benchmark]
    [Category("Cryptography")]
    public async Task<HashResult> BenchmarkHashGeneration_4KB()
    {
        return await _enclaveWrapper.GenerateHashAsync(_mediumData, "SHA-256");
    }

    [Benchmark]
    [Category("Cryptography")]
    public async Task<HashResult> BenchmarkHashGeneration_64KB()
    {
        return await _enclaveWrapper.GenerateHashAsync(_largeData, "SHA-256");
    }

    #endregion

    #region JavaScript Execution Benchmarks

    [Benchmark]
    [Category("JavaScript")]
    public async Task<JavaScriptExecutionResult> BenchmarkJavaScript_Simple()
    {
        var context = new JavaScriptContext
        {
            Input = new { value = 42 },
            Timeout = TimeSpan.FromSeconds(1),
            MemoryLimit = 16 * 1024 * 1024, // 16MB
            SecurityConstraints = new JavaScriptSecurityConstraints
            {
                DisallowNetworking = true,
                DisallowFileSystem = true,
                DisallowProcessExecution = true
            }
        };

        return await _enclaveWrapper.ExecuteJavaScriptAsync(_simpleScript, context);
    }

    [Benchmark]
    [Category("JavaScript")]
    public async Task<JavaScriptExecutionResult> BenchmarkJavaScript_Mathematical()
    {
        var context = new JavaScriptContext
        {
            Input = new { iterations = 1000 },
            Timeout = TimeSpan.FromSeconds(5),
            MemoryLimit = 32 * 1024 * 1024, // 32MB
            SecurityConstraints = new JavaScriptSecurityConstraints
            {
                DisallowNetworking = true,
                DisallowFileSystem = true,
                DisallowProcessExecution = true
            }
        };

        return await _enclaveWrapper.ExecuteJavaScriptAsync(_mathScript, context);
    }

    [Benchmark]
    [Category("JavaScript")]
    public async Task<JavaScriptExecutionResult> BenchmarkJavaScript_Complex()
    {
        var context = new JavaScriptContext
        {
            Input = new { n = 25, data = Enumerable.Range(1, 1000).ToArray() },
            Timeout = TimeSpan.FromSeconds(10),
            MemoryLimit = 64 * 1024 * 1024, // 64MB
            SecurityConstraints = new JavaScriptSecurityConstraints
            {
                DisallowNetworking = true,
                DisallowFileSystem = true,
                DisallowProcessExecution = true
            }
        };

        return await _enclaveWrapper.ExecuteJavaScriptAsync(_complexScript, context);
    }

    #endregion

    #region Storage Benchmarks

    [Benchmark]
    [Category("Storage")]
    public async Task<bool> BenchmarkSecureStorage_Store()
    {
        var storageKey = $"storage-benchmark-{DataSize}";
        var testData = GenerateTestData(DataSize);
        
        return await _enclaveWrapper.StoreSecureDataAsync(storageKey, testData);
    }

    [Benchmark]
    [Category("Storage")]
    public async Task<byte[]?> BenchmarkSecureStorage_Retrieve()
    {
        var storageKey = $"storage-benchmark-{DataSize}";
        return await _enclaveWrapper.RetrieveSecureDataAsync(storageKey);
    }

    #endregion

    #region Attestation Benchmarks

    [Benchmark]
    [Category("Attestation")]
    public async Task<AttestationResult> BenchmarkAttestationGeneration()
    {
        return await _enclaveWrapper.GetAttestationAsync();
    }

    [Benchmark]
    [Category("Attestation")]
    public async Task<bool> BenchmarkAttestationVerification()
    {
        var attestation = await _enclaveWrapper.GetAttestationAsync();
        return await _enclaveWrapper.VerifyAttestationAsync(attestation);
    }

    #endregion

    #region Network Benchmarks

    [Benchmark]
    [Category("Network")]
    public async Task<HttpResponseData> BenchmarkSecureHttpRequest_Small()
    {
        var request = new HttpRequestData
        {
            Url = "https://httpbin.org/json",
            Method = "GET",
            Timeout = TimeSpan.FromSeconds(30),
            MaxResponseSize = 1024 * 1024 // 1MB
        };

        return await _enclaveWrapper.MakeSecureHttpRequestAsync(request);
    }

    [Benchmark]
    [Category("Network")]
    public async Task<HttpResponseData> BenchmarkSecureHttpRequest_POST()
    {
        var requestData = JsonSerializer.Serialize(new { message = "benchmark test", size = DataSize });
        var request = new HttpRequestData
        {
            Url = "https://httpbin.org/post",
            Method = "POST",
            Body = Encoding.UTF8.GetBytes(requestData),
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
            Timeout = TimeSpan.FromSeconds(30),
            MaxResponseSize = 1024 * 1024 // 1MB
        };

        return await _enclaveWrapper.MakeSecureHttpRequestAsync(request);
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
            SGXMode = "SIM",
            EnableDebug = false,
            Cryptography = new CryptographyConfig
            {
                EncryptionAlgorithm = EncryptionAlgorithm,
                SigningAlgorithm = SigningAlgorithm
            },
            Performance = new PerformanceConfig
            {
                EnableMetrics = false,
                EnableBenchmarking = true
            }
        };

        var initResult = await _enclaveWrapper.InitializeAsync(config);
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
        _sealedSmallData = await _enclaveWrapper.SealDataAsync(_smallData, "benchmark-key-256");
        _sealedMediumData = await _enclaveWrapper.SealDataAsync(_mediumData, "benchmark-key-4KB");
        _sealedLargeData = await _enclaveWrapper.SealDataAsync(_largeData, "benchmark-key-64KB");
    }

    private async Task PrepareCryptoData()
    {
        _signatureResult = await _enclaveWrapper.SignDataAsync(_hashData, "crypto-benchmark-key");
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
    public static void Main(string[] args)
    {
        var config = ManualConfig.Create(DefaultConfig.Instance)
            .WithOptions(ConfigOptions.DisableOptimizationsValidator);

        BenchmarkRunner.Run<EnclaveBenchmarks>(config, args);
    }
} 