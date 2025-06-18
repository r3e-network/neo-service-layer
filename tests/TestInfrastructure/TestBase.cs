using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Tee.Host.Services;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.TestInfrastructure;

/// <summary>
/// Base class for service tests providing common mocking setup and utilities.
/// </summary>
public abstract class TestBase
{
    protected static readonly Mock<IEnclaveManager> MockEnclaveWrapper;

    static TestBase()
    {
        MockEnclaveWrapper = new Mock<IEnclaveManager>();
        SetupMockEnclaveWrapper();
    }

    private static void SetupMockEnclaveWrapper()
    {
        // Setup basic enclave operations
        MockEnclaveWrapper.Setup(x => x.InitializeEnclaveAsync()).ReturnsAsync(true);
        MockEnclaveWrapper.Setup(x => x.DestroyEnclaveAsync()).ReturnsAsync(true);
        MockEnclaveWrapper.Setup(x => x.IsEnclaveInitialized).Returns(true);

        // Setup JavaScript execution
        MockEnclaveWrapper.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("{\"success\": true, \"result\": \"test_execution_result\"}");

        // Setup data operations
        MockEnclaveWrapper.Setup(x => x.GetDataAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("{\"value\": 42, \"timestamp\": \"2025-01-22T10:00:00Z\"}");

        MockEnclaveWrapper.Setup(x => x.StoreDataAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync("{\"success\": true, \"key\": \"test_key\", \"stored_at\": \"2025-01-22T10:00:00Z\"}");

        MockEnclaveWrapper.Setup(x => x.RetrieveDataAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new byte[] { 0x01, 0x02, 0x03, 0x04 });

        // Setup random number generation
        MockEnclaveWrapper.Setup(x => x.GenerateRandomAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(42);

        MockEnclaveWrapper.Setup(x => x.GenerateRandomBytesAsync(It.IsAny<int>()))
            .ReturnsAsync((int length) => Enumerable.Range(0, length).Select(i => (byte)(i % 256)).ToArray());

        // Setup cryptographic operations
        MockEnclaveWrapper.Setup(x => x.EncryptAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>()))
            .ReturnsAsync((byte[] data, byte[] key) => data.Concat(new byte[] { 0xFF }).ToArray());

        MockEnclaveWrapper.Setup(x => x.DecryptAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>()))
            .ReturnsAsync((byte[] data, byte[] key) => data.Length > 0 ? data.Take(data.Length - 1).ToArray() : data);

        MockEnclaveWrapper.Setup(x => x.SignAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>()))
            .ReturnsAsync(new byte[] { 0x01, 0x02, 0x03, 0x04 });

        MockEnclaveWrapper.Setup(x => x.VerifyAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>()))
            .ReturnsAsync(true);

        // Setup key management
        MockEnclaveWrapper.Setup(x => x.GenerateKeyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync("{\"success\": true, \"keyId\": \"test_key_id\", \"keyType\": \"test_type\", \"created\": \"2025-01-22T10:00:00Z\"}");

        // Setup computation operations
        MockEnclaveWrapper.Setup(x => x.ExecuteComputationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("{\"success\": true, \"result\": \"computation_result\", \"computationId\": \"test_computation_id\"}");

        // Setup AI operations
        MockEnclaveWrapper.Setup(x => x.TrainAIModelAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double[]>(), It.IsAny<string>()))
            .ReturnsAsync("{\"success\": true, \"modelId\": \"test_model_id\", \"accuracy\": 0.95, \"trainingTime\": 1000}");

        MockEnclaveWrapper.Setup(x => x.PredictWithAIModelAsync(It.IsAny<string>(), It.IsAny<double[]>()))
            .ReturnsAsync((new double[] { 0.1, 0.2, 0.7 }, "{\"confidence\": 0.95, \"modelVersion\": \"1.0\"}"));

        // Setup blockchain operations
        MockEnclaveWrapper.Setup(x => x.CreateAbstractAccountAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("{\"success\": true, \"accountId\": \"test_account_id\", \"address\": \"0x1234567890abcdef\"}");

        // Setup storage metadata
        MockEnclaveWrapper.Setup(x => x.GetStorageMetadataAsync(It.IsAny<string>()))
            .ReturnsAsync("{\"success\": true, \"key\": \"test_key\", \"size\": 1024, \"created\": \"2025-01-22T10:00:00Z\"}");

        MockEnclaveWrapper.Setup(x => x.DeleteDataAsync(It.IsAny<string>()))
            .ReturnsAsync("{\"success\": true, \"key\": \"test_key\", \"deleted_at\": \"2025-01-22T10:00:00Z\"}");
    }

    /// <summary>
    /// Verifies that a logger was called with a specific log level and message.
    /// </summary>
    protected static void VerifyLoggerCalled<T>(Mock<ILogger<T>> logger, LogLevel level, string messageContains)
    {
        logger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(messageContains)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    /// <summary>
    /// Verifies that a logger was called with a specific log level.
    /// </summary>
    protected static void VerifyLoggerCalled<T>(Mock<ILogger<T>> logger, LogLevel level)
    {
        logger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    /// <summary>
    /// Creates a test blockchain configuration.
    /// </summary>
    protected static BlockchainConfiguration CreateTestBlockchainConfiguration()
    {
        return new BlockchainConfiguration
        {
            NeoN3 = new NeoN3Configuration
            {
                RpcUrl = "http://localhost:20332",
                NetworkMagic = 860833102,
                AddressVersion = 0x35
            },
            NeoX = new NeoXConfiguration
            {
                RpcUrl = "http://localhost:8545",
                ChainId = 12227332,
                NetworkName = "NeoX TestNet"
            }
        };
    }

    /// <summary>
    /// Generates a test address for the specified blockchain type.
    /// </summary>
    protected static string GenerateTestAddress(BlockchainType blockchainType)
    {
        return blockchainType switch
        {
            BlockchainType.NeoN3 => $"N{Guid.NewGuid().ToString("N")[..33]}",
            BlockchainType.NeoX => $"0x{Guid.NewGuid().ToString("N")[..40]}",
            _ => throw new NotSupportedException($"Blockchain type {blockchainType} not supported")
        };
    }

    /// <summary>
    /// Generates a test transaction hash.
    /// </summary>
    protected static string GenerateTestTransactionHash()
    {
        return $"0x{Guid.NewGuid().ToString("N")}{Guid.NewGuid().ToString("N")}";
    }
}