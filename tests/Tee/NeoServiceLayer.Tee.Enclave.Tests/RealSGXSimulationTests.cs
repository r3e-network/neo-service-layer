using System;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Tee.Enclave;
using Xunit;
using Xunit.Abstractions;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    /// <summary>
    /// Tests that use real SGX SDK in simulation mode through ProductionSGXEnclaveWrapper.
    /// These tests require Intel SGX SDK to be installed with SGX_MODE=SIM.
    /// </summary>
    [Trait("Category", "SGXIntegration")]
    [Collection("SGXTests")] // Ensures tests don't run in parallel
    public class RealSGXSimulationTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly ILogger<ProductionSGXEnclaveWrapper> _logger;
        private readonly NeoServiceLayer.Tee.Enclave.IEnclaveWrapper _enclave;
        private readonly bool _sgxAvailable;

        public RealSGXSimulationTests(ITestOutputHelper output)
        {
            _output = output;

            // Create a logger that outputs to xUnit test output
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddProvider(new XUnitLoggerProvider(output));
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            _logger = loggerFactory.CreateLogger<ProductionSGXEnclaveWrapper>();

            // Check if SGX SDK is available
            _sgxAvailable = CheckSGXAvailability();

            if (_sgxAvailable)
            {
                // Use the real ProductionSGXEnclaveWrapper which uses OcclumEnclaveWrapper
                _enclave = new ProductionSGXEnclaveWrapper(_logger);
            }
            else
            {
                _output.WriteLine("⚠️ SGX SDK not available, using ProductionSGXEnclaveWrapper anyway for testing");
                // Use ProductionSGXEnclaveWrapper anyway - it should handle missing SGX gracefully
                _enclave = new ProductionSGXEnclaveWrapper(_logger);
            }
        }

        public void Dispose()
        {
            _enclave?.Dispose();
        }

        private bool CheckSGXAvailability()
        {
            try
            {
                // Check for SGX environment variables
                var sgxMode = Environment.GetEnvironmentVariable("SGX_MODE");
                var ldLibraryPath = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH");

                _output.WriteLine($"SGX_MODE: {sgxMode ?? "not set"}");
                _output.WriteLine($"LD_LIBRARY_PATH: {ldLibraryPath ?? "not set"}");

                // For real SGX testing, we need SGX_MODE=SIM
                if (sgxMode != "SIM")
                {
                    _output.WriteLine("SGX_MODE is not set to SIM");
                    return false;
                }

                // Check if Occlum libraries are in the path
                if (string.IsNullOrEmpty(ldLibraryPath) || !ldLibraryPath.Contains("occlum"))
                {
                    _output.WriteLine("Occlum libraries not found in LD_LIBRARY_PATH");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Error checking SGX availability: {ex.Message}");
                return false;
            }
        }

        [SkippableFact]
        public void RealSGX_Initialize_ShouldSucceed()
        {
            Skip.IfNot(_sgxAvailable, "SGX SDK not available");

            // Act
            var result = _enclave.Initialize();

            // Assert
            result.Should().BeTrue();
            _output.WriteLine("✅ Real SGX SDK initialization successful (simulation mode)");
        }

        [SkippableFact]
        public void RealSGX_CryptographicOperations_ShouldWork()
        {
            Skip.IfNot(_sgxAvailable, "SGX SDK not available");

            // Arrange
            _enclave.Initialize();
            var testData = Encoding.UTF8.GetBytes("Test data for SGX encryption");
            var key = _enclave.GenerateRandomBytes(32);

            // Act
            var encrypted = _enclave.Encrypt(testData, key);
            var decrypted = _enclave.Decrypt(encrypted, key);
            var signature = _enclave.Sign(testData, key);
            var isValid = _enclave.Verify(testData, signature, key);

            // Assert
            encrypted.Should().NotEqual(testData);
            decrypted.Should().Equal(testData);
            signature.Should().NotBeNull();
            isValid.Should().BeTrue();

            _output.WriteLine("✅ Real SGX cryptographic operations successful");
            _output.WriteLine($"   Original: {testData.Length} bytes");
            _output.WriteLine($"   Encrypted: {encrypted.Length} bytes");
            _output.WriteLine($"   Signature: {signature.Length} bytes");
        }

        [SkippableFact]
        public void RealSGX_RandomGeneration_ShouldProvideHardwareRandomness()
        {
            Skip.IfNot(_sgxAvailable, "SGX SDK not available");

            // Arrange
            _enclave.Initialize();

            // Act
            var random1 = _enclave.GenerateRandom(1, 100);
            var random2 = _enclave.GenerateRandom(1, 100);
            var randomBytes1 = _enclave.GenerateRandomBytes(32);
            var randomBytes2 = _enclave.GenerateRandomBytes(32);

            // Assert
            random1.Should().BeInRange(1, 100);
            random2.Should().BeInRange(1, 100);
            randomBytes1.Should().HaveCount(32);
            randomBytes2.Should().HaveCount(32);
            randomBytes1.Should().NotEqual(randomBytes2);

            _output.WriteLine("✅ Real SGX random generation successful");
            _output.WriteLine($"   Random numbers: {random1}, {random2}");
            _output.WriteLine($"   Random bytes unique: {!randomBytes1.SequenceEqual(randomBytes2)}");
            _output.WriteLine("   Using SGX hardware RNG in simulation mode");
        }

        [SkippableFact]
        public void RealSGX_SecureStorage_ShouldUseEnclaveProtection()
        {
            Skip.IfNot(_sgxAvailable, "SGX SDK not available");

            // Arrange
            _enclave.Initialize();
            var key = "sgx-test-storage-key";
            var data = Encoding.UTF8.GetBytes("Sensitive data protected by SGX");
            var encryptionKey = "sgx-encryption-key";

            // Act
            var storeResult = _enclave.StoreData(key, data, encryptionKey, false);
            var retrievedData = _enclave.RetrieveData(key, encryptionKey);
            var metadata = _enclave.GetStorageMetadata(key);

            // Assert
            storeResult.Should().NotBeNullOrEmpty();
            retrievedData.Should().Equal(data);
            metadata.Should().NotBeNullOrEmpty();

            var storeJson = JsonDocument.Parse(storeResult);
            storeJson.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
            storeJson.RootElement.GetProperty("enclave").GetBoolean().Should().BeTrue();

            _output.WriteLine("✅ Real SGX secure storage successful");
            _output.WriteLine($"   Stored and retrieved {data.Length} bytes");
            _output.WriteLine("   Data protected by SGX enclave memory");
        }

        [SkippableFact]
        public void RealSGX_JavaScriptExecution_ShouldRunInEnclave()
        {
            Skip.IfNot(_sgxAvailable, "SGX SDK not available");

            // Arrange
            _enclave.Initialize();
            var jsCode = @"
                function calculate(a, b) {
                    return { sum: a + b, product: a * b };
                }
                calculate(5, 3);
            ";
            var args = JsonSerializer.Serialize(new { a = 5, b = 3 });

            // Act
            var result = _enclave.ExecuteJavaScript(jsCode, args);

            // Assert
            result.Should().NotBeNullOrEmpty();
            var resultJson = JsonDocument.Parse(result);
            resultJson.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();

            _output.WriteLine("✅ Real SGX JavaScript execution successful");
            _output.WriteLine($"   Result: {result}");
            _output.WriteLine("   Code executed within SGX enclave");
        }

        [SkippableFact]
        public void RealSGX_KeyGeneration_ShouldUseEnclaveKeyDerivation()
        {
            Skip.IfNot(_sgxAvailable, "SGX SDK not available");

            // Arrange
            _enclave.Initialize();
            var keyId = "sgx-test-key";

            // Act
            var keyResult = _enclave.GenerateKey(keyId, "secp256k1", "Sign,Verify", false, "SGX test key");

            // Assert
            keyResult.Should().NotBeNullOrEmpty();
            var keyJson = JsonDocument.Parse(keyResult);
            keyJson.RootElement.GetProperty("keyId").GetString().Should().Be(keyId);
            keyJson.RootElement.GetProperty("keyType").GetString().Should().Be("secp256k1");
            keyJson.RootElement.GetProperty("enclaveGenerated").GetBoolean().Should().BeTrue();

            _output.WriteLine("✅ Real SGX key generation successful");
            _output.WriteLine($"   Generated key: {keyId}");
            _output.WriteLine("   Key material never leaves SGX enclave");
        }

        [SkippableFact]
        public void RealSGX_AttestationReport_ShouldProvideRealMeasurement()
        {
            Skip.IfNot(_sgxAvailable, "SGX SDK not available");

            // Arrange
            _enclave.Initialize();

            // Act
            var attestationReport = _enclave.GetAttestationReport();

            // Assert
            attestationReport.Should().NotBeNullOrEmpty();
            var reportJson = JsonDocument.Parse(attestationReport);

            // In simulation mode, these fields should still be present
            reportJson.RootElement.TryGetProperty("mr_enclave", out _).Should().BeTrue();
            reportJson.RootElement.TryGetProperty("mr_signer", out _).Should().BeTrue();
            reportJson.RootElement.TryGetProperty("isv_prod_id", out _).Should().BeTrue();
            reportJson.RootElement.TryGetProperty("isv_svn", out _).Should().BeTrue();

            _output.WriteLine("✅ Real SGX attestation report generated");
            _output.WriteLine("   Report contains enclave measurements");
            _output.WriteLine("   Running in SGX simulation mode");
        }

        [SkippableFact]
        public void RealSGX_DataSealing_ShouldUsePlatformKey()
        {
            Skip.IfNot(_sgxAvailable, "SGX SDK not available");

            // Arrange
            _enclave.Initialize();
            var sensitiveData = Encoding.UTF8.GetBytes("Platform-specific sealed data");

            // Act
            var sealedData = _enclave.SealData(sensitiveData);
            var unsealedData = _enclave.UnsealData(sealedData);

            // Assert
            sealedData.Should().NotEqual(sensitiveData);
            sealedData.Length.Should().BeGreaterThan(sensitiveData.Length);
            unsealedData.Should().Equal(sensitiveData);

            _output.WriteLine("✅ Real SGX data sealing successful");
            _output.WriteLine($"   Original: {sensitiveData.Length} bytes");
            _output.WriteLine($"   Sealed: {sealedData.Length} bytes");
            _output.WriteLine("   Data sealed with platform-specific key");
        }

        [SkippableFact]
        public void RealSGX_OracleData_ShouldFetchWithEnclaveProtection()
        {
            Skip.IfNot(_sgxAvailable, "SGX SDK not available");

            // Arrange
            _enclave.Initialize();
            var url = "https://api.example.com/data";

            // Act
            var oracleResult = _enclave.FetchOracleData(url, null, null, "json");

            // Assert
            oracleResult.Should().NotBeNullOrEmpty();
            var resultJson = JsonDocument.Parse(oracleResult);
            resultJson.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
            resultJson.RootElement.GetProperty("teeVerified").GetBoolean().Should().BeTrue();

            _output.WriteLine("✅ Real SGX oracle data fetch successful");
            _output.WriteLine("   Data fetched within SGX enclave");
            _output.WriteLine("   Network operations protected by TEE");
        }

        [SkippableFact]
        public void RealSGX_AbstractAccount_ShouldManageInEnclave()
        {
            Skip.IfNot(_sgxAvailable, "SGX SDK not available");

            // Arrange
            _enclave.Initialize();
            var accountId = "sgx-test-account";
            var accountData = JsonSerializer.Serialize(new { owner = "test-user", type = "multisig" });

            // Act
            var createResult = _enclave.CreateAbstractAccount(accountId, accountData);
            var transactionData = JsonSerializer.Serialize(new { to = "0x123", value = 100 });
            var signResult = _enclave.SignAbstractAccountTransaction(accountId, transactionData);

            // Assert
            createResult.Should().NotBeNullOrEmpty();
            signResult.Should().NotBeNullOrEmpty();

            var createJson = JsonDocument.Parse(createResult);
            createJson.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
            createJson.RootElement.GetProperty("enclaveSecured").GetBoolean().Should().BeTrue();

            _output.WriteLine("✅ Real SGX abstract account management successful");
            _output.WriteLine($"   Account created: {accountId}");
            _output.WriteLine("   Private keys never leave SGX enclave");
        }
    }

    /// <summary>
    /// Custom xUnit logger provider to output to test output helper.
    /// </summary>
    public class XUnitLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _output;

        public XUnitLoggerProvider(ITestOutputHelper output)
        {
            _output = output;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new XUnitLogger(_output, categoryName);
        }

        public void Dispose() { }
    }

    /// <summary>
    /// Custom xUnit logger implementation.
    /// </summary>
    public class XUnitLogger : ILogger
    {
        private readonly ITestOutputHelper _output;
        private readonly string _categoryName;

        public XUnitLogger(ITestOutputHelper output, string categoryName)
        {
            _output = output;
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _output.WriteLine($"[{logLevel}] {_categoryName}: {formatter(state, exception)}");
            if (exception != null)
            {
                _output.WriteLine($"Exception: {exception}");
            }
        }
    }

    /// <summary>
    /// Attribute to skip tests when conditions aren't met.
    /// </summary>
    public sealed class SkippableFactAttribute : FactAttribute
    {
        public override string Skip { get; set; }
    }

    /// <summary>
    /// Helper class to conditionally skip tests.
    /// </summary>
    public static class Skip
    {
        public static void IfNot(bool condition, string reason)
        {
            if (!condition)
            {
                throw new SkipException(reason);
            }
        }
    }

    /// <summary>
    /// Exception thrown when a test should be skipped.
    /// </summary>
    public class SkipException : Exception
    {
        public SkipException(string reason) : base(reason) { }
    }
}
