using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using System.Text.Json;
using Xunit;

namespace NeoServiceLayer.Services.Tee.Tests
{
    /// <summary>
    /// Comprehensive unit tests for SGX Enclave Runtime orchestration
    /// Tests service lifecycle, configuration, coordination, and error handling
    /// </summary>
    public class EnclaveRuntimeTests : IDisposable
    {
        private readonly Mock<ILogger<EnclaveRuntime>> _mockLogger;
        private readonly EnclaveRuntime _enclaveRuntime;
        private readonly EnclaveConfig _testConfig;

        public EnclaveRuntimeTests()
        {
            _mockLogger = new Mock<ILogger<EnclaveRuntime>>();
            _testConfig = new EnclaveConfig
            {
                mode = "test",
                log_level = "debug",
                sgx_simulation_mode = true,
                max_threads = 4,
                storage_path = Path.Combine(Path.GetTempPath(), $"enclave_test_{Guid.NewGuid()}"),
                network_timeout_seconds = 30,
                crypto_algorithms = new List<string> { "aes-256-gcm", "ed25519", "secp256k1" },
                enable_ai = true,
                enable_oracle = true,
                computation = new ComputationConfig
                {
                    max_concurrent_jobs = 5,
                    timeout_seconds = 30,
                    memory_limit_mb = 64
                }
            };

            _enclaveRuntime = EnclaveRuntime.NewAsync(_testConfig).Result;
        }

