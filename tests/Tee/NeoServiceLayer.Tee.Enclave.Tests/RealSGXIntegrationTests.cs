using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Enclave;
using Xunit;
using Xunit.Abstractions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;


namespace NeoServiceLayer.Tee.Enclave.Tests
{
    /// <summary>
    /// Integration tests that use the real SGX SDK through ProductionSGXEnclaveWrapper
    /// These tests will use Occlum LibOS when running in the Docker container
    /// </summary>
    [Collection("SGX Tests")]
    [Trait("Category", "SGXIntegration")]
    public class RealSGXIntegrationTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly ILogger<RealSGXIntegrationTests> _logger;
        private readonly NeoServiceLayer.Tee.Enclave.IEnclaveWrapper _enclave;
        private readonly bool _isRealSGXAvailable;

        public RealSGXIntegrationTests(ITestOutputHelper output)
        {
            _output = output;

            // Check if we're in CI environment first
            var isCI = Environment.GetEnvironmentVariable("CI") == "true" ||
                      Environment.GetEnvironmentVariable("TEST_ENVIRONMENT") == "CI";

            if (isCI)
            {
                _output.WriteLine("✅ CI environment detected - tests will be skipped");
                // Don't create any enclave wrapper in CI to avoid initialization errors
                _enclave = null;
                _isRealSGXAvailable = false;
                _logger = null;
                return;
            }

            // Create service provider for dependency injection
            var services = new ServiceCollection();
            services.AddLogging(builder => builder
                .SetMinimumLevel(LogLevel.Debug));

            var serviceProvider = services.BuildServiceProvider();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            _logger = loggerFactory.CreateLogger<RealSGXIntegrationTests>();

            // Always use ProductionSGXEnclaveWrapper for real SGX testing
            // This ensures we're always testing with the real SGX SDK, even in simulation mode
            _output.WriteLine("Using ProductionSGXEnclaveWrapper for real SGX SDK testing");
            var prodLogger = loggerFactory.CreateLogger<ProductionSGXEnclaveWrapper>();
            var occlumLogger = loggerFactory.CreateLogger<OcclumEnclaveWrapper>();
            var occlumWrapper = new OcclumEnclaveWrapper(occlumLogger);
            _enclave = new ProductionSGXEnclaveWrapper(occlumWrapper, prodLogger);

            // Check if we're running in an SGX environment (real or simulated)
            _isRealSGXAvailable = CheckRealSGXAvailability();
        }

        private bool CheckRealSGXAvailability()
        {
            // Check for SGX SDK environment variables
            var sgxMode = Environment.GetEnvironmentVariable("SGX_MODE");
            var sgxSdk = Environment.GetEnvironmentVariable("SGX_SDK");

            _output.WriteLine($"SGX_MODE: {sgxMode ?? "not set"}");
            _output.WriteLine($"SGX_SDK: {sgxSdk ?? "not set"}");

            // If SGX_MODE is set (even to SIM), we assume we're in an SGX environment
            return !string.IsNullOrEmpty(sgxMode) && !string.IsNullOrEmpty(sgxSdk);
        }

        [Fact(Skip = "Skipped in CI - SGX hardware tests require physical SGX hardware")]
        public void Initialize_WithRealSGX_ShouldSucceed()
        {
            // Always skip in CI environments for reliability
            var isCI = Environment.GetEnvironmentVariable("CI") == "true" ||
                      Environment.GetEnvironmentVariable("TEST_ENVIRONMENT") == "CI";
            if (isCI) return;

            // Arrange
            _output.WriteLine($"Running Initialize test with {(_isRealSGXAvailable ? "real SGX" : "simulation")}");

            // Act
            var result = _enclave.Initialize();

            // Assert
            result.Should().BeTrue();
            _output.WriteLine("✅ Enclave initialization successful");
        }

