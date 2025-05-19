using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core.Models;
using NeoServiceLayer.Shared.Models;
using NeoServiceLayer.Tee.Enclave;
using NeoServiceLayer.Tee.Host.Services;
using NeoServiceLayer.Tee.Host.Tests.Mocks;
using Xunit;

namespace NeoServiceLayer.Tee.Host.Tests.Services
{
    public class TeeHostServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<TeeHostService>> _mockLogger;
        private readonly Mock<ILogger<TeeEnclaveFactory>> _mockFactoryLogger;
        private readonly Mock<ILogger<OpenEnclaveInterface>> _mockEnclaveLogger;
        private readonly TeeHostService _service;

        public TeeHostServiceTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<TeeHostService>>();
            _mockFactoryLogger = new Mock<ILogger<TeeEnclaveFactory>>();
            _mockEnclaveLogger = new Mock<ILogger<OpenEnclaveInterface>>();

            var configurationSection = new Mock<IConfigurationSection>();
            configurationSection.Setup(x => x.Value).Returns("true");
            _mockConfiguration.Setup(x => x.GetSection("Tee:SimulationMode")).Returns(configurationSection.Object);

            var enclavePathSection = new Mock<IConfigurationSection>();
            enclavePathSection.Setup(x => x.Value).Returns("mock_enclave.signed.so");
            _mockConfiguration.Setup(x => x.GetSection("Tee:Enclave:Path")).Returns(enclavePathSection.Object);

            // Create a mock enclave interface
            var mockEnclaveInterface = new Mock<ITeeEnclaveInterface>();
            mockEnclaveInterface.Setup(e => e.GetEnclaveId()).Returns(IntPtr.Zero);
            mockEnclaveInterface.Setup(e => e.GetMrEnclave()).Returns(new byte[32]);
            mockEnclaveInterface.Setup(e => e.GetMrSigner()).Returns(new byte[32]);

            // Create a mock factory
            var mockFactory = new Mock<NeoServiceLayer.Tee.Host.Services.ITeeEnclaveFactory>();
            mockFactory.Setup(f => f.CreateEnclave(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(mockEnclaveInterface.Object);

            // Create the service with the mock factory
            _service = new TeeHostService(_mockConfiguration.Object, _mockLogger.Object, mockFactory.Object);
        }

        [Fact]
        public async System.Threading.Tasks.Task InitializeTeeAsync_SimulationMode_ReturnsTrue()
        {
            // Act
            var result = await _service.InitializeTeeAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async System.Threading.Tasks.Task ExecuteTaskAsync_ValidTask_ReturnsResult()
        {
            // Arrange
            var task = new Core.Models.Task
            {
                Id = Guid.NewGuid().ToString(),
                UserId = "user123",
                Type = TaskType.SmartContractExecution,
                Status = Core.Models.TaskStatus.Pending,
                Data = new Dictionary<string, object>
                {
                    { "contract", "0x1234567890abcdef" },
                    { "method", "transfer" },
                    { "params", new[] { "address1", "address2", "100" } }
                },
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var result = await _service.ExecuteTaskAsync(task);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("success", result["result"]);
            Assert.Equal("contract_executed", result["output"]);
            Assert.Equal(1000, result["gas_used"]);
        }

        [Fact]
        public async System.Threading.Tasks.Task ExecuteTaskAsync_NullTask_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.ExecuteTaskAsync(null!));
        }

        [Fact]
        public async System.Threading.Tasks.Task SendMessageAsync_JavaScriptExecution_ReturnsResponse()
        {
            // Arrange
            var jsRequest = new NeoServiceLayer.Tee.Host.Services.JavaScriptExecutionRequest
            {
                FunctionId = "test-function-id",
                FunctionCode = "function main(input) { return { result: 'success' }; }",
                Input = "{\"param1\": \"value1\"}",
                UserId = "test-user-id",
                GasLimit = 1000
            };

            var message = new TeeMessage
            {
                Id = Guid.NewGuid().ToString(),
                Type = TeeMessageType.JavaScriptExecution,
                Data = JsonSerializer.Serialize(jsRequest),
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var result = await _service.SendMessageAsync(message);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(TeeMessageType.JavaScriptExecution, result.Type);
            Assert.NotNull(result.Data);
            Assert.Contains("success", result.Data);
        }

        [Fact]
        public async System.Threading.Tasks.Task SendMessageAsync_Attestation_ReturnsResponse()
        {
            // Arrange
            var message = new TeeMessage
            {
                Id = Guid.NewGuid().ToString(),
                Type = TeeMessageType.Attestation,
                Data = "{}",
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var result = await _service.SendMessageAsync(message);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(TeeMessageType.Attestation, result.Type);
            Assert.NotNull(result.Data);
            Assert.Contains("Report", result.Data);
            Assert.Contains("MrEnclave", result.Data);
            Assert.Contains("MrSigner", result.Data);
        }

        [Fact]
        public async System.Threading.Tasks.Task SendMessageAsync_NullMessage_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.SendMessageAsync(null!));
        }

        [Fact]
        public async System.Threading.Tasks.Task GetAttestationProofAsync_ReturnsAttestationProof()
        {
            // Act
            var result = await _service.GetAttestationProofAsync();

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Id);
            Assert.NotNull(result.Report);
            Assert.NotNull(result.Signature);
            Assert.NotNull(result.MrEnclave);
            Assert.NotNull(result.MrSigner);
            Assert.NotNull(result.ProductId);
            Assert.NotNull(result.SecurityVersion);
            Assert.NotNull(result.Attributes);
            Assert.NotEqual(default, result.CreatedAt);
            Assert.NotEqual(default, result.ExpiresAt);
        }

        [Fact]
        public async System.Threading.Tasks.Task VerifyAttestationProofAsync_ValidProof_ReturnsTrue()
        {
            // Arrange
            var attestationProof = await _service.GetAttestationProofAsync();

            // Act
            var result = await _service.VerifyAttestationProofAsync(attestationProof);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async System.Threading.Tasks.Task VerifyAttestationProofAsync_NullProof_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.VerifyAttestationProofAsync(null!));
        }

        [Fact]
        public async System.Threading.Tasks.Task GetStatusAsync_ReturnsStatus()
        {
            // Act
            var result = await _service.GetStatusAsync();

            // Assert
            Assert.Equal(Core.Models.TeeStatus.Running, result);
        }

        [Fact]
        public async System.Threading.Tasks.Task Dispose_CallsDisposeTrue()
        {
            // Act
            _service.Dispose();

            // Assert
            // Cannot directly test that Dispose(true) was called, but we can test that the service is disposed
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SendMessageAsync(new TeeMessage()));
        }
    }
}
