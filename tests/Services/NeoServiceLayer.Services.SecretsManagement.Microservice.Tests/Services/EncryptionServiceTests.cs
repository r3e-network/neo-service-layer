using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neo.SecretsManagement.Service.Services;
using NUnit.Framework;
using Moq;
using System.Security.Cryptography;

namespace Neo.SecretsManagement.Service.Tests.Services;

[TestFixture]
public class EncryptionServiceTests
{
    private Mock<IHsmService> _mockHsmService = null!;
    private Mock<ILogger<EncryptionService>> _mockLogger = null!;
    private EncryptionServiceOptions _options = null!;

    [SetUp]
    public void Setup()
    {
        _mockHsmService = new Mock<IHsmService>();
        _mockLogger = new Mock<ILogger<EncryptionService>>();

        _options = new EncryptionServiceOptions
        {
            DefaultAlgorithm = "AES-256-GCM",
            DefaultMasterKeyId = "master-key-id",
            UseHsmForMasterKeys = false, // Use software for testing
            KeyDerivationIterations = 10000,
            EnableHsm = false
        };

        // Setup HSM mock to simulate unavailable HSM for software fallback testing
        _mockHsmService.Setup(x => x.GetStatusAsync())
            .ReturnsAsync(new HsmStatus { IsAvailable = false });
    }

    [Test]
    public async Task EncryptAsync_ValidPlaintext_ReturnsEncryptedData()
    {
        // Arrange
        var service = CreateEncryptionService();
        const string plaintext = "This is a test secret value that needs to be encrypted";
        const string keyId = "test-key-id";

        // Act
        var encryptedData = await service.EncryptAsync(plaintext, keyId);

        // Assert
        Assert.That(encryptedData, Is.Not.Null);
        Assert.That(encryptedData, Is.Not.Empty);
        Assert.That(encryptedData, Is.Not.EqualTo(plaintext));
        
        // Verify it's base64 encoded (should not throw)
        Assert.DoesNotThrow(() => Convert.FromBase64String(encryptedData));
    }

    [Test]
    public async Task DecryptAsync_ValidEncryptedData_ReturnsOriginalPlaintext()
    {
        // Arrange
        var service = CreateEncryptionService();
        const string originalPlaintext = "This is a test secret value";
        const string keyId = "test-key-id";

        // Encrypt first
        var encryptedData = await service.EncryptAsync(originalPlaintext, keyId);

        // Act
        var decryptedData = await service.DecryptAsync(encryptedData, keyId);

        // Assert
        Assert.That(decryptedData, Is.EqualTo(originalPlaintext));
    }

    [Test]
    public async Task EncryptDecryptRoundTrip_WithDifferentDataSizes_WorksCorrectly()
    {
        // Arrange
        var service = CreateEncryptionService();
        const string keyId = "test-key-id";

        var testData = new[]
        {
            "",                                    // Empty string
            "a",                                   // Single character
            "Hello, World!",                      // Short string
            new string('x', 1000),                // Medium string
            new string('y', 10000)                // Large string
        };

        foreach (var plaintext in testData)
        {
            // Act
            var encrypted = await service.EncryptAsync(plaintext, keyId);
            var decrypted = await service.DecryptAsync(encrypted, keyId);

            // Assert
            Assert.That(decrypted, Is.EqualTo(plaintext), $"Failed for data of length {plaintext.Length}");
        }
    }

    [Test]
    public async Task EncryptAsync_WithDifferentKeys_ProducesDifferentCiphertext()
    {
        // Arrange
        var service = CreateEncryptionService();
        const string plaintext = "Same plaintext for different keys";

        // Act
        var encrypted1 = await service.EncryptAsync(plaintext, "key-1");
        var encrypted2 = await service.EncryptAsync(plaintext, "key-2");

        // Assert
        Assert.That(encrypted1, Is.Not.EqualTo(encrypted2));

        // Both should decrypt back to the same plaintext with their respective keys
        var decrypted1 = await service.DecryptAsync(encrypted1, "key-1");
        var decrypted2 = await service.DecryptAsync(encrypted2, "key-2");
        
        Assert.That(decrypted1, Is.EqualTo(plaintext));
        Assert.That(decrypted2, Is.EqualTo(plaintext));
    }