        [Fact(Skip = "Skipped in CI - SGX hardware tests require physical SGX hardware")]
        public void Encrypt_WithRealSGX_ShouldProduceValidCiphertext()
        {
            // Always skip in CI environments for reliability
            var isCI = Environment.GetEnvironmentVariable("CI") == "true" ||
                      Environment.GetEnvironmentVariable("TEST_ENVIRONMENT") == "CI";
            if (isCI) return;

            // Arrange
            _enclave.Initialize();
            var plaintext = "Test data for real SGX encryption";
            var data = Encoding.UTF8.GetBytes(plaintext);
            var key = _enclave.GenerateRandomBytes(32); // 256-bit key
            _output.WriteLine($"Encrypting: {plaintext}");

            // Act
            var encrypted = _enclave.Encrypt(data, key);

            // Assert
            encrypted.Should().NotBeNull();
            encrypted.Should().NotBeEmpty();
            encrypted.Should().NotBeEquivalentTo(data);
            _output.WriteLine($"✅ Encryption successful, ciphertext length: {encrypted.Length}");
        }

        [Fact(Skip = "Skipped in CI - SGX hardware tests require physical SGX hardware")]
        public void GenerateKey_WithRealSGX_ShouldProduceUniqueKeys()
        {
            // Always skip in CI environments for reliability
            var isCI = Environment.GetEnvironmentVariable("CI") == "true" ||
                      Environment.GetEnvironmentVariable("TEST_ENVIRONMENT") == "CI";
            if (isCI) return;

            // Arrange
            _enclave.Initialize();
            _output.WriteLine("Generating keys with real SGX...");

            // Act
            var key1Result = _enclave.GenerateKey("test-key-1", "Secp256k1", "Sign,Verify", false, "Test key 1");
            var key2Result = _enclave.GenerateKey("test-key-2", "Ed25519", "Sign,Verify", false, "Test key 2");

            // Assert
            key1Result.Should().NotBeNull();
            key2Result.Should().NotBeNull();
            key1Result.Should().NotBe(key2Result);
            _output.WriteLine($"✅ Generated unique keys with metadata");
            _output.WriteLine($"Key 1: {key1Result}");
            _output.WriteLine($"Key 2: {key2Result}");
        }

        [Fact(Skip = "Skipped in CI - SGX hardware tests require physical SGX hardware")]
        public void GetAttestationReport_WithRealSGX_ShouldReturnReport()
        {
            // Always skip in CI environments for reliability
            var isCI = Environment.GetEnvironmentVariable("CI") == "true" ||
                      Environment.GetEnvironmentVariable("TEST_ENVIRONMENT") == "CI";
            if (isCI) return;

            // Arrange
            _enclave.Initialize();
            _output.WriteLine("Requesting SGX attestation report...");

            // Act
            var report = _enclave.GetAttestationReport();

            // Assert
            report.Should().NotBeNull();
            report.Should().NotBeEmpty();
            _output.WriteLine($"✅ Attestation report generated");
            _output.WriteLine($"Report: {report}");

            // In simulation mode, the report will be simulated but still valid
            if (_isRealSGXAvailable)
            {
                _output.WriteLine("Note: Running in SGX simulation mode - report is simulated");
            }
        }

        [Fact(Skip = "Skipped in CI - SGX hardware tests require physical SGX hardware")]
        public void SecureStorage_WithRealSGX_ShouldPersistData()
        {
            // Always skip in CI environments for reliability
            var isCI = Environment.GetEnvironmentVariable("CI") == "true" ||
                      Environment.GetEnvironmentVariable("TEST_ENVIRONMENT") == "CI";
            if (isCI) return;

            // Arrange
            _enclave.Initialize();
            var storageKey = "secure-storage-test";
            var data = Encoding.UTF8.GetBytes("Sensitive data for SGX secure storage");
            var encryptionKey = "test-encryption-key-12345";
            _output.WriteLine($"Storing data with key: {storageKey}");

            // Act - Store
            var storeResult = _enclave.StoreData(storageKey, data, encryptionKey, true);
            storeResult.Should().NotBeNull();
            _output.WriteLine($"Store result: {storeResult}");

            // Act - Retrieve
            var retrieved = _enclave.RetrieveData(storageKey, encryptionKey);

            // Assert
            retrieved.Should().NotBeNull();
            retrieved.Should().BeEquivalentTo(data);
            _output.WriteLine("✅ Secure storage operations successful");
        }

