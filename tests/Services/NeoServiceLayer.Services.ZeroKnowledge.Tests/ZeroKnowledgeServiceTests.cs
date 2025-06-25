using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.ServiceFramework;
using NeoServiceLayer.Services.ZeroKnowledge;
using NeoServiceLayer.Services.ZeroKnowledge.Models;
using NeoServiceLayer.Tee.Host.Services;
using Xunit;

namespace NeoServiceLayer.Services.ZeroKnowledge.Tests;

/// <summary>
/// Comprehensive unit tests for ZeroKnowledgeService covering all ZK operations.
/// Tests circuit compilation, proof generation, verification, and enclave operations.
/// </summary>
public class ZeroKnowledgeServiceTests : IDisposable
{
    private readonly Mock<ILogger<ZeroKnowledgeService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IEnclaveManager> _mockEnclaveManager;
    private readonly ZeroKnowledgeService _service;

    public ZeroKnowledgeServiceTests()
    {
        _mockLogger = new Mock<ILogger<ZeroKnowledgeService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockEnclaveManager = new Mock<IEnclaveManager>();

        SetupConfiguration();
        SetupEnclaveManager();

        _service = new ZeroKnowledgeService(
            _mockLogger.Object,
            _mockEnclaveManager.Object,
            null);

        // Initialize the service synchronously for tests
        _service.InitializeAsync().GetAwaiter().GetResult();
        _service.StartAsync().GetAwaiter().GetResult();
    }

    #region Service Lifecycle Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ServiceLifecycle")]
    public async Task StartAsync_ValidConfiguration_InitializesSuccessfully()
    {
        // Act
        await _service.StartAsync();

        // Assert
        _service.IsRunning.Should().BeTrue();
        VerifyLoggerCalled(LogLevel.Information, "Zero-Knowledge Service");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ServiceLifecycle")]
    public async Task StopAsync_RunningService_StopsSuccessfully()
    {
        // Arrange
        await _service.StartAsync();

        // Act
        await _service.StopAsync();

        // Assert
        _service.IsRunning.Should().BeFalse();
        VerifyLoggerCalled(LogLevel.Information, "Zero-Knowledge Service");
    }

    #endregion

    #region Circuit Compilation Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "CircuitCompilation")]
    public async Task CompileCircuitAsync_ValidCircuitDefinition_ReturnsCircuitId()
    {
        // Arrange
        const string circuitId = "test_circuit_001";
        const string circuitDefinition = @"
            function main(private_input, public_input) {
                assert(private_input * private_input == public_input);
                return 1;
            }";

        // Act
        var circuitDef = new ZkCircuitDefinition
        {
            Name = circuitId,
            Type = ZkCircuitType.Computation,
            Description = "Test circuit",
            Constraints = new[] { circuitDefinition },
            InputSchema = new Dictionary<string, object> { ["public_input"] = "uint256" },
            OutputSchema = new Dictionary<string, object> { ["private_input"] = "uint256" }
        };
        var result = await _service.CompileCircuitAsync(circuitDef, BlockchainType.NeoN3);

        // Assert
        result.Should().Be(circuitId);
        VerifyLoggerCalled(LogLevel.Information, "Circuit compilation completed successfully");
        _mockEnclaveManager.Verify(x => x.StorageStoreDataAsync(
            It.Is<string>(s => s.Contains(circuitId)),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "CircuitCompilation")]
    public async Task CompileCircuitAsync_InvalidCircuitDefinition_ThrowsArgumentException()
    {
        // Arrange
        const string circuitId = "invalid_circuit";
        const string invalidCircuitDefinition = "invalid circuit syntax {{{";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CompileCircuitAsync(new ZkCircuitDefinition { Name = circuitId, Type = ZkCircuitType.Computation, Description = "Invalid", Constraints = new[] { invalidCircuitDefinition } }, BlockchainType.NeoN3));

        exception.Message.Should().Contain("Invalid circuit definition");
    }

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("Component", "CircuitCompilation")]
    [InlineData("comparison_circuit", "function main(a, b) { assert(a > b); return 1; }")]
    [InlineData("hash_circuit", "function main(preimage, hash) { assert(sha256(preimage) == hash); return 1; }")]
    [InlineData("range_proof_circuit", "function main(value, min, max) { assert(value >= min && value <= max); return 1; }")]
    public async Task CompileCircuitAsync_VariousCircuitTypes_CompilesSuccessfully(string circuitId, string circuitDefinition)
    {
        // Act
        var circuitDef = new ZkCircuitDefinition { Name = circuitId, Type = ZkCircuitType.Computation, Description = "Test", Constraints = new[] { circuitDefinition } };
        var result = await _service.CompileCircuitAsync(circuitDef, BlockchainType.NeoN3);

        // Assert
        result.Should().Be(circuitId);
        VerifyLoggerCalled(LogLevel.Information, "Circuit compilation completed successfully");
    }

    #endregion