    [Test]
    public async Task EncryptAsync_SamePlaintextMultipleTimes_ProducesDifferentCiphertext()
    {
        // Arrange
        var service = CreateEncryptionService();
        const string plaintext = "Same plaintext encrypted multiple times";
        const string keyId = "test-key-id";

        // Act
        var encrypted1 = await service.EncryptAsync(plaintext, keyId);
        var encrypted2 = await service.EncryptAsync(plaintext, keyId);

        // Assert
        Assert.That(encrypted1, Is.Not.EqualTo(encrypted2), "Same plaintext should produce different ciphertext due to random IV/nonce");

        // Both should decrypt to the same plaintext
        var decrypted1 = await service.DecryptAsync(encrypted1, keyId);
        var decrypted2 = await service.DecryptAsync(encrypted2, keyId);
        
        Assert.That(decrypted1, Is.EqualTo(plaintext));
        Assert.That(decrypted2, Is.EqualTo(plaintext));
    }

    [Test]
    public async Task DecryptAsync_WithWrongKey_ThrowsCryptographicException()
    {
        // Arrange
        var service = CreateEncryptionService();
        const string plaintext = "Test secret";
        
        var encrypted = await service.EncryptAsync(plaintext, "correct-key");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<CryptographicException>(
            () => service.DecryptAsync(encrypted, "wrong-key"));
        
        Assert.That(ex.Message, Contains.Substring("decryption").IgnoreCase);
    }

    [Test]
    public async Task DecryptAsync_WithInvalidBase64_ThrowsFormatException()
    {
        // Arrange
        var service = CreateEncryptionService();
        const string invalidBase64 = "This is not valid base64!@#$";

        // Act & Assert
        var ex = await Assert.ThrowsAsync<FormatException>(
            () => service.DecryptAsync(invalidBase64, "any-key"));
        
        Assert.That(ex.Message, Contains.Substring("base64").IgnoreCase);
    }

    [Test]
    public async Task DecryptAsync_WithTamperedCiphertext_ThrowsCryptographicException()
    {
        // Arrange
        var service = CreateEncryptionService();
        const string plaintext = "Test secret";
        const string keyId = "test-key-id";
        
        var validEncrypted = await service.EncryptAsync(plaintext, keyId);
        var encryptedBytes = Convert.FromBase64String(validEncrypted);
        
        // Tamper with the last byte (authentication tag for GCM mode)
        encryptedBytes[^1] ^= 0x01;
        var tamperedEncrypted = Convert.ToBase64String(encryptedBytes);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<CryptographicException>(
            () => service.DecryptAsync(tamperedEncrypted, keyId));
        
        Assert.That(ex.Message, Contains.Substring("authentication").IgnoreCase.Or.Contains("integrity").IgnoreCase);
    }

    [Test]
    public async Task EncryptWithDataKeyAsync_ReturnsEncryptedDataAndKey()
    {
        // Arrange
        var service = CreateEncryptionService();
        const string plaintext = "Test data for envelope encryption";

        // Act
        var result = await service.EncryptWithDataKeyAsync(plaintext, out string encryptedDataKey);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
        Assert.That(encryptedDataKey, Is.Not.Null);
        Assert.That(encryptedDataKey, Is.Not.Empty);
        
        // Both should be valid base64
        Assert.DoesNotThrow(() => Convert.FromBase64String(result));
        Assert.DoesNotThrow(() => Convert.FromBase64String(encryptedDataKey));
    }

    [Test]
    public async Task DecryptWithDataKeyAsync_ValidData_ReturnsOriginalPlaintext()
    {
        // Arrange
        var service = CreateEncryptionService();
        const string originalPlaintext = "Test data for envelope encryption";

        var encryptedData = await service.EncryptWithDataKeyAsync(originalPlaintext, out string encryptedDataKey);

        // Act
        var decryptedData = await service.DecryptWithDataKeyAsync(encryptedData, encryptedDataKey);

        // Assert
        Assert.That(decryptedData, Is.EqualTo(originalPlaintext));
    }

