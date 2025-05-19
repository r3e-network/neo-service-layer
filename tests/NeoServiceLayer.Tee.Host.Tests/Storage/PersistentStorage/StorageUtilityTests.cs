using System;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using NeoServiceLayer.Tee.Enclave;
using NeoServiceLayer.Tee.Host.Storage.PersistentStorage;
using Xunit;

namespace NeoServiceLayer.Tee.Host.Tests.Storage.PersistentStorage
{
    public class StorageUtilityTests
    {
        private readonly Mock<ILogger<StorageUtility>> _loggerMock;
        private readonly Mock<IOpenEnclaveInterface> _enclaveInterfaceMock;
        private readonly StorageUtility _utility;

        public StorageUtilityTests()
        {
            _loggerMock = new Mock<ILogger<StorageUtility>>();
            _enclaveInterfaceMock = new Mock<IOpenEnclaveInterface>();
            
            // Setup the enclave interface mock to simulate encryption/decryption
            _enclaveInterfaceMock.Setup(e => e.SealData(It.IsAny<byte[]>()))
                .Returns<byte[]>(data => {
                    // Simple "encryption" for testing - just append a marker
                    byte[] encrypted = new byte[data.Length + 4];
                    Array.Copy(data, 0, encrypted, 0, data.Length);
                    encrypted[data.Length] = 0xDE;
                    encrypted[data.Length + 1] = 0xAD;
                    encrypted[data.Length + 2] = 0xBE;
                    encrypted[data.Length + 3] = 0xEF;
                    return encrypted;
                });
            
            _enclaveInterfaceMock.Setup(e => e.UnsealData(It.IsAny<byte[]>()))
                .Returns<byte[]>(encrypted => {
                    // Simple "decryption" for testing - just remove the marker
                    if (encrypted.Length < 4)
                        throw new ArgumentException("Invalid encrypted data");
                    
                    // Check for our marker
                    if (encrypted[encrypted.Length - 4] != 0xDE ||
                        encrypted[encrypted.Length - 3] != 0xAD ||
                        encrypted[encrypted.Length - 2] != 0xBE ||
                        encrypted[encrypted.Length - 1] != 0xEF)
                        throw new ArgumentException("Invalid encrypted data");
                    
                    byte[] decrypted = new byte[encrypted.Length - 4];
                    Array.Copy(encrypted, 0, decrypted, 0, decrypted.Length);
                    return decrypted;
                });
            
            _utility = new StorageUtility(_loggerMock.Object, _enclaveInterfaceMock.Object);
        }

        [Fact]
        public void Encrypt_Decrypt_RoundTrip()
        {
            // Arrange
            byte[] originalData = Encoding.UTF8.GetBytes("test_data");

            // Act
            byte[] encryptedData = _utility.Encrypt(originalData);
            byte[] decryptedData = _utility.Decrypt(encryptedData);

            // Assert
            Assert.NotEqual(originalData, encryptedData); // Encryption should change the data
            Assert.Equal(originalData, decryptedData); // Decryption should restore the original data
        }

        [Fact]
        public void Compress_Decompress_RoundTrip()
        {
            // Arrange
            byte[] originalData = Encoding.UTF8.GetBytes(new string('a', 1000)); // Repeating data compresses well

            // Act
            byte[] compressedData = _utility.Compress(originalData);
            byte[] decompressedData = _utility.Decompress(compressedData);

            // Assert
            Assert.True(compressedData.Length < originalData.Length); // Compression should reduce size
            Assert.Equal(originalData, decompressedData); // Decompression should restore the original data
        }

        [Fact]
        public void EncryptAndCompress_DecryptAndDecompress_RoundTrip()
        {
            // Arrange
            byte[] originalData = Encoding.UTF8.GetBytes(new string('a', 1000));

            // Act
            byte[] processedData = _utility.EncryptAndCompress(originalData);
            byte[] restoredData = _utility.DecryptAndDecompress(processedData);

            // Assert
            Assert.NotEqual(originalData, processedData);
            Assert.Equal(originalData, restoredData);
        }

        [Fact]
        public void ComputeHash_ReturnsConsistentHash()
        {
            // Arrange
            byte[] data = Encoding.UTF8.GetBytes("test_data");

            // Act
            string hash1 = _utility.ComputeHash(data);
            string hash2 = _utility.ComputeHash(data);

            // Assert
            Assert.Equal(hash1, hash2); // Same data should produce same hash
            Assert.Equal(64, hash1.Length); // SHA-256 hash is 64 hex characters
        }

        [Fact]
        public void ComputeHash_DifferentData_DifferentHashes()
        {
            // Arrange
            byte[] data1 = Encoding.UTF8.GetBytes("test_data_1");
            byte[] data2 = Encoding.UTF8.GetBytes("test_data_2");

            // Act
            string hash1 = _utility.ComputeHash(data1);
            string hash2 = _utility.ComputeHash(data2);

            // Assert
            Assert.NotEqual(hash1, hash2); // Different data should produce different hashes
        }

        [Fact]
        public void Encrypt_NullData_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _utility.Encrypt(null));
        }

        [Fact]
        public void Decrypt_NullData_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _utility.Decrypt(null));
        }

        [Fact]
        public void Compress_NullData_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _utility.Compress(null));
        }

        [Fact]
        public void Decompress_NullData_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _utility.Decompress(null));
        }

        [Fact]
        public void EncryptAndCompress_NullData_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _utility.EncryptAndCompress(null));
        }

        [Fact]
        public void DecryptAndDecompress_NullData_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _utility.DecryptAndDecompress(null));
        }

        [Fact]
        public void ComputeHash_NullData_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _utility.ComputeHash(null));
        }
    }
}
