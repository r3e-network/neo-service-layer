using NBomber.CSharp;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using FluentAssertions;
using System.Text;
using System.Diagnostics;
using System.Text.Json;
using NeoServiceLayer.Infrastructure;
using NeoServiceLayer.Tee.Enclave;
using NeoServiceLayer.Tee.Enclave.Tests;

namespace NeoServiceLayer.Performance.Tests;

/// <summary>
/// Comprehensive NBomber load tests for enclave operations.
/// Tests various scenarios under different load patterns with performance monitoring.
/// </summary>
[TestFixture]
[Category("LoadTest")]
public class NBomberLoadTests
{
    private ServiceProvider _serviceProvider = null!;
    private IConfiguration _configuration = null!;
    private LoadTestConfiguration _loadTestConfig = null!;
    private PerformanceMonitor _performanceMonitor = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Load load test configuration
        var configJson = File.ReadAllText("load-test-config.json");
        var configRoot = JsonSerializer.Deserialize<JsonElement>(configJson);
        _loadTestConfig = JsonSerializer.Deserialize<LoadTestConfiguration>(
            configRoot.GetProperty("LoadTestConfiguration").GetRawText())!;

        // Set up test configuration
        var configurationBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tee:EnclaveType"] = "SGX",
                ["Tee:EnableRemoteAttestation"] = "false",
                ["Enclave:SGXMode"] = _loadTestConfig.SGXMode,
                ["Enclave:EnableDebug"] = _loadTestConfig.EnableDebug.ToString(),
                ["Enclave:Cryptography:EncryptionAlgorithm"] = "AES-256-GCM",
                ["Enclave:Cryptography:SigningAlgorithm"] = "secp256k1",
                ["Enclave:Performance:EnableMetrics"] = "true"
            });

        _configuration = configurationBuilder.Build();

        // Set environment variables for SGX simulation mode
        Environment.SetEnvironmentVariable("SGX_MODE", _loadTestConfig.SGXMode);
        Environment.SetEnvironmentVariable("SGX_DEBUG", _loadTestConfig.EnableDebug ? "1" : "0");
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "LoadTest");

        // Build service collection
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddSingleton(_configuration);
        services.AddNeoServiceLayer(_configuration);

        _serviceProvider = services.BuildServiceProvider();
        _performanceMonitor = new PerformanceMonitor(_loadTestConfig.ResourceMonitoring);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _performanceMonitor?.Dispose();
        _serviceProvider?.Dispose();
    }

    [Test]
    [Category("DataSealing")]
    public void LoadTest_DataSealing_ShouldHandleHighThroughput()
    {
        // Load scenario configuration
        var scenarioConfig = LoadScenarioConfig("DataSealing");

        var scenario = Scenario.Create("data_sealing_load_test", async context =>
        {
            using var scope = _serviceProvider.CreateScope();
            var enclaveWrapper = scope.ServiceProvider.GetRequiredService<IEnclaveWrapper>();

            // Initialize enclave if not already done
            if (!await IsEnclaveInitializedAsync(enclaveWrapper))
            {
                await InitializeEnclaveAsync(enclaveWrapper);
            }

            // Generate test data with varying sizes
            var dataSizes = new[] { 1024, 4096, 16384, 65536 };
            var dataSize = dataSizes[context.InvocationNumber % dataSizes.Length];
            var testData = GenerateTestData(dataSize);
            var keyId = $"load-test-key-{context.InvocationNumber}";

            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var sealedData = await enclaveWrapper.SealDataAsync(testData, keyId);
                stopwatch.Stop();

                // Validate result
                if (sealedData?.EncryptedData == null || sealedData.EncryptedData.Length == 0)
                {
                    return Response.Fail("Sealed data is null or empty");
                }

                // Add custom metrics
                context.Logger.Debug($"Sealed {dataSize} bytes in {stopwatch.ElapsedMilliseconds}ms");
                
                return Response.Ok(sizeBytes: dataSize);
            }
            catch (Exception ex)
            {
                context.Logger.Error(ex, $"Data sealing failed for {dataSize} bytes");
                return Response.Fail(ex.Message);
            }
        })
        .WithLoadSimulations(
            Simulation.RampingInject(rate: scenarioConfig.MaxRps,
                                   interval: TimeSpan.FromSeconds(1),
                                   during: TimeSpan.Parse(scenarioConfig.Duration))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("load-test-reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv, ReportFormat.Md)
            .Run();

        // Validate performance expectations
        ValidateScenarioPerformance(stats, "data_sealing_load_test", scenarioConfig);
    }

    [Test]
    [Category("Cryptography")]
    public void LoadTest_CryptographicOperations_ShouldMaintainPerformance()
    {
        var scenarioConfig = LoadScenarioConfig("CryptographicOperations");

        var signatureScenario = Scenario.Create("signature_generation_load_test", async context =>
        {
            using var scope = _serviceProvider.CreateScope();
            var enclaveWrapper = scope.ServiceProvider.GetRequiredService<IEnclaveWrapper>();

            if (!await IsEnclaveInitializedAsync(enclaveWrapper))
            {
                await InitializeEnclaveAsync(enclaveWrapper);
            }

            var testData = GenerateTestData(256);
            var keyId = $"crypto-key-{context.InvocationNumber % 10}"; // Reuse keys

            try
            {
                var signature = await enclaveWrapper.SignDataAsync(testData, keyId);
                
                if (signature?.Signature == null || signature.PublicKey == null)
                {
                    return Response.Fail("Signature generation failed");
                }

                return Response.Ok();
            }
            catch (Exception ex)
            {
                context.Logger.Error(ex, "Signature generation failed");
                return Response.Fail(ex.Message);
            }
        })
        .WithLoadSimulations(
            Simulation.RampingConstant(copies: scenarioConfig.ConcurrentCopies,
                                     during: TimeSpan.Parse(scenarioConfig.Duration))
        );

        var verificationScenario = Scenario.Create("signature_verification_load_test", async context =>
        {
            using var scope = _serviceProvider.CreateScope();
            var enclaveWrapper = scope.ServiceProvider.GetRequiredService<IEnclaveWrapper>();

            if (!await IsEnclaveInitializedAsync(enclaveWrapper))
            {
                await InitializeEnclaveAsync(enclaveWrapper);
            }

            var testData = GenerateTestData(256);
            var keyId = $"crypto-key-{context.InvocationNumber % 10}";

            try
            {
                // Generate signature first
                var signature = await enclaveWrapper.SignDataAsync(testData, keyId);
                
                // Verify signature
                var isValid = await enclaveWrapper.VerifySignatureAsync(
                    testData, signature.Signature, signature.PublicKey);

                if (!isValid)
                {
                    return Response.Fail("Signature verification failed");
                }

                return Response.Ok();
            }
            catch (Exception ex)
            {
                context.Logger.Error(ex, "Signature verification failed");
                return Response.Fail(ex.Message);
            }
        })
        .WithLoadSimulations(
            Simulation.RampingConstant(copies: scenarioConfig.ConcurrentCopies,
                                     during: TimeSpan.Parse(scenarioConfig.Duration))
        );

        var stats = NBomberRunner
            .RegisterScenarios(signatureScenario, verificationScenario)
            .WithReportFolder("load-test-reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .Run();

        // Validate both scenarios
        ValidateScenarioPerformance(stats, "signature_generation_load_test", scenarioConfig);
        ValidateScenarioPerformance(stats, "signature_verification_load_test", scenarioConfig);
    }

    [Test]
    [Category("JavaScript")]
    public void LoadTest_JavaScriptExecution_ShouldScaleUnderLoad()
    {
        var scenarioConfig = LoadScenarioConfig("JavaScriptExecution");

        var scenario = Scenario.Create("javascript_execution_load_test", async context =>
        {
            using var scope = _serviceProvider.CreateScope();
            var enclaveWrapper = scope.ServiceProvider.GetRequiredService<IEnclaveWrapper>();

            if (!await IsEnclaveInitializedAsync(enclaveWrapper))
            {
                await InitializeEnclaveAsync(enclaveWrapper);
            }

            // Different JavaScript complexities
            var scripts = new[]
            {
                "function simple(input) { return input.value * 2; } simple(input);",
                @"function moderate(input) { 
                    let result = 0;
                    for(let i = 0; i < input.iterations; i++) {
                        result += Math.sin(i) * Math.cos(i);
                    }
                    return { result, timestamp: Date.now() };
                } moderate(input);",
                @"function complex(input) {
                    function fibonacci(n) {
                        if (n <= 1) return n;
                        return fibonacci(n-1) + fibonacci(n-2);
                    }
                    return {
                        fib: fibonacci(input.n),
                        processed: input.data.map(x => x * 2),
                        timestamp: Date.now()
                    };
                } complex(input);"
            };

            var inputs = new object[]
            {
                new { value = 42 },
                new { iterations = 1000 },
                new { n = 20, data = Enumerable.Range(1, 100).ToArray() }
            };

            var scriptIndex = context.InvocationNumber % scripts.Length;
            var jsCode = scripts[scriptIndex];
            var input = inputs[scriptIndex];

            var jsContext = new JavaScriptContext
            {
                Input = input,
                Timeout = TimeSpan.FromSeconds(5),
                MemoryLimit = 64 * 1024 * 1024,
                SecurityConstraints = new JavaScriptSecurityConstraints
                {
                    DisallowNetworking = true,
                    DisallowFileSystem = true,
                    DisallowProcessExecution = true
                }
            };

            try
            {
                var result = await enclaveWrapper.ExecuteJavaScriptAsync(jsCode, jsContext);
                
                if (!result.Success)
                {
                    return Response.Fail($"JavaScript execution failed: {result.ErrorMessage}");
                }

                return Response.Ok();
            }
            catch (Exception ex)
            {
                context.Logger.Error(ex, "JavaScript execution failed");
                return Response.Fail(ex.Message);
            }
        })
        .WithLoadSimulations(
            Simulation.RampingInject(rate: scenarioConfig.MaxRps,
                                   interval: TimeSpan.FromSeconds(1),
                                   during: TimeSpan.Parse(scenarioConfig.Duration))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("load-test-reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .Run();

        ValidateScenarioPerformance(stats, "javascript_execution_load_test", scenarioConfig);
    }

    [Test]
    [Category("StressTest")]
    public void StressTest_MemoryPressure_ShouldHandleResourceConstraints()
    {
        var stressConfig = LoadStressTestConfig("MemoryPressure");

        var scenario = Scenario.Create("memory_pressure_stress_test", async context =>
        {
            using var scope = _serviceProvider.CreateScope();
            var enclaveWrapper = scope.ServiceProvider.GetRequiredService<IEnclaveWrapper>();

            if (!await IsEnclaveInitializedAsync(enclaveWrapper))
            {
                await InitializeEnclaveAsync(enclaveWrapper);
            }

            // Create larger data to pressure memory
            var dataSize = 65536 * stressConfig.DataSizeMultiplier; // Multiply base size
            var testData = GenerateTestData(dataSize);
            var keyId = $"stress-test-key-{context.InvocationNumber}";

            try
            {
                // Perform multiple operations to increase memory pressure
                var sealedData = await enclaveWrapper.SealDataAsync(testData, keyId);
                var unsealedData = await enclaveWrapper.UnsealDataAsync(sealedData, keyId);
                
                // Validate data integrity under stress
                if (!testData.SequenceEqual(unsealedData))
                {
                    return Response.Fail("Data integrity check failed under memory pressure");
                }

                return Response.Ok(sizeBytes: dataSize);
            }
            catch (OutOfMemoryException)
            {
                // Expected under extreme memory pressure
                return Response.Fail("OutOfMemory");
            }
            catch (Exception ex)
            {
                context.Logger.Error(ex, $"Memory pressure test failed for {dataSize} bytes");
                return Response.Fail(ex.Message);
            }
        })
        .WithLoadSimulations(
            Simulation.RampingConstant(copies: stressConfig.ConcurrentOperations,
                                     during: TimeSpan.Parse(stressConfig.Duration))
        );

        // Start performance monitoring
        _performanceMonitor.StartMonitoring();

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("stress-test-reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .Run();

        var resourceStats = _performanceMonitor.StopMonitoring();

        // Validate system remained stable under stress
        ValidateStressTestResults(stats, resourceStats, "memory_pressure_stress_test");
    }

    [Test]
    [Category("BurstLoad")]
    public void LoadTest_BurstLoad_ShouldHandleTrafficSpikes()
    {
        var burstConfig = LoadStressTestConfig("BurstLoad");

        var scenario = Scenario.Create("burst_load_test", async context =>
        {
            using var scope = _serviceProvider.CreateScope();
            var enclaveWrapper = scope.ServiceProvider.GetRequiredService<IEnclaveWrapper>();

            if (!await IsEnclaveInitializedAsync(enclaveWrapper))
            {
                await InitializeEnclaveAsync(enclaveWrapper);
            }

            var testData = GenerateTestData(1024);
            var keyId = $"burst-test-key-{context.InvocationNumber % 100}";

            try
            {
                var sealedData = await enclaveWrapper.SealDataAsync(testData, keyId);
                return Response.Ok();
            }
            catch (Exception ex)
            {
                context.Logger.Error(ex, "Burst load operation failed");
                return Response.Fail(ex.Message);
            }
        });

        // Create burst load pattern
        var loadSimulations = new List<LoadSimulation>();
        for (int i = 0; i < burstConfig.TotalBursts; i++)
        {
            // Burst phase
            loadSimulations.Add(
                Simulation.RampingInject(rate: burstConfig.BurstRps,
                                       interval: TimeSpan.FromSeconds(1),
                                       during: TimeSpan.FromSeconds(burstConfig.BurstDurationSeconds))
            );
            
            // Rest phase
            if (i < burstConfig.TotalBursts - 1) // Don't add rest after last burst
            {
                loadSimulations.Add(
                    Simulation.RampingInject(rate: 1, // Minimal load during rest
                                           interval: TimeSpan.FromSeconds(1),
                                           during: TimeSpan.FromSeconds(burstConfig.RestDurationSeconds))
                );
            }
        }

        scenario = scenario.WithLoadSimulations(loadSimulations.ToArray());

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder("burst-test-reports")
            .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
            .Run();

        // Validate burst load handling
        ValidateBurstLoadResults(stats, "burst_load_test", burstConfig);
    }

    #region Helper Methods

    private static async Task<bool> IsEnclaveInitializedAsync(IEnclaveWrapper enclaveWrapper)
    {
        try
        {
            // Try a simple operation to check if enclave is initialized
            await enclaveWrapper.GetAttestationAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task InitializeEnclaveAsync(IEnclaveWrapper enclaveWrapper)
    {
        var config = new EnclaveConfig
        {
            SGXMode = "SIM",
            EnableDebug = true,
            Cryptography = new CryptographyConfig
            {
                EncryptionAlgorithm = "AES-256-GCM",
                SigningAlgorithm = "secp256k1"
            },
            Performance = new PerformanceConfig
            {
                EnableMetrics = true,
                EnableBenchmarking = false
            }
        };

        var result = await enclaveWrapper.InitializeAsync(config);
        if (!result)
        {
            throw new InvalidOperationException("Failed to initialize enclave for load testing");
        }
    }

    private static byte[] GenerateTestData(int size)
    {
        var data = new byte[size];
        var random = new Random(42); // Fixed seed for reproducibility
        random.NextBytes(data);
        return data;
    }

    private ScenarioConfig LoadScenarioConfig(string scenarioName)
    {
        var configJson = File.ReadAllText("load-test-config.json");
        var configRoot = JsonSerializer.Deserialize<JsonElement>(configJson);
        var scenariosElement = configRoot.GetProperty("Scenarios");
        var scenarioElement = scenariosElement.GetProperty(scenarioName);
        
        return JsonSerializer.Deserialize<ScenarioConfig>(scenarioElement.GetRawText())!;
    }

    private StressTestConfig LoadStressTestConfig(string testName)
    {
        var configJson = File.ReadAllText("load-test-config.json");
        var configRoot = JsonSerializer.Deserialize<JsonElement>(configJson);
        var stressElement = configRoot.GetProperty("StressTestScenarios");
        var testElement = stressElement.GetProperty(testName);
        
        return JsonSerializer.Deserialize<StressTestConfig>(testElement.GetRawText())!;
    }

    private void ValidateScenarioPerformance(NBomberStats stats, string scenarioName, ScenarioConfig config)
    {
        var scenarioStats = stats.AllScenarioStats.FirstOrDefault(s => s.ScenarioName == scenarioName);
        scenarioStats.Should().NotBeNull($"Scenario {scenarioName} should have stats");

        // Validate error rate
        var errorRate = (double)scenarioStats!.AllFailCount / scenarioStats.AllRequestCount * 100;
        errorRate.Should().BeLessOrEqualTo(1.0, "Error rate should be less than 1%");

        // Validate latency
        scenarioStats.Ok.Latency.P95.Should().BeLessOrEqualTo(TimeSpan.FromMilliseconds(config.ExpectedLatencyP95Ms),
            $"P95 latency should be less than {config.ExpectedLatencyP95Ms}ms");
        
        scenarioStats.Ok.Latency.P99.Should().BeLessOrEqualTo(TimeSpan.FromMilliseconds(config.ExpectedLatencyP99Ms),
            $"P99 latency should be less than {config.ExpectedLatencyP99Ms}ms");

        TestContext.WriteLine($"Scenario {scenarioName} Performance:");
        TestContext.WriteLine($"  Total Requests: {scenarioStats.AllRequestCount}");
        TestContext.WriteLine($"  Success Rate: {(double)scenarioStats.AllOkCount / scenarioStats.AllRequestCount * 100:F2}%");
        TestContext.WriteLine($"  P95 Latency: {scenarioStats.Ok.Latency.P95.TotalMilliseconds:F2}ms");
        TestContext.WriteLine($"  P99 Latency: {scenarioStats.Ok.Latency.P99.TotalMilliseconds:F2}ms");
        TestContext.WriteLine($"  RPS: {scenarioStats.Ok.Request.RPS:F2}");
    }

    private void ValidateStressTestResults(NBomberStats stats, ResourceUsageStats resourceStats, string testName)
    {
        var scenarioStats = stats.AllScenarioStats.FirstOrDefault(s => s.ScenarioName == testName);
        scenarioStats.Should().NotBeNull($"Stress test {testName} should have stats");

        // Allow higher error rates under stress but validate system didn't crash
        var errorRate = (double)scenarioStats!.AllFailCount / scenarioStats.AllRequestCount * 100;
        errorRate.Should().BeLessOrEqualTo(10.0, "Error rate under stress should be manageable");

        // Validate resource usage stayed within bounds
        var thresholds = LoadPerformanceThresholds();
        resourceStats.MaxCpuUsagePercent.Should().BeLessOrEqualTo(thresholds.MaxCpuUsagePercent * 1.2, // Allow 20% tolerance under stress
            "CPU usage under stress should be manageable");

        TestContext.WriteLine($"Stress Test {testName} Results:");
        TestContext.WriteLine($"  Max CPU Usage: {resourceStats.MaxCpuUsagePercent:F2}%");
        TestContext.WriteLine($"  Max Memory Usage: {resourceStats.MaxMemoryUsageMB:F2}MB");
        TestContext.WriteLine($"  Error Rate: {errorRate:F2}%");
    }

    private void ValidateBurstLoadResults(NBomberStats stats, string testName, StressTestConfig burstConfig)
    {
        var scenarioStats = stats.AllScenarioStats.FirstOrDefault(s => s.ScenarioName == testName);
        scenarioStats.Should().NotBeNull($"Burst test {testName} should have stats");

        // Validate system handled bursts without complete failure
        var errorRate = (double)scenarioStats!.AllFailCount / scenarioStats.AllRequestCount * 100;
        errorRate.Should().BeLessOrEqualTo(5.0, "Error rate during burst load should be acceptable");

        TestContext.WriteLine($"Burst Load Test {testName} Results:");
        TestContext.WriteLine($"  Total Requests: {scenarioStats.AllRequestCount}");
        TestContext.WriteLine($"  Average RPS: {scenarioStats.Ok.Request.RPS:F2}");
        TestContext.WriteLine($"  Error Rate: {errorRate:F2}%");
    }

    private PerformanceThresholds LoadPerformanceThresholds()
    {
        var configJson = File.ReadAllText("load-test-config.json");
        var configRoot = JsonSerializer.Deserialize<JsonElement>(configJson);
        var thresholdsElement = configRoot.GetProperty("PerformanceThresholds");
        
        return JsonSerializer.Deserialize<PerformanceThresholds>(thresholdsElement.GetRawText())!;
    }

    #endregion
}

#region Configuration Classes

public class LoadTestConfiguration
{
    public string SGXMode { get; set; } = "SIM";
    public bool EnableDebug { get; set; } = true;
    public int TestDurationMinutes { get; set; } = 5;
    public int WarmupDurationSeconds { get; set; } = 30;
    public int ReportingIntervalSeconds { get; set; } = 10;
    public int MaxConcurrentUsers { get; set; } = 100;
    public ResourceMonitoringConfig ResourceMonitoring { get; set; } = new();
}

public class ResourceMonitoringConfig
{
    public bool EnableCpuMonitoring { get; set; } = true;
    public bool EnableMemoryMonitoring { get; set; } = true;
    public bool EnableSgxMemoryMonitoring { get; set; } = true;
    public int MonitoringIntervalMs { get; set; } = 1000;
}

public class ScenarioConfig
{
    public int MaxRps { get; set; }
    public string Duration { get; set; } = "00:05:00";
    public int ConcurrentCopies { get; set; }
    public int ExpectedLatencyP95Ms { get; set; }
    public int ExpectedLatencyP99Ms { get; set; }
}

public class StressTestConfig
{
    public int DataSizeMultiplier { get; set; } = 1;
    public int ConcurrentOperations { get; set; } = 10;
    public string Duration { get; set; } = "00:05:00";
    public int TargetRps { get; set; }
    public int SteadyStateRps { get; set; }
    public int BurstRps { get; set; }
    public int BurstDurationSeconds { get; set; }
    public int RestDurationSeconds { get; set; }
    public int TotalBursts { get; set; }
}

public class PerformanceThresholds
{
    public double MaxCpuUsagePercent { get; set; } = 80;
    public double MaxMemoryUsageMB { get; set; } = 2048;
    public double MaxSgxMemoryUsageMB { get; set; } = 256;
    public double MaxErrorRatePercent { get; set; } = 1;
    public double MinThroughputRps { get; set; } = 50;
}

#endregion 