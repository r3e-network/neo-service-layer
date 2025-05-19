using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    /// <summary>
    /// Tests for the OpenEnclaveInterface.
    /// </summary>
    [Collection("SimulationMode")]
    public class OpenEnclaveInterfaceTests
    {
        private readonly SimulationModeFixture _fixture;
        private readonly IOpenEnclaveInterface _oeInterface;
        private readonly ILogger<OpenEnclaveInterfaceTests> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenEnclaveInterfaceTests"/> class.
        /// </summary>
        /// <param name="fixture">The simulation mode fixture.</param>
        public OpenEnclaveInterfaceTests(SimulationModeFixture fixture)
        {
            _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
            _oeInterface = _fixture.OpenEnclaveInterface;
            _logger = _fixture.LoggerFactory.CreateLogger<OpenEnclaveInterfaceTests>();
            
            // Skip tests if the enclave could not be created
            Skip.If(_oeInterface == null, "Failed to create Open Enclave interface");
        }

        [Fact]
        public void GetEnclaveId_ReturnsNonZeroId()
        {
            // Act
            var enclaveId = _oeInterface.GetEnclaveId();

            // Assert
            Assert.NotEqual(IntPtr.Zero, enclaveId);
            _logger.LogInformation("Enclave ID: {EnclaveId}", enclaveId);
        }

        [Fact]
        public void GetMrEnclave_ReturnsNonEmptyValue()
        {
            // Act
            var mrEnclave = _oeInterface.GetMrEnclave();

            // Assert
            Assert.NotNull(mrEnclave);
            Assert.Equal(32, mrEnclave.Length);
            _logger.LogInformation("MRENCLAVE: {MrEnclave}", Convert.ToBase64String(mrEnclave));
        }

        [Fact]
        public void GetMrSigner_ReturnsNonEmptyValue()
        {
            // Act
            var mrSigner = _oeInterface.GetMrSigner();

            // Assert
            Assert.NotNull(mrSigner);
            Assert.Equal(32, mrSigner.Length);
            _logger.LogInformation("MRSIGNER: {MrSigner}", Convert.ToBase64String(mrSigner));
        }

        [Fact]
        public void GetRandomBytes_ReturnsRandomData()
        {
            // Arrange
            const int length = 32;

            // Act
            var randomBytes1 = _oeInterface.GetRandomBytes(length);
            var randomBytes2 = _oeInterface.GetRandomBytes(length);

            // Assert
            Assert.NotNull(randomBytes1);
            Assert.NotNull(randomBytes2);
            Assert.Equal(length, randomBytes1.Length);
            Assert.Equal(length, randomBytes2.Length);
            Assert.NotEqual(randomBytes1, randomBytes2); // This could theoretically fail, but it's extremely unlikely
            _logger.LogInformation("Random bytes 1: {RandomBytes1}", Convert.ToBase64String(randomBytes1));
            _logger.LogInformation("Random bytes 2: {RandomBytes2}", Convert.ToBase64String(randomBytes2));
        }

        [Fact]
        public void SealAndUnsealData_WorksCorrectly()
        {
            // Arrange
            var originalData = Encoding.UTF8.GetBytes("This is a test message to seal and unseal.");

            // Act
            var sealedData = _oeInterface.SealData(originalData);
            var unsealedData = _oeInterface.UnsealData(sealedData);

            // Assert
            Assert.NotNull(sealedData);
            Assert.NotNull(unsealedData);
            Assert.NotEqual(originalData.Length, sealedData.Length);
            Assert.Equal(originalData.Length, unsealedData.Length);
            Assert.Equal(originalData, unsealedData);
            _logger.LogInformation("Original data: {OriginalData}", Encoding.UTF8.GetString(originalData));
            _logger.LogInformation("Sealed data length: {SealedDataLength}", sealedData.Length);
            _logger.LogInformation("Unsealed data: {UnsealedData}", Encoding.UTF8.GetString(unsealedData));
        }

        [Fact]
        public void SignAndVerifyData_WorksCorrectly()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes("This is a test message to sign and verify.");

            // Act
            var signature = _oeInterface.SignData(data);
            var isValid = _oeInterface.VerifySignature(data, signature);

            // Assert
            Assert.NotNull(signature);
            Assert.True(isValid);
            _logger.LogInformation("Data: {Data}", Encoding.UTF8.GetString(data));
            _logger.LogInformation("Signature length: {SignatureLength}", signature.Length);
        }

        [Fact]
        public void GetAttestationReport_ReturnsValidReport()
        {
            // Arrange
            var reportData = Encoding.UTF8.GetBytes("This is a test report data.");

            // Act
            var report = _oeInterface.GetAttestationReport(reportData);

            // Assert
            Assert.NotNull(report);
            Assert.True(report.Length > 0);
            _logger.LogInformation("Report length: {ReportLength}", report.Length);
        }

        [Fact]
        public async Task InitializeOcclumAsync_DoesNotThrow()
        {
            // Arrange
            var instanceDir = "/tmp/occlum_instance";
            var logLevel = "info";

            // Act & Assert
            await _oeInterface.InitializeOcclumAsync(instanceDir, logLevel);
            _logger.LogInformation("Occlum initialized successfully");
        }

        [Fact]
        public async Task ExecuteOcclumCommandAsync_ReturnsZeroExitCode()
        {
            // Arrange
            var path = "/bin/echo";
            var args = new[] { "Hello, Occlum!" };
            var env = new[] { "PATH=/bin:/usr/bin" };

            // Act
            await _oeInterface.InitializeOcclumAsync("/tmp/occlum_instance", "info");
            var exitCode = await _oeInterface.ExecuteOcclumCommandAsync(path, args, env);

            // Assert
            Assert.Equal(0, exitCode);
            _logger.LogInformation("Command executed successfully with exit code: {ExitCode}", exitCode);
        }

        [Fact]
        public void GetOpenEnclaveVersion_ReturnsNonEmptyString()
        {
            // Act
            var version = _oeInterface.GetOpenEnclaveVersion();

            // Assert
            Assert.NotNull(version);
            Assert.NotEmpty(version);
            _logger.LogInformation("Open Enclave version: {Version}", version);
        }

        [Fact]
        public void IsOcclumSupportEnabled_ReturnsTrue()
        {
            // Act
            var isEnabled = _oeInterface.IsOcclumSupportEnabled();

            // Assert
            Assert.True(isEnabled);
            _logger.LogInformation("Occlum support is enabled: {IsEnabled}", isEnabled);
        }

        [Fact]
        public void GetEnclaveConfiguration_ReturnsValidJson()
        {
            // Act
            var configuration = _oeInterface.GetEnclaveConfiguration();

            // Assert
            Assert.NotNull(configuration);
            Assert.NotEmpty(configuration);
            var configObject = JsonDocument.Parse(configuration);
            Assert.NotNull(configObject);
            _logger.LogInformation("Enclave configuration: {Configuration}", configuration);
        }

        [Fact]
        public async Task UpdateEnclaveConfigurationAsync_DoesNotThrow()
        {
            // Arrange
            var configuration = "{\"HeapSize\": \"2048MB\", \"StackSize\": \"128KB\"}";

            // Act & Assert
            await _oeInterface.UpdateEnclaveConfigurationAsync(configuration);
            var updatedConfig = _oeInterface.GetEnclaveConfiguration();
            _logger.LogInformation("Updated enclave configuration: {Configuration}", updatedConfig);
        }

        [Fact]
        public async Task StoreAndGetUserSecretAsync_WorksCorrectly()
        {
            // Arrange
            var userId = "test_user";
            var secretName = "api_key";
            var secretValue = "test_api_key_value";

            // Act
            await _oeInterface.StoreUserSecretAsync(userId, secretName, secretValue);
            var retrievedSecret = await _oeInterface.GetUserSecretAsync(userId, secretName);

            // Assert
            Assert.Equal(secretValue, retrievedSecret);
            _logger.LogInformation("Secret stored and retrieved successfully");
        }

        [Fact]
        public async Task DeleteUserSecretAsync_RemovesSecret()
        {
            // Arrange
            var userId = "test_user";
            var secretName = "temp_secret";
            var secretValue = "temp_secret_value";

            // Act
            await _oeInterface.StoreUserSecretAsync(userId, secretName, secretValue);
            await _oeInterface.DeleteUserSecretAsync(userId, secretName);
            var retrievedSecret = await _oeInterface.GetUserSecretAsync(userId, secretName);

            // Assert
            Assert.NotEqual(secretValue, retrievedSecret);
            _logger.LogInformation("Secret deleted successfully");
        }

        [Fact]
        public async Task ExecuteJavaScriptAsync_SimpleFunction_ReturnsCorrectResult()
        {
            // Arrange
            var code = @"
                function process(input) {
                    return { result: input.value * 2 };
                }
                process(input);
            ";
            var input = "{\"value\": 42}";
            var secrets = "{}";
            var functionId = "simple_function_test";
            var userId = "test_user";

            // Act
            var result = await _oeInterface.ExecuteJavaScriptAsync(code, input, secrets, functionId, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("\"result\"", result);
            Assert.Contains("84", result);
            _logger.LogInformation("JavaScript result: {Result}", result);
        }

        [Fact]
        public void RecordExecutionMetricsAsync_DoesNotThrow()
        {
            // Arrange
            var functionId = "test_function";
            var userId = "test_user";
            var gasUsed = 1000L;

            // Act & Assert
            _oeInterface.RecordExecutionMetricsAsync(functionId, userId, gasUsed);
            _logger.LogInformation("Execution metrics recorded successfully");
        }

        [Fact]
        public void RecordExecutionFailureAsync_DoesNotThrow()
        {
            // Arrange
            var functionId = "test_function";
            var userId = "test_user";
            var errorMessage = "Test error message";

            // Act & Assert
            _oeInterface.RecordExecutionFailureAsync(functionId, userId, errorMessage);
            _logger.LogInformation("Execution failure recorded successfully");
        }
    }
}
