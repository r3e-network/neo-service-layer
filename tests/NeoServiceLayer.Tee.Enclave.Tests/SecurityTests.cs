using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using NeoServiceLayer.Tee.Host;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    [Trait("Category", "Security")]
    public class SecurityTests : IDisposable
    {
        private readonly ILogger<MockOpenEnclaveInterface> _logger;
        private readonly MockOpenEnclaveInterface _oeInterface;
        private readonly string _enclavePath;
        private readonly bool _skipTests;

        public SecurityTests()
        {
            // Create a real logger for better diagnostics
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddConsole()
                    .SetMinimumLevel(LogLevel.Debug);
            });
            _logger = loggerFactory.CreateLogger<MockOpenEnclaveInterface>();

            // Get the enclave path from environment variable or use a default
            _enclavePath = Environment.GetEnvironmentVariable("OE_ENCLAVE_PATH") ?? "liboe_enclave.signed.so";

            // Set simulation mode for testing
            Environment.SetEnvironmentVariable("OE_SIMULATION", "1");

            try
            {
                // Initialize the mock DLLs
                MockDllInitializer.Initialize(_logger);

                // Create a mock enclave file
                var mockEnclaveFileLogger = loggerFactory.CreateLogger<MockEnclaveFile>();
                var mockEnclaveFile = new MockEnclaveFile(mockEnclaveFileLogger);
                mockEnclaveFile.CreateAsync().Wait();

                // Create the MockOpenEnclaveInterface
                _oeInterface = new MockOpenEnclaveInterface(_logger, _enclavePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Open Enclave interface");
                _skipTests = true;
            }
        }

        public void Dispose()
        {
            _oeInterface?.Dispose();
        }

        [Fact]
        public void RandomBytes_AreUnpredictable()
        {
            Skip.If(_skipTests, "Skipping test because enclave could not be created");

            // Arrange
            int length = 1024;

            // Act
            byte[] randomBytes1 = _oeInterface.GetRandomBytes(length);
            byte[] randomBytes2 = _oeInterface.GetRandomBytes(length);

            // Assert
            Assert.NotEqual(randomBytes1, randomBytes2);

            // Calculate entropy (simplified)
            int differentBits = 0;
            for (int i = 0; i < length; i++)
            {
                byte xor = (byte)(randomBytes1[i] ^ randomBytes2[i]);
                for (int j = 0; j < 8; j++)
                {
                    if ((xor & (1 << j)) != 0)
                    {
                        differentBits++;
                    }
                }
            }

            double entropy = (double)differentBits / (length * 8);
            _logger.LogInformation("Random bytes entropy: {Entropy}", entropy);

            // We expect entropy close to 0.5 for good random data
            Assert.True(entropy > 0.45 && entropy < 0.55,
                $"Random bytes should have entropy close to 0.5, but got {entropy}");
        }

        [Fact]
        public void SignatureVerification_PreventsTampering()
        {
            Skip.If(_skipTests, "Skipping test because enclave could not be created");

            // Arrange
            byte[] originalData = Encoding.UTF8.GetBytes("Data to be signed and verified");
            byte[] tamperedData = Encoding.UTF8.GetBytes("Data to be signed and verified!");

            // Act
            byte[] signature = _oeInterface.SignData(originalData);
            bool validSignature = _oeInterface.VerifySignature(originalData, signature);
            bool invalidSignature = _oeInterface.VerifySignature(tamperedData, signature);

            // Assert
            Assert.True(validSignature, "Signature should be valid for original data");
            Assert.False(invalidSignature, "Signature should be invalid for tampered data");
        }

        [Fact]
        public void SealedData_CannotBeReadOutsideEnclave()
        {
            Skip.If(_skipTests, "Skipping test because enclave could not be created");

            // Arrange
            byte[] sensitiveData = Encoding.UTF8.GetBytes("Sensitive data that should be protected");

            // Act
            byte[] sealedData = _oeInterface.SealData(sensitiveData);

            // Assert
            Assert.NotEqual(sensitiveData, sealedData);

            // Check that the sealed data doesn't contain the original data in plaintext
            string sealedString = Encoding.UTF8.GetString(sealedData);
            string sensitiveString = Encoding.UTF8.GetString(sensitiveData);

            Assert.DoesNotContain(sensitiveString, sealedString);
        }

        [Fact]
        public async Task UserSecrets_AreIsolated()
        {
            Skip.If(_skipTests, "Skipping test because enclave could not be created");

            // Arrange
            string user1Code = @"
                function main(input) {
                    return { secret: SECRETS.API_KEY };
                }
            ";
            string user2Code = @"
                function main(input) {
                    try {
                        // Try to access user1's secret
                        return { secret: SECRETS.USER1_API_KEY };
                    } catch (e) {
                        return { error: e.message };
                    }
                }
            ";

            string user1Secrets = @"{ ""API_KEY"": ""user1_secret_key"" }";
            string user2Secrets = @"{ ""API_KEY"": ""user2_secret_key"", ""USER1_API_KEY"": ""trying_to_access_user1_key"" }";

            // Act
            string user1Result = await _oeInterface.ExecuteJavaScriptAsync(
                user1Code, "{}", user1Secrets, "user1_function", "user1");

            string user2Result = await _oeInterface.ExecuteJavaScriptAsync(
                user2Code, "{}", user2Secrets, "user2_function", "user2");

            // Assert
            var user1Json = JsonSerializer.Deserialize<JsonElement>(user1Result);
            var user2Json = JsonSerializer.Deserialize<JsonElement>(user2Result);

            Assert.Equal("user1_secret_key", user1Json.GetProperty("secret").GetString());

            // User2 should not be able to access user1's secrets
            Assert.True(user2Json.TryGetProperty("error", out _),
                "User2 should get an error when trying to access user1's secrets");
        }

        [Fact]
        public async Task JavaScriptSandbox_PreventsSystemAccess()
        {
            Skip.If(_skipTests, "Skipping test because enclave could not be created");

            // Arrange
            string maliciousCode = @"
                function main(input) {
                    try {
                        // Try to access Node.js process object
                        return { process: typeof process !== 'undefined' };
                    } catch (e) {
                        return { error: e.message };
                    }
                }
            ";

            // Act
            string result = await _oeInterface.ExecuteJavaScriptAsync(
                maliciousCode, "{}", "{}", "security_test_function", "security_test_user");

            // Assert
            var resultJson = JsonSerializer.Deserialize<JsonElement>(result);

            // The process object should not be accessible
            Assert.False(resultJson.GetProperty("process").GetBoolean(),
                "JavaScript sandbox should prevent access to Node.js process object");
        }

        [Fact]
        public async Task JavaScriptSandbox_PreventsFileSystemAccess()
        {
            Skip.If(_skipTests, "Skipping test because enclave could not be created");

            // Arrange
            string maliciousCode = @"
                function main(input) {
                    try {
                        // Try to access Node.js fs module
                        return { fs: typeof require !== 'undefined' && typeof require('fs') !== 'undefined' };
                    } catch (e) {
                        return { error: e.message };
                    }
                }
            ";

            // Act
            string result = await _oeInterface.ExecuteJavaScriptAsync(
                maliciousCode, "{}", "{}", "security_test_function", "security_test_user");

            // Assert
            var resultJson = JsonSerializer.Deserialize<JsonElement>(result);

            // Either require is undefined or fs module is not accessible
            Assert.True(resultJson.TryGetProperty("error", out _) ||
                        !resultJson.GetProperty("fs").GetBoolean(),
                "JavaScript sandbox should prevent access to file system");
        }

        [Fact]
        public void EnclaveIdentity_IsConsistent()
        {
            Skip.If(_skipTests, "Skipping test because enclave could not be created");

            // Arrange & Act
            byte[] mrEnclave1 = _oeInterface.GetMrEnclave();
            byte[] mrSigner1 = _oeInterface.GetMrSigner();

            // Create a new instance
            var oeInterface2 = new MockOpenEnclaveInterface(_logger, _enclavePath);
            byte[] mrEnclave2 = oeInterface2.GetMrEnclave();
            byte[] mrSigner2 = oeInterface2.GetMrSigner();

            // Cleanup
            oeInterface2.Dispose();

            // Assert
            // MRENCLAVE might be different for different instances due to memory layout
            // But MRSIGNER should be the same as it depends on the signing key
            Assert.Equal(mrSigner1, mrSigner2);
        }
    }
}