        [Fact(Skip = "Skipped in CI - SGX hardware tests require physical SGX hardware")]
        public void ExecuteJavaScript_WithRealSGX_ShouldRunInEnclave()
        {
            // Always skip in CI environments for reliability
            var isCI = Environment.GetEnvironmentVariable("CI") == "true" ||
                      Environment.GetEnvironmentVariable("TEST_ENVIRONMENT") == "CI";
            if (isCI) return;

            // Arrange
            _enclave.Initialize();
            var script = "function add(a, b) { return a + b; } add(5, 3);";
            _output.WriteLine($"Executing JavaScript in enclave: {script}");

            // Act
            var result = _enclave.ExecuteJavaScript(script, "{}");

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("8");
            _output.WriteLine($"✅ JavaScript execution result: {result}");
        }

        [Fact(Skip = "Skipped in CI - SGX hardware tests require physical SGX hardware")]
        public void RandomGeneration_WithRealSGX_ShouldUseHardwareRNG()
        {
            // Always skip in CI environments for reliability
            var isCI = Environment.GetEnvironmentVariable("CI") == "true" ||
                      Environment.GetEnvironmentVariable("TEST_ENVIRONMENT") == "CI";
            if (isCI) return;

            // Arrange
            _enclave.Initialize();
            const int size = 32;
            _output.WriteLine($"Generating {size} random bytes using SGX...");

            // Act
            var random1 = _enclave.GenerateRandomBytes(size);
            var random2 = _enclave.GenerateRandomBytes(size);

            // Assert
            random1.Should().NotBeNull();
            random1.Should().HaveCount(size);
            random2.Should().NotBeNull();
            random2.Should().HaveCount(size);
            random1.Should().NotBeEquivalentTo(random2);
            _output.WriteLine($"✅ Generated unique random values using {(_isRealSGXAvailable ? "SGX hardware RNG" : "simulated RNG")}");
        }

        [Fact(Skip = "Skipped in CI - SGX hardware tests require physical SGX hardware")]
        public void CompleteWorkflow_WithRealSGX_ShouldSucceed()
        {
            // Always skip in CI environments for reliability
            var isCI = Environment.GetEnvironmentVariable("CI") == "true" ||
                      Environment.GetEnvironmentVariable("TEST_ENVIRONMENT") == "CI";
            if (isCI) return;

            // This test combines multiple operations to verify the real SGX integration
            _output.WriteLine("Running complete SGX workflow test...");

            // Initialize
            var initResult = _enclave.Initialize();
            initResult.Should().BeTrue();
            _output.WriteLine("✅ Step 1: Initialization complete");

            // Generate key
            var keyResult = _enclave.GenerateKey("workflow-key", "AES256", "Encrypt,Decrypt", false, "Workflow encryption key");
            keyResult.Should().NotBeNull();
            _output.WriteLine("✅ Step 2: Key generation complete");

            // Generate encryption key
            var encKey = _enclave.GenerateRandomBytes(32);

            // Encrypt data
            var plaintext = Encoding.UTF8.GetBytes("Workflow test data");
            var ciphertext = _enclave.Encrypt(plaintext, encKey);
            ciphertext.Should().NotBeNull();
            _output.WriteLine("✅ Step 3: Encryption complete");

            // Store encrypted data
            var stored = _enclave.StoreData("workflow-data", ciphertext, "workflow-storage-key", true);
            stored.Should().NotBeNull();
            _output.WriteLine("✅ Step 4: Secure storage complete");

            // Retrieve and decrypt
            var retrieved = _enclave.RetrieveData("workflow-data", "workflow-storage-key");
            var decrypted = _enclave.Decrypt(retrieved, encKey);
            decrypted.Should().BeEquivalentTo(plaintext);
            _output.WriteLine("✅ Step 5: Retrieval and decryption complete");

            // Get attestation
            var report = _enclave.GetAttestationReport();
            report.Should().NotBeNull();
            _output.WriteLine("✅ Step 6: Attestation report generated");

            // Test sealing/unsealing (SGX specific operations)
            var sealedData = _enclave.SealData(plaintext);
            var unsealedData = _enclave.UnsealData(sealedData);
            unsealedData.Should().BeEquivalentTo(plaintext);
            _output.WriteLine("✅ Step 7: Data sealing/unsealing complete");

            _output.WriteLine("✅ Complete workflow test passed!");
        }

        public void Dispose()
        {
            _enclave?.Dispose();
            _output.WriteLine("Test cleanup completed");
        }
    }
}
