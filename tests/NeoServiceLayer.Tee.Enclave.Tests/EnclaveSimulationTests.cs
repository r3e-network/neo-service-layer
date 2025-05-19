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
    [Trait("Category", "OpenEnclave")]
    public class EnclaveSimulationTests : IClassFixture<SimulationModeFixture>
    {
        private readonly SimulationModeFixture _fixture;
        private readonly ITestOutputHelper _output;
        private readonly ILogger _logger;

        public EnclaveSimulationTests(SimulationModeFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
            
            // Create a logger that writes to the test output
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new XunitLoggerProvider(_output));
            _logger = loggerFactory.CreateLogger<EnclaveSimulationTests>();
            
            _logger.LogInformation("Test initialized with {UsingRealSdk}", 
                _fixture.UsingRealSdk ? "real SDK" : "mock implementation");
        }

        [Fact]
        public void EnclaveInterface_ShouldBeInitialized()
        {
            // Arrange & Act
            var enclaveId = _fixture.TeeInterface.GetEnclaveId();
            
            // Assert
            Assert.NotEqual(IntPtr.Zero, enclaveId);
            _logger.LogInformation("Enclave ID: {EnclaveId}", enclaveId);
        }

        [Fact]
        public void EnclaveInterface_ShouldReturnMeasurements()
        {
            // Arrange & Act
            var mrEnclave = _fixture.TeeInterface.GetMrEnclave();
            var mrSigner = _fixture.TeeInterface.GetMrSigner();
            
            // Assert
            Assert.NotNull(mrEnclave);
            Assert.NotNull(mrSigner);
            Assert.True(mrEnclave.Length > 0, "MRENCLAVE should not be empty");
            Assert.True(mrSigner.Length > 0, "MRSIGNER should not be empty");
            
            _logger.LogInformation("MRENCLAVE: {MrEnclave}", Convert.ToBase64String(mrEnclave));
            _logger.LogInformation("MRSIGNER: {MrSigner}", Convert.ToBase64String(mrSigner));
        }

        [Fact]
        public async Task EnclaveInterface_ShouldExecuteJavaScript()
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
            string expectedOutput = @"{""result"":""Hello, World""}";

            // Act
            var result = await _fixture.TeeInterface.ExecuteJavaScriptAsync(jsCode, input);
            
            // Assert
            Assert.NotNull(result);
            Assert.Contains("result", result);
            Assert.Contains("Hello, World", result);
            
            _logger.LogInformation("JavaScript execution result: {Result}", result);
        }

        [Fact]
        public void EnclaveInterface_ShouldSealAndUnsealData()
        {
            // Arrange
            byte[] originalData = Encoding.UTF8.GetBytes("This is a test of data sealing and unsealing");
            
            // Act
            byte[] sealedData = _fixture.TeeInterface.SealData(originalData);
            byte[] unsealedData = _fixture.TeeInterface.UnsealData(sealedData);
            
            // Assert
            Assert.NotNull(sealedData);
            Assert.NotNull(unsealedData);
            Assert.NotEqual(originalData, sealedData); // Sealed data should be different
            Assert.Equal(originalData, unsealedData);  // Unsealed data should match original
            
            _logger.LogInformation("Original data length: {OriginalLength}, Sealed data length: {SealedLength}", 
                originalData.Length, sealedData.Length);
        }

        [Fact]
        public async Task EnclaveInterface_ShouldGenerateAttestation()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping attestation test because we're not using the real SDK");
                return;
            }

            // Act
            var attestationReport = await _fixture.TeeInterface.GenerateAttestationReportAsync();
            
            // Assert
            Assert.NotNull(attestationReport);
            Assert.True(attestationReport.Length > 0, "Attestation report should not be empty");
            
            _logger.LogInformation("Attestation report length: {Length}", attestationReport.Length);
        }

        [Fact]
        public async Task EnclaveInterface_ShouldHandleUserSecrets()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping user secrets test because we're not using the real SDK");
                return;
            }

            // Arrange
            string userId = "test-user-" + Guid.NewGuid().ToString();
            string secretName = "test-secret";
            string secretValue = "test-secret-value";
            
            // Act - Store secret
            bool storeResult = await _fixture.TeeInterface.StoreUserSecretAsync(userId, secretName, secretValue);
            
            // Act - Retrieve secret
            string retrievedSecret = await _fixture.TeeInterface.GetUserSecretAsync(userId, secretName);
            
            // Act - List secrets
            var secretsList = await _fixture.TeeInterface.ListUserSecretsAsync(userId);
            
            // Act - Delete secret
            bool deleteResult = await _fixture.TeeInterface.DeleteUserSecretAsync(userId, secretName);
            
            // Assert
            Assert.True(storeResult, "Storing secret should succeed");
            Assert.Equal(secretValue, retrievedSecret);
            Assert.Contains(secretName, secretsList);
            Assert.True(deleteResult, "Deleting secret should succeed");
            
            _logger.LogInformation("User secrets test passed for user {UserId}", userId);
        }

        [Fact]
        public async Task EnclaveInterface_ShouldTrackGasUsage()
        {
            // Skip if not using real SDK
            if (!_fixture.UsingRealSdk)
            {
                _logger.LogInformation("Skipping gas usage test because we're not using the real SDK");
                return;
            }

            // Arrange
            string jsCode = @"
                function main(input) {
                    // Do some computation to use gas
                    let result = 0;
                    for (let i = 0; i < 1000000; i++) {
                        result += i;
                    }
                    return { result: result };
                }
            ";
            string input = @"{}";
            
            // Act
            var result = await _fixture.TeeInterface.ExecuteJavaScriptWithGasAsync(jsCode, input, out ulong gasUsed);
            
            // Assert
            Assert.NotNull(result);
            Assert.True(gasUsed > 0, "Gas usage should be greater than zero");
            
            _logger.LogInformation("JavaScript execution used {GasUsed} gas units", gasUsed);
        }
    }

    // Logger provider that writes to the test output
    public class XunitLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public XunitLoggerProvider(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new XunitLogger(_testOutputHelper, categoryName);
        }

        public void Dispose()
        {
        }
    }

    public class XunitLogger : ILogger
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly string _categoryName;

        public XunitLogger(ITestOutputHelper testOutputHelper, string categoryName)
        {
            _testOutputHelper = testOutputHelper;
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            try
            {
                _testOutputHelper.WriteLine($"{DateTime.Now:HH:mm:ss.fff} [{logLevel}] {_categoryName}: {formatter(state, exception)}");
                if (exception != null)
                {
                    _testOutputHelper.WriteLine($"Exception: {exception}");
                }
            }
            catch
            {
                // Ignore errors writing to test output
            }
        }
    }
}
