using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Tee.Enclave;
using Xunit;
using Xunit.Abstractions;

namespace NeoServiceLayer.Tee.Enclave.Tests
{
    [Trait("Category", "Occlum")]
    [Collection("SimulationMode")]
    public class OcclumEnclaveTests
    {
        private readonly SimulationModeFixture _fixture;
        private readonly ITestOutputHelper _output;
        private readonly ILogger _logger;

        public OcclumEnclaveTests(SimulationModeFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
            
            // Create a logger that writes to the test output
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new XunitLoggerProvider(_output));
            _logger = loggerFactory.CreateLogger<OcclumEnclaveTests>();
            
            _logger.LogInformation("Test initialized with {UsingRealSdk}", 
                _fixture.UsingRealSdk ? "real SDK" : "mock implementation");
        }

        [Fact]
        public void OcclumEnclave_ShouldInitializeSuccessfully()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange & Act
            var enclaveId = _fixture.OcclumInterface.EnclaveId;
            
            // Assert
            Assert.NotEqual(IntPtr.Zero, enclaveId);
            _logger.LogInformation("Enclave ID: {EnclaveId}", enclaveId);
        }

        [Fact]
        public void OcclumEnclave_ShouldReturnMeasurements()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping test because we're not using the real SDK");
                return;
            }

            // Arrange & Act
            var mrEnclave = _fixture.OcclumInterface.MrEnclave;
            var mrSigner = _fixture.OcclumInterface.MrSigner;
            
            // Assert
            Assert.NotNull(mrEnclave);
            Assert.NotNull(mrSigner);
            Assert.True(mrEnclave.Length > 0, "MRENCLAVE should not be empty");
            Assert.True(mrSigner.Length > 0, "MRSIGNER should not be empty");
            
            _logger.LogInformation("MRENCLAVE: {MrEnclave}", Convert.ToBase64String(mrEnclave));
            _logger.LogInformation("MRSIGNER: {MrSigner}", Convert.ToBase64String(mrSigner));
        }

        [Fact]
        public async Task OcclumEnclave_ShouldExecuteJavaScript()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping JavaScript execution test because we're not using the real SDK");
                return;
            }

            // Arrange
            string jsCode = @"
                function main(input) {
                    return { result: 'Hello, ' + input.name };
                }
            ";
            string input = @"{ ""name"": ""World"" }";
            string secrets = @"{}";
            string functionId = "test-function";
            string userId = "test-user";

            // Act
            var result = await _fixture.OcclumInterface.ExecuteJavaScriptAsync(
                jsCode, input, secrets, functionId, userId);
            
            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success, "JavaScript execution should succeed");
            Assert.Contains("Hello, World", result.Result);
            
            _logger.LogInformation("JavaScript execution result: {Result}", result.Result);
            _logger.LogInformation("Gas used: {GasUsed}", result.GasUsed);
        }

        [Fact]
        public void OcclumEnclave_ShouldGenerateRandomBytes()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping random bytes test because we're not using the real SDK");
                return;
            }

            // Arrange
            int length = 32;
            
            // Act
            byte[] randomBytes1 = _fixture.OcclumInterface.GetRandomBytes(length);
            byte[] randomBytes2 = _fixture.OcclumInterface.GetRandomBytes(length);
            
            // Assert
            Assert.NotNull(randomBytes1);
            Assert.NotNull(randomBytes2);
            Assert.Equal(length, randomBytes1.Length);
            Assert.Equal(length, randomBytes2.Length);
            Assert.NotEqual(randomBytes1, randomBytes2); // Should be different
            
            _logger.LogInformation("Random bytes 1: {RandomBytes1}", Convert.ToBase64String(randomBytes1));
            _logger.LogInformation("Random bytes 2: {RandomBytes2}", Convert.ToBase64String(randomBytes2));
        }

        [Fact]
        public void OcclumEnclave_ShouldSignAndVerifyData()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping signing test because we're not using the real SDK");
                return;
            }

            // Arrange
            byte[] data = Encoding.UTF8.GetBytes("This is a test message to sign");
            
            // Act
            byte[] signature = _fixture.OcclumInterface.SignData(data);
            bool isValid = _fixture.OcclumInterface.VerifySignature(data, signature);
            
            // Tamper with the data
            byte[] tamperedData = Encoding.UTF8.GetBytes("This is a tampered test message");
            bool isInvalid = _fixture.OcclumInterface.VerifySignature(tamperedData, signature);
            
            // Assert
            Assert.NotNull(signature);
            Assert.True(signature.Length > 0, "Signature should not be empty");
            Assert.True(isValid, "Signature should be valid for the original data");
            Assert.False(isInvalid, "Signature should be invalid for tampered data");
            
            _logger.LogInformation("Signature: {Signature}", Convert.ToBase64String(signature));
        }

        [Fact]
        public void OcclumEnclave_ShouldSealAndUnsealData()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping sealing test because we're not using the real SDK");
                return;
            }

            // Arrange
            byte[] data = Encoding.UTF8.GetBytes("This is a test message to seal");
            
            // Act
            byte[] sealedData = _fixture.OcclumInterface.SealData(data);
            byte[] unsealedData = _fixture.OcclumInterface.UnsealData(sealedData);
            
            // Assert
            Assert.NotNull(sealedData);
            Assert.NotNull(unsealedData);
            Assert.True(sealedData.Length > data.Length, "Sealed data should be larger than original data");
            Assert.Equal(data, unsealedData);
            
            _logger.LogInformation("Original data: {OriginalData}", Convert.ToBase64String(data));
            _logger.LogInformation("Sealed data: {SealedData}", Convert.ToBase64String(sealedData));
            _logger.LogInformation("Unsealed data: {UnsealedData}", Convert.ToBase64String(unsealedData));
        }
    }
}