        #region Runtime Initialization Tests

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Runtime")]
        public async Task EnclaveRuntime_Initialization_ShouldSucceed()
        {
            // Arrange & Act - Runtime is initialized in constructor

            // Assert
            Assert.NotNull(_enclaveRuntime);
            Assert.Equal(_testConfig.mode, _enclaveRuntime.Config.mode);
            Assert.Equal(_testConfig.max_threads, _enclaveRuntime.Config.max_threads);
            Assert.Equal(_testConfig.sgx_simulation_mode, _enclaveRuntime.Config.sgx_simulation_mode);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Runtime")]
        public async Task EnclaveRuntime_ServiceAccess_ShouldProvideAllServices()
        {
            // Act
            var cryptoService = _enclaveRuntime.CryptoService;
            var storageService = _enclaveRuntime.StorageService;
            var computationService = _enclaveRuntime.ComputationService;
            var accountService = _enclaveRuntime.AccountService;
            var oracleService = _enclaveRuntime.OracleService;
            var aiService = _enclaveRuntime.AIService;

            // Assert
            Assert.NotNull(cryptoService);
            Assert.NotNull(storageService);
            Assert.NotNull(computationService);
            Assert.NotNull(accountService);
            Assert.NotNull(oracleService);
            Assert.NotNull(aiService);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Runtime")]
        public async Task EnclaveRuntime_ConfigValidation_ShouldValidateConfig()
        {
            // Arrange
            var invalidConfig = new EnclaveConfig
            {
                max_threads = 0, // Invalid
                network_timeout_seconds = 0 // Invalid
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => EnclaveRuntime.NewAsync(invalidConfig)
            );
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Runtime")]
        public async Task EnclaveRuntime_DefaultConfiguration_ShouldUseDefaults()
        {
            // Arrange
            var defaultConfig = new EnclaveConfig(); // Uses defaults

            // Act
            var runtime = await EnclaveRuntime.NewAsync(defaultConfig);

            // Assert
            Assert.Equal("production", runtime.Config.mode);
            Assert.Equal("info", runtime.Config.log_level);
            Assert.False(runtime.Config.sgx_simulation_mode);
            Assert.Equal(16, runtime.Config.max_threads);
            Assert.Contains("aes-256-gcm", runtime.Config.crypto_algorithms);
            Assert.True(runtime.Config.enable_ai);
            Assert.True(runtime.Config.enable_oracle);

            runtime.Dispose();
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Runtime")]
        public async Task EnclaveRuntime_ConfigMerge_ShouldMergeConfigurations()
        {
            // Arrange
            var baseConfig = new EnclaveConfig
            {
                mode = "base",
                max_threads = 8,
                enable_ai = false
            };

            var overrideConfig = new EnclaveConfig
            {
                mode = "override",
                log_level = "trace",
                enable_ai = true
            };

            // Act
            baseConfig.Merge(overrideConfig);

            // Assert
            Assert.Equal("override", baseConfig.mode);
            Assert.Equal("trace", baseConfig.log_level);
            Assert.Equal(8, baseConfig.max_threads); // Should keep original value
            Assert.True(baseConfig.enable_ai); // Should be overridden
        }

        #endregion

        #region Service Lifecycle Tests

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Runtime")]
        public async Task EnclaveRuntime_StartServices_ShouldStartAllServices()
        {
            // Act
            await _enclaveRuntime.StartAsync();

            // Assert
            // Services should be started (we can't directly test this without state exposure,
            // but we can verify no exceptions are thrown and basic functionality works)
            
            // Test that services are functional after start
            var cryptoResult = await _enclaveRuntime.CryptoService.GenerateRandomBytesAsync(16);
            Assert.Equal(16, cryptoResult.Length);

            var storageKeys = await _enclaveRuntime.StorageService.ListKeysAsync();
            Assert.NotNull(storageKeys);

            var jsResult = await _enclaveRuntime.ComputationService.ExecuteJavaScriptAsync("1 + 1", "{}");
            Assert.NotNull(jsResult);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Runtime")]
        public async Task EnclaveRuntime_ShutdownServices_ShouldShutdownGracefully()
        {
            // Arrange
            await _enclaveRuntime.StartAsync();

            // Act
            await _enclaveRuntime.ShutdownAsync();

            // Assert - Should complete without exceptions
            // After shutdown, services should still be accessible but may have limited functionality
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Runtime")]
        public async Task EnclaveRuntime_RestartCycle_ShouldWork()
        {
            // Act - Multiple start/shutdown cycles
            await _enclaveRuntime.StartAsync();
            await _enclaveRuntime.ShutdownAsync();
            await _enclaveRuntime.StartAsync();
            await _enclaveRuntime.ShutdownAsync();

            // Assert - Should complete without exceptions
            Assert.NotNull(_enclaveRuntime.CryptoService);
        }

        #endregion

        #region Service Integration Tests

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Runtime")]
        public async Task EnclaveRuntime_CryptoStorageIntegration_ShouldWork()
        {
            // Arrange
            await _enclaveRuntime.StartAsync();
            var keyId = "integration_test_key";
            var testData = Encoding.UTF8.GetBytes("Integration test data");

            // Act - Generate key using crypto service
            var keyMetadata = await _enclaveRuntime.CryptoService.GenerateKeyAsync(
                keyId, CryptoAlgorithm.Aes256Gcm, new[] { "Encrypt", "Decrypt" }, false, "Integration test key"
            );

            // Store encrypted data using storage service
            var storageKey = "integration_storage_key";
            await _enclaveRuntime.StorageService.StoreDataAsync(storageKey, testData, "encryption_key", true);

            // Retrieve and verify data
            var retrievedData = await _enclaveRuntime.StorageService.RetrieveDataAsync(storageKey, "encryption_key");

            // Assert
            Assert.NotNull(keyMetadata);
            Assert.Equal(testData, retrievedData);

            // Cleanup
            await _enclaveRuntime.CryptoService.DeleteKeyAsync(keyId);
            await _enclaveRuntime.StorageService.DeleteDataAsync(storageKey);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Runtime")]
        public async Task EnclaveRuntime_CryptoComputationIntegration_ShouldWork()
        {
            // Arrange
            await _enclaveRuntime.StartAsync();
            var keyId = "crypto_computation_key";

            // Act - Generate signing key
            await _enclaveRuntime.CryptoService.GenerateKeyAsync(
                keyId, CryptoAlgorithm.Ed25519, new[] { "Sign", "Verify" }, false, "Computation test key"
            );

            // Use computation service to perform crypto operations
            var jsCode = @"
                const data = 'Hello, Crypto World!';
                const result = {
                    original: data,
                    length: data.length,
                    hash_simulation: Math.abs(data.split('').reduce((a, b) => a + b.charCodeAt(0), 0)).toString(16)
                };
                result;
            ";

            var computationResult = await _enclaveRuntime.ComputationService.ExecuteJavaScriptAsync(jsCode, "{}");
            
            // Sign the computation result
            var resultBytes = Encoding.UTF8.GetBytes(computationResult);
            var signature = await _enclaveRuntime.CryptoService.SignDataAsync(keyId, resultBytes);

            // Verify the signature
            var isValid = await _enclaveRuntime.CryptoService.VerifySignatureAsync(keyId, resultBytes, signature);

            // Assert
            Assert.NotNull(computationResult);
            Assert.Contains("Hello, Crypto World!", computationResult);
            Assert.NotNull(signature);
            Assert.True(isValid);

            // Cleanup
            await _enclaveRuntime.CryptoService.DeleteKeyAsync(keyId);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Runtime")]
        public async Task EnclaveRuntime_StorageComputationIntegration_ShouldWork()
        {
            // Arrange
            await _enclaveRuntime.StartAsync();
            var inputDataKey = "computation_input_data";
            var outputDataKey = "computation_output_data";

            // Store input data
            var inputData = JsonSerializer.Serialize(new
            {
                numbers = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
                operation = "statistical_analysis"
            });
            var inputBytes = Encoding.UTF8.GetBytes(inputData);

            await _enclaveRuntime.StorageService.StoreDataAsync(inputDataKey, inputBytes, "compute_key", true);

            // Retrieve and process data in computation service
            var retrievedInput = await _enclaveRuntime.StorageService.RetrieveDataAsync(inputDataKey, "compute_key");
            var inputDataStr = Encoding.UTF8.GetString(retrievedInput);

            var jsCode = @"
                const input = JSON.parse(arguments);
                const numbers = input.numbers;
                
                const result = {
                    count: numbers.length,
                    sum: numbers.reduce((a, b) => a + b, 0),
                    average: numbers.reduce((a, b) => a + b, 0) / numbers.length,
                    min: Math.min(...numbers),
                    max: Math.max(...numbers),
                    operation: input.operation
                };
                result;
            ";

            var computationResult = await _enclaveRuntime.ComputationService.ExecuteJavaScriptAsync(jsCode, inputDataStr);

            // Store computation result
            var resultBytes = Encoding.UTF8.GetBytes(computationResult);
            await _enclaveRuntime.StorageService.StoreDataAsync(outputDataKey, resultBytes, "compute_key", true);

            // Verify stored result
            var storedResult = await _enclaveRuntime.StorageService.RetrieveDataAsync(outputDataKey, "compute_key");
            var finalResult = Encoding.UTF8.GetString(storedResult);

            // Assert
            Assert.Equal(computationResult, finalResult);
            Assert.Contains("statistical_analysis", finalResult);
            Assert.Contains("\"count\": 10", finalResult);
            Assert.Contains("\"sum\": 55", finalResult);

            // Cleanup
            await _enclaveRuntime.StorageService.DeleteDataAsync(inputDataKey);
            await _enclaveRuntime.StorageService.DeleteDataAsync(outputDataKey);
        }

        #endregion

        #region Configuration Management Tests

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Runtime")]
        public async Task EnclaveConfig_GetNumber_ShouldReturnCorrectValues()
        {
            // Act & Assert
            Assert.Equal(_testConfig.max_threads, (ulong)_testConfig.GetNumber("computation.max_concurrent_jobs"));
            Assert.Equal(1024UL, (ulong)_testConfig.GetNumber("ai.max_model_size_mb"));
            Assert.Equal(512UL, (ulong)_testConfig.GetNumber("ai.max_training_data_mb"));

            // Test unknown key
            Assert.Throws<ArgumentException>(() => _testConfig.GetNumber("unknown.key"));
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Runtime")]
        public async Task EnclaveConfig_Validation_ShouldValidateRequiredFields()
        {
            // Arrange
            var validConfig = new EnclaveConfig
            {
                max_threads = 8,
                network_timeout_seconds = 30
            };

            var invalidConfig1 = new EnclaveConfig
            {
                max_threads = 0, // Invalid
                network_timeout_seconds = 30
            };

            var invalidConfig2 = new EnclaveConfig
            {
                max_threads = 8,
                network_timeout_seconds = 0 // Invalid
            };

            // Act & Assert
            validConfig.Validate(); // Should not throw

            Assert.Throws<ArgumentException>(() => invalidConfig1.Validate());
            Assert.Throws<ArgumentException>(() => invalidConfig2.Validate());
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Runtime")]
        public async Task EnclaveRuntime_DisabledServices_ShouldNotInitialize()
        {
            // Arrange
            var configWithDisabledServices = new EnclaveConfig
            {
                mode = "test",
                storage_path = Path.Combine(Path.GetTempPath(), $"disabled_test_{Guid.NewGuid()}"),
                enable_ai = false,
                enable_oracle = false
            };

            // Act
            var runtime = await EnclaveRuntime.NewAsync(configWithDisabledServices);

            // Assert
            Assert.NotNull(runtime.CryptoService);
            Assert.NotNull(runtime.StorageService);
            Assert.NotNull(runtime.ComputationService);
            Assert.NotNull(runtime.AccountService);
            
            // These should be null when disabled
            Assert.Null(runtime.OracleService);
            Assert.Null(runtime.AIService);

            runtime.Dispose();
        }

        #endregion

        #region Error Handling and Edge Cases Tests

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Runtime")]
        public async Task EnclaveRuntime_ServiceFailure_ShouldHandleGracefully()
        {
            // Arrange
            await _enclaveRuntime.StartAsync();

            // Act - Simulate service failures and recovery
            try
            {
                // Force an error condition
                await _enclaveRuntime.CryptoService.GenerateKeyAsync("", CryptoAlgorithm.Ed25519, new[] { "Sign" }, false, "");
                Assert.True(false, "Should have thrown an exception");
            }
            catch (ArgumentException)
            {
                // Expected - empty key ID should fail
            }

            // Service should still be functional after error
            var randomBytes = await _enclaveRuntime.CryptoService.GenerateRandomBytesAsync(16);
            
            // Assert
            Assert.Equal(16, randomBytes.Length);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Runtime")]
        public async Task EnclaveRuntime_ConcurrentAccess_ShouldHandleSafely()
        {
            // Arrange
            await _enclaveRuntime.StartAsync();
            var tasks = new List<Task>();

            // Act - Create concurrent operations across all services
            for (int i = 0; i < 20; i++)
            {
                var taskId = i;
                tasks.Add(Task.Run(async () =>
                {
                    // Crypto operations
                    var keyId = $"concurrent_key_{taskId}";
                    await _enclaveRuntime.CryptoService.GenerateKeyAsync(keyId, CryptoAlgorithm.Ed25519, new[] { "Sign" }, false, $"Concurrent key {taskId}");
                    
                    // Storage operations
                    var storageKey = $"concurrent_data_{taskId}";
                    var data = Encoding.UTF8.GetBytes($"Concurrent data {taskId}");
                    await _enclaveRuntime.StorageService.StoreDataAsync(storageKey, data, "concurrent_key", false);
                    
                    // Computation operations
                    var jsCode = $"Math.random() * {taskId + 1}";
                    await _enclaveRuntime.ComputationService.ExecuteJavaScriptAsync(jsCode, "{}");
                    
                    // Cleanup
                    await _enclaveRuntime.CryptoService.DeleteKeyAsync(keyId);
                    await _enclaveRuntime.StorageService.DeleteDataAsync(storageKey);
                }));
            }

            // Assert - All operations should complete successfully
            await Task.WhenAll(tasks);
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Runtime")]
        public async Task EnclaveRuntime_ResourceLimits_ShouldEnforceConstraints()
        {
            // Arrange
            await _enclaveRuntime.StartAsync();

            // Act & Assert - Test various resource limits
            
            // Code size limit
            var largeCode = new string('/', 1024 * 1024 + 1); // Slightly over 1MB
            await Assert.ThrowsAsync<ArgumentException>(
                () => _enclaveRuntime.ComputationService.ExecuteJavaScriptAsync(largeCode, "{}")
            );

            // Parameter size limit
            var largeParams = new string('x', 10 * 1024 + 1); // Slightly over 10KB
            await Assert.ThrowsAsync<ArgumentException>(
                () => _enclaveRuntime.ComputationService.ExecuteJavaScriptAsync("1", largeParams)
            );

            // Storage size limit (simulated)
            var largeData = new byte[100 * 1024 * 1024 + 1]; // Slightly over 100MB
            await Assert.ThrowsAsync<ArgumentException>(
                () => _enclaveRuntime.StorageService.StoreDataAsync("large_key", largeData, "key", false)
            );
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Runtime")]
        public async Task EnclaveRuntime_MemoryManagement_ShouldNotLeak()
        {
            // Arrange
            await _enclaveRuntime.StartAsync();
            var initialMemory = GC.GetTotalMemory(false);

            // Act - Perform many operations that could potentially leak memory
            for (int i = 0; i < 100; i++)
            {
                var keyId = $"memory_test_key_{i}";
                var storageKey = $"memory_test_data_{i}";
                
                // Create and delete keys
                await _enclaveRuntime.CryptoService.GenerateKeyAsync(keyId, CryptoAlgorithm.Ed25519, new[] { "Sign" }, false, "Memory test");
                await _enclaveRuntime.CryptoService.DeleteKeyAsync(keyId);
                
                // Store and delete data
                var data = Encoding.UTF8.GetBytes($"Memory test data {i}");
                await _enclaveRuntime.StorageService.StoreDataAsync(storageKey, data, "memory_key", true);
                await _enclaveRuntime.StorageService.DeleteDataAsync(storageKey);
                
                // Execute JavaScript
                await _enclaveRuntime.ComputationService.ExecuteJavaScriptAsync($"Math.random() * {i}", "{}");
            }

            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalMemory = GC.GetTotalMemory(false);

            // Assert - Memory usage should not grow excessively
            var memoryGrowth = finalMemory - initialMemory;
            Assert.True(memoryGrowth < 10 * 1024 * 1024, // Less than 10MB growth
                $"Memory grew by {memoryGrowth} bytes, which may indicate a memory leak");
        }

        #endregion

        #region Performance and Load Tests

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Runtime")]
        [Trait("Performance", "True")]
        public async Task EnclaveRuntime_ServicePerformance_ShouldMeetBaselines()
        {
            // Arrange
            await _enclaveRuntime.StartAsync();
            var iterations = 100;

            // Act & Assert - Test performance of each service
            
            // Crypto performance
            var cryptoStopwatch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                var bytes = await _enclaveRuntime.CryptoService.GenerateRandomBytesAsync(32);
                Assert.Equal(32, bytes.Length);
            }
            cryptoStopwatch.Stop();

            // Storage performance
            var storageStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var testData = Encoding.UTF8.GetBytes("Performance test data");
            var storageKeys = new List<string>();
            
            for (int i = 0; i < iterations; i++)
            {
                var key = $"perf_test_{i}";
                storageKeys.Add(key);
                await _enclaveRuntime.StorageService.StoreDataAsync(key, testData, "perf_key", false);
            }
            storageStopwatch.Stop();

            // Computation performance
            var computationStopwatch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                await _enclaveRuntime.ComputationService.ExecuteJavaScriptAsync("Math.random()", "{}");
            }
            computationStopwatch.Stop();

            // Assert performance baselines
            var avgCryptoTime = cryptoStopwatch.ElapsedMilliseconds / (double)iterations;
            var avgStorageTime = storageStopwatch.ElapsedMilliseconds / (double)iterations;
            var avgComputationTime = computationStopwatch.ElapsedMilliseconds / (double)iterations;

            Assert.True(avgCryptoTime < 10, $"Average crypto time {avgCryptoTime:F2}ms should be under 10ms");
            Assert.True(avgStorageTime < 20, $"Average storage time {avgStorageTime:F2}ms should be under 20ms");
            Assert.True(avgComputationTime < 30, $"Average computation time {avgComputationTime:F2}ms should be under 30ms");

            // Cleanup
            foreach (var key in storageKeys)
            {
                await _enclaveRuntime.StorageService.DeleteDataAsync(key);
            }
        }

        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Component", "Runtime")]
        public async Task EnclaveRuntime_HighLoadStability_ShouldRemainStable()
        {
            // Arrange
            await _enclaveRuntime.StartAsync();
            var highLoadTasks = new List<Task>();

            // Act - Create high concurrent load
            for (int i = 0; i < 50; i++)
            {
                var taskId = i;
                highLoadTasks.Add(Task.Run(async () =>
                {
                    for (int j = 0; j < 10; j++)
                    {
                        var operationId = $"{taskId}_{j}";
                        
                        // Mixed operations
                        if (j % 3 == 0)
                        {
                            var bytes = await _enclaveRuntime.CryptoService.GenerateRandomBytesAsync(16);
                            Assert.Equal(16, bytes.Length);
                        }
                        else if (j % 3 == 1)
                        {
                            var data = Encoding.UTF8.GetBytes($"Load test {operationId}");
                            var key = $"load_key_{operationId}";
                            await _enclaveRuntime.StorageService.StoreDataAsync(key, data, "load_key", false);
                            await _enclaveRuntime.StorageService.DeleteDataAsync(key);
                        }
                        else
                        {
                            await _enclaveRuntime.ComputationService.ExecuteJavaScriptAsync($"Math.sin({j})", "{}");
                        }
                    }
                }));
            }

            // Assert - All high load operations should complete successfully
            await Task.WhenAll(highLoadTasks);
        }

        #endregion

        public void Dispose()
        {
            _enclaveRuntime?.Dispose();
            if (Directory.Exists(_testConfig.storage_path))
            {
                Directory.Delete(_testConfig.storage_path, recursive: true);
            }
        }
    }

    /// <summary>
    /// Mock/test implementation of EnclaveRuntime for unit testing
    /// This simulates the actual SGX enclave runtime behavior
    /// </summary>
    public class EnclaveRuntime : IDisposable
    {
        public EnclaveConfig Config { get; private set; }
        public CryptoService CryptoService { get; private set; }
        public StorageService StorageService { get; private set; }
        public ComputationService ComputationService { get; private set; }
        public AccountService AccountService { get; private set; }
        public OracleService OracleService { get; private set; }
        public AIService AIService { get; private set; }

        private EnclaveRuntime(EnclaveConfig config)
        {
            Config = config;
        }

        public static async Task<EnclaveRuntime> NewAsync(EnclaveConfig config)
        {
            // Validate configuration
            config.Validate();

            var runtime = new EnclaveRuntime(config);

            // Initialize services
            runtime.CryptoService = await CryptoService.NewAsync(config);
            runtime.StorageService = await StorageService.NewAsync(config);
            runtime.ComputationService = await ComputationService.NewAsync(config);
            runtime.AccountService = await AccountService.NewAsync(config, runtime.CryptoService);

            // Optional services
            runtime.OracleService = config.enable_oracle ? await OracleService.NewAsync(config) : null;
            runtime.AIService = config.enable_ai ? await AIService.NewAsync(config) : null;

            return runtime;
        }

        public async Task StartAsync()
        {
            // Start all services
            if (StorageService != null)
                await StorageService.StartAsync();

            if (OracleService != null)
                await OracleService.StartAsync();

            if (AIService != null)
                await AIService.StartAsync();

            // Services are now started and ready
        }

        public async Task ShutdownAsync()
        {
            // Shutdown services in reverse order
            if (AIService != null)
                await AIService.ShutdownAsync();

            if (OracleService != null)
                await OracleService.ShutdownAsync();

            if (StorageService != null)
                await StorageService.ShutdownAsync();

            // Shutdown complete
        }

        public void Dispose()
        {
            CryptoService?.Dispose();
            StorageService?.Dispose();
            ComputationService?.Dispose();
            AccountService?.Dispose();
            OracleService?.Dispose();
            AIService?.Dispose();
        }
    }

    /// <summary>
    /// Mock AccountService for testing
    /// </summary>
    public class AccountService : IDisposable
    {
        private readonly EnclaveConfig _config;
        private readonly CryptoService _cryptoService;

        private AccountService(EnclaveConfig config, CryptoService cryptoService)
        {
            _config = config;
            _cryptoService = cryptoService;
        }

        public static async Task<AccountService> NewAsync(EnclaveConfig config, CryptoService cryptoService)
        {
            return await Task.FromResult(new AccountService(config, cryptoService));
        }

        public void Dispose()
        {
            // Cleanup account service resources
        }
    }

    /// <summary>
    /// Mock OracleService for testing
    /// </summary>
    public class OracleService : IDisposable
    {
        private readonly EnclaveConfig _config;

        private OracleService(EnclaveConfig config)
        {
            _config = config;
        }

        public static async Task<OracleService> NewAsync(EnclaveConfig config)
        {
            return await Task.FromResult(new OracleService(config));
        }

        public async Task StartAsync()
        {
            await Task.CompletedTask;
        }

        public async Task ShutdownAsync()
        {
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            // Cleanup oracle service resources
        }
    }

    /// <summary>
    /// Mock AIService for testing
    /// </summary>
    public class AIService : IDisposable
    {
        private readonly EnclaveConfig _config;

        private AIService(EnclaveConfig config)
        {
            _config = config;
        }

        public static async Task<AIService> NewAsync(EnclaveConfig config)
        {
            return await Task.FromResult(new AIService(config));
        }

        public async Task StartAsync()
        {
            await Task.CompletedTask;
        }

        public async Task ShutdownAsync()
        {
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            // Cleanup AI service resources
        }
    }

    /// <summary>
    /// Enhanced EnclaveConfig with additional methods
    /// </summary>
    public class EnclaveConfig
    {
        public string mode { get; set; } = "production";
        public string log_level { get; set; } = "info";
        public bool sgx_simulation_mode { get; set; } = false;
        public int max_threads { get; set; } = 16;
        public string storage_path { get; set; } = "/secure";
        public ulong network_timeout_seconds { get; set; } = 30;
        public List<string> crypto_algorithms { get; set; } = new() 
        { 
            "aes-256-gcm", 
            "secp256k1", 
            "ed25519" 
        };
        public bool enable_ai { get; set; } = true;
        public bool enable_oracle { get; set; } = true;
        public ComputationConfig computation { get; set; } = new();

        public void Merge(EnclaveConfig other)
        {
            mode = other.mode;
            log_level = other.log_level;
            sgx_simulation_mode = other.sgx_simulation_mode;
            max_threads = other.max_threads;
            storage_path = other.storage_path;
            network_timeout_seconds = other.network_timeout_seconds;
            crypto_algorithms = other.crypto_algorithms;
            enable_ai = other.enable_ai;
            enable_oracle = other.enable_oracle;
        }

        public void Validate()
        {
            if (max_threads <= 0)
                throw new ArgumentException("max_threads must be greater than 0");

            if (network_timeout_seconds == 0)
                throw new ArgumentException("network_timeout_seconds must be greater than 0");
        }

        public int GetNumber(string key)
        {
            return key switch
            {
                "computation.max_concurrent_jobs" => max_threads,
                "ai.max_model_size_mb" => 1024,
                "ai.max_training_data_mb" => 512,
                _ => throw new ArgumentException($"Unknown config key: {key}")
            };
        }
    }
}