    [Test]
    public void GenerateAes256Key_ReturnsValidKey()
    {
        // Arrange
        var service = CreateEncryptionService();

        // Act
        var key = service.GenerateAes256Key();

        // Assert
        Assert.That(key, Is.Not.Null);
        Assert.That(key, Has.Length.EqualTo(32)); // 256 bits = 32 bytes
        
        // Generate another key and ensure they're different
        var key2 = service.GenerateAes256Key();
        Assert.That(key, Is.Not.EqualTo(key2));
    }

    [Test]
    public void GenerateSecureRandomBytes_ReturnsRandomBytes()
    {
        // Arrange
        var service = CreateEncryptionService();
        const int size = 16;

        // Act
        var bytes1 = service.GenerateSecureRandomBytes(size);
        var bytes2 = service.GenerateSecureRandomBytes(size);

        // Assert
        Assert.That(bytes1, Has.Length.EqualTo(size));
        Assert.That(bytes2, Has.Length.EqualTo(size));
        Assert.That(bytes1, Is.Not.EqualTo(bytes2)); // Should be different
    }

    [Test]
    public async Task RotateKeyAsync_UpdatesKeySuccessfully()
    {
        // Arrange
        var service = CreateEncryptionService();
        const string keyId = "rotation-test-key";
        const string testData = "Data to test key rotation";

        // Encrypt with original key
        var originalEncrypted = await service.EncryptAsync(testData, keyId);

        // Act
        var rotationResult = await service.RotateKeyAsync(keyId);

        // Assert
        Assert.That(rotationResult, Is.True);

        // Original data should still be decryptable (backwards compatibility)
        var decrypted = await service.DecryptAsync(originalEncrypted, keyId);
        Assert.That(decrypted, Is.EqualTo(testData));

        // New encryptions should work with rotated key
        var newEncrypted = await service.EncryptAsync(testData, keyId);
        var newDecrypted = await service.DecryptAsync(newEncrypted, keyId);
        Assert.That(newDecrypted, Is.EqualTo(testData));
    }

    [Test]
    public async Task ValidateKeyAsync_WithValidKey_ReturnsTrue()
    {
        // Arrange
        var service = CreateEncryptionService();
        const string keyId = "valid-test-key";

        // Create a key by doing an encryption (which should create the key if it doesn't exist)
        await service.EncryptAsync("test", keyId);

        // Act
        var isValid = await service.ValidateKeyAsync(keyId);

        // Assert
        Assert.That(isValid, Is.True);
    }

    [Test]
    public async Task GetKeyInfoAsync_ReturnsKeyInformation()
    {
        // Arrange
        var service = CreateEncryptionService();
        const string keyId = "info-test-key";

        // Create a key
        await service.EncryptAsync("test", keyId);

        // Act
        var keyInfo = await service.GetKeyInfoAsync(keyId);

        // Assert
        Assert.That(keyInfo, Is.Not.Null);
        Assert.That(keyInfo.KeyId, Is.EqualTo(keyId));
        Assert.That(keyInfo.Algorithm, Is.Not.Null);
        Assert.That(keyInfo.KeySize, Is.GreaterThan(0));
        Assert.That(keyInfo.CreatedAt, Is.LessThanOrEqualTo(DateTime.UtcNow));
    }

    [Test]
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public async Task EncryptAsync_WithInvalidPlaintext_ThrowsArgumentException(string? invalidPlaintext)
    {
        // Arrange
        var service = CreateEncryptionService();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.EncryptAsync(invalidPlaintext!, "test-key"));
        
        Assert.That(ex.ParamName, Is.EqualTo("plaintext"));
    }

    [Test]
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public async Task EncryptAsync_WithInvalidKeyId_ThrowsArgumentException(string? invalidKeyId)
    {
        // Arrange
        var service = CreateEncryptionService();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.EncryptAsync("test", invalidKeyId!));
        
        Assert.That(ex.ParamName, Is.EqualTo("keyId"));
    }

    private EncryptionService CreateEncryptionService()
    {
        return new EncryptionService(
            _mockHsmService.Object,
            _mockLogger.Object,
            Options.Create(_options)
        );
    }
}

public class EncryptionServiceOptions
{
    public string DefaultAlgorithm { get; set; } = string.Empty;
    public string DefaultMasterKeyId { get; set; } = string.Empty;
    public bool UseHsmForMasterKeys { get; set; }
    public int KeyDerivationIterations { get; set; }
    public bool EnableHsm { get; set; }
}