    #region Proof Generation Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ProofGeneration")]
    public async Task GenerateProofAsync_ValidInputs_ReturnsProof()
    {
        // Arrange
        var circuit = CreateTestCircuit();
        var inputs = new Dictionary<string, object> { ["public_input"] = 25 };
        var witnesses = new Dictionary<string, object> { ["private_input"] = 5 };

        SetupCircuitStorage(circuit);

        // Act
        var proof = await _service.GenerateProofAsync(new NeoServiceLayer.Core.Models.ProofRequest { CircuitId = circuit.Id, PublicInputs = inputs, PrivateInputs = witnesses }, BlockchainType.NeoN3);

        // Assert
        proof.Should().NotBeNull();
        proof.ProofData.Length.Should().BeGreaterThan(0);
        var originalInputs = new Dictionary<string, object> { ["public_input"] = 25 };
        VerifyLoggerCalled(LogLevel.Information, "Generated ZK proof");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ProofGeneration")]
    public async Task GenerateProofAsync_InvalidWitnesses_ThrowsArgumentException()
    {
        // Arrange
        var circuit = CreateTestCircuit();
        var inputs = new Dictionary<string, object> { ["public_input"] = 25 };
        var invalidWitnesses = new Dictionary<string, object> { ["private_input"] = 6 }; // 6^2 != 25

        SetupCircuitStorage(circuit);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GenerateProofAsync(new NeoServiceLayer.Core.Models.ProofRequest { CircuitId = circuit.Id, PublicInputs = inputs, PrivateInputs = invalidWitnesses }, BlockchainType.NeoN3));

        exception.Message.Should().Contain("Invalid witnesses");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ProofGeneration")]
    public async Task GenerateProofAsync_NonExistentCircuit_ThrowsInvalidOperationException()
    {
        // Arrange
        var nonExistentCircuit = new Circuit { Id = "non_existent_circuit" };
        var inputs = new Dictionary<string, object>();
        var witnesses = new Dictionary<string, object>();

        _mockEnclaveManager
            .Setup(x => x.StorageRetrieveDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.GenerateProofAsync(new NeoServiceLayer.Core.Models.ProofRequest { CircuitId = nonExistentCircuit.Id, PublicInputs = inputs, PrivateInputs = witnesses }, BlockchainType.NeoN3));

        exception.Message.Should().Contain("Circuit not found");
    }

    #endregion

    #region Proof Verification Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ProofVerification")]
    public async Task VerifyProofAsync_ValidProof_ReturnsTrue()
    {
        // Arrange
        var circuit = CreateTestCircuit();
        var inputs = new Dictionary<string, object> { ["public_input"] = 25 };
        var witnesses = new Dictionary<string, object> { ["private_input"] = 5 };

        SetupCircuitStorage(circuit);

        var proof = await _service.GenerateProofAsync(new NeoServiceLayer.Core.Models.ProofRequest { CircuitId = circuit.Id, PublicInputs = inputs, PrivateInputs = witnesses }, BlockchainType.NeoN3);

        // Act
        var isValid = await _service.VerifyProofAsync(new NeoServiceLayer.Core.Models.ProofVerification { CircuitId = circuit.Id, ProofData = proof.ProofData, PublicInputs = inputs }, BlockchainType.NeoN3);

        // Assert
        isValid.Should().BeTrue();
        VerifyLoggerCalled(LogLevel.Information, "Proof verification completed");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ProofVerification")]
    public async Task VerifyProofAsync_InvalidProof_ReturnsFalse()
    {
        // Arrange
        var circuit = CreateTestCircuit();
        var inputs = new Dictionary<string, object> { ["public_input"] = 25 };
        var invalidProof = new byte[] { 0x00, 0x01, 0x02, 0x03 }; // Invalid proof

        SetupCircuitStorage(circuit);

        // Act
        var isValid = await _service.VerifyProofAsync(new NeoServiceLayer.Core.Models.ProofVerification { CircuitId = circuit.Id, ProofData = invalidProof, PublicInputs = inputs }, BlockchainType.NeoN3);

        // Assert
        isValid.Should().BeFalse();
        VerifyLoggerCalled(LogLevel.Warning, "Proof verification failed");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Component", "ProofVerification")]
    public async Task VerifyProofAsync_TamperedInputs_ReturnsFalse()
    {
        // Arrange
        var circuit = CreateTestCircuit();
        var originalInputs = new Dictionary<string, object> { ["public_input"] = 25 };
        var tamperedInputs = new Dictionary<string, object> { ["public_input"] = 36 };
        var witnesses = new Dictionary<string, object> { ["private_input"] = 5 };

        SetupCircuitStorage(circuit);

        var proof = await _service.GenerateProofAsync(new NeoServiceLayer.Core.Models.ProofRequest { CircuitId = circuit.Id, PublicInputs = originalInputs, PrivateInputs = witnesses }, BlockchainType.NeoN3);

        // Act
        var isValid = await _service.VerifyProofAsync(new NeoServiceLayer.Core.Models.ProofVerification { CircuitId = circuit.Id, ProofData = proof.ProofData, PublicInputs = tamperedInputs }, BlockchainType.NeoN3);

        // Assert
        isValid.Should().BeFalse();
        VerifyLoggerCalled(LogLevel.Warning, "Proof verification failed");
    }

    #endregion

    #region Performance Tests

    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Component", "ProofGeneration")]
    public async Task GenerateProofAsync_MultipleProofs_PerformsEfficiently()
    {
        // Arrange
        var circuit = CreateTestCircuit();
        const int proofCount = 10;
        var tasks = new List<Task<NeoServiceLayer.Core.Models.ProofResult>>();

        SetupCircuitStorage(circuit);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < proofCount; i++)
        {
            var inputs = new Dictionary<string, object> { ["public_input"] = (i + 1) * (i + 1) };
            var witnesses = new Dictionary<string, object> { ["private_input"] = i + 1 };
            tasks.Add(_service.GenerateProofAsync(new NeoServiceLayer.Core.Models.ProofRequest { CircuitId = circuit.Id, PublicInputs = inputs, PrivateInputs = witnesses }, BlockchainType.NeoN3));
        }

        var proofs = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        proofs.Should().HaveCount(proofCount);
        proofs.Should().AllSatisfy(p => p.Should().NotBeNull());
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(30000); // Should complete within 30 seconds
    }

    #endregion

    #region Helper Methods

    private void SetupConfiguration()
    {
        var configSection = new Mock<Microsoft.Extensions.Configuration.IConfigurationSection>();
        configSection.Setup(x => x.Value).Returns("test_value");

        _mockConfiguration
            .Setup(x => x.GetSection(It.IsAny<string>()))
            .Returns(configSection.Object);
    }

    private void SetupEnclaveManager()
    {
        // Setup basic manager properties
        _mockEnclaveManager.Setup(x => x.IsInitialized).Returns(true);

        // Setup initialization
        _mockEnclaveManager.Setup(x => x.InitializeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockEnclaveManager.Setup(x => x.InitializeEnclaveAsync())
            .ReturnsAsync(true);
        _mockEnclaveManager.Setup(x => x.DestroyEnclaveAsync())
            .ReturnsAsync(true);

        // Setup storage operations
        _mockEnclaveManager
            .Setup(x => x.StorageStoreDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("mock-storage-id");
        _mockEnclaveManager.Setup(x => x.StorageRetrieveDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-data");
        _mockEnclaveManager.Setup(x => x.StorageDeleteDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Setup cryptographic operations
        _mockEnclaveManager.Setup(x => x.EncryptDataAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string data, string key) => $"encrypted-{data}");
        _mockEnclaveManager.Setup(x => x.DecryptDataAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string data, string key) => data.StartsWith("encrypted-") ? data.Substring(10) : data);
        _mockEnclaveManager.Setup(x => x.SignDataAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("mock-signature");
        _mockEnclaveManager.Setup(x => x.VerifySignatureAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

    }

    private void SetupCircuitStorage(Circuit circuit)
    {
        // Create a proper ZkCircuit and add it to the service's circuit collection
        var zkCircuit = new ZkCircuit
        {
            CircuitId = circuit.Id,
            Name = circuit.Id,
            Description = "Test circuit",
            Type = ZkCircuitType.Computation,
            VerificationKey = "mock_verification_key",
            ProvingKey = "mock_proving_key",
            IsActive = true,
            CompiledAt = DateTime.UtcNow
        };

        // Access the private circuits field using reflection
        var circuitsField = typeof(ZeroKnowledgeService).GetField("_circuits", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var circuits = (Dictionary<string, ZkCircuit>)circuitsField!.GetValue(_service)!;

        lock (circuits)
        {
            circuits[circuit.Id] = zkCircuit;
        }

        // Also setup the enclave storage mock
        var circuitData = new
        {
            CircuitId = circuit.Id,
            R1CS = new { Constraints = new object[0], Variables = new object[0] },
            ProvingKey = "mock_proving_key",
            VerificationKey = "mock_verification_key",
            CompiledAt = DateTime.UtcNow
        };

        _mockEnclaveManager
            .Setup(x => x.StorageRetrieveDataAsync($"circuit_{circuit.Id}", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(System.Text.Json.JsonSerializer.Serialize(circuitData));
    }

    private static Circuit CreateTestCircuit()
    {
        return new Circuit
        {
            Id = "test_square_circuit",
            Definition = "function main(private_input, public_input) { assert(private_input * private_input == public_input); return 1; }",
            Type = CircuitType.Arithmetic
        };
    }

    private void VerifyLoggerCalled(LogLevel level, string message)
    {
        _mockLogger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    public void Dispose()
    {
        _service?.Dispose();
    }

    #endregion

    #region Test Data Models

    public class Circuit
    {
        public string Id { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public CircuitType Type { get; set; }
    }

    public enum CircuitType
    {
        Arithmetic,
        Boolean,
        Hash,
        Signature
    }

    #endregion
}
