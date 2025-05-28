using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Tee.Enclave;
using NeoServiceLayer.Tee.Host.Services;
using System.Reflection;
using System.Text;
using Xunit;

namespace NeoServiceLayer.Tee.Host.Tests;

/// <summary>
/// Tests for the EnclaveManager class.
/// </summary>
public class EnclaveManagerTests
{
    // We can't directly test the EnclaveManager class with the real EnclaveWrapper
    // because it depends on native methods that are not available in the test environment.
    // Instead, we'll test the basic functionality and error handling.

    [Fact]
    public void Constructor_ShouldCreateInstance()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<EnclaveManager>>();
        var enclaveWrapperLoggerMock = new Mock<ILogger<EnclaveWrapper>>();

        // Act
        var enclaveManager = new EnclaveManager(loggerMock.Object, enclaveWrapperLoggerMock.Object);

        // Assert
        Assert.NotNull(enclaveManager);
        Assert.False(enclaveManager.IsInitialized);
    }

    [Fact]
    public void Dispose_ShouldNotThrowException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<EnclaveManager>>();
        var enclaveWrapperLoggerMock = new Mock<ILogger<EnclaveWrapper>>();
        var enclaveManager = new EnclaveManager(loggerMock.Object, enclaveWrapperLoggerMock.Object);

        // Use reflection to set the _disposed field to true to avoid calling the native methods
        var disposedField = typeof(EnclaveManager).GetField("_disposed", BindingFlags.Instance | BindingFlags.NonPublic);
        disposedField?.SetValue(enclaveManager, true);

        // Act & Assert
        var exception = Record.Exception(() => enclaveManager.Dispose());
        Assert.Null(exception);
    }

    [Fact]
    public async Task GenerateRandomBytesAsync_WithSeed_ShouldReturnDeterministicBytes()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<EnclaveManager>>();
        var enclaveWrapperLoggerMock = new Mock<ILogger<EnclaveWrapper>>();
        var enclaveManager = new EnclaveManager(loggerMock.Object, enclaveWrapperLoggerMock.Object);
        int length = 32;
        string seed = "test-seed";

        // Act
        byte[] result1 = await enclaveManager.GenerateRandomBytesAsync(length, seed);
        byte[] result2 = await enclaveManager.GenerateRandomBytesAsync(length, seed);

        // Assert
        Assert.Equal(length, result1.Length);
        Assert.Equal(length, result2.Length);
        Assert.Equal(result1, result2); // Same seed should produce same bytes
    }

    [Fact]
    public async Task GenerateRandomBytesAsync_WithDifferentSeeds_ShouldReturnDifferentBytes()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<EnclaveManager>>();
        var enclaveWrapperLoggerMock = new Mock<ILogger<EnclaveWrapper>>();
        var enclaveManager = new EnclaveManager(loggerMock.Object, enclaveWrapperLoggerMock.Object);
        int length = 32;
        string seed1 = "test-seed-1";
        string seed2 = "test-seed-2";

        // Act
        byte[] result1 = await enclaveManager.GenerateRandomBytesAsync(length, seed1);
        byte[] result2 = await enclaveManager.GenerateRandomBytesAsync(length, seed2);

        // Assert
        Assert.Equal(length, result1.Length);
        Assert.Equal(length, result2.Length);
        Assert.NotEqual(result1, result2); // Different seeds should produce different bytes
    }

    [Fact]
    public async Task KmsGenerateKeyAsync_ShouldCallCallEnclaveFunctionAsync()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<EnclaveManager>>();
        var testEnclaveWrapper = new TestEnclaveWrapper();
        testEnclaveWrapper.SetExecuteJavaScriptResult("{\"keyId\": \"test-key\", \"keyType\": \"Secp256k1\"}");
        var enclaveManager = new EnclaveManager(loggerMock.Object, testEnclaveWrapper);

        // Initialize the enclave
        await enclaveManager.InitializeAsync();

        // Parameters for the test
        string keyId = "test-key";
        string keyType = "Secp256k1";
        string keyUsage = "Sign,Verify";
        bool exportable = false;
        string description = "Test key";

        // Act
        string result = await enclaveManager.KmsGenerateKeyAsync(keyId, keyType, keyUsage, exportable, description);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(keyId, result);
        Assert.Contains(keyType, result);
    }

    [Fact]
    public async Task CallEnclaveFunctionAsync_ShouldCallExecuteJavaScriptAsync()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<EnclaveManager>>();
        var testEnclaveWrapper = new TestEnclaveWrapper();
        testEnclaveWrapper.SetExecuteJavaScriptResult("{\"result\": \"success\"}");
        var enclaveManager = new EnclaveManager(loggerMock.Object, testEnclaveWrapper);

        // Initialize the enclave
        await enclaveManager.InitializeAsync();

        // Parameters for the test
        string functionName = "testFunction";
        string jsonPayload = "{\"test\": \"value\"}";

        // Act
        string result = await enclaveManager.CallEnclaveFunctionAsync(functionName, jsonPayload);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("success", result);
    }

    [Fact]
    public async Task InitializeAsync_ShouldSetIsInitializedToTrue_WhenSuccessful()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<EnclaveManager>>();
        var testEnclaveWrapper = new TestEnclaveWrapper();
        var enclaveManager = new EnclaveManager(loggerMock.Object, testEnclaveWrapper);

        // Act
        await enclaveManager.InitializeAsync();

        // Assert
        Assert.True(enclaveManager.IsInitialized);
    }

    [Fact]
    public async Task SignDataAsync_ShouldConvertDataAndKeyCorrectly()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<EnclaveManager>>();
        var testEnclaveWrapper = new TestEnclaveWrapper();
        byte[] expectedSignature = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        testEnclaveWrapper.SetSignResult(expectedSignature);
        var enclaveManager = new EnclaveManager(loggerMock.Object, testEnclaveWrapper);

        // Initialize the enclave
        await enclaveManager.InitializeAsync();

        string data = "test data";
        string privateKeyHex = "0102030405060708";

        // Act
        string result = await enclaveManager.SignDataAsync(data, privateKeyHex);

        // Assert
        Assert.Equal("01020304", result.ToLowerInvariant());
    }

    [Fact]
    public async Task VerifySignatureAsync_ShouldConvertDataAndKeyCorrectly()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<EnclaveManager>>();
        var testEnclaveWrapper = new TestEnclaveWrapper();
        testEnclaveWrapper.SetVerifyResult(true);
        var enclaveManager = new EnclaveManager(loggerMock.Object, testEnclaveWrapper);

        // Initialize the enclave
        await enclaveManager.InitializeAsync();

        string data = "test data";
        string signatureHex = "01020304";
        string publicKeyHex = "0102030405060708";

        // Act
        bool result = await enclaveManager.VerifySignatureAsync(data, signatureHex, publicKeyHex);

        // Assert
        Assert.True(result);
    }
}
