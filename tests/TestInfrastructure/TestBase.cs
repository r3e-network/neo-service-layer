using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Core;
using NeoServiceLayer.Infrastructure;
using NeoServiceLayer.Tee.Enclave;
using NeoServiceLayer.Tee.Host.Services;
using IBlockchainClient = NeoServiceLayer.Infrastructure.IBlockchainClient;
using IBlockchainClientFactory = NeoServiceLayer.Infrastructure.IBlockchainClientFactory;

namespace NeoServiceLayer.TestInfrastructure;

/// <summary>
/// Base class for service tests providing common mocking setup and utilities.
/// </summary>
public abstract class TestBase
{
    protected static readonly Mock<IEnclaveWrapper> MockEnclaveWrapper;
    protected static readonly Mock<IEnclaveManager> MockEnclaveManager;

    static TestBase()
    {
        MockEnclaveWrapper = new Mock<IEnclaveWrapper>();
        MockEnclaveManager = new Mock<IEnclaveManager>();
        SetupMockEnclaveWrapper();
        SetupMockEnclaveManager();
    }

    private static void SetupMockEnclaveWrapper()
    {
        // Setup basic enclave operations
        MockEnclaveWrapper.Setup(x => x.Initialize()).Returns(true);
        MockEnclaveWrapper.Setup(x => x.Dispose());

        // Setup JavaScript execution
        MockEnclaveWrapper.Setup(x => x.ExecuteJavaScript(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("{\"success\": true, \"result\": \"test_execution_result\"}");

        // Setup data operations
        MockEnclaveWrapper.Setup(x => x.SealData(It.IsAny<byte[]>()))
            .Returns((byte[] data) => data); // Return data as-is for testing

        MockEnclaveWrapper.Setup(x => x.UnsealData(It.IsAny<byte[]>()))
            .Returns((byte[] data) => data); // Return data as-is for testing

        // Setup random number generation
        MockEnclaveWrapper.Setup(x => x.GenerateRandomBytes(It.IsAny<int>()))
            .Returns((int length) => Enumerable.Range(0, length).Select(i => (byte)(i % 256)).ToArray());

        // Setup cryptographic operations
        MockEnclaveWrapper.Setup(x => x.Sign(It.IsAny<byte[]>(), It.IsAny<byte[]>()))
            .Returns(new byte[] { 0x01, 0x02, 0x03, 0x04 });

        MockEnclaveWrapper.Setup(x => x.Verify(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>()))
            .Returns(true);

        // Setup attestation
        MockEnclaveWrapper.Setup(x => x.GetAttestationReport())
            .Returns(Convert.ToBase64String(new byte[] { 0x01, 0x02, 0x03, 0x04 }));

        // IEnclaveWrapper interface setup is now complete
    }

    private static void SetupMockEnclaveManager()
    {
        // Setup basic manager properties
        MockEnclaveManager.Setup(x => x.IsInitialized).Returns(true);
        // EnclaveType doesn't exist on IEnclaveManager

        // Setup initialization
        MockEnclaveManager.Setup(x => x.InitializeAsync(null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        MockEnclaveManager.Setup(x => x.DestroyEnclaveAsync())
            .ReturnsAsync(true);

        // Setup storage operations
        MockEnclaveManager.Setup(x => x.StorageStoreDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("stored");
        MockEnclaveManager.Setup(x => x.StorageRetrieveDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-data");
        MockEnclaveManager.Setup(x => x.StorageDeleteDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Setup cryptographic operations
        MockEnclaveManager.Setup(x => x.EncryptDataAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string data, string key) => $"encrypted-{data}");
        MockEnclaveManager.Setup(x => x.DecryptDataAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string data, string key) => data.StartsWith("encrypted-") ? data.Substring(10) : data);
        MockEnclaveManager.Setup(x => x.SignDataAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("mock-signature");
        MockEnclaveManager.Setup(x => x.VerifySignatureAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        // Setup key management
        MockEnclaveManager.Setup(x => x.KmsGenerateKeyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync((string keyId, string keyType, string keyUsage, bool exportable, string description) => keyId);

        // Setup JavaScript execution
        MockEnclaveManager.Setup(x => x.ExecuteJavaScriptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("{\"result\": 42}");
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
    protected static NeoServiceLayer.Infrastructure.BlockchainConfiguration CreateTestBlockchainConfiguration()
    {
        return new NeoServiceLayer.Infrastructure.BlockchainConfiguration
        {
            NeoN3 = new NeoN3Configuration
            {
                RpcUrl = "http://localhost:20332",
                NetworkMagic = 860833102
            },
            NeoX = new NeoXConfiguration
            {
                RpcUrl = "http://localhost:8545",
                ChainId = 12227332
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
            BlockchainType.NeoN3 => GenerateNeoN3Address(),
            BlockchainType.NeoX => $"0x{Guid.NewGuid().ToString("N")[..40]}",
            _ => throw new NotSupportedException($"Blockchain type {blockchainType} not supported")
        };
    }

    private static string GenerateNeoN3Address()
    {
        // Generate a valid-looking Neo N3 address using base58 characters
        // Base58 excludes 0, O, I, and l
        const string base58Chars = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        var random = new Random();
        var addressChars = new char[33];
        
        for (int i = 0; i < 33; i++)
        {
            addressChars[i] = base58Chars[random.Next(base58Chars.Length)];
        }
        
        // Neo N3 addresses start with 'N'
        return $"N{new string(addressChars)}";
    }

    /// <summary>
    /// Generates a test transaction hash.
    /// </summary>
    protected static string GenerateTestTransactionHash()
    {
        return $"0x{Guid.NewGuid().ToString("N")}{Guid.NewGuid().ToString("N")}";
    }
